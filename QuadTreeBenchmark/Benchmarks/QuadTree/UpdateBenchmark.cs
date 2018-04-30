using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using QuadTree;
using SFML.Graphics;
using SFML.System;

namespace QuadTreeBenchmark.Benchmarks.QuadTree
{
    public class UpdateBenchmark : IBenchmark<QuadTree<TestObject>>
    {
        private List<TestObject> m_Objects;
        private QuadTree<TestObject> m_Tree;
        private readonly Random m_Random = new Random(0);

        [Params(100,1000,10000)]
        public int N;

        public string Name => "Update-QT";

        [GlobalSetup]
        public void Setup()
        {
            m_Tree = new QuadTree<TestObject>(new FloatRect(0, 0, 1000, 1000));
            m_Objects = new List<TestObject>(N);

            for (int i = 0; i < N; i++)
            {
                var obj = new TestObject(RandomPosition());
                m_Objects.Add(obj);
                m_Tree.Add(obj);
            }
        }

        [Benchmark]
        public void RandomizingPositionsQT()
        {
            for (int i = 0; i < N; i++)
            {
                m_Objects[i].Position = m_Objects[(i + 1) % N].Position;
            }

            m_Tree.Update();
        }

        [Benchmark]
        public void AddDeleteQT()
        {
            var obj = m_Objects[m_Random.Next(0, m_Objects.Count)];

            m_Tree.Remove(obj);
            m_Tree.Update();
            m_Tree.Add(obj);
            m_Tree.Update();
        }

        private Vector2f RandomPosition()
        {
            return new Vector2f((float)(m_Random.NextDouble() * 1000), (float)(m_Random.NextDouble() * 1000));
        }
    }
}
