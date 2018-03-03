using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using PriorityQueue;
using QuadTree;
using SFML.Graphics;
using SFML.System;

namespace SFQuadTree
{
    public class QuadTree<T> : ISpacePartitioner<T> 
        where T : Transformable
    {
        // To avoid memory allocation, we define statics collection to be re-used for scratch work
        // Note that these are not used in function chains claiming to be thread safe
        private static readonly PriorityQueue<T> CachedSortList = new MaxPriorityQueue<T>();
        private static readonly List<T> CachedList = new List<T>();

        private readonly Queue<T> m_PendingInsertion;
        private readonly Queue<T> m_PendingRemoval;

        private FloatRect m_Region;
        private readonly List<T> m_Objects;
        private readonly QuadTree<T>[] m_ChildNodes = new QuadTree<T>[4];

        private byte m_ActiveNodes; //Used a a bitmask to track active nodes
        private const int MIN_SIZE = 5;
        private const int NUM_OBJECTS = 1;

        public int Count
        {
            get { return m_Objects.Count + m_ChildNodes.Where(c => c != null).Sum(c => c.Count); }
        }

        public FloatRect Bounds => m_Region;

        private QuadTree(FloatRect region, List<T> objects, QuadTree<T> parent)
        {
            if (region.Width != region.Height)
                throw new ArgumentException("QuadTree height must equal QuadTree width");
            if (objects == null)
                throw new NullReferenceException("Cannot have a null list of objects");

            m_Region = region;
            m_Objects = objects;

            //If we are a child, we wont need these allocations
            if (parent == null)
            {
                m_PendingInsertion = new Queue<T>();
                m_PendingRemoval = new Queue<T>();
            }

            BuildTree();
        }

        public QuadTree(FloatRect region, List<T> objects)
            : this (region, objects, null)
        {
        }

        public QuadTree(FloatRect region)
            : this(region, new List<T>(), null)
        {
        }

        public void Update()
        {
            Insert(InternalUpdate());
        }

        /// <summary>
        /// Adds the given <see cref="Transformable"/> to the QuadTree.
        /// Internal QuadTree is not updated until the next call to Update.
        /// </summary>
        public void Add(T t)
        {
#if DEBUG
            if (t == null)
                throw new ArgumentException("Cannot add a null object to the QuadTree");
#endif
            lock (m_PendingInsertion)
                m_PendingInsertion.Enqueue(t);
        }

        /// <summary>
        /// Removes the given <see cref="Transformable"/> from the QuadTree.
        /// Internal QuadTree is not updated until the next call to Update.
        /// </summary>
        public void Remove(T t)
        {
#if DEBUG
            if (t == null)
                throw new ArgumentException("Cannot remove a null object from the QuadTree");
#endif
            lock (m_PendingRemoval)
                m_PendingRemoval.Enqueue(t);
        }

#region Non-Thread-Safe Queeries

        /// <summary>
        /// Gets the K closest objects to a given position.
        /// This version of the queery is not thread safe.
        /// </summary>
        public T[] GetKClosestObjects(Vector2f pos, uint k, float range = float.MaxValue)
        {
#if DEBUG
            if (range < 0f)
                throw new ArgumentException("Range cannot be negative");
#endif
            CachedSortList.Clear();
            float r = range * range;
            KNearestNeighborSearch(pos, k, ref r, CachedSortList);
            return CachedSortList.ToArray();
        }

        /// <summary>
        /// Gets all objects within the given range of the given position.
        /// This version of the queery is not thread safe.
        /// </summary>
        public T[] GetObjectsInRange(Vector2f pos, float range = float.MaxValue)
        {
#if DEBUG
            if (range < 0f)
                throw new ArgumentException("Range cannot be negative");
#endif
            CachedList.Clear();
            AllNearestNeighborsSearch(pos, range * range, CachedList);

            return CachedList.ToArray();
        }

        /// <summary>
        /// Gets all objects within the given FloatRect.
        /// This version of the queery is not thread safe.
        /// </summary>
        public T[] GetObjectsInRect(FloatRect rect)
        {
            CachedList.Clear();
            ObjectsInRectSearch(rect, CachedList);

            return CachedList.ToArray();
        }

#endregion

#region Thread-Safe Queeries

        /// <summary>
        /// Gets the closest object to the given position.
        /// This version of the queery is thread safe as long as
        /// <see cref="Update"/> does not execute during the queery.
        /// </summary>
        public T GetClosestObject(Vector2f pos, float maxDistance = float.MaxValue)
        {
            return NearestNeighborSearch(pos, maxDistance * maxDistance);
        }

        /// <summary>
        /// Gets the K closest objects to a given position.
        /// This version of the queery is thread safe as long as
        /// <see cref="Update"/> does not execute during the queery.
        /// </summary>
        public void GetKClosestObjects(Vector2f pos, uint k, float range, PriorityQueue<T> results)
        {
#if DEBUG
            if (range < 0f)
                throw new ArgumentException("Range cannot be negative");
            if (results == null)
                throw new ArgumentException("Results queue cannot be null");
#endif
            float r = range * range;
            KNearestNeighborSearch(pos, k, ref r, results);
        }

        /// <summary>
        /// Gets all objects within the given range of the given position.
        /// This version of the queery is thread safe as long as
        /// <see cref="Update"/> does not execute during the queery.
        /// </summary>
        public void GetObjectsInRange(Vector2f pos, float range, IList<T> results)
        {
#if DEBUG
            if (range < 0f)
                throw new ArgumentException("Range cannot be negative");
            if (results == null)
                throw new ArgumentException("Results list cannot be null");
#endif
            AllNearestNeighborsSearch(pos, range * range, results);
        }

        /// <summary>
        /// Gets all objects within the given FloatRect.
        /// This version of the queery is thread safe as long as
        /// <see cref="Update"/> does not execute during the queery.
        /// </summary>
        public void GetObjectsInRect(FloatRect rect, IList<T> results)
        {
            ObjectsInRectSearch(rect, results);
        }

#endregion

#region Internal Queeries
        private T NearestNeighborSearch(Vector2f pos, float distanceSquared)
        {
            T closest = null;

            //We have no children, check objects in this node
            if (m_ActiveNodes == 0)
            {
                for (var i = 0; i < m_Objects.Count; i++)
                {
                    var obj = m_Objects[i];

                    var ds = (pos - obj.Position).SquaredLength();

                    if (!(ds < distanceSquared))
                        continue;

                    distanceSquared = ds;
                    closest = obj;
                }
                return closest;
            }

            for (var i = 0; i < 4; i++)
            {
                if ((m_ActiveNodes & (1 << i)) == 0)
                    continue;

                //If a border is closer than the closest distance so far, it might have a closer object
                var distToChildBorder = m_ChildNodes[i].m_Region.SquaredDistance(pos);

                if (!(distToChildBorder < distanceSquared))
                    continue;

                var testObject = m_ChildNodes[i].NearestNeighborSearch(pos, distanceSquared);
                if (testObject == null) continue; //Didn't find a closer object

                closest = testObject;
                distanceSquared = (pos - closest.Position).SquaredLength();
            }

            return closest;
        }

        private void KNearestNeighborSearch(Vector2f pos, uint k, ref float rangeSquared, PriorityQueue<T> results)
        {
            //We have no children, check objects in this node
            if (m_ActiveNodes == 0)
            {
                for (var i = 0; i < m_Objects.Count; i++)
                {
                    var obj = m_Objects[i];

                    var ds = (pos - obj.Position).SquaredLength();

                    if (ds > rangeSquared)
                        continue;

                    //If results list has empty elements
                    if (results.Count < k)
                    {
                        results.Enqueue(obj, ds);
                        continue;
                    }

                    if (ds < results.GetPriority(results.Peek()))
                    {
                        results.Dequeue();
                        results.Enqueue(obj, ds);
                        rangeSquared = (float) results.GetPriority(results.Peek());
                    }
                }
                return;
            }

            //Check if we should check children
            for (var i = 0; i < 4; i++)
            {
                if ((m_ActiveNodes & (1 << i)) == 0 ||
                    (m_ChildNodes[i].m_Objects.Count == 0 && m_ChildNodes[i].m_ActiveNodes == 0))
                {
                    continue;
                }

                //If a border is closer than the farthest distance so far, it might have a closer object
                var distToChildBorder = m_ChildNodes[i].m_Region.SquaredDistance(pos);

                if (distToChildBorder > rangeSquared)
                    continue;

                m_ChildNodes[i].KNearestNeighborSearch(pos, k, ref rangeSquared, results);
            }
        }

        private void AllNearestNeighborsSearch(Vector2f pos, float rangeSquared, IList<T> results)
        {
            //We have no children, check objects in this node
            if (m_ActiveNodes == 0)
            {
                for (var i = 0; i < m_Objects.Count; i++)
                {
                    var obj = m_Objects[i];

                    var ds = (pos - obj.Position).SquaredLength();

                    if (ds > rangeSquared)
                        continue;

                    results.Add(obj);
                }
                return;
            }

            //Check if we should check children
            for (var i = 0; i < 4; i++)
            {
                if ((m_ActiveNodes & (1 << i)) == 0)
                    continue;

                //If a border is closer than the farthest distance so far, it might have a closer object
                var distToChildBorder = m_ChildNodes[i].m_Region.SquaredDistance(pos);

                if ((distToChildBorder > rangeSquared))
                    continue;

                m_ChildNodes[i].AllNearestNeighborsSearch(pos, rangeSquared, results);
            }
        }

        private void ObjectsInRectSearch(FloatRect rect, ICollection<T> results)
        {
            if (m_ActiveNodes == 0)
            {
                for (var i = 0; i < m_Objects.Count; i++)
                {
                    var obj = m_Objects[i];

                    if (!rect.Contains(obj.Position.X, obj.Position.Y))
                        return;

                    results.Add(obj);
                }
            }

            //Check if we should check children
            for (var i = 0; i < 4; i++)
            {
                if ((m_ActiveNodes & (1 << i)) == 0)
                    continue;

                if (m_ChildNodes[i].m_Region.Intersects(rect))
                    m_ChildNodes[i].ObjectsInRectSearch(rect, results);
            }
        }

        #endregion

        #region Internal Operations
        private List<T> InternalUpdate()
        {
            var objsInserting = new List<T>();
            var objsMovingUp = new List<T>();

            //Update active branches
            for (var i = 0; i < 4; i++)
            {
                if ((m_ActiveNodes & (1 << i)) == 0)
                    continue;

                var movedUp = m_ChildNodes[i].InternalUpdate();
                for (var j = 0; j < movedUp.Count; j++)
                {
                    if (m_Region.Contains(movedUp[j].Position.X, movedUp[j].Position.Y))
                    {
                        objsInserting.Add(movedUp[j]);
                    }
                    else
                    {
                        objsMovingUp.Add(movedUp[j]);
                    }
                }
            }

            //Move children up to parent if they have moved out of our bounds
            for (var i = m_Objects.Count - 1; i >= 0; i--)
            {
                var obj = m_Objects[i];

                if (!m_Region.Contains(obj.Position.X, obj.Position.Y))
                {
                    objsMovingUp.Add(obj);
                    m_Objects.RemoveAt(i);
                }
            }

            Insert(objsInserting);

            UpdateTree();

            //prune out any dead branches in the tree
            for (var i = 0; i < 4; i++)
            {
                if ((m_ActiveNodes & (1 << i)) != 0 && m_ChildNodes[i].m_Objects.Count == 0 &&
                    m_ChildNodes[i].m_ActiveNodes == 0)
                {
                    m_ChildNodes[i] = null;
                    m_ActiveNodes ^= (byte)(1 << i);
                }
            }

            return objsMovingUp;
        }

        private void UpdateTree()
        {
            if (m_PendingInsertion != null)
            {
                lock (m_PendingInsertion)
                {
                    while (m_PendingInsertion.Count != 0)
                    {
                        Insert(m_PendingInsertion.Dequeue());
                    }
                }
            }

            if (m_PendingRemoval != null)
            {
                lock (m_PendingRemoval)
                {
                    while (m_PendingRemoval.Count != 0)
                    {
                        Delete(m_PendingRemoval.Dequeue());
                    }
                }
            }

            for (var i = 0; i < m_Objects.Count; i++)
            {
                if (m_Objects[i] == null)
                {
                    m_Objects.RemoveAt(i);
                    i--;
                }
            }
        }

        /// <summary>
        /// Builds the initial state of a new QuadTree by attempting
        /// to move children into sub-trees if needed and valid
        /// </summary>
        private void BuildTree()
        {
            if (m_Objects.Count <= NUM_OBJECTS)
            {
                return; //We are a leaf node - we are done
            }

            var dimensions = m_Region.Dimensions();

            //Smallest we can get, no more subdividing
            //For a quadTree, all the bounds are squares, so we only 
            //need to check one axis
            if (dimensions.X <= MIN_SIZE)
            {
                return;
            }

            MoveObjectsToChildren();
        }

        private void Insert(T obj)
        {
            if (obj == null)
                return;

            m_Objects.Add(obj);

            if (m_Objects.Count < NUM_OBJECTS && m_ActiveNodes == 0)
            {
                return;
            }

            //Smallest we can get, no more subdividing
            //For an quadtree, all the bounds are squares, so we only 
            //need to check one axis
            if (m_Region.Width > MIN_SIZE)
            {
                MoveObjectsToChildren();
            }
        }

        private void Insert(List<T> objs)
        {
            bool overflow = false;
            for (var i = 0; i < objs.Count; i++)
            {
                m_Objects.Add(objs[i]);

                if (m_Objects.Count >= NUM_OBJECTS || m_ActiveNodes != 0)
                {
                    overflow = true;
                }
            }

            //Smallest we can get, no more subdividing
            //For an quadtree, all the bounds are squares, so we only 
            //need to check one axis
            if (overflow && m_Region.Width > MIN_SIZE)
            {
                MoveObjectsToChildren();
            }
        }

        /// <summary>
        /// Attempts to move all objects at this level of the QuadTree into child nodes
        /// </summary>
        private void MoveObjectsToChildren()
        {
            var halflen = m_Region.Width / 2f;

            //Create child bounds
            var quads = new FloatRect[4];
            quads[0] = new FloatRect(m_Region.Left, m_Region.Top, halflen, halflen);
            quads[1] = new FloatRect(m_Region.Left + halflen, m_Region.Top, halflen, halflen);
            quads[2] = new FloatRect(m_Region.Left + halflen, m_Region.Top + halflen, halflen, halflen);
            quads[3] = new FloatRect(m_Region.Left, m_Region.Top + halflen, halflen, halflen);

            //Objects that go in each octant
            //Since these lists will be used by the octants
            //there is no reason to cache them
            var octList = new List<T>[4];
            for (var i = 0; i < 4; i++) octList[i] = new List<T>();

            //list of objects moved into children
            var delist = new List<int>();

            //Move objects into children
            for (var index = 0; index < m_Objects.Count; index++)
            {
                var obj = m_Objects[index];

                if (obj == null)
                {
                    //Get rid of the null object
                    m_Objects.RemoveAt(index);
                    index--;
                    continue;
                }

                for (var i = 0; i < 4; i++)
                {
                    //TODO: Expand this to deal with objects with bounds, not just points
                    if (quads[i].Contains(obj.Position.X, obj.Position.Y))
                    {
                        octList[i].Add(obj);
                        delist.Add(index);
                        break;
                    }
                }
            }

            //Delist objects that were moved into the children
            for (var i = delist.Count - 1; i >= 0; i--)
            {
                m_Objects.RemoveAt(delist[i]);
            }

            for (var i = 0; i < 4; i++)
            {
                // If there are no objects to insert into child
                if (octList[i].Count == 0)
                    continue;

                // If the child node does not exist
                if ((m_ActiveNodes & (1 << i)) == 0)
                {
                    m_ChildNodes[i] = CreateChildNode(quads[i], octList[i]);
                    m_ActiveNodes |= (byte) (1 << i);
                }
                else
                {
                    m_ChildNodes[i].Insert(octList[i]);
                }
            }
        }

        private bool Delete(T t)
        {
            if (!m_Region.Contains(t.Position.X, t.Position.Y))
                return false;

            if (m_Objects.Count > 0 && m_Objects.Remove(t))
                return true;

            //For each active node, try to delete from that node
            for (int flags = m_ActiveNodes, index = 0; flags > 0; flags >>= 1, index++)
            {
                if ((flags & 1) == 1 && m_ChildNodes[index].Delete(t))
                {
                    return true;
                }
            }

            return false;
        }

        private QuadTree<T> CreateChildNode(FloatRect region, List<T> objects)
        {
            return objects.Count == 0
                ? null
                : new QuadTree<T>(region, objects, this);
        }
#endregion

        public void GetAllRegions(List<FloatRect> regions)
        {
            regions.Add(m_Region);
            foreach (var childNode in m_ChildNodes)
            {
                childNode?.GetAllRegions(regions);
            }
        }
    }
}
