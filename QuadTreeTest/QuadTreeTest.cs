using System;
using System.Collections.Generic;
using System.Linq;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using SFQuadTree;

namespace QuadTreeTest
{
    internal enum TestType
    {
        Graphical,
        Console
    }

    public class QuadTreeTest : Drawable
    {
        private static readonly Random Random = new Random();
        private readonly QuadTree m_Tree;
        private readonly Dictionary<CircleShape, Vector2f> m_TestObjects = new Dictionary<CircleShape, Vector2f>(); 

        private float m_TestRefreshRate = 2f;
        private float m_TestRefreshCounter;
        private readonly CircleShape m_MainTestObject;
        private readonly CircleShape m_PuppetTestObject;

        private FloatRect m_Bounds;
        private float m_SpeedMultiplier = 200f;
        private int m_NumCircles = 500;

        private bool m_ShowCircles = true;

        private float m_QueeryRange = 300f;

        private TestType TestType = TestType.Graphical;

        public QuadTreeTest()
        {
            m_Bounds = new FloatRect(new Vector2f(0, 0), (Vector2f) Game.Window.Size);

            for (var i = 0; i < m_NumCircles; i++)
            {
                var pos = GetRandomPos();
                m_TestObjects.Add(new CircleShape(2f)
                {
                    FillColor = Color.Blue,
                    Position = pos
                }, RandomVelocity());
            }

            m_MainTestObject = m_TestObjects.First().Key;
            m_MainTestObject.Position = m_Bounds.Center();

            m_PuppetTestObject = m_TestObjects.Last().Key;

            m_Tree = new QuadTree(m_Bounds, m_TestObjects.Select(kvp => kvp.Key).Cast<Transformable>().ToList());
            QuadTree.OnCheckAgainstObject = OnVisit;

            Game.Window.KeyPressed += (sender, args) =>
            {
                if (args.Code == Keyboard.Key.Space)
                    m_ShowCircles = !m_ShowCircles;
            };
            Game.Window.MouseWheelMoved += (sender, args) =>
            {
                m_NumCircles += args.Delta * 3;
                if (m_NumCircles < 10)
                    m_NumCircles = 10;
                Console.WriteLine(m_NumCircles + " " + m_TestObjects.Count);
            };
        }

        public void Update(float dt)
        {
            if (TestType == TestType.Graphical)
                GraphicalTestUpdate(dt);
            else
                ConsoleTestUpdate(dt);
        }

        private void GraphicalTestUpdate(float dt)
        {
            m_Tree.Update();

            m_TestRefreshCounter += dt;
            if (m_TestRefreshCounter > m_TestRefreshRate)
            {
                m_TestRefreshCounter = 0f;
                //RefreshTest();
            }

            var mousePos = GetMousePos();
            foreach (var testObject in m_TestObjects)
            {
                var toMouse = (mousePos - testObject.Key.Position).Normalized();
                testObject.Key.Position += (testObject.Value + toMouse) * dt * m_SpeedMultiplier;
                testObject.Key.FillColor = Color.Blue;
                WrapPosition(testObject.Key);
            }

            //m_MainTestObject.Position = m_Bounds.Center();
            //m_PuppetTestObject.Position = new Vector2f(Mouse.GetPosition(Game.Window).X, Mouse.GetPosition(Game.Window).Y);

            //var kClosest = m_Tree.GetObjectsInRect(new FloatRect(mousePos.X - 75f, mousePos.Y - 75f, 150f, 150f));
            var kClosest = m_Tree.GetKClosestObjects(GetMousePos(), 30, m_QueeryRange);
            foreach (var circle in kClosest.Cast<CircleShape>())
            {
                circle.FillColor = Color.Green;
            }
            if (kClosest.Length > 1)
                ((CircleShape) kClosest[0]).FillColor = Color.Red;


            while (m_NumCircles > m_TestObjects.Count)
            {
                var obj = new CircleShape(2f)
                {
                    FillColor = Color.Blue,
                    Position = GetRandomPos()
                };
                m_TestObjects.Add(obj, RandomVelocity());
                m_Tree.Add(obj);
            }
            while (m_NumCircles < m_TestObjects.Count)
            {
                var obj = m_TestObjects.Keys.First();
                m_TestObjects.Remove(obj);
                m_Tree.Remove(obj);
            }
            /*var allClosest = m_Tree.GetObjectsInRange(m_MainTestObject.Position, m_QueeryRange);
            foreach (var circle in allClosest.Cast<CircleShape>())
            {
                circle.FillColor = Color.Green;
            }*/

            //var closest = m_Tree.GetClosestObject(m_MainTestObject.Position);
            //((CircleShape)closest).FillColor = Color.Cyan;
        }

        private void ConsoleTestUpdate(float dt)
        {
            foreach (var testObject in m_TestObjects)
            {
                testObject.Key.Position += testObject.Value * dt * m_SpeedMultiplier;
                testObject.Key.FillColor = Color.Blue;
                WrapPosition(testObject.Key);
            }
            //QuadTree.CheckCount = 0;
            //QuadTree.ExtraOpCount = 0;
            m_Tree.Update();
            Clock.BenchmarkTime(() => 
            {
                var closest = m_Tree.GetKClosestObjects(m_MainTestObject.Position, 30, 300f);
            }, 10000);
            //Console.WriteLine("End Quad Tree test, checked: " + QuadTree.CheckCount);
            //Console.WriteLine("ExtraOp Count: " + QuadTree.ExtraOpCount);
            Console.WriteLine("Watch Time: " + Extensions.Watch.ElapsedMilliseconds);
            Extensions.Watch.Reset();
            var simpleCount = 0;
            Clock.BenchmarkTime(() =>
            {
                var k = 30;
                var results = new SortedList<float, CircleShape>();
                var rangeSquared = 300f * 300f;
                foreach (var testObject in m_TestObjects)
                {
                    if (testObject.Key == null)
                        continue;
                    simpleCount++;
                    var ds = (m_MainTestObject.Position - testObject.Key.Position).SquaredLength();
                    if (ds > rangeSquared)
                        continue;
                    //If results list has empty elements
                    if (results.Count < k)
                    {
                        while (results.ContainsKey(ds))
                        {
                            //Break any ties
                            ds += .01f;
                        }
                        results.Add(ds, testObject.Key);
                        continue;
                    }

                    if (ds < results.Keys[results.Count - 1])
                    {
                        while (results.ContainsKey(ds))
                        {
                            //Break any ties
                            ds += .01f;
                        }
                        results.RemoveAt(results.Count - 1);
                        results.Add(ds, testObject.Key);
                        rangeSquared = ds;
                    }
                }

                var shapes = new CircleShape[Math.Min(30, results.Count)];
                for (var i = 0; i < shapes.Length; i++)
                {
                    shapes[i] = results.Values[i];
                }
            }, 10000);

            
            Console.WriteLine("End Simple Test, checked: " + simpleCount);
        }

        public void Draw(RenderTarget target, RenderStates states)
        {
            DrawAllRegions(target, states);

            if (m_ShowCircles)
            {
                foreach (var circleShape in m_TestObjects)
                {
                    target.Draw(circleShape.Key, states);
                }
            }
            

            /*var range = new CircleShape(m_QueeryRange)
            {
                FillColor = Color.Transparent,
                OutlineColor = Color.Red,
                OutlineThickness = 2,
                Position = m_MainTestObject.Position,
                Origin = new Vector2f(m_QueeryRange, m_QueeryRange)
            };
            target.Draw(range, states);*/
        }

        private void DrawAllRegions(RenderTarget target, RenderStates states)
        {
            var regions = new List<FloatRect>();
            m_Tree.GetAllRegions(regions);

            var c = Color.Red;
            var shape = new RectangleShape
            {
                FillColor = Color.Transparent,
                OutlineThickness = 1f,
                OutlineColor = c
            };

            foreach (var fr in regions)
            {
                shape.OutlineColor = new Color(c.R, (byte) (c.G + (255 * 1 / (fr.Width / m_Bounds.Width))), c.B, 100);
                shape.Size = fr.Dimensions();
                shape.Position = fr.Min();
                target.Draw(shape, states);
            }
        }

        private void RefreshTest()
        {
            foreach (var testObject in m_TestObjects)
            {
                var pos = GetRandomPos();
                testObject.Key.Position = pos;
            }
            m_MainTestObject.Position = m_Bounds.Center();
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

        private float SquaredLength(Vector2f v)
        {
            return (v.X * v.X) + (v.Y * v.Y);
        }

        private void OnVisit(Transformable t)
        {
            ((CircleShape)t).FillColor = Color.Yellow;
        }

        private static Vector2f RandomVelocity()
        {
            return new Vector2f((float) (Random.NextDouble() - Random.NextDouble()),
                (float) (Random.NextDouble() - Random.NextDouble()));
        }

        private static Vector2f GetMousePos()
        {
            return new Vector2f(Mouse.GetPosition(Game.Window).X, Mouse.GetPosition(Game.Window).Y);
        }
    }
}
