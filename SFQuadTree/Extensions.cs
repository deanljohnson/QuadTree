using System;
using System.Diagnostics;
using SFML.Graphics;
using SFML.System;

namespace SFQuadTree
{
    public static class Extensions
    {
        public static Vector2f Normalized(this Vector2f v)
        {
            float l = v.Length();
            if (l == 0f)
                return new Vector2f(0,0);
            return v / l;
        }

        public static float Length(this Vector2f v)
        {
            return (float) Math.Sqrt((v.X * v.X) + (v.Y * v.Y));
        }

        public static float SquaredLength(this Vector2f v)
        {
            return (v.X * v.X) + (v.Y * v.Y);
        }

        public static float Dot(this Vector2f a, Vector2f b)
        {
            return (a.X * b.X) + (a.Y * b.Y);
        }

        public static float SquaredDistance(this FloatRect f, Vector2f v)
        {
            //var min = f.Min();
            var max = f.Max();

            var dx = Math.Max(f.Left - v.X, Math.Max(0f, v.X - max.X));
            var dy = Math.Max(f.Top - v.Y, Math.Max(0f, v.Y - max.Y));

            /*var dx = (v.X > max.X)
                ? max.X
                : v.X < min.X
                    ? min.X
                    : v.X;

            var dy = (v.Y > max.Y)
                ? max.Y
                : v.Y < min.Y
                    ? min.Y
                    : v.Y;

            dx -= v.X;
            dy -= v.Y;*/
            
            return (dx * dx) + (dy * dy);
            /*var dx = v.X - Math.Max(Math.Min(v.X, f.Left + f.Width), f.Left);
            var dy = v.Y - Math.Max(Math.Min(v.Y, f.Top + f.Height), f.Top);
            return (dx * dx) + (dy * dy);*/
        }

        public static Vector2f Max(this FloatRect f)
        {
            return new Vector2f(f.Left + f.Width, f.Top + f.Height);
        }

        public static Vector2f Min(this FloatRect f)
        {
            return new Vector2f(f.Left, f.Top);
        }

        public static Vector2f Dimensions(this FloatRect f)
        {
            return new Vector2f(f.Width, f.Height);
        }

        public static Vector2f Center(this FloatRect f)
        {
            return new Vector2f(f.Left + (f.Width / 2f), f.Top + (f.Height / 2f));
        }
    }
}
