using System;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    class ObjMagicRodShot : GameObject
    {
        private readonly CSprite _sprite;
        private readonly CBox _damageBox;

        private const int SpawnTime = 25;
        // ~ time to move from the left side of a room to the right
        private const int DespawnTime = 500;
        private const int FadeInTime = 25;
        private const int FadeOutTime = 50;

        private float _spawnCounter;
        private bool _dead;

        public ObjMagicRodShot(Map.Map map, Vector3 position, Vector2 direction, int dir) : base(map)
        {
            EntityPosition = new CPosition(position.X, position.Y, position.Z);
            EntitySize = new Rectangle(-8, -8, 16, 16);

            _damageBox = new CBox(EntityPosition, -3, -3, 0, 6, 6, 8);

            _sprite = new CSprite("magicRodShot", EntityPosition)
            {
                Color = Color.Transparent
            };

            var body = new BodyComponent(EntityPosition, -2 + (dir == 1 ? 2 : (dir == 3 ? -2 : 0)), -2, 4, 4, 8)
            {
                VelocityTarget = direction,
                CollisionTypesIgnore = Values.CollisionTypes.ThrowWeaponIgnore,
                MoveCollision = OnCollision,
                IgnoreHoles = true,
                IgnoresZ = true,
                IgnoreInsideCollision = false,
                Level = MapStates.GetLevel(MapManager.ObjLink._body.CurrentFieldState)
            };

            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(BodyComponent.Index, body);
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(_sprite, Values.LayerPlayer));
        }

        private void Update()
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
                    _dead = true;
                    Map.Objects.DeleteObjects.Add(this);
                    return;
                }

            }

            var collision = Map.Objects.Hit(this, EntityPosition.Position, _damageBox.Box, HitType.MagicRod, 2, false);
            if ((collision & (Values.HitCollision.Blocking | Values.HitCollision.Repelling | Values.HitCollision.Enemy)) != 0)
            {
                _dead = true;
                Map.Objects.DeleteObjects.Add(this);
            }
        }

        private void OnCollision(Values.BodyCollision collision)
        {
            if (_dead)
                return;

            Game1.GameManager.PlaySoundEffect("D378-18-12");

            var animation = new ObjAnimator(Map, (int)EntityPosition.X, (int)EntityPosition.Y - (int)EntityPosition.Z,
                0, 0, Values.LayerPlayer, "Particles/flame", "idle", true);
            Map.Objects.SpawnObject(animation);

            Map.Objects.DeleteObjects.Add(this);
        }
    }
}
