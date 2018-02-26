using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using PriorityQueue;
using SFML.Graphics;
using SFML.System;

namespace SFQuadTree
{
    public class QuadTree
    {
        // To avoid memory allocation, we define statics collection to be re-used for scratch work
        // Note that these are not used in function chains claiming to be thread safe
        private static readonly PriorityQueue<Transformable> CachedSortList = new PriorityQueue<Transformable>(true);
        private static readonly HashSet<Transformable> CachedHashSet = new HashSet<Transformable>();
        private static readonly List<Transformable> CachedList = new List<Transformable>();

        private readonly Queue<Transformable> m_PendingInsertion;
        private readonly Queue<Transformable> m_PendingRemoval;
        private bool m_TreeBuilt;

        private FloatRect m_Region;
        private readonly QuadTree m_Parent;
        private readonly List<Transformable> m_Objects;
        private readonly QuadTree[] m_ChildNodes = new QuadTree[4];

        private byte m_ActiveNodes; //Used a a bitmask to track active nodes
        private const int MIN_SIZE = 5;
        private const int NUM_OBJECTS = 1;
    
        public int Count
        {
            get { return m_Objects.Count + m_ChildNodes.Where(c => c != null).Sum(c => c.Count); }
        }

        private QuadTree(FloatRect region, List<Transformable> objects, QuadTree parent)
        {
            if (objects == null)
                throw new NullReferenceException("Cannot have a null list of objects");

            m_Region = region;
            m_Objects = objects;
            m_Parent = parent;

            //If we are a child, we wont need these allocations
            if (parent == null)
            {
                m_PendingInsertion = new Queue<Transformable>();
                m_PendingRemoval = new Queue<Transformable>();
            }
        }

        public QuadTree(FloatRect region, List<Transformable> objects)
            : this (region, objects, null)
        {
        }

        public QuadTree(FloatRect region)
            : this(region, new List<Transformable>(), null)
        {
        }

        public void Update()
        {
            //Remove null references
            for (var i = 0; i < m_Objects.Count; i++)
            {
                if (m_Objects[i] == null)
                {
                    m_Objects.RemoveAt(i);
                    i--;
                }
            }

            //Update active branches
            for (var i = 0; i < 4; i++)
            {
                if ((m_ActiveNodes & (1 << i)) != 0)
                {
                    //Debug.Assert(m_ChildNodes[i] != null);
                    m_ChildNodes[i].Update();
                }
            }

            CachedHashSet.Clear(); //Used as scratch to track items already re-inserted
                                   //Move children up to parent if they have moved out of our bounds
            //var movedObjects = m_Objects.ToArray();
            for (var i = 0; i < m_Objects.Count; i++)
            {
                //If an object hasn't moved, then it is still in the correct octree
                //CachedHashSet contains items that we already re-inserted
                //TODO: Implement hasChanged for SFML Transformable
                if ( /*!m_Objects[i].hasChanged || */CachedHashSet.Contains(m_Objects[i]))
                    continue;

                var obj = m_Objects[i];
                var current = this;

                //TODO: rework this to work with objects that have bounds
                while (!current.m_Region.Contains(obj.Position.X, obj.Position.Y))
                {
                    if (current.m_Parent != null) current = current.m_Parent;
                    else break;
                }


                m_Objects.RemoveAt(i--);
                current.Insert(obj);
            
                CachedHashSet.Add(obj);
            }

            //prune out any dead branches in the tree
            for (int flags = m_ActiveNodes, index = 0; flags > 0; flags >>= 1, index++)
            {
                if ((flags & 1) == 1 && m_ChildNodes[index].m_Objects.Count == 0 &&
                    m_ChildNodes[index].m_ActiveNodes == 0)
                {
                    m_ChildNodes[index] = null;
                    m_ActiveNodes ^= (byte)(1 << index);       //remove the node from the active nodes flag list
                }
            }

            UpdateTree();
        }

        /// <summary>
        /// Adds the given <see cref="Transformable"/> to the QuadTree.
        /// Internal QuadTree is not updated until the next call to Update.
        /// </summary>
        public void Add(Transformable t)
        {
            if (t == null)
                return;

            m_PendingInsertion.Enqueue(t);
        }

        /// <summary>
        /// Removes the given <see cref="Transformable"/> from the QuadTree.
        /// Internal QuadTree is not updated until the next call to Update.
        /// </summary>
        public void Remove(Transformable t)
        {
            if (t == null)
                return;

            m_PendingRemoval.Enqueue(t);
        }

        #region Non-Thread-Safe Queeries

        /// <summary>
        /// Gets the K closest objects to a given position.
        /// This version of the queery is not thread safe.
        /// </summary>
        public Transformable[] GetKClosestObjects(Vector2f pos, uint k, float range = float.MaxValue)
        {
#if DEBUG
            if (range < 0f)
                throw new ArgumentException("Range cannot be negative");
#endif
            CachedSortList.Clear();
            float r = range * range;
            KNearestNeighborSearch(ref pos, k, ref r, CachedSortList);
            return CachedSortList.ToArray();
        }

        /// <summary>
        /// Gets all objects within the given range of the given position.
        /// This version of the queery is not thread safe.
        /// </summary>
        public Transformable[] GetObjectsInRange(Vector2f pos, float range = float.MaxValue)
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
        public Transformable[] GetObjectsInRect(FloatRect rect)
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
        public Transformable GetClosestObject(Vector2f pos, float maxDistance = float.MaxValue)
        {
            return NearestNeighborSearch(pos, maxDistance * maxDistance);
        }

        /// <summary>
        /// Gets the K closest objects to a given position.
        /// This version of the queery is thread safe as long as
        /// <see cref="Update"/> does not execute during the queery.
        /// </summary>
        public void GetKClosestObjects(Vector2f pos, uint k, float range, PriorityQueue<Transformable> results)
        {
#if DEBUG
            if (range < 0f)
                throw new ArgumentException("Range cannot be negative");
            if (results == null)
                throw new ArgumentException("Results queue cannot be null");
#endif
            float r = range * range;
            KNearestNeighborSearch(ref pos, k, ref r, results);
        }

        /// <summary>
        /// Gets all objects within the given range of the given position.
        /// This version of the queery is thread safe as long as
        /// <see cref="Update"/> does not execute during the queery.
        /// </summary>
        public void GetObjectsInRange(Vector2f pos, float range, IList<Transformable> results)
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
        public void GetObjectsInRect(FloatRect rect, List<Transformable> results)
        {
            ObjectsInRectSearch(rect, results);
        }

        #endregion

        #region Internal Queeries
        private Transformable NearestNeighborSearch(Vector2f pos, float distanceSquared)
        {
            Transformable closest = null;

            //We have no children, check objects in this node
            if (m_ActiveNodes == 0)
            {
                for (var i = 0; i < m_Objects.Count; i++)
                {
                    var obj = m_Objects[i];
                    if (obj == null)
                        continue;

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

        private void KNearestNeighborSearch(ref Vector2f pos, uint k, ref float rangeSquared, PriorityQueue<Transformable> results)
        {
            //We have no children, check objects in this node
            if (m_ActiveNodes == 0)
            {
                for (var i = 0; i < m_Objects.Count; i++)
                {
                    var obj = m_Objects[i];
                    if (obj == null)
                        continue;

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

                m_ChildNodes[i].KNearestNeighborSearch(ref pos, k, ref rangeSquared, results);
            }
        }

        private void AllNearestNeighborsSearch(Vector2f pos, float rangeSquared, IList<Transformable> results)
        {
            //We have no children, check objects in this node
            if (m_ActiveNodes == 0)
            {
                for (var i = 0; i < m_Objects.Count; i++)
                {
                    var obj = m_Objects[i];
                    if (obj == null)
                        continue;

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

        private void ObjectsInRectSearch(FloatRect rect, ICollection<Transformable> results)
        {
            if (m_ActiveNodes == 0)
            {
                for (var i = 0; i < m_Objects.Count; i++)
                {
                    var obj = m_Objects[i];
                    if (obj == null)
                        continue;

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
        private void UpdateTree()
        {
            if (m_PendingInsertion != null)
            {
                while (m_PendingInsertion.Count != 0)
                {
                    Insert(m_PendingInsertion.Dequeue());
                }
            }

            if (m_PendingRemoval != null)
            {
                while (m_PendingRemoval.Count != 0)
                {
                    Delete(m_PendingRemoval.Dequeue());
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

            if (!m_TreeBuilt)
                BuildTree();
        }

        // TODO: Cleanup
        private void BuildTree()
        {
            if (m_Objects.Count <= NUM_OBJECTS)
            {
                m_TreeBuilt = true;
                return; //We are a leaf node - we are done
            }

            var dimensions = m_Region.Dimensions();

            //Smallest we can get, no more subdividing
            //For a quadTree, all the bounds are squares, so we only 
            //need to check one axis it seems
            if (dimensions.X <= MIN_SIZE
                /*&& dimensions.y <= MIN_SIZE*/)
            {
                m_TreeBuilt = true;
                return;
            }

            MoveObjectsToChildren();

            m_TreeBuilt = true;
        }

        private void Insert(Transformable obj)
        {
            if (obj == null)
                return;

            if (m_Objects.Count < NUM_OBJECTS && m_ActiveNodes == 0)
            {
                m_Objects.Add(obj);
                return;
            }

            var dimensions = m_Region.Dimensions();

            //Smallest we can get, no more subdividing
            //For an octree, all the bounds are cubes, so we only 
            //need to check one axis it seems
            if (dimensions.X <= MIN_SIZE
                /*&& dimensions.y <= MIN_SIZE
                && dimensions.z <= MIN_SIZE*/)
            {
                m_Objects.Add(obj);
                return;
            }

            m_Objects.Add(obj);
            MoveObjectsToChildren();
        }

        /// <summary>
        /// Attempts to move all objects at this level of the QuadTree into child nodes
        /// </summary>
        private void MoveObjectsToChildren()
        {
            var dimensions = m_Region.Dimensions();

            var half = dimensions / 2f;
            var halflen = half.X / 1f; //a quarter of the length of a side of this region
            var center = m_Region.Center();

            //Create child bounds
            var quads = new FloatRect[4];
            quads[0] = new FloatRect(center + new Vector2f(-halflen, -halflen), half);
            quads[1] = new FloatRect(center + new Vector2f(0, -halflen), half);
            quads[2] = new FloatRect(center, half);
            quads[3] = new FloatRect(center + new Vector2f(-halflen, 0), half);

            //Objects that go in each octant
            //Since these lists will be used by the octants
            //there is no reason to cache them
            var octList = new List<Transformable>[4];
            for (var i = 0; i < 4; i++) octList[i] = new List<Transformable>();

            CachedList.Clear();
            //list of objects moved into children
            var delist = CachedList;

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
                        delist.Add(obj);
                        break;
                    }
                }
            }

            //Delist objects that were moved into the children
            for (var i = 0; i < delist.Count; i++)
            {
                m_Objects.Remove(delist[i]);
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
                    m_ChildNodes[i].BuildTree();
                }
                else
                {
                    for (int j = 0; j < octList[i].Count; j++)
                    {
                        m_ChildNodes[i].Insert(octList[i][j]);
                    }
                }
            }
        }

        private bool Delete(Transformable t)
        {
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

        private QuadTree CreateChildNode(FloatRect region, List<Transformable> objects)
        {
            return objects.Count == 0
                ? null
                : new QuadTree(region, objects, this);
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
