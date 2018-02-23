using System;
using System.Diagnostics;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace QuadTreeTest
{
    public static class Game
    {
        private static readonly Vector2u DefaultWindowSize = new Vector2u(900, 900);
        private const uint DisplayRate = 60;
        private const String GameName = "My Game";
        private static Stopwatch Timer { get; } = new Stopwatch();
        private static long LastTime { get; set; }

        public static RenderWindow Window { get; private set; }
        public static QuadTreeTest Test { get; set; }

        public static void Main()
        {
            InitializeWindow();
            Test = new QuadTreeTest();
            Start();
        }

        private static void Start()
        {
            Timer.Start();

            while (Window.IsOpen)
            {
                Window.DispatchEvents();

                Update(GetDeltaTime());

                Window.Clear(Color.Black);

                Render();

                Window.Display();
            }
        }

        private static void Update(float dt)
        {
            Test.Update(dt);
        }

        private static void Render()
        {
            Test.Draw(Window, RenderStates.Default);
        }

        private static float GetDeltaTime()
        {
            var elapsedMs = Timer.ElapsedMilliseconds - LastTime;
            LastTime = Timer.ElapsedMilliseconds;
            return (elapsedMs / 1000f);
        }

        private static void InitializeWindow()
        {
            Window = new RenderWindow(new VideoMode(DefaultWindowSize.X, DefaultWindowSize.Y, 32), GameName, Styles.Default);
            //Window.SetFramerateLimit(DisplayRate);

            Window.Closed += OnWindowClose;
        }

        private static void OnWindowClose(object sender, EventArgs e)
        {
            Window.Close();
        }
    }
}
