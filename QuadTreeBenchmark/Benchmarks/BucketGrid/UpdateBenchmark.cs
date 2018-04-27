using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using QuadTree;
using SFML.Graphics;
using SFML.System;

namespace QuadTreeBenchmark.Benchmarks.BucketGrid
{
    public class UpdateBenchmark : IBenchmark<BucketGrid<TestObject>>
    {
        private List<TestObject> m_Objects;
        private BucketGrid<TestObject> m_Tree;
        private readonly Random m_Random;

        public string Name => "Update-BG";

        [Params(100,1000,10000)]
        public int N;

        [Params(100, 1000)]
        public int NumBuckets;

        public UpdateBenchmark()
        {
            m_Random = new Random();
        }

        [GlobalSetup]
        public void Setup()
        {
            m_Tree = new BucketGrid<TestObject>(new FloatRect(0, 0, 100, 100), (int)Math.Sqrt(NumBuckets), (int)Math.Sqrt(NumBuckets));
            m_Objects = new List<TestObject>(N);

            for (int i = 0; i < N; i++)
            {
                var obj = new TestObject(RandomPosition());
                m_Objects.Add(obj);
                m_Tree.Add(obj);
            }
        }

        [Benchmark]
        public void RandomizingPositionsBG()
        {
            for (int i = 0; i < N; i++)
            {
                m_Objects[i].Position = m_Objects[(i + 1) % N].Position;
            }

            m_Tree.Update();
        }

        [Benchmark]
        public void AddDeleteBG()
        {
            var obj = m_Objects[m_Random.Next(0, m_Objects.Count)];

            m_Tree.Remove(obj);
            m_Tree.Update();
            m_Tree.Add(obj);
            m_Tree.Update();
        }

        private Vector2f RandomPosition()
        {
            return new Vector2f((float)(m_Random.NextDouble() * 100), (float)(m_Random.NextDouble() * 100));
        }
    }
}
