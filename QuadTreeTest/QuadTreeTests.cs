using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PriorityQueue;
using SFML.Graphics;
using SFML.System;
using QuadTree;

namespace QuadTreeTest
{
    [TestClass]
    public class QuadTreeTests
    {
        private readonly FloatRect m_Bounds = new FloatRect(0,0,1000,1000);

        [TestMethod]
        public void ConstructorTests()
        {
            QuadTree<TestObject> tree = new QuadTree<TestObject>(m_Bounds);
            Assert.AreEqual(tree.Count, 0);

            QuadTree<TestObject> tree2 = new QuadTree<TestObject>(m_Bounds, 
                new List<TestObject> {new TestObject(), new TestObject()});
            Assert.AreEqual(tree2.Count, 2);

            AssertThrows<NullReferenceException>(() => new QuadTree<TestObject>(m_Bounds, null));

            FloatRect nonSquareBounds = new FloatRect(0,0,5,10);
            AssertThrows<ArgumentException>(() => new QuadTree<TestObject>(nonSquareBounds));
        }

        [TestMethod]
        public void AddRemoveTest()
        {
            QuadTree<TestObject> tree = new QuadTree<TestObject>(m_Bounds);

            TestObject one = new TestObject();
            TestObject two = new TestObject();

            Assert.AreEqual(tree.Count, 0);
            tree.Update();
            Assert.AreEqual(tree.Count, 0);

            tree.Add(one);
            Assert.AreEqual(tree.Count, 0);
            tree.Update();
            Assert.AreEqual(tree.Count, 1);

            tree.Add(two);
            tree.Update();
            Assert.AreEqual(tree.Count, 2);

            Assert.AreEqual(tree.Count, 2);
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

            // Making sure removing an object not
            // in the tree doesn't cause any problems
            tree.Add(one);
            tree.Remove(two); // Not in tree
            tree.Update();
            Assert.AreEqual(tree.Count, 1);

#if DEBUG
            AssertThrows<ArgumentException>(() => tree.Add(null));
            AssertThrows<ArgumentException>(() => tree.Remove(null));
#endif
        }

        [TestMethod]
        public void GetKClosestObjectsTest()
        {
            QuadTree<TestObject> tree = new QuadTree<TestObject>(m_Bounds);

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
            HaveSameElements(results, new []{ objs[0] });

            results = tree.GetKClosestObjects(new Vector2f(1, 1), 3);
            Assert.IsTrue(HaveSameElements(results, new[] { objs[0], objs[1], objs[2] }));

            results = tree.GetKClosestObjects(new Vector2f(51, 51), 3);
            Assert.IsTrue(HaveSameElements(results, new[] { objs[1], objs[2], objs[3] }));

            results = tree.GetKClosestObjects(new Vector2f(51, 51), 3, 10f);
            Assert.IsTrue(HaveSameElements(results, new[] { objs[3] }));

#if DEBUG
            AssertThrows<ArgumentException>(
                () => tree.GetKClosestObjects(new Vector2f(51, 51), 3, -10f));
#endif

            PriorityQueue<TestObject> resultsQueue = new PriorityQueue<TestObject>(true);
            tree.GetKClosestObjects(new Vector2f(1, 1), 1, float.MaxValue, resultsQueue);
            Assert.IsTrue(HaveSameElements(resultsQueue.ToArray(), new[] { objs[0] }));

            resultsQueue.Clear();
            tree.GetKClosestObjects(new Vector2f(1, 1), 3, float.MaxValue, resultsQueue);
            Assert.IsTrue(HaveSameElements(resultsQueue.ToArray(), new[] { objs[0], objs[1], objs[2] }));

            resultsQueue.Clear();
            tree.GetKClosestObjects(new Vector2f(51, 51), 3, float.MaxValue, resultsQueue);
            Assert.IsTrue(HaveSameElements(resultsQueue.ToArray(), new[] { objs[1], objs[2], objs[3] }));

            resultsQueue.Clear();
            tree.GetKClosestObjects(new Vector2f(51, 51), 3, 10f, resultsQueue);
            Assert.IsTrue(HaveSameElements(resultsQueue.ToArray(), new[] { objs[3] }));

#if DEBUG
            resultsQueue.Clear();
            AssertThrows<ArgumentException>(
                () => tree.GetKClosestObjects(new Vector2f(51, 51), 3, -10f, resultsQueue));
            AssertThrows<ArgumentException>(
                () => tree.GetKClosestObjects(new Vector2f(51, 51), 3, 10f, null));
#endif
        }

        [TestMethod]
        public void GetObjectsInRangeTest()
        {
            QuadTree<TestObject> tree = new QuadTree<TestObject>(m_Bounds);

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

            Transformable[] results = tree.GetObjectsInRange(new Vector2f(-1, -1), 1);
            Assert.IsTrue(HaveSameElements(results, new Transformable[0]));

            results = tree.GetObjectsInRange(new Vector2f(0, 0), 5);
            Assert.IsTrue(HaveSameElements(results, new [] { objs[0] }));

            results = tree.GetObjectsInRange(new Vector2f(5, 5), 10);
            Assert.IsTrue(HaveSameElements(results, new[] { objs[0], objs[1], objs[2] }));

            results = tree.GetObjectsInRange(new Vector2f(5, 5));
            Assert.IsTrue(HaveSameElements(results, new[] { objs[0], objs[1], objs[2], objs[3] }));

#if DEBUG
            AssertThrows<ArgumentException>(
                () => tree.GetObjectsInRange(new Vector2f(0, 0), -5f));
#endif

            List<TestObject> resultsList = new List<TestObject>();
            tree.GetObjectsInRange(new Vector2f(-1, -1), 1, resultsList);
            Assert.IsTrue(HaveSameElements(resultsList.ToArray(), new TestObject[0]));

            resultsList.Clear();
            tree.GetObjectsInRange(new Vector2f(0, 0), 5, resultsList);
            Assert.IsTrue(HaveSameElements(resultsList.ToArray(), new[] { objs[0] }));

            resultsList.Clear();
            tree.GetObjectsInRange(new Vector2f(5, 5), 10, resultsList);
            Assert.IsTrue(HaveSameElements(resultsList.ToArray(), new[] { objs[0], objs[1], objs[2] }));

            resultsList.Clear();
            tree.GetObjectsInRange(new Vector2f(5, 5), float.MaxValue, resultsList);
            Assert.IsTrue(HaveSameElements(resultsList.ToArray(), new[] { objs[0], objs[1], objs[2], objs[3] }));

#if DEBUG
            AssertThrows<ArgumentException>(
                () => tree.GetObjectsInRange(new Vector2f(0, 0), -5f, resultsList));
            AssertThrows<ArgumentException>(
                () => tree.GetObjectsInRange(new Vector2f(0, 0), 5f, null));
#endif
        }

        [TestMethod]
        public void GetObjectsInRectTest()
        {
            QuadTree<TestObject> tree = new QuadTree<TestObject>(m_Bounds);

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

            TestObject[] results = tree.GetObjectsInRect(new FloatRect(-1, -1, 0, 0));
            Assert.IsTrue(HaveSameElements(results, new TestObject[0]));

            results = tree.GetObjectsInRect(new FloatRect(8, -1, 7, 7));
            Assert.IsTrue(HaveSameElements(results, new []{ objs[1] }));

            results = tree.GetObjectsInRect(new FloatRect(-1, -1, 15, 20));
            Assert.IsTrue(HaveSameElements(results, new[] { objs[0], objs[1], objs[2] }));

            results = tree.GetObjectsInRect(new FloatRect(-1, -1, 60, 60));
            Assert.IsTrue(HaveSameElements(results, new[] { objs[0], objs[1], objs[2], objs[3] }));

            List<TestObject> resultsList = new List<TestObject>();
            tree.GetObjectsInRect(new FloatRect(-1, -1, 0, 0), resultsList);
            Assert.IsTrue(HaveSameElements(resultsList.ToArray(), new TestObject[0]));

            resultsList.Clear();
            tree.GetObjectsInRect(new FloatRect(8, -1, 7, 7), resultsList);
            Assert.IsTrue(HaveSameElements(resultsList.ToArray(), new[] { objs[1] }));

            resultsList.Clear();
            tree.GetObjectsInRect(new FloatRect(-1, -1, 15, 20), resultsList);
            Assert.IsTrue(HaveSameElements(resultsList.ToArray(), new[] { objs[0], objs[1], objs[2] }));

            resultsList.Clear();
            tree.GetObjectsInRect(new FloatRect(-1, -1, 60, 60), resultsList);
            Assert.IsTrue(HaveSameElements(resultsList.ToArray(), new[] { objs[0], objs[1], objs[2], objs[3] }));
        }

        [TestMethod]
        public void GetClosestObjectTest()
        {
            QuadTree<TestObject> tree = new QuadTree<TestObject>(m_Bounds);

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

            Assert.AreEqual(tree.GetClosestObject(new Vector2f(0,0)), objs[0]);
            Assert.AreEqual(tree.GetClosestObject(new Vector2f(5,5)), objs[2]);
            Assert.AreEqual(tree.GetClosestObject(new Vector2f(5,5)), objs[2]);
            Assert.AreEqual(tree.GetClosestObject(new Vector2f(5,5), 1), null);
            Assert.AreEqual(tree.GetClosestObject(new Vector2f(-1,-1)), objs[0]);
        }

        [TestMethod]
        public void MovingObjectsTest()
        {
            TestObject[] objs = {
                new TestObject(250,250),
                new TestObject(450,450),
                new TestObject(750,250),
                new TestObject(750,750),
                new TestObject(250,750)
            };

            QuadTree<TestObject> tree = new QuadTree<TestObject>(m_Bounds);

            foreach (var obj in objs)
            {
                tree.Add(obj);
            }

            List<FloatRect> regions = new List<FloatRect>();

            tree.Update();
            tree.GetAllRegions(regions);
            Assert.AreEqual(8, regions.Count);
            regions.Clear();

            objs[1].Position = new Vector2f(125, 125);
            tree.Update();
            tree.GetAllRegions(regions);
            Assert.AreEqual(8, regions.Count);
            regions.Clear();

            objs[0].Position = new Vector2f(675, 675);
            tree.Remove(objs[0]);
            tree.Update();
            // Currently takes an extra update for the tree
            // to prune a certain empty node. Hopefully this 
            // can be improved eventually. This test should 
            // tell us if something ever changes with this issue.
            tree.Update();
            tree.GetAllRegions(regions);
            Assert.AreEqual(7, regions.Count);
            regions.Clear();
        }

        [TestMethod]
        public void InvalidObjectPositionTest()
        {
            var tree = new QuadTree<TestObject>(m_Bounds);

            tree.Add(new TestObject(-10, -10));
            tree.Add(new TestObject(10, 10));

            AssertThrows<Exception>(() => tree.Update());
        }

        [TestMethod]
        public void EnumerationTests()
        {
            var tree = new QuadTree<TestObject>(m_Bounds);

            var one = new TestObject(1,1);
            var two = new TestObject(2,2);
            var three = new TestObject(3,3);

            tree.Add(one);
            tree.Add(two);
            tree.Add(three);

            tree.Update();

            HashSet<TestObject> enumerated = new HashSet<TestObject>(tree);

            Assert.IsTrue(enumerated.Contains(one));
            Assert.IsTrue(enumerated.Contains(two));
            Assert.IsTrue(enumerated.Contains(three));
            Assert.AreEqual(3, enumerated.Count);
        }

        private bool HaveSameElements<T>(T[] a, T[] b)
        {
            for (int i = 0; i < a.Length; i++)
            {
                if (!b.Contains(a[i]))
                    return false;
            }

            for (int i = 0; i < b.Length; i++)
            {
                if (!a.Contains(b[i]))
                    return false;
            }

            return true;
        }

        internal static void AssertThrows<TException>(Action method)
            where TException : Exception
        {
            try
            {
                method.Invoke();
            }
            catch (TException)
            {
                return; // Expected exception.
            }
            catch (Exception ex)
            {
                Assert.Fail("Wrong exception thrown: " + ex.Message);
            }
            Assert.Fail("No exception thrown");
        }
    }
}
