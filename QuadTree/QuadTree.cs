using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SFML.Graphics;
using SFML.System;

namespace SFQuadTree
{
    public class QuadTree
    {
        //To avoid memory allocation, we define statics collection to be re-used for scratch work
        private static readonly QuadTreeResultList CachedSortList = new QuadTreeResultList();
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
        /*private int m_MaxLifespan = 2; //How long to wait before deleting an empty node
        private int m_CurrentLife;*/

        public static int CheckCount = 0;
        public static int ExtraOpCount = 0;

        public static Action<Transformable> OnCheckAgainstObject;
    
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
            //m_CurrentLife = -1;

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
            /*if (!m_TreeBuilt)
                return;*/

            //If empty node, start death ticking
            /*if (m_Objects.Count == 0 && m_ActiveNodes == 0)
            {
                if (m_CurrentLife == -1)
                    m_CurrentLife = m_MaxLifespan;
                else if (m_CurrentLife > 0)
                    m_CurrentLife--;
            }
            else if (m_CurrentLife != -1)
            {
                if (m_MaxLifespan <= 8)
                    m_MaxLifespan *= 2;

                m_CurrentLife = -1;
            }*/
            
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

            /*for (var i = 0; i < m_Objects.Count; i++)
            {
                var obj = m_Objects[i];
                var current = this;

                while (!current.m_Region.Contains(obj.Position.X, obj.Position.Y))
                {
                    if (current.m_Parent != null) current = current.m_Parent;
                    else break;
                }

                m_Objects.RemoveAt(i--);
                current.Insert(obj);
            }*/

            //prune out any dead branches in the tree
            for (int flags = m_ActiveNodes, index = 0; flags > 0; flags >>= 1, index++)
            {
                if ((flags & 1) == 1 && m_ChildNodes[index].m_Objects.Count == 0 &&
                    m_ChildNodes[index].m_ActiveNodes == 0)
                {
                    m_ChildNodes[index] = null;
                    m_ActiveNodes ^= (byte)(1 << index);       //remove the node from the active nodes flag list
                }

                /*if ((flags & 1) == 1 && m_ChildNodes[index].m_CurrentLife == 0 && m_ChildNodes[index].m_Objects.Count == 0)
                {
                    m_ChildNodes[index] = null;
                    m_ActiveNodes ^= (byte)(1 << index);       //remove the node from the active nodes flag list
                }*/
            }


            //Prune dead branches
            /*for (var index = 0; index < 4; index++)
            {
                if ((m_ActiveNodes & (1 << index)) != 0 && m_ChildNodes[index].m_CurrentLife == 0)
                {
                    if (m_ChildNodes[index].m_Objects.Count != 0) continue;
                    m_ChildNodes[index] = null;
                    m_ActiveNodes &= (byte)(~(1 << index));
                }
            }*/
        }

        public void Add(Transformable t)
        {
            if (t == null)
                return;

            m_PendingInsertion.Enqueue(t);
        }

        public void Remove(Transformable t)
        {
            if (t == null)
                return;
            m_PendingRemoval.Enqueue(t);
        }

        #region Queeries
        public Transformable GetClosestObject(Vector2f pos, float maxDistance = float.MaxValue)
        {
            UpdateTree();

            return NearestNeighborSearch(pos, maxDistance * maxDistance);
        }

        public Transformable[] GetKClosestObjects(Vector2f pos, int k, float range = float.MaxValue)
        {
            UpdateTree();

            CachedSortList.Clear();
            float r = range * range;
            KNearestNeighborSearch(ref pos, k, ref r/*, CachedSortList*/);
            return CachedSortList.GetObjects();
            /*var ret = new Transformable[Math.Min(k, CachedSortList.Count)];
            for (var i = 0; i < ret.Length; i++)
            {
                ret[i] = CachedSortList.Values[i];
            }
            return ret;*/
        }

        public Transformable[] GetObjectsInRange(Vector2f pos, float range = float.MaxValue)
        {
            UpdateTree();

            CachedSortList.Clear();
            AllNearestNeighborsSearch(pos, range * range, CachedSortList);

            return CachedSortList.GetObjects();
            /*var ret = new Transformable[CachedSortList.Count];
            for (var i = 0; i < ret.Length; i++)
            {
                ret[i] = CachedSortList.Values[i];
            }
            return ret;*/
        }

        public Transformable[] GetObjectsInRect(FloatRect rect)
        {
            UpdateTree();

            CachedList.Clear();
            ObjectsInRectSearch(rect, CachedList);

            return CachedList.ToArray();
        }

        public void GetObjectsInRect(FloatRect rect, List<Transformable> results)
        {
            UpdateTree();

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

        private void KNearestNeighborSearch(ref Vector2f pos, int k, ref float rangeSquared/*, SortedList<float, Transformable> results*/)
        {
            //We have no children, check objects in this node
            if (m_ActiveNodes == 0)
            {
                for (var i = 0; i < m_Objects.Count; i++)
                {
                    //OnCheckAgainstObject?.Invoke(m_Objects[i]);
                    var obj = m_Objects[i];
                    if (obj == null)
                        continue;
                    CheckCount++;
                    var ds = (pos - obj.Position).SquaredLength();

                    if (ds > rangeSquared)
                        continue;

                    //If results list has empty elements
                    if (CachedSortList.Count < k)
                    {
                        /*while (CachedSortList.ContainsKey(ds))
                        {
                            //Break any ties
                            ds += 1f;
                        }*/
                        CachedSortList.Add(ds, obj);
                        continue;
                    }

                    if (ds < CachedSortList.GetDistance(CachedSortList.Count - 1))
                    {
                        /*while (CachedSortList.ContainsKey(ds))
                        {
                            //Break any ties
                            ds += 1f;
                        }*/
                        CachedSortList.RemoveAt(CachedSortList.Count - 1);
                        CachedSortList.Add(ds, obj);
                        rangeSquared = CachedSortList.GetDistance(CachedSortList.Count - 1);
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

                m_ChildNodes[i].KNearestNeighborSearch(ref pos, k, ref rangeSquared);
            }
        }

        private void AllNearestNeighborsSearch(Vector2f pos, float rangeSquared, QuadTreeResultList results)
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

                    results.Add(ds, obj);
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
            while (m_PendingInsertion.Count != 0)
            {
                Insert(m_PendingInsertion.Dequeue());
            }

            while (m_PendingRemoval.Count != 0)
            {
                Delete(m_PendingRemoval.Dequeue());
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
                if (octList[i].Count == 0)
                    continue;

                m_ChildNodes[i] = CreateChildNode(quads[i], octList[i]);
                m_ActiveNodes |= (byte)(1 << i);
                m_ChildNodes[i].BuildTree();
            }

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

            var half = dimensions / 2f;
            var halflen = half.X / 1f; //a quarter of the length of a side of this region
            var center = m_Region.Center();

            //Create child bounds, using existing values if possible
            var quads = new FloatRect[4];
            quads[0] = m_ChildNodes[0]?.m_Region ?? new FloatRect(center + new Vector2f(-halflen, -halflen), half);
            quads[1] = m_ChildNodes[1]?.m_Region ?? new FloatRect(center + new Vector2f(0, -halflen), half);
            quads[2] = m_ChildNodes[2]?.m_Region ?? new FloatRect(center, half);
            quads[3] = m_ChildNodes[3]?.m_Region ?? new FloatRect(center + new Vector2f(-halflen, 0), half);

            var found = false;
            for (var i = 0; i < 4 && !found; i++)
            {
                //TODO: Expand this to deal with objects with bounds, not just points
                if (quads[i].Contains(obj.Position.X, obj.Position.Y))
                {
                    if (m_ChildNodes[i] != null)
                    {
                        m_ChildNodes[i].Insert(obj);
                    }
                    else
                    {
                        m_ChildNodes[i] = CreateChildNode(quads[i], new List<Transformable> { obj });
                        m_ActiveNodes |= (byte)(1 << i);
                        //m_ChildNodes[i].BuildTree();
                    }
                    found = true;
                }
            }
            if (!found)
                m_Objects.Add(obj);
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
