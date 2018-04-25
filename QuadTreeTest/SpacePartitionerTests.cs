using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PriorityQueue;
using QuadTree;
using SFML.Graphics;
using SFML.System;

namespace QuadTreeTest
{
    public static class SpacePartitionerTests
    {
        public static void AddRemoveTest(ISpacePartitioner<TestObject> partitioner)
        {
            TestObject one = new TestObject();
            TestObject two = new TestObject();

            Assert.AreEqual(partitioner.Count, 0);
            partitioner.Update();
            Assert.AreEqual(partitioner.Count, 0);

            partitioner.Add(one);
            Assert.AreEqual(partitioner.Count, 0);
            partitioner.Update();
            Assert.AreEqual(partitioner.Count, 1);

            partitioner.Add(two);
            partitioner.Update();
            Assert.AreEqual(partitioner.Count, 2);

            Assert.AreEqual(partitioner.Count, 2);
            partitioner.Update();
            Assert.AreEqual(partitioner.Count, 2);

            partitioner.Remove(one);
            partitioner.Update();
            Assert.AreEqual(partitioner.Count, 1);
            partitioner.Remove(two);
            partitioner.Update();
            Assert.AreEqual(partitioner.Count, 0);

            partitioner.Add(one);
            Assert.AreEqual(partitioner.Count, 0);
            partitioner.Add(two);
            Assert.AreEqual(partitioner.Count, 0);
            partitioner.Update();
            Assert.AreEqual(partitioner.Count, 2);

            partitioner.Remove(one);
            Assert.AreEqual(partitioner.Count, 2);
            partitioner.Remove(two);
            Assert.AreEqual(partitioner.Count, 2);
            partitioner.Update();
            Assert.AreEqual(partitioner.Count, 0);

            // Making sure removing an object not
            // in the partitioner doesn't cause any problems
            partitioner.Add(one);
            partitioner.Remove(two); // Not in partitioner
            partitioner.Update();
            Assert.AreEqual(partitioner.Count, 1);

#if DEBUG
            AssertThrows<ArgumentException>(() => partitioner.Add(null));
            AssertThrows<ArgumentException>(() => partitioner.Remove(null));
#endif
        }

        public static void GetKClosestObjectsTest(ISpacePartitioner<TestObject> partitioner)
        {
            TestObject[] objs = {
                new TestObject(0,0),
                new TestObject(10,0),
                new TestObject(7,7),
                new TestObject(50,50)
            };

            for (int i = 0; i < objs.Length; i++)
            {
                partitioner.Add(objs[i]);
            }

            partitioner.Update();

            Transformable[] results = partitioner.GetKClosestObjects(new Vector2f(1, 1), 1);
            HaveSameElements(results, new[] { objs[0] });

            results = partitioner.GetKClosestObjects(new Vector2f(1, 1), 3);
            Assert.IsTrue(HaveSameElements(results, new[] { objs[0], objs[1], objs[2] }));

            results = partitioner.GetKClosestObjects(new Vector2f(51, 51), 3);
            Assert.IsTrue(HaveSameElements(results, new[] { objs[1], objs[2], objs[3] }));

            results = partitioner.GetKClosestObjects(new Vector2f(51, 51), 3, 10f);
            Assert.IsTrue(HaveSameElements(results, new[] { objs[3] }));

#if DEBUG
            AssertThrows<ArgumentException>(
                () => partitioner.GetKClosestObjects(new Vector2f(51, 51), 3, -10f));
#endif

            PriorityQueue<TestObject> resultsQueue = new PriorityQueue<TestObject>(true);
            partitioner.GetKClosestObjects(new Vector2f(1, 1), 1, float.MaxValue, resultsQueue);
            Assert.IsTrue(HaveSameElements(resultsQueue.ToArray(), new[] { objs[0] }));

            resultsQueue.Clear();
            partitioner.GetKClosestObjects(new Vector2f(1, 1), 3, float.MaxValue, resultsQueue);
            Assert.IsTrue(HaveSameElements(resultsQueue.ToArray(), new[] { objs[0], objs[1], objs[2] }));

            resultsQueue.Clear();
            partitioner.GetKClosestObjects(new Vector2f(51, 51), 3, float.MaxValue, resultsQueue);
            Assert.IsTrue(HaveSameElements(resultsQueue.ToArray(), new[] { objs[1], objs[2], objs[3] }));

            resultsQueue.Clear();
            partitioner.GetKClosestObjects(new Vector2f(51, 51), 3, 10f, resultsQueue);
            Assert.IsTrue(HaveSameElements(resultsQueue.ToArray(), new[] { objs[3] }));

#if DEBUG
            resultsQueue.Clear();
            AssertThrows<ArgumentException>(
                () => partitioner.GetKClosestObjects(new Vector2f(51, 51), 3, -10f, resultsQueue));
            AssertThrows<ArgumentException>(
                () => partitioner.GetKClosestObjects(new Vector2f(51, 51), 3, 10f, null));
#endif
        }

        public static void GetObjectsInRangeTest(ISpacePartitioner<TestObject> partitioner)
        {
            TestObject[] objs = {
                new TestObject(0,0),
                new TestObject(10,0),
                new TestObject(7,7),
                new TestObject(50,50)
            };

            for (int i = 0; i < objs.Length; i++)
            {
                partitioner.Add(objs[i]);
            }

            partitioner.Update();

            Transformable[] results = partitioner.GetObjectsInRange(new Vector2f(-1, -1), 1);
            Assert.IsTrue(HaveSameElements(results, new Transformable[0]));

            results = partitioner.GetObjectsInRange(new Vector2f(0, 0), 5);
            Assert.IsTrue(HaveSameElements(results, new[] { objs[0] }));

            results = partitioner.GetObjectsInRange(new Vector2f(5, 5), 10);
            Assert.IsTrue(HaveSameElements(results, new[] { objs[0], objs[1], objs[2] }));

            results = partitioner.GetObjectsInRange(new Vector2f(5, 5));
            Assert.IsTrue(HaveSameElements(results, new[] { objs[0], objs[1], objs[2], objs[3] }));

#if DEBUG
            AssertThrows<ArgumentException>(
                () => partitioner.GetObjectsInRange(new Vector2f(0, 0), -5f));
#endif

            List<TestObject> resultsList = new List<TestObject>();
            partitioner.GetObjectsInRange(new Vector2f(-1, -1), 1, resultsList);
            Assert.IsTrue(HaveSameElements(resultsList.ToArray(), new TestObject[0]));

            resultsList.Clear();
            partitioner.GetObjectsInRange(new Vector2f(0, 0), 5, resultsList);
            Assert.IsTrue(HaveSameElements(resultsList.ToArray(), new[] { objs[0] }));

            resultsList.Clear();
            partitioner.GetObjectsInRange(new Vector2f(5, 5), 10, resultsList);
            Assert.IsTrue(HaveSameElements(resultsList.ToArray(), new[] { objs[0], objs[1], objs[2] }));

            resultsList.Clear();
            partitioner.GetObjectsInRange(new Vector2f(5, 5), float.MaxValue, resultsList);
            Assert.IsTrue(HaveSameElements(resultsList.ToArray(), new[] { objs[0], objs[1], objs[2], objs[3] }));

#if DEBUG
            AssertThrows<ArgumentException>(
                () => partitioner.GetObjectsInRange(new Vector2f(0, 0), -5f, resultsList));
            AssertThrows<ArgumentException>(
                () => partitioner.GetObjectsInRange(new Vector2f(0, 0), 5f, null));
#endif
        }

        public static void GetObjectsInRectTest(ISpacePartitioner<TestObject> partitioner)
        {
            TestObject[] objs = {
                new TestObject(0,0),
                new TestObject(10,0),
                new TestObject(7,7),
                new TestObject(50,50)
            };

            for (int i = 0; i < objs.Length; i++)
            {
                partitioner.Add(objs[i]);
            }

            partitioner.Update();

            TestObject[] results = partitioner.GetObjectsInRect(new FloatRect(-1, -1, 0, 0));
            Assert.IsTrue(HaveSameElements(results, new TestObject[0]));

            results = partitioner.GetObjectsInRect(new FloatRect(8, -1, 7, 7));
            Assert.IsTrue(HaveSameElements(results, new[] { objs[1] }));

            results = partitioner.GetObjectsInRect(new FloatRect(-1, -1, 15, 20));
            Assert.IsTrue(HaveSameElements(results, new[] { objs[0], objs[1], objs[2] }));

            results = partitioner.GetObjectsInRect(new FloatRect(-1, -1, 60, 60));
            Assert.IsTrue(HaveSameElements(results, new[] { objs[0], objs[1], objs[2], objs[3] }));

            List<TestObject> resultsList = new List<TestObject>();
            partitioner.GetObjectsInRect(new FloatRect(-1, -1, 0, 0), resultsList);
            Assert.IsTrue(HaveSameElements(resultsList.ToArray(), new TestObject[0]));

            resultsList.Clear();
            partitioner.GetObjectsInRect(new FloatRect(8, -1, 7, 7), resultsList);
            Assert.IsTrue(HaveSameElements(resultsList.ToArray(), new[] { objs[1] }));

            resultsList.Clear();
            partitioner.GetObjectsInRect(new FloatRect(-1, -1, 15, 20), resultsList);
            Assert.IsTrue(HaveSameElements(resultsList.ToArray(), new[] { objs[0], objs[1], objs[2] }));

            resultsList.Clear();
            partitioner.GetObjectsInRect(new FloatRect(-1, -1, 60, 60), resultsList);
            Assert.IsTrue(HaveSameElements(resultsList.ToArray(), new[] { objs[0], objs[1], objs[2], objs[3] }));
        }

        public static void GetClosestObjectTest(ISpacePartitioner<TestObject> partitioner)
        {
            TestObject[] objs = {
                new TestObject(0,0),
                new TestObject(10,0),
                new TestObject(7,7),
                new TestObject(50,50)
            };

            for (int i = 0; i < objs.Length; i++)
            {
                partitioner.Add(objs[i]);
            }

            partitioner.Update();

            Assert.AreEqual(partitioner.GetClosestObject(new Vector2f(0, 0)), objs[0]);
            Assert.AreEqual(partitioner.GetClosestObject(new Vector2f(5, 5)), objs[2]);
            Assert.AreEqual(partitioner.GetClosestObject(new Vector2f(5, 5)), objs[2]);
            Assert.AreEqual(partitioner.GetClosestObject(new Vector2f(5, 5), 1), null);
            Assert.AreEqual(partitioner.GetClosestObject(new Vector2f(-1, -1)), objs[0]);
        }

        private static bool HaveSameElements<T>(T[] a, T[] b)
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
