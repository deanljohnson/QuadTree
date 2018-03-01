using SFML.Graphics;
using SFML.System;

namespace QuadTreeBenchmark
{
    public class TestObject : Transformable
    {
        public TestObject(Vector2f pos)
        {
            Position = pos;
        }
    }
}
