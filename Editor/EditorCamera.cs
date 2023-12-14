using System;
using Microsoft.Xna.Framework;

namespace ProjectZ.Editor
{
    public class EditorCamera
    {
        public Matrix TransformMatrix => Matrix.CreateScale(Scale) *
                                         Matrix.CreateTranslation(new Vector3(Location.X, Location.Y, 0));
        public Point Location = new Point(0, 0);
        public float Scale = 1;

        public float MinScale = 0.25f;
        public float MaxScale = 15.0f;

        public void Zoom(float dir, Point mousePosition)
        {
            var stepSize = Scale / 4 * dir;

            var preScale = Scale;

            if (Scale + stepSize < MinScale || MaxScale < Scale + stepSize)
                Scale = stepSize < 0 ? MinScale : MaxScale;
            else
                Scale += stepSize;
            
            Scale = (int)Math.Round(Scale * 100) / 100f;

            var scale = Scale / preScale;
            Location.X = mousePosition.X - (int)((mousePosition.X - Location.X) * scale);
            Location.Y = mousePosition.Y - (int)((mousePosition.Y - Location.Y) * scale);
        }
    }
}
