using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using QuadTree;
using SFML.Graphics;
using SFML.System;

namespace QuadTreeBenchmark.Benchmarks.BucketGrid
{
    public class KClosestBenchmark : IBenchmark<BucketGrid<TestObject>>
    {
        private List<TestObject> m_Objects;
        private BucketGrid<TestObject> m_Grid;
        private readonly Random m_Random;

        public string Name => "KClosest-BG";

        [Params(100, 1000, 10000)]
        public int NumObjects;

        [Params(10,100)]
        public uint K;

        [Params(100, 1000)]
        public int NumBuckets;

        public KClosestBenchmark()
        {
            m_Random = new Random();
        }

        [GlobalSetup]
        public void Setup()
        {
            m_Grid = new BucketGrid<TestObject>(new FloatRect(0, 0, 100, 100), (int)Math.Sqrt(NumBuckets), (int)Math.Sqrt(NumBuckets));
            m_Objects = new List<TestObject>(NumObjects);

            for (int i = 0; i < NumObjects; i++)
            {
                var obj = new TestObject(RandomPosition());
                m_Objects.Add(obj);
                m_Grid.Add(obj);
            }

            m_Grid.Update();
        }

        [Benchmark]
        public void KClosestBG()
        {
            m_Grid.GetKClosestObjects(RandomPosition(), K, (float) (m_Random.NextDouble() * 100));
        }

        private Vector2f RandomPosition()
        {
            return new Vector2f((float)(m_Random.NextDouble() * 100), (float)(m_Random.NextDouble() * 100));
        }
    }
}
