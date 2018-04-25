using SFML.Graphics;
using SFML.System;

namespace QuadTreeTest
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
}