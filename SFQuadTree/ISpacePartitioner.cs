using System.Collections.Generic;
using SFML.Graphics;
using SFML.System;
using PriorityQueue;

namespace QuadTree
{
    interface ISpacePartitioner<T>
    {
        void Update();
        void Add(T t);
        void Remove(T t);

        T GetClosestObject(Vector2f pos, float maxDistance = float.MaxValue);

        T[] GetKClosestObjects(Vector2f pos, uint k, float range = float.MaxValue);
        T[] GetObjectsInRange(Vector2f pos, float range = float.MaxValue);
        T[] GetObjectsInRect(FloatRect rect);

        void GetKClosestObjects(Vector2f pos, uint k, float range, PriorityQueue<T> results);
        void GetObjectsInRange(Vector2f pos, float range, IList<T> results);
        void GetObjectsInRect(FloatRect rect, IList<T> results);
    }
}
