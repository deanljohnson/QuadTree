using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using SFML.Graphics;
using SFML.System;
using SFQuadTree;

namespace QuadTreeBenchmark.Benchmarks
{
    public class UpdateBenchmark : IBenchmark
    {
        private List<TestObject> m_Objects;
        private QuadTree<TestObject> m_Tree;
        private BucketGrid<TestObject> m_Grid;
        private readonly Random m_Random;

        [Params(100,1000,10000)]
        public int N;

        public string Name => "Update";

        public UpdateBenchmark()
        {
            m_Random = new Random();
        }

        [GlobalSetup]
        public void Setup()
        {
            m_Tree = new QuadTree<TestObject>(new FloatRect(0, 0, 100, 100));
            m_Grid = new BucketGrid<TestObject>(new FloatRect(0, 0, 100, 100), 100, 100);
            m_Objects = new List<TestObject>(N);

            for (int i = 0; i < N; i++)
            {
                var obj = new TestObject(RandomPosition());
                m_Objects.Add(obj);
                m_Tree.Add(obj);
                m_Grid.Add(obj);
            }
        }

        [Benchmark]
        public void RandomizingPositionsQuad()
        {
            for (int i = 0; i < N; i++)
            {
                m_Objects[i].Position = m_Objects[(i + 1) % N].Position;
            }

            m_Tree.Update();
        }

        [Benchmark]
        public void RandomizingPositionsGrid()
        {
            for (int i = 0; i < N; i++)
            {
                m_Objects[i].Position = m_Objects[(i + 1) % N].Position;
            }

            m_Grid.Update();
        }

        [Benchmark]
        public void QuadTreeRebuild()
        {
            m_Tree = new QuadTree<TestObject>(new FloatRect(0, 0, 100, 100), m_Objects);
        }

        /*[Benchmark]
        public void BucketGridRebuild()
        {
            m_Grid = new QuadTree<TestObject>(new FloatRect(0, 0, 100, 100), m_Objects);
        }*/

        private Vector2f RandomPosition()
        {
            return new Vector2f((float)(m_Random.NextDouble() * 100), (float)(m_Random.NextDouble() * 100));
        }
    }
}
