using PriorityQueue;

namespace QuadTree
{
    /// <summary>
    /// Convenience wrapper around a PriorityQueue set to prioritize
    /// higher priorities
    /// </summary>
    public class MaxPriorityQueue<T> : PriorityQueue<T>
    {
        public MaxPriorityQueue()
            : base(true)
        {
        }
    }
}
