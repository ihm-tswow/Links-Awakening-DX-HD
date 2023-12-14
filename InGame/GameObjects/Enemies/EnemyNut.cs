using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    class EnemyNut : GameObject
    {
        private readonly BodyComponent _body;
        private readonly CSprite _sprite;

        private int _collisionCount;
        private bool _wasHit;

        public EnemyNut(Map.Map map, Vector3 position, Vector3 direction) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(position.X, position.Y, position.Z);
            EntitySize = new Rectangle(-6, -48, 12, 48);

            _sprite = new CSprite(Resources.SprEnemies, EntityPosition, new Rectangle(306, 2, 12, 12), new Vector2(-6, -12));

            _body = new BodyComponent(EntityPosition, -6, -12, 12, 12, 8)
            {
                MoveCollision = MoveCollision,
                CollisionTypes = Values.CollisionTypes.None,
                Gravity = -0.1f,
                DragAir = 1.0f,
                Bounciness = 0.75f
            };
            _body.Velocity = direction;

            var hitBox = new CBox(EntityPosition, -5, -11, 0, 10, 10, 10, true);
            AddComponent(PushableComponent.Index, new PushableComponent(hitBox, OnPush));
            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(hitBox, HitType.Enemy, 2));
            AddComponent(HittableComponent.Index, new HittableComponent(hitBox, OnHit));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(_sprite, Values.LayerPlayer));

            var shadow = new DrawShadowSpriteComponent(Resources.SprShadow, EntityPosition, new Rectangle(0, 0, 65, 66), new Vector2(-6, -6), 12, 6);
            AddComponent(DrawShadowComponent.Index, shadow);
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type == PushableComponent.PushType.Impact)
                _body.Velocity = new Vector3(direction.X * 1.75f, direction.Y * 1.75f, _body.Velocity.Z);

            return true;
        }

        private void MoveCollision(Values.BodyCollision collisionType)
        {
            _collisionCount++;

            if (_collisionCount > 3 || _wasHit)
            {
                // spawn explosion effect
                if (_wasHit)
                {
                    Game1.GameManager.PlaySoundEffect("D360-03-03");
                    Map.Objects.SpawnObject(new ObjAnimator(Map, (int)EntityPosition.X - 12,
                        (int)EntityPosition.Y - 12, Values.LayerTop, "Particles/explosion0", "run", true));

                    if (Game1.RandomNumber.Next(0, 2) == 0)
                    {
                        var objItem = new ObjItem(Map, (int)EntityPosition.X - 8, (int)EntityPosition.Y - 8, "j", "", "ruby", "", true);
                        objItem.SetSpawnDelay(250);
                        Map.Objects.SpawnObject(objItem);
                    }
                }

                Map.Objects.DeleteObjects.Add(this);
                return;
            }

            if (!_wasHit)
                Game1.GameManager.PlaySoundEffect("D360-09-09");

            // set a new random direction
            var angle = (Game1.RandomNumber.Next(0, 100) / 100f) * (float)Math.PI * 2f;
            _body.Velocity = new Vector3((float)Math.Sin(angle), (float)Math.Cos(angle), _body.Velocity.Z);

            // flip the sprite
            _sprite.SpriteEffect ^= SpriteEffects.FlipHorizontally;
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (_wasHit)
                return Values.HitCollision.None;

            _body.Velocity = new Vector3(direction.X, direction.Y, 0.1f) * 3.5f;
            EntityPosition.Set(new Vector3(EntityPosition.X, EntityPosition.Y - EntityPosition.Z, 0));
            _wasHit = true;

            return Values.HitCollision.Enemy;
        }
    }
}