using System;
using Microsoft.Xna.Framework;

namespace ProjectZ.Base
{
    public struct RectangleF
    {
        public static readonly RectangleF Empty = new RectangleF();

        public float X;

        public float Y;

        public float Width;

        public float Height;

        public float Left => X;

        public float Right => X + Width;

        public float Top => Y;

        public float Bottom => Y + Height;

        public Vector2 Center => new Vector2(X + Width / 2, Y + Height / 2);

        public RectangleF(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public bool Intersects(RectangleF second)
        {
            return second.Left < Right && Left < second.Right &&
                   second.Top < Bottom && Top < second.Bottom;
        }

        public bool Contains(RectangleF second)
        {
            return Left <= second.Left && second.Right <= Right &&
                   Top <= second.Top && second.Bottom <= Bottom;
        }

        public bool Contains(Vector2 position)
        {
            return Left <= position.X && position.X <= Right &&
                   Top <= position.Y && position.Y <= Bottom;
        }

        public RectangleF GetIntersection(RectangleF second)
        {
            var left = Math.Max(Left, second.Left);
            var right = Math.Min(Right, second.Right);
            var top = Math.Max(Top, second.Top);
            var down = Math.Min(Bottom, second.Bottom);

            return new RectangleF(left, top, right - left, down - top);
        }

        public static implicit operator RectangleF(Rectangle rectangle)
        {
            return new RectangleF(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
        }
    }
}
