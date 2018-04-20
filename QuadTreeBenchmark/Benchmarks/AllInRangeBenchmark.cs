using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using SFML.Graphics;
using SFML.System;
using QuadTree;

namespace QuadTreeBenchmark.Benchmarks
{
    public class AllInRangeBenchmark : IBenchmark
    {
        private List<TestObject> m_Objects;
        private QuadTree<TestObject> m_Tree;
        private BucketGrid<TestObject> m_Grid;
        private readonly Random m_Random;

        public string Name => "AllInRange";

        [Params(100, 1000, 10000)]
        public int NumObjects;

        public AllInRangeBenchmark()
        {
            m_Random = new Random();
        }

        [GlobalSetup]
        public void Setup()
        {
            m_Tree = new QuadTree<TestObject>(new FloatRect(0, 0, 100, 100));
            m_Grid = new BucketGrid<TestObject>(new FloatRect(0, 0, 100, 100), 100, 100);
            m_Objects = new List<TestObject>(NumObjects);

            for (int i = 0; i < NumObjects; i++)
            {
                var obj = new TestObject(RandomPosition());
                m_Objects.Add(obj);
                m_Tree.Add(obj);
                m_Grid.Add(obj);
            }

            m_Tree.Update();
            m_Grid.Update();
        }

        [Benchmark]
        public void AllInRangeQuad()
        {
            m_Tree.GetObjectsInRange(RandomPosition(), (float)(m_Random.NextDouble() * 100));
        }

        [Benchmark]
        public void AllInRangeGrid()
        {
            m_Grid.GetObjectsInRange(RandomPosition(), (float)(m_Random.NextDouble() * 100));
        }

        private Vector2f RandomPosition()
        {
            return new Vector2f((float)(m_Random.NextDouble() * 100), (float)(m_Random.NextDouble() * 100));
        }
    }
}
