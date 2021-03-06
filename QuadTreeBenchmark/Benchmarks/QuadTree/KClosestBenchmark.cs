﻿using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using QuadTree;
using SFML.Graphics;
using SFML.System;

namespace QuadTreeBenchmark.Benchmarks.QuadTree
{
    public class KClosestBenchmark : IBenchmark<QuadTree<TestObject>>
    {
        private List<TestObject> m_Objects;
        private QuadTree<TestObject> m_Tree;
        private readonly Random m_Random = new Random(0);

        public string Name => "KClosest-QT";

        [Params(100, 1000, 10000)]
        public int NumObjects;

        [Params(10,100)]
        public uint K;

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

            m_Tree.Update();
        }

        [Benchmark]
        public void KClosestQT()
        {
            m_Tree.GetKClosestObjects(RandomPosition(), K, (float) (m_Random.NextDouble() * 100));
        }

        private Vector2f RandomPosition()
        {
            return new Vector2f((float)(m_Random.NextDouble() * 100), (float)(m_Random.NextDouble() * 100));
        }
    }
}
