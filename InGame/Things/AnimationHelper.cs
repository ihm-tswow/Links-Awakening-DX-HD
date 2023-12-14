using System;
using Microsoft.Xna.Framework;

namespace ProjectZ.InGame.Things
{
    class AnimationHelper
    {
        public static Vector2[] DirectionOffset =
        {
            new Vector2(-1, 0), new Vector2(0, -1), new Vector2(1, 0), new Vector2(0, 1)
        };

        /// <summary>
        /// Offsets the direction and make sure if stays between 0 and 3
        /// </summary>
        /// <param name="direction">Init direction Value before the offset gets added</param>
        /// <param name="offset">Values between -3 and 3</param>
        /// <returns>Returns the direction value with the offset added and looped to be between 0 and 3</returns>
        public static int OffsetDirection(int direction, int offset)
        {
            direction += offset;

            if (direction >= 4)
                direction %= 4;
            if (direction < 0)
                direction += 4;

            return direction;
        }

        public static int GetDirection(Vector2 direction, float rotationOffset = MathF.PI * 1.25f)
        {
            var degree = MathHelper.ToDegrees((float)(Math.Atan2(direction.Y, direction.X) + rotationOffset));

            while (degree >= 360)
                degree -= 360;

            return (int)(degree / 90);
        }

        public static Vector2 RotateVector(Vector2 input, float angle)
        {
            return new Vector2(
                (float)(Math.Cos(angle) * input.X - Math.Sin(angle) * input.Y),
                (float)(Math.Sin(angle) * input.X + Math.Cos(angle) * input.Y));
        }

        public static float MoveToTarget(float currentValue, float targetValue, float maxAmount)
        {
            if (Math.Abs(currentValue - targetValue) < maxAmount)
                return targetValue;

            if (currentValue < targetValue)
                currentValue += maxAmount;
            if (currentValue > targetValue)
                currentValue -= maxAmount;

            return currentValue;
        }

        public static Vector2 MoveToTarget(Vector2 currentVelocity, Vector2 targetVelocity, float maxAmount)
        {
            var direction = targetVelocity - currentVelocity;
            if (direction.Length() <= maxAmount)
                return targetVelocity;

            direction.Normalize();
            var newVelocity = currentVelocity + direction * maxAmount;
            return newVelocity;
        }
    }
}
