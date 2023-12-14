using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Things;
using static ProjectZ.InGame.GameObjects.Base.Components.PushableComponent;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyBeamosProjectile : GameObject
    {
        private readonly DamageFieldComponent _damageField;
        private readonly BodyComponent _body;

        private bool _isFirstProjectile;

        public EnemyBeamosProjectile(Map.Map map, Vector2 position, Vector2 velocityTarget, bool isFirstProjectile) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(position.X, position.Y, 0);
            EntitySize = new Rectangle(-3, -3, 6, 6);

            _isFirstProjectile = isFirstProjectile;

            var sprite = new CSprite("beamos projectile", EntityPosition, new Vector2(-2, -2));

            _body = new BodyComponent(EntityPosition, -1, -1, 2, 2, 8)
            {
                IgnoresZ = true,
                IgnoreHoles = true,
                // can go over some colliders
                Level = 1,
                // the reason for the simple movement is to not align the body
                // with the colliding object and spawn the particle at an offset position
                SimpleMovement = true,
                VelocityTarget = velocityTarget,
                MoveCollision = OnCollision,
                CollisionTypes = Values.CollisionTypes.Normal
            };

            var damageCollider = new CBox(EntityPosition, -2, -2, 0, 4, 4, 4);
            _damageField = new DamageFieldComponent(damageCollider, HitType.Enemy, 4)
            {
                OnDamage = OnDamage
            };

            AddComponent(PushableComponent.Index, new PushableComponent(damageCollider, OnPush));
            AddComponent(DamageFieldComponent.Index, _damageField);
            AddComponent(BodyComponent.Index, _body);
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(sprite, Values.LayerPlayer));
        }

        private bool OnPush(Vector2 direction, PushType pushType)
        {
            if (pushType == PushType.Impact)
                DeleteProjectile();

            return false;
        }

        private bool OnDamage()
        {
            DeleteProjectile();

            return _damageField.DamagePlayer();
        }

        private void OnCollision(Values.BodyCollision collision)
        {
            DeleteProjectile();
        }

        private void DeleteProjectile()
        {
            Map.Objects.DeleteObjects.Add(this);

            if (_isFirstProjectile)
            {
                // spawn particle
                var animation = new ObjAnimator(Map, 0, 0, Values.LayerTop, "Particles/despawnParticle", "idle", true);
                animation.EntityPosition.Set(EntityPosition.Position + _body.VelocityTarget * Game1.TimeMultiplier);
                Map.Objects.SpawnObject(animation);
            }
        }
    }
}