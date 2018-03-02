using System;
using System.Collections.Generic;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using SFQuadTree;

namespace BucketGridDemo
{
    public class BucketGridDemo
    {
        private static readonly Random Random = new Random();

        private readonly Dictionary<CircleShape, Vector2f> m_TestObjects = new Dictionary<CircleShape, Vector2f>();
        private readonly BucketGrid<CircleShape> m_Grid;

        private float m_SpeedMultiplier = 200f;
        private int m_NumCircles = 500;

        private FloatRect m_Bounds;

        public BucketGridDemo()
        {
            m_Bounds = new FloatRect(new Vector2f(0, 0), (Vector2f)Game.Window.Size);

            m_Grid = new BucketGrid<CircleShape>(m_Bounds, 100, 100);

            for (var i = 0; i < m_NumCircles; i++)
            {
                var pos = GetRandomPos();
                var obj = new CircleShape(2f)
                {
                    FillColor = Color.Blue,
                    Position = pos
                };

                m_TestObjects.Add(obj, RandomVelocity());

                m_Grid.Add(obj);
            }

            m_Grid.Update();
        }

        public void Update(float dt)
        {
            var mousePos = GetMousePos();
            foreach (var testObject in m_TestObjects)
            {
                var toMouse = (mousePos - testObject.Key.Position).Normalized();
                testObject.Key.Position += (testObject.Value + toMouse) * dt * m_SpeedMultiplier;
                testObject.Key.FillColor = Color.Blue;
                WrapPosition(testObject.Key);
            }

            m_Grid.Update();

            foreach (var obj in m_Grid.GetKClosestObjects(GetMousePos(), 30))
            {
                obj.FillColor = Color.Red;
            }

            var closest = m_Grid.GetClosestObject(GetMousePos());
            if (closest != null)
                closest.FillColor = Color.Green;
        }

        public void Draw(RenderTarget target, RenderStates states)
        {
            foreach (var obj in m_TestObjects)
            {
                target.Draw(obj.Key, states);
            }
        }

        private Vector2f GetRandomPos()
        {
            return new Vector2f((float)(Random.NextDouble() * m_Bounds.Width),
                (float)(Random.NextDouble() * m_Bounds.Height));
        }

        private void WrapPosition(CircleShape c)
        {
            c.Position = new Vector2f(c.Position.X % m_Bounds.Width, c.Position.Y % m_Bounds.Height);
            if (c.Position.X < 0)
                c.Position = new Vector2f(m_Bounds.Width + c.Position.X, c.Position.Y);
            if (c.Position.Y < 0)
                c.Position = new Vector2f(c.Position.X, m_Bounds.Height + c.Position.Y);
        }

        private static Vector2f RandomVelocity()
        {
            return new Vector2f((float)(Random.NextDouble() - Random.NextDouble()),
                (float)(Random.NextDouble() - Random.NextDouble()));
        }

        private static Vector2f GetMousePos()
        {
            return new Vector2f(Mouse.GetPosition(Game.Window).X, Mouse.GetPosition(Game.Window).Y);
        }
    }
}
