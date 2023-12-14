using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Things;
using System;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjZZZ : GameObject
    {
        private readonly CSprite _sprite;
        private readonly Vector2 _direction;
        private readonly Vector2 _dirOrthogonal;
        private float _moveCounter;
        private float _transparency;

        public ObjZZZ(Map.Map map, Vector2 position, Vector2 direction) : base(map)
        {
            EntityPosition = new CPosition(position.X, position.Y, 0);
            EntitySize = new Rectangle(-8, -8, 16, 16);

            _sprite = new CSprite("tarin_zzz", EntityPosition);

            _direction = direction;
            _dirOrthogonal = new Vector2(_direction.Y, -_direction.X);

            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(_sprite, Values.LayerTop));
        }

        private void Update()
        {
            // fade in/out
            var target = _moveCounter < 750 ? 1 : 0;
            _transparency = AnimationHelper.MoveToTarget(_transparency, target, 0.25f * Game1.TimeMultiplier);
            _sprite.Color = Color.White * _transparency;

            // move
            _moveCounter += Game1.DeltaTime;
            EntityPosition.Set(EntityPosition.Position + _direction * 0.175f * Game1.TimeMultiplier + _dirOrthogonal * Game1.TimeMultiplier * 0.125f * MathF.Sin(_moveCounter / 100));

            // despawn
            if (_moveCounter > 850)
                Map.Objects.DeleteObjects.Add(this);
        }
    }
}