using System;
using Microsoft.Xna.Framework;

namespace ProjectZ.InGame.Things
{
    class CubicBezier
    {
        private Vector2 _firstPoint;
        private Vector2 _secondPoint;

        private readonly int _dataCount;
        public float[] Data;

        public CubicBezier(int dataCount, Vector2 firstPoint, Vector2 secondPoint)
        {
            _dataCount = dataCount;
            Data = new float[_dataCount];

            SetData(firstPoint, secondPoint);
        }

        public void SetData(Vector2 firstPoint, Vector2 secondPoint)
        {
            _firstPoint = firstPoint;
            _secondPoint = secondPoint;
            FillData();
        }

        private void FillData()
        {
            var lastValue = EvaluatePosition(0);
            var dataSize = 1 / (float)(Data.Length - 1);
            var stepSize = 1 / (float)_dataCount / 2;
            var position = stepSize;
            var dataIndex = 0;

            for (var i = 0; i < Data.Length; i++)
                Data[i] = 0;
            Data[_dataCount - 1] = 1;

            while (true)
            {
                var newValue = EvaluatePosition(position);
                position += stepSize;

                while (newValue.X >= dataIndex * dataSize)
                {
                    var distance = newValue.X - lastValue.X;
                    var indexDistance = dataIndex * dataSize - lastValue.X;
                    var percentage = indexDistance / distance;

                    Data[dataIndex] = Vector2.Lerp(lastValue, newValue, percentage).Y;
                    dataIndex++;
                }

                lastValue = newValue;

                if (position >= 1 || dataIndex >= Data.Length - 1)
                    break;
            }
        }

        /// <summary>
        /// Get the interpolated y value from the given x value.
        /// </summary>
        /// <param name="x">The value on the x axis where we want to get the y value.</param>
        /// <returns></returns>
        public float EvaluateX(float x)
        {
            x = MathHelper.Clamp(x, 0, 1);

            // interpolate between two points to get the value at "time"
            var index = (int)(x * (_dataCount - 1));
            var dataSize = 1 / (_dataCount - 1.0f);
            var percentage = (x % dataSize) / dataSize;
            var value = Data[index] * (1 - percentage);
            if (index < _dataCount - 1)
                value += Data[index + 1] * percentage;

            return value;
        }

        public Vector2 EvaluatePosition(float time)
        {
            time = MathHelper.Clamp(time, 0, 1);

            var point = // (float)Math.Pow(1 - time, 3) * Vector2.Zero +
                (float)(3 * Math.Pow(1 - time, 2) * time) * _firstPoint +
                ((3 * (1 - time) * time * time) * _secondPoint) +
                (float)Math.Pow(time, 3) * Vector2.One;

            return point;
        }
    }
}
