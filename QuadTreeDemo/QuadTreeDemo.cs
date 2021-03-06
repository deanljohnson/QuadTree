﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Priority_Queue;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using QuadTree;
using Clock = QuadTreeTest.Clock;

namespace QuadTreeDemo
{
    internal enum TestType
    {
        Graphical,
        Console,
        ConsoleThreaded
    }

    public class QuadTreeDemo : Drawable
    {
        private static readonly Random Random = new Random();
        private readonly QuadTree<Transformable> m_Tree;
        private readonly Dictionary<CircleShape, Vector2f> m_TestObjects = new Dictionary<CircleShape, Vector2f>(); 

        private float m_TestRefreshRate = 2f;
        private float m_TestRefreshCounter;
        private readonly CircleShape m_MainTestObject;

        private FloatRect m_Bounds;
        private float m_SpeedMultiplier = 200f;
        private int m_NumCircles = 500;

        private bool m_ShowCircles = true;

        private float m_QueeryRange = 300f;

        private TestType TestType = TestType.Graphical;

        public QuadTreeDemo()
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

            m_Tree = new QuadTree<Transformable>(m_Bounds);
            foreach (var obj in m_TestObjects.Select(kvp => kvp.Key).Cast<Transformable>().ToList())
            {
                m_Tree.Add(obj);
            }

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
            switch (TestType)
            {
                case TestType.Graphical:
                    GraphicalTestUpdate(dt);
                    break;
                case TestType.Console:
                    ConsoleTestUpdate(dt);
                    break;
                case TestType.ConsoleThreaded:
                    ConsoleThreadedTest(dt);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
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

            for (int i = 0; i < 10; i++)
            {
                var removedObj = m_TestObjects.Keys.ToArray()[Random.Next(0, m_TestObjects.Count)];
                //m_TestObjects.Remove(removedObj);
                //m_Tree.Remove(removedObj);
            }
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
            /*Clock.BenchmarkTime(() => 
            {
                var closest = m_Tree.GetKClosestObjects(m_MainTestObject.Position, 30, 300f);
            }, 10000);*/
            //Console.WriteLine("End Quad Tree test, checked: " + QuadTree.CheckCount);
            //Console.WriteLine("ExtraOp Count: " + QuadTree.ExtraOpCount);
            Clock.BenchmarkTime(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    foreach (var testObject in m_TestObjects)
                    {
                        testObject.Key.Position = GetRandomPos();
                    }
                    m_Tree.Update();
                }
            }, 100);
            /*Clock.BenchmarkTime(() =>
            {
                m_Tree.GetKClosestObjects(GetRandomPos(), (uint) Random.Next(5, 20), (float)(100 + Random.NextDouble() * 400), new PriorityQueue<Transformable>(true));
                m_Tree.GetObjectsInRange(GetRandomPos(), (float)(100 + Random.NextDouble() * 400), new List<Transformable>());
                m_Tree.GetObjectsInRect(new FloatRect(GetRandomPos(), GetRandomPos()), new List<Transformable>());
            }, 10000);*/

            Console.WriteLine("End Simple Test");
        }

        /// <summary>
        /// The purpose of this test is to determine whether or 
        /// not multi-threaded execution has race-donditions and deadlocks
        /// by executing MANY thread operations at once
        /// </summary>
        /// <param name="dt"></param>
        public void ConsoleThreadedTest(float dt)
        {
            // Move all objects according to their velocities
            foreach (var testObject in m_TestObjects)
            {
                testObject.Key.Position += testObject.Value * dt * m_SpeedMultiplier;
                testObject.Key.FillColor = Color.Blue;
                WrapPosition(testObject.Key);
            }

            while (true)
            {
                m_Tree.Update();

                // Create threads for various queeries
                List<Thread> threads = new List<Thread>();

                ManualResetEvent start = new ManualResetEvent(false);

                threads.Add(new Thread(() => { start.WaitOne(); m_Tree.GetClosestObject(GetRandomPos(), (float)(100 + Random.NextDouble() * 400)); }));
                threads.Add(new Thread(() => { start.WaitOne(); m_Tree.GetClosestObject(GetRandomPos(), (float)(100 + Random.NextDouble() * 400)); }));
                threads.Add(new Thread(() => { start.WaitOne(); m_Tree.GetClosestObject(GetRandomPos(), (float)(100 + Random.NextDouble() * 400)); }));
                threads.Add(new Thread(() => { start.WaitOne(); m_Tree.GetClosestObject(GetRandomPos(), (float)(100 + Random.NextDouble() * 400)); }));
                threads.Add(new Thread(() => { start.WaitOne(); m_Tree.GetClosestObject(GetRandomPos(), (float)(100 + Random.NextDouble() * 400)); }));

                threads.Add(new Thread(() => { start.WaitOne(); m_Tree.GetKClosestObjects(GetRandomPos(), (uint)Random.Next(5, 20), (float)(100 + Random.NextDouble() * 400), new FastPriorityQueue<ItemNode<Transformable>>(20)); }));
                threads.Add(new Thread(() => { start.WaitOne(); m_Tree.GetKClosestObjects(GetRandomPos(), (uint)Random.Next(5, 20), (float)(100 + Random.NextDouble() * 400), new FastPriorityQueue<ItemNode<Transformable>>(20)); }));
                threads.Add(new Thread(() => { start.WaitOne(); m_Tree.GetKClosestObjects(GetRandomPos(), (uint)Random.Next(5, 20), (float)(100 + Random.NextDouble() * 400), new FastPriorityQueue<ItemNode<Transformable>>(20)); }));
                threads.Add(new Thread(() => { start.WaitOne(); m_Tree.GetKClosestObjects(GetRandomPos(), (uint)Random.Next(5, 20), (float)(100 + Random.NextDouble() * 400), new FastPriorityQueue<ItemNode<Transformable>>(20)); }));
                threads.Add(new Thread(() => { start.WaitOne(); m_Tree.GetKClosestObjects(GetRandomPos(), (uint)Random.Next(5, 20), (float)(100 + Random.NextDouble() * 400), new FastPriorityQueue<ItemNode<Transformable>>(20)); }));

                threads.Add(new Thread(() => { start.WaitOne(); m_Tree.GetObjectsInRange(GetRandomPos(), (float)(100 + Random.NextDouble() * 400), new List<Transformable>()); }));
                threads.Add(new Thread(() => { start.WaitOne(); m_Tree.GetObjectsInRange(GetRandomPos(), (float)(100 + Random.NextDouble() * 400), new List<Transformable>()); }));
                threads.Add(new Thread(() => { start.WaitOne(); m_Tree.GetObjectsInRange(GetRandomPos(), (float)(100 + Random.NextDouble() * 400), new List<Transformable>()); }));
                threads.Add(new Thread(() => { start.WaitOne(); m_Tree.GetObjectsInRange(GetRandomPos(), (float)(100 + Random.NextDouble() * 400), new List<Transformable>()); }));
                threads.Add(new Thread(() => { start.WaitOne(); m_Tree.GetObjectsInRange(GetRandomPos(), (float)(100 + Random.NextDouble() * 400), new List<Transformable>()); }));

                threads.Add(new Thread(() => { start.WaitOne(); m_Tree.GetObjectsInRect(new FloatRect(GetRandomPos(), GetRandomPos()), new List<Transformable>()); }));
                threads.Add(new Thread(() => { start.WaitOne(); m_Tree.GetObjectsInRect(new FloatRect(GetRandomPos(), GetRandomPos()), new List<Transformable>()); }));
                threads.Add(new Thread(() => { start.WaitOne(); m_Tree.GetObjectsInRect(new FloatRect(GetRandomPos(), GetRandomPos()), new List<Transformable>()); }));
                threads.Add(new Thread(() => { start.WaitOne(); m_Tree.GetObjectsInRect(new FloatRect(GetRandomPos(), GetRandomPos()), new List<Transformable>()); }));
                threads.Add(new Thread(() => { start.WaitOne(); m_Tree.GetObjectsInRect(new FloatRect(GetRandomPos(), GetRandomPos()), new List<Transformable>()); }));

                for (int i = 0; i < threads.Count; i++)
                {
                    threads[i].Start();
                }

                start.Set();

                for (int i = 0; i < threads.Count; i++)
                {
                    threads[i].Join();
                }

                start.Reset();

                Console.WriteLine($"Executed parallel update with dt {dt}");
            }
        }

        public void Draw(RenderTarget target, RenderStates states)
        {
            QuadTreeVisualization.DrawRegions(m_Tree, target, states, new Color(255,0,0,100));

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
