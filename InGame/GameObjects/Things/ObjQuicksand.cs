using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;

namespace ProjectZ.InGame.GameObjects.Things
{
    class ObjQuicksand : GameObject
    {
        private readonly List<GameObject> _collidingObjects = new List<GameObject>();

        private readonly RectangleF _collisionBox;
        private readonly Vector2 _direction;
        private readonly int _mode;

        public ObjQuicksand() : base("rollband_0") { }

        public ObjQuicksand(Map.Map map, int posX, int posY, float dirX, float dirY, int mode) : base(map)
        {
            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 16, 16);

            _direction = new Vector2(dirX, dirY);
            // mode 0: quicksand
            // mode 1: only move the raft
            _mode = mode;

            // why was this not the full size?
            //_collisionBox = new Rectangle(posX + 2, posY + 3, 12, 10);
            _collisionBox = new Rectangle(posX, posY, 16, 16);
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
        }

        private void Update()
        {
            // get and move the components colliding with the quicksand
            _collidingObjects.Clear();
            Map.Objects.GetComponentList(_collidingObjects,
                (int)_collisionBox.Left, (int)_collisionBox.Top, (int)_collisionBox.Width, (int)_collisionBox.Height, BodyComponent.Mask);

            foreach (var gameObject in _collidingObjects)
            {
                if (_mode == 1 && !(gameObject is ObjRaft))
                    continue;

                var gameObjectBody = ((BodyComponent)gameObject.Components[BodyComponent.Index]);
                if (gameObjectBody.IsActive && gameObjectBody.IsGrounded &&
                    _collisionBox.Intersects(gameObjectBody.BodyBox.Box.Rectangle()))
                {
                    if (gameObjectBody.AdditionalMovementVT == Vector2.Zero)
                        gameObjectBody.AdditionalMovementVT = gameObjectBody.LastAdditionalMovementVT;

                    var distance = gameObjectBody.BodyBox.Box.Center - _collisionBox.Center;
                    var distanceMult = 2 - Math.Clamp(distance.Length() / 8f, 0, 2);
                    gameObjectBody.AdditionalMovementVT = Vector2.Lerp(gameObjectBody.AdditionalMovementVT, _direction, 0.125f * Game1.TimeMultiplier * distanceMult);
                }
            }
        }
    }
}
