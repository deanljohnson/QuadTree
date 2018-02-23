using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PriorityQueue;
using SFML.Graphics;
using SFML.System;
using SFQuadTree;

namespace QuadTreeTest
{
    [TestClass]
    public class QuadTreeTests
    {
        public class TestObject : Transformable
        {
            public TestObject()
            {

            }

            public TestObject(Vector2f pos)
            {
                Position = pos;
            }

            public TestObject(float x, float y)
            {
                Position = new Vector2f(x, y);
            }
        }

        private readonly FloatRect m_Bounds = new FloatRect(0,0,1000,1000);

        [TestMethod]
        public void ConstructorTests()
        {
            QuadTree tree = new QuadTree(m_Bounds);
            Assert.AreEqual(tree.Count, 0);

            QuadTree tree2 = new QuadTree(m_Bounds, 
                new List<Transformable> {new TestObject(), new TestObject()});
            Assert.AreEqual(tree2.Count, 2);

            Assert.ThrowsException<NullReferenceException>(() => new QuadTree(m_Bounds, null));
        }

        [TestMethod]
        public void AddRemoveTest()
        {
            QuadTree tree = new QuadTree(m_Bounds);

            TestObject one = new TestObject();
            TestObject two = new TestObject();

            tree.Add(one);
            Assert.AreEqual(tree.Count, 0);
            tree.Update();
            Assert.AreEqual(tree.Count, 1);
            tree.Add(two);
            tree.Update();
            Assert.AreEqual(tree.Count, 2);

            tree.Remove(one);
            tree.Update();
            Assert.AreEqual(tree.Count, 1);
            tree.Remove(two);
            tree.Update();
            Assert.AreEqual(tree.Count, 0);

            tree.Add(one);
            Assert.AreEqual(tree.Count, 0);
            tree.Add(two);
            Assert.AreEqual(tree.Count, 0);
            tree.Update();
            Assert.AreEqual(tree.Count, 2);

            tree.Remove(one);
            Assert.AreEqual(tree.Count, 2);
            tree.Remove(two);
            Assert.AreEqual(tree.Count, 2);
            tree.Update();
            Assert.AreEqual(tree.Count, 0);
        }

        [TestMethod]
        public void GetKClosestObjectsTest()
        {
            QuadTree tree = new QuadTree(m_Bounds);

            TestObject[] objs = {
                new TestObject(0,0), 
                new TestObject(10,0), 
                new TestObject(7,7), 
                new TestObject(50,50)
            };

            for (int i = 0; i < objs.Length; i++)
            {
                tree.Add(objs[i]);
            }

            tree.Update();

            Transformable[] results = tree.GetKClosestObjects(new Vector2f(1, 1), 1);
            AssertHaveSameElements(results, new []{ objs[0] });

            results = tree.GetKClosestObjects(new Vector2f(1, 1), 3);
            AssertHaveSameElements(results, new[] { objs[0], objs[1], objs[2] });

            results = tree.GetKClosestObjects(new Vector2f(51, 51), 3);
            AssertHaveSameElements(results, new[] { objs[1], objs[2], objs[3] });

            results = tree.GetKClosestObjects(new Vector2f(51, 51), 3, 10f);
            AssertHaveSameElements(results, new[] { objs[3] });

            PriorityQueue<Transformable> resultsQueue = new PriorityQueue<Transformable>(true);
            tree.GetKClosestObjects(new Vector2f(1, 1), 1, float.MaxValue, resultsQueue);
            AssertHaveSameElements(resultsQueue.ToArray(), new[] { objs[0] });

            resultsQueue.Clear();
            tree.GetKClosestObjects(new Vector2f(1, 1), 3, float.MaxValue, resultsQueue);
            AssertHaveSameElements(resultsQueue.ToArray(), new[] { objs[0], objs[1], objs[2] });

            resultsQueue.Clear();
            tree.GetKClosestObjects(new Vector2f(51, 51), 3, float.MaxValue, resultsQueue);
            AssertHaveSameElements(resultsQueue.ToArray(), new[] { objs[1], objs[2], objs[3] });

            resultsQueue.Clear();
            tree.GetKClosestObjects(new Vector2f(51, 51), 3, 10f, resultsQueue);
            AssertHaveSameElements(resultsQueue.ToArray(), new[] { objs[3] });
        }

        private void AssertHaveSameElements(Transformable[] a, Transformable[] b)
        {
            for (int i = 0; i < a.Length; i++)
            {
                Assert.IsTrue(b.Contains(a[i]));
            }

            for (int i = 0; i < b.Length; i++)
            {
                Assert.IsTrue(a.Contains(b[i]));
            }
        }
    }
}
