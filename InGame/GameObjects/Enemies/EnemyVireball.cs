using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyVireball : GameObject
    {
        private readonly CSprite _sprite;
        private double _liveTime = 2500;

        public EnemyVireball(Map.Map map, Vector2 position, Vector2 velocity) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(position.X, position.Y, 0);
            EntitySize = new Rectangle(-5, -5, 10, 10);

            var animator = AnimatorSaveLoad.LoadAnimator("Enemies/vireball");
            animator.Play("idle");

            _sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(animator, _sprite, Vector2.Zero);

            var body = new BodyComponent(EntityPosition, -5, -5, 10, 10, 8)
            {
                IgnoresZ = true,
                IgnoreHoles = true,
                CollisionTypes = Values.CollisionTypes.None
            };

            body.VelocityTarget = velocity;

            var damageCollider = new CBox(EntityPosition, -3, -3, 0, 6, 6, 4);
            var hittableBox = new CBox(EntityPosition, -4, -4, 0, 8, 8, 8);

            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageCollider, HitType.Enemy, 2));
            AddComponent(BodyComponent.Index, body);
            AddComponent(PushableComponent.Index, new PushableComponent(body.BodyBox, OnPush));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(_sprite, Values.LayerTop));
        }

        private void Update()
        {
            _liveTime -= Game1.DeltaTime;

            // fade out
            if (_liveTime <= 100)
                _sprite.Color = Color.White * ((float)_liveTime / 100f);

            if (_liveTime < 0)
                Map.Objects.DeleteObjects.Add(this);
        }
        
        private Values.HitCollision OnHit(GameObject originObject, Vector2 direction, HitType type, int damage, bool pieceOfPower)
        {
            OnDeath();
            
            return Values.HitCollision.Enemy;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type == PushableComponent.PushType.Impact)
                OnDeath();

            return true;
        }

        private void OnDeath()
        {
            var splashAnimator = new ObjAnimator(Map, 0, 0, 0, 0, Values.LayerTop, "Particles/spawn", "run", true);
            splashAnimator.EntityPosition.Set(EntityPosition.Position - new Vector2(8, 8));
            Map.Objects.SpawnObject(splashAnimator);

            // TODO: add sound effect
            Map.Objects.DeleteObjects.Add(this);
        }
    }
}