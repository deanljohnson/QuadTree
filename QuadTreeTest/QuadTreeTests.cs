using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
        public void BulkCreationTest()
        {
            QuadTree<TestObject> individualTree = new QuadTree<TestObject>(m_Bounds);
            QuadTree<TestObject> bulkTree = new QuadTree<TestObject>(m_Bounds);

            Random random = new Random(0);
            List<TestObject> objects = new List<TestObject>();
            for (int i = 0; i < 500; i++)
            {
                objects.Add(new TestObject(
                    new Vector2f((float) random.NextDouble(), (float) random.NextDouble()) * 1000));
                individualTree.Add(objects[objects.Count - 1]);
                bulkTree.Add(objects[objects.Count - 1]);
                individualTree.Update();
            }

            bulkTree.Update();

            List<FloatRect> regions1 = new List<FloatRect>();
            List<FloatRect> regions2 = new List<FloatRect>();

            individualTree.GetAllRegions(regions1);
            bulkTree.GetAllRegions(regions2);

            HashSet<FloatRect> remaining = new HashSet<FloatRect>(regions1);
            remaining.ExceptWith(regions2);

            Assert.AreEqual(0, remaining.Count);
        }

        [TestMethod]
        public void AddRemoveTest()
        {
            QuadTree<TestObject> tree = new QuadTree<TestObject>(m_Bounds);
            SpacePartitionerTests.AddRemoveTest(tree);
        }

        [TestMethod]
        public void GetKClosestObjectsTest()
        {
            QuadTree<TestObject> tree = new QuadTree<TestObject>(m_Bounds);
            SpacePartitionerTests.GetKClosestObjectsTest(tree);
        }

        [TestMethod]
        public void GetObjectsInRangeTest()
        {
            QuadTree<TestObject> tree = new QuadTree<TestObject>(m_Bounds);
            SpacePartitionerTests.GetObjectsInRangeTest(tree);
        }

        [TestMethod]
        public void GetObjectsInRectTest()
        {
            QuadTree<TestObject> tree = new QuadTree<TestObject>(m_Bounds);
            SpacePartitionerTests.GetObjectsInRectTest(tree);
        }

        [TestMethod]
        public void GetClosestObjectTest()
        {
            QuadTree<TestObject> tree = new QuadTree<TestObject>(m_Bounds);
            SpacePartitionerTests.GetClosestObjectTest(tree);
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
