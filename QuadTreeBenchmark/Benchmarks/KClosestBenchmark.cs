using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using SFML.Graphics;
using SFML.System;
using SFQuadTree;

namespace QuadTreeBenchmark.Benchmarks
{
    public class KClosestBenchmark : IBenchmark
    {
        private List<TestObject> m_Objects;
        private QuadTree<TestObject> m_Tree;
        private readonly Random m_Random;

        public string Name => "KClosest";

        [Params(100, 1000, 10000)]
        public int NumObjects;

        [Params(10,100)]
        public int K;

        public KClosestBenchmark()
        {
            m_Random = new Random();
        }

        [GlobalSetup]
        public void Setup()
        {
            m_Tree = new QuadTree<TestObject>(new FloatRect(0, 0, 100, 100));
            m_Objects = new List<TestObject>(NumObjects);

            for (int i = 0; i < NumObjects; i++)
            {
                var obj = new TestObject(RandomPosition());
                m_Objects.Add(obj);
                m_Tree.Add(obj);
            }
        }

        [Benchmark]
        public void KClosest()
        {
            m_Tree.GetKClosestObjects(RandomPosition(), (uint) K, (float) (m_Random.NextDouble() * 100));
        }

        private Vector2f RandomPosition()
        {
            return new Vector2f((float)(m_Random.NextDouble() * 100), (float)(m_Random.NextDouble() * 100));
        }
    }
}
