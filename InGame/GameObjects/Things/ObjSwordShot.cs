using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    class ObjSwordShot : GameObject
    {
        private readonly CSprite _sprite;
        private readonly BodyComponent _body;
        private readonly CBox _damageBox;

        private readonly Vector2 _spawnPosition;

        private float _spawnCounter;
        private const int SpawnTime = 10;
        // ~ time to move from the left side of a room to the right
        private const int DespawnTime = 500;
        private const int FadeInTime = 15;
        private const int FadeOutTime = 25;

        private const float MoveSpeed = 4;

        public ObjSwordShot(Map.Map map, Vector3 position, int direction) : base(map)
        {
            EntityPosition = new CPosition(position);
            EntitySize = new Rectangle(-8, -8, 16, 16);

            _spawnPosition = new Vector2(position.X, position.Y);
            _damageBox = new CBox(EntityPosition, -3, -3, 0, 6, 6, 8);

            _sprite = new CSprite("swordShot", EntityPosition)
            {
                Color = Color.Transparent,
                Rotation = (direction - 1) * MathF.PI * 0.5f,
                DrawOffset = -AnimationHelper.DirectionOffset[direction] * 4
            };

            // offset the body to not collide with the wall if the player is standing next to one
            var directionOffset = AnimationHelper.DirectionOffset[(direction + 1) % 4];
            _body = new BodyComponent(EntityPosition, -1 + (int)directionOffset.X * 2, -1 + (int)directionOffset.Y * 2, 2, 2, 8)
            {
                VelocityTarget = AnimationHelper.DirectionOffset[direction] * MoveSpeed,
                MoveCollision = OnCollision,
                IgnoreHoles = true,
                IgnoresZ = true,
                IgnoreInsideCollision = false,
                Level = MapStates.GetLevel(MapManager.ObjLink._body.CurrentFieldState)
            };

            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerPlayer, EntityPosition));
        }

        public override void Init()
        {
            Game1.GameManager.PlaySoundEffect("D360-59-3B");
        }

        public void Update()
        {
            _spawnCounter += Game1.DeltaTime;

            // only start showing the sprite after the spawn time
            if (_spawnCounter > SpawnTime)
            {
                if (_spawnCounter > DespawnTime)
                    // fade out
                    _sprite.Color = Color.White * (1 - Math.Clamp((_spawnCounter - DespawnTime) / FadeOutTime, 0, 1));
                else
                    // fade in
                    _sprite.Color = Color.White * Math.Clamp((_spawnCounter - SpawnTime) / FadeInTime, 0, 1);

                if (_spawnCounter > DespawnTime + FadeOutTime)
                {
                    Map.Objects.DeleteObjects.Add(this);
                    return;
                }
            }

            var collision = Map.Objects.Hit(this, EntityPosition.Position, _damageBox.Box, HitType.SwordShot, 2, false);
            if ((collision & (Values.HitCollision.Blocking | Values.HitCollision.Enemy)) != 0)
                Map.Objects.DeleteObjects.Add(this);
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            // draw the trail
            var spawnDistance = (new Vector2(EntityPosition.X, EntityPosition.Y) - _spawnPosition).Length();
            var trailCount = 3;
            var distMult = 1.5f;
            for (int i = 0; i < trailCount; i++)
            {
                var drawPosition = new Vector2(EntityPosition.X, EntityPosition.Y) + _sprite.DrawOffset - new Vector2(_body.VelocityTarget.X, _body.VelocityTarget.Y) * (trailCount - i) * distMult;
                // make sure to not show the tail behind the actual spawn position
                if (spawnDistance > ((trailCount - i) * MoveSpeed * distMult))
                    spriteBatch.Draw(_sprite.SprTexture, drawPosition, _sprite.SourceRectangle, _sprite.Color * (0.20f + 0.30f * ((i + 1) / (float)trailCount)),
                        _sprite.Rotation, _sprite.Center * _sprite.Scale, new Vector2(_sprite.Scale), SpriteEffects.None, 0);
            }

            // draw the actual sprite
            _sprite.Draw(spriteBatch);
        }

        private void OnCollision(Values.BodyCollision collision)
        {
            var animation = new ObjAnimator(Map, (int)EntityPosition.X, (int)EntityPosition.Y - (int)EntityPosition.Z,
                0, 0, Values.LayerTop, "Particles/swordShotDespawn", "run", true);
            Map.Objects.SpawnObject(animation);

            Map.Objects.DeleteObjects.Add(this);
        }
    }
}
