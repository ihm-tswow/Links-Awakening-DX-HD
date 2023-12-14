using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;
using System;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyRock : GameObject
    {
        private readonly CSprite _sprite;
        private readonly BodyComponent _body;
        private readonly Vector2 _spawnPosition;

        private double _liveTime = 125;

        public EnemyRock(Map.Map map, Vector2 position) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(position.X, position.Y, Game1.RandomNumber.Next(8, 24));
            EntitySize = new Rectangle(-8, -40, 16, 48);

            _spawnPosition = EntityPosition.Position;

            var animator = AnimatorSaveLoad.LoadAnimator("Enemies/rock");
            animator.Play("idle");

            _sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(animator, _sprite, Vector2.Zero);

            _body = new BodyComponent(EntityPosition, -5, -5, 10, 10, 8)
            {
                Gravity = -0.125f,
                CollisionTypes = Values.CollisionTypes.None
            };

            var damageBox = new CBox(EntityPosition, -6, -6, 0, 12, 12, 8, true);
            var hittableBox = new CBox(EntityPosition, -7, -7, 0, 14, 14, 8, true);

            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageBox, HitType.Enemy, 2));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(PushableComponent.Index, new PushableComponent(damageBox, OnPush));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(_sprite, Values.LayerTop));
            AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(_body, _sprite) { ShadowWidth = 10, ShadowHeight = 6 });
        }

        private void Update()
        {
            // start despawning
            if (EntityPosition.Y > _spawnPosition.Y + 128)
            {
                _liveTime -= Game1.DeltaTime;

                // fade out
                _sprite.Color = Color.White * ((float)_liveTime / 125f);
                if (_liveTime < 0)
                    Map.Objects.DeleteObjects.Add(this);
            }

            // bounce in a random direction
            if (_body.IsGrounded)
            {
                Game1.GameManager.PlaySoundEffect("D360-32-20", true, EntityPosition.Position);

                _body.Velocity.Z = Game1.RandomNumber.Next(150, 250) / 100f;

                var length = Game1.RandomNumber.Next(75, 150) / 150f;
                var direction = (-100 + Game1.RandomNumber.Next(0, 200)) / 100f;
                _body.VelocityTarget = new Vector2(MathF.Sin(direction), MathF.Cos(direction)) * length;
            }
        }

        private Values.HitCollision OnHit(GameObject originObject, Vector2 direction, HitType type, int damage, bool pieceOfPower)
        {
            return Values.HitCollision.Enemy | Values.HitCollision.RepellingParticle;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            return true;
        }
    }
}