using Microsoft.Xna.Framework;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Things;
using System;

namespace ProjectZ.InGame.GameObjects
{
    public partial class ObjLink
    {
        /// <summary>
        /// Check if the box is colliding with a destroyable wall
        /// </summary>
        private bool DestroyableWall(Box box)
        {
            _destroyableWallList.Clear();
            Map.Objects.GetComponentList(_destroyableWallList, (int)box.X, (int)box.Y, (int)box.Width + 1, (int)box.Height + 1, CollisionComponent.Mask);

            var collidingBox = Box.Empty;
            foreach (var gameObject in _destroyableWallList)
            {
                var collisionObject = gameObject.Components[CollisionComponent.Index] as CollisionComponent;
                if ((collisionObject.CollisionType & Values.CollisionTypes.Destroyable) != 0 &&
                    collisionObject.Collision(box, 0, 0, ref collidingBox))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get the direction with a tolerance in the current direction
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        private int ToDirection(Vector2 direction)
        {
            int value;

            // this makes it so that if you start walking diagonal the player won't change the direction
            var dirMultiply = (Direction == 0 || Direction == 2) ? 1.05f : 0.95f;
            if (Math.Abs(direction.X) * dirMultiply > Math.Abs(direction.Y))
                value = direction.X > 0 ? 2 : 0;
            else
                value = direction.Y > 0 ? 3 : 1;

            return value;
        }
    }
}
