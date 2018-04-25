using Microsoft.VisualStudio.TestTools.UnitTesting;
using QuadTree;
using SFML.Graphics;

namespace QuadTreeTest
{
    [TestClass]
    public class BucketGridTests
    {
        private readonly FloatRect m_Bounds = new FloatRect(0, 0, 1000, 1000);

        [TestMethod]
        public void AddRemoveTest()
        {
            BucketGrid<TestObject> tree = new BucketGrid<TestObject>(m_Bounds, 10, 10);
            SpacePartitionerTests.AddRemoveTest(tree);
        }

        [TestMethod]
        public void GetKClosestObjectsTest()
        {
            BucketGrid<TestObject> tree = new BucketGrid<TestObject>(m_Bounds, 10, 10);
            SpacePartitionerTests.GetKClosestObjectsTest(tree);
        }

        [TestMethod]
        public void GetObjectsInRangeTest()
        {
            BucketGrid<TestObject> tree = new BucketGrid<TestObject>(m_Bounds, 10, 10);
            SpacePartitionerTests.GetObjectsInRangeTest(tree);
        }

        [TestMethod]
        public void GetObjectsInRectTest()
        {
            BucketGrid<TestObject> tree = new BucketGrid<TestObject>(m_Bounds, 10, 10);
            SpacePartitionerTests.GetObjectsInRectTest(tree);
        }

        [TestMethod]
        public void GetClosestObjectTest()
        {
            BucketGrid<TestObject> tree = new BucketGrid<TestObject>(m_Bounds, 10, 10);
            SpacePartitionerTests.GetClosestObjectTest(tree);
        }
    }
}
