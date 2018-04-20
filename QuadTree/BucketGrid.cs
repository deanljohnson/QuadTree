using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using PriorityQueue;
using QuadTree;
using SFML.Graphics;
using SFML.System;

namespace QuadTree
{
    public class BucketGrid<T> : ISpacePartitioner<T>
        where T : Transformable
    {
        private readonly List<T> CachedList = new List<T>(); 
        private readonly MaxPriorityQueue<T> CachedQueue = new MaxPriorityQueue<T>();
        private readonly float m_BucketWidth;
        private readonly float m_BucketHeight;
        private readonly int m_NumBucketsWidth;
        private readonly int m_NumBucketsHeight;

        private readonly Queue<T> m_PendingInsertion;
        private readonly Queue<T> m_PendingRemoval;

        private readonly FloatRect m_Region;
        private readonly List<T>[] m_Buckets;

        public BucketGrid(FloatRect region, int numBucketsWidth, int numBucketsHeight)
        {
            m_Region = region;
            m_Region.Left -= float.Epsilon;
            m_Region.Top -= float.Epsilon;
            m_Region.Width += float.Epsilon;
            m_Region.Height += float.Epsilon;
            m_Buckets = new List<T>[numBucketsWidth * numBucketsHeight];

            m_NumBucketsWidth = numBucketsWidth;
            m_NumBucketsHeight = numBucketsHeight;

            m_BucketWidth = region.Width / m_NumBucketsWidth;
            m_BucketHeight = region.Height / m_NumBucketsHeight;

            m_PendingInsertion = new Queue<T>();
            m_PendingRemoval = new Queue<T>();
        }

        public void Update()
        {
            // Make sure all objects are in the right buckets
            for (var i = 0; i < m_Buckets.Length; i++)
            {
                var bucket = m_Buckets[i];
                if (bucket == null)
                    continue;

                for (var j = bucket.Count - 1; j >= 0; j--)
                {
                    var idx = FindBucketIndex(bucket[j].Position);
                    if (idx == i) continue;
                    if (m_Buckets[idx] == null) m_Buckets[idx] = new List<T>();
                    m_Buckets[idx].Add(bucket[j]);
                    bucket.RemoveAt(j);
                }
            }

            lock (m_PendingRemoval)
            {
                while (m_PendingRemoval.Count > 0)
                {
                    var obj = m_PendingRemoval.Dequeue();
                    var idx = FindBucketIndex(obj.Position);
                    if (m_Buckets[idx] == null) m_Buckets[idx] = new List<T>();
                    m_Buckets[idx].Remove(obj);
                }
            }

            lock (m_PendingInsertion)
            {
                while (m_PendingInsertion.Count > 0)
                {
                    var obj = m_PendingInsertion.Dequeue();
                    var idx = FindBucketIndex(obj.Position);
                    if (m_Buckets[idx] == null) m_Buckets[idx] = new List<T>();
                    m_Buckets[idx].Add(obj);
                }
            }
        }

        /// <summary>
        /// Adds the given <see cref="Transformable"/> to the BucketGrid.
        /// Internal BucketGrid is not updated until the next call to Update.
        /// </summary>
        public void Add(T t)
        {
#if DEBUG
            if (t == null)
                throw new ArgumentException("Cannot add a null object to the BucketGrid");
#endif
            lock (m_PendingInsertion)
                m_PendingInsertion.Enqueue(t);
        }

        /// <summary>
        /// Removes the given <see cref="Transformable"/> from the BucketGrid.
        /// Internal BucketGrid is not updated until the next call to Update.
        /// </summary>
        public void Remove(T t)
        {
#if DEBUG
            if (t == null)
                throw new ArgumentException("Cannot remove a null object from the BucketGrid");
#endif
            lock (m_PendingRemoval)
                m_PendingRemoval.Enqueue(t);
        }

        public T GetClosestObject(Vector2f pos, float maxDistance = float.MaxValue)
        {
            return NearestNeighborSearch(pos, maxDistance);
        }

        public T[] GetKClosestObjects(Vector2f pos, uint k, float range = float.MaxValue)
        {
            CachedQueue.Clear();
            KNearestNeighborSearch(pos, k, range, CachedQueue);
            return CachedQueue.ToArray();
        }

        public T[] GetObjectsInRange(Vector2f pos, float range = float.MaxValue)
        {
            CachedList.Clear();
            AllNearestNeighborSearch(pos, range, CachedList);
            return CachedList.ToArray();
        }

        public T[] GetObjectsInRect(FloatRect rect)
        {
            CachedList.Clear();
            ObjectsInRectSearch(rect, CachedList);
            return CachedList.ToArray();
        }

        public void GetKClosestObjects(Vector2f pos, uint k, float range, PriorityQueue<T> results)
        {
            KNearestNeighborSearch(pos, k, range, results);
        }

        public void GetObjectsInRange(Vector2f pos, float range, IList<T> results)
        {
            AllNearestNeighborSearch(pos, range, results);
        }

        public void GetObjectsInRect(FloatRect rect, IList<T> results)
        {
            ObjectsInRectSearch(rect, results);
        }

        private T NearestNeighborSearch(Vector2f pos, float range)
        {
            T closest = null;
            var idx = FindBucketIndex(pos);

            var bucketRangeX = (int) (range / m_BucketWidth) + 1;
            if (bucketRangeX < 0) bucketRangeX = m_NumBucketsWidth / 2;
            var bucketRangeY = (int) (range / m_BucketHeight) + 1;
            if (bucketRangeY < 0) bucketRangeY = m_NumBucketsHeight / 2;

            for (int i = 0; i <= bucketRangeX; i++)
            {
                for (int j = 0; j <= bucketRangeY; j++)
                {
                    for (int m = -1; m < 2; m += 2)
                    {
                        var nextIdx = idx + (m * (i + (j * m_NumBucketsWidth)));
                        // If index is out of range
                        if (nextIdx < 0 || nextIdx >= m_Buckets.Length)
                            continue;

                        if (m_Buckets[nextIdx] == null)
                            continue;

                        var bucket = m_Buckets[nextIdx];
                        for (int k = 0; k < bucket.Count; k++)
                        {
                            var ds = (bucket[k].Position - pos).SquaredLength();
                            if (ds < range * range)
                            {
                                closest = bucket[k];
                                range = (float)Math.Sqrt(ds);
                            }
                        }

                        bucketRangeX = (int)(range / m_BucketWidth) + 1;
                        if (bucketRangeX < 0) bucketRangeX = m_NumBucketsWidth / 2;
                        bucketRangeY = (int)(range / m_BucketHeight) + 1;
                        if (bucketRangeY < 0) bucketRangeY = m_NumBucketsHeight / 2;

                        if (i < -bucketRangeX) i = -bucketRangeX;
                        if (i > bucketRangeX) i = bucketRangeX;
                        if (j < -bucketRangeY) j = -bucketRangeY;
                        if (j > bucketRangeY) j = bucketRangeY;
                    }
                }
            }

            return closest;
        }

        private void AllNearestNeighborSearch(Vector2f pos, float range, IList<T> results)
        {
            var idx = FindBucketIndex(pos);

            var bucketRangeX = (int)(range / m_BucketWidth) + 1;
            if (bucketRangeX < 0) bucketRangeX = m_NumBucketsWidth / 2;
            var bucketRangeY = (int)(range / m_BucketHeight) + 1;
            if (bucketRangeY < 0) bucketRangeY = m_NumBucketsHeight / 2;

            for (int i = -bucketRangeX; i <= bucketRangeX; i++)
            {
                for (int j = -bucketRangeY; j <= bucketRangeY; j++)
                {
                    var nextIdx = idx + (i + (j * m_NumBucketsWidth));
                    // If index is out of range
                    if (nextIdx < 0 || nextIdx >= m_Buckets.Length)
                        continue;

                    if (m_Buckets[nextIdx] == null)
                        continue;

                    var bucket = m_Buckets[nextIdx];
                    for (int n = 0; n < bucket.Count; n++)
                    {
                        var ds = (bucket[n].Position - pos).SquaredLength();
                        if (ds > range * range)
                            continue;

                        results.Add(bucket[n]);
                    }
                }
            }
        }

        private void KNearestNeighborSearch(Vector2f pos, uint k, float range, PriorityQueue<T> results)
        {
            var idx = FindBucketIndex(pos);

            var bucketRangeX = (int)(range / m_BucketWidth) + 1;
            if (bucketRangeX < 0) bucketRangeX = m_NumBucketsWidth / 2;
            var bucketRangeY = (int)(range / m_BucketHeight) + 1;
            if (bucketRangeY < 0) bucketRangeY = m_NumBucketsHeight / 2;

            for (int i = 0; i <= bucketRangeX; i++)
            {
                for (int j = 0; j <= bucketRangeY; j++)
                {
                    for (int m = -1; m < 2; m += 2)
                    {
                        var nextIdx = idx + (m * (i + (j * m_NumBucketsWidth)));
                        // If index is out of range
                        if (nextIdx < 0 || nextIdx >= m_Buckets.Length)
                            continue;

                        if (m_Buckets[nextIdx] == null)
                            continue;

                        var bucket = m_Buckets[nextIdx];
                        for (int n = 0; n < bucket.Count; n++)
                        {
                            var ds = (bucket[n].Position - pos).SquaredLength();
                            if (ds > range * range)
                                continue;

                            if (results.Count < k)
                            {
                                results.Enqueue(bucket[n], ds);
                                continue;
                            }

                            if (ds < results.GetPriority(results.Peek()))
                            {
                                results.Dequeue();
                                results.Enqueue(bucket[n], ds);
                                range = (float)Math.Sqrt(results.GetPriority(results.Peek()));
                            }
                        }

                        bucketRangeX = (int)(range / m_BucketWidth) + 1;
                        if (bucketRangeX < 0) bucketRangeX = m_NumBucketsWidth / 2;
                        bucketRangeY = (int)(range / m_BucketHeight) + 1;
                        if (bucketRangeY < 0) bucketRangeY = m_NumBucketsHeight / 2;

                        if (i < -bucketRangeX) i = -bucketRangeX - 1;
                        if (i > bucketRangeX) i = bucketRangeX;
                        if (j < -bucketRangeY) j = -bucketRangeY - 1;
                        if (j > bucketRangeY) j = bucketRangeY;
                    }
                }
            }
        }

        private void ObjectsInRectSearch(FloatRect rect, ICollection<T> results)
        {
            var idx = FindBucketIndex(rect.Center());

            var range = (rect.Center() - rect.Min()).Length();

            var bucketRangeX = (int)(range / m_BucketWidth) + 1;
            if (bucketRangeX < 0) bucketRangeX = m_NumBucketsWidth / 2;
            var bucketRangeY = (int)(range / m_BucketHeight) + 1;
            if (bucketRangeY < 0) bucketRangeY = m_NumBucketsHeight / 2;

            for (int i = -bucketRangeX; i <= bucketRangeX; i++)
            {
                for (int j = -bucketRangeY; j <= bucketRangeY; j++)
                {
                    var nextIdx = idx + (i + (j * m_NumBucketsWidth));
                    // If index is out of range
                    if (nextIdx < 0 || nextIdx >= m_Buckets.Length)
                        continue;

                    if (m_Buckets[nextIdx] == null)
                        continue;

                    var bucket = m_Buckets[nextIdx];
                    for (int n = 0; n < bucket.Count; n++)
                    {
                        if (!rect.Contains(bucket[n].Position.X, bucket[n].Position.Y))
                            continue;

                        results.Add(bucket[n]);
                    }
                }
            }
        }

        private List<T> FindBucket(Vector2f pos)
        {
            // TODO: what happens if pos is out of bounds?

            var fromLeft = pos.X - m_Region.Left;
            var x = (int)(fromLeft / m_BucketWidth);

            var fromTop = pos.Y - m_Region.Top;
            var y = (int)(fromTop / m_BucketHeight);

            return m_Buckets[x + (y*m_NumBucketsWidth)];
        }

        private int FindBucketIndex(Vector2f pos)
        {
            // TODO: what happens if pos is out of bounds?

            var fromLeft = pos.X - m_Region.Left;
            var x = (int)(fromLeft / m_BucketWidth);

            var fromTop = pos.Y - m_Region.Top;
            var y = (int)(fromTop / m_BucketHeight);

            return x + (y * m_NumBucketsWidth);
        }
    }
}
