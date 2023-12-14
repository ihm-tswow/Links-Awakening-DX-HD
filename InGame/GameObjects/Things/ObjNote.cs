using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Things;
using System;

namespace ProjectZ.InGame.GameObjects.Things
{
    public class ObjNote : GameObject
    {
        private readonly CSprite _sprite;
        private readonly Vector3 _startPosition;
        private readonly Vector3 _endPosition;
        private readonly Vector2 _moveDir;

        private float _counter;
        private const int LiveTime = 1000;

        public ObjNote(Map.Map map, Vector2 position, int direction) : base(map)
        {
            EntityPosition = new CPosition(position.X, position.Y, 8);
            EntitySize = new Rectangle(-4, -32, 8, 32);

            _startPosition = new Vector3(EntityPosition.X, EntityPosition.Y, EntityPosition.Z);
            _endPosition = _startPosition + new Vector3(15 * direction, 0, 17);

            _moveDir = new Vector2(_endPosition.X, _endPosition.Y + _endPosition.Z) -
                       new Vector2(_startPosition.X, _startPosition.Y + _startPosition.Z);
            _moveDir.Normalize();

            _sprite = new CSprite("note", EntityPosition, Vector2.Zero);
            _sprite.Color = Color.Transparent;

            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(_sprite, Values.LayerPlayer));
        }

        private void Update()
        {
            _counter += Game1.DeltaTime;

            // fade in/out
            var transparency = 1.0f;
            if (_counter > LiveTime - 100)
                transparency = (LiveTime - _counter) / 100f;
            else if (_counter < 100)
                transparency = _counter / 100;
            _sprite.Color = Color.White * transparency;

            // update the position
            var percentage = _counter / LiveTime;
            var newPosition = Vector3.Lerp(_startPosition, _endPosition, percentage);
            EntityPosition.Set(newPosition);

            // offset to the sides
            _sprite.DrawOffset = new Vector2(-3, -12) + _moveDir * (float)MathF.Sin(_counter * 0.015f) * 1.25f;

            // despawn
            if (_counter > LiveTime)
                Map.Objects.DeleteObjects.Add(this);
        }
    }
}