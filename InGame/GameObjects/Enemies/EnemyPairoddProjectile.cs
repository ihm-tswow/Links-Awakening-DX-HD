using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyPairoddProjectile : GameObject
    {
        public EnemyPairoddProjectile(Map.Map map, Vector2 position, float speed) : base(map)
        {
            Tags = Values.GameObjectTag.Damage;

            EntityPosition = new CPosition(position.X, position.Y, 0);
            EntitySize = new Rectangle(-7, -7, 14, 14);

            var animator = AnimatorSaveLoad.LoadAnimator("Enemies/pairodd projectile");
            animator.Play("idle");

            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(animator, sprite, Vector2.Zero);

            var body = new BodyComponent(EntityPosition, -2, -2, 4, 4, 8)
            {
                IgnoresZ = true,
                IgnoreHoles = true,
                CollisionTypes = Values.CollisionTypes.Normal,
                MoveCollision = OnCollision
            };

            var velocity = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            if (velocity != Vector2.Zero)
                velocity.Normalize();
            body.VelocityTarget = velocity * speed;

            var damageCollider = new CBox(EntityPosition, -3, -3, 0, 6, 6, 4);

            AddComponent(PushableComponent.Index, new PushableComponent(body.BodyBox, OnPush));
            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageCollider, HitType.Enemy, 2) { OnDamagedPlayer = DamagedPlayer });
            AddComponent(BodyComponent.Index, body);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(sprite, Values.LayerPlayer));
        }

        private void DamagedPlayer()
        {
            Despawn();
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType pushType)
        {
            Despawn();

            return true;
        }

        private void OnCollision(Values.BodyCollision collision)
        {
            Despawn();
        }

        private void Despawn()
        {
            // spawn despawn effect
            var animation = new ObjAnimator(Map, (int)EntityPosition.X, (int)EntityPosition.Y, Values.LayerTop, "Particles/swordPoke", "run", true);
            Map.Objects.SpawnObject(animation);

            Map.Objects.DeleteObjects.Add(this);
        }
    }
}