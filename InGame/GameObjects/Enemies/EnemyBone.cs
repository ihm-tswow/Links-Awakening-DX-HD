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
    internal class EnemyBone : GameObject
    {
        private readonly CSprite _sprite;

        private double _liveTime = 1250;

        public EnemyBone(Map.Map map, int posX, int posY, float speed) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(-5, -5, 10, 10);

            var animator = AnimatorSaveLoad.LoadAnimator("Enemies/bone");
            animator.Play("idle");

            _sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(animator, _sprite, new Vector2(-5, -5));

            var body = new BodyComponent(EntityPosition, -3, -3, 6, 6, 8)
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

            var damageBox = new CBox(EntityPosition, -3, -3, 0, 6, 6, 4);
            var hittableBox = new CBox(EntityPosition, -3, -3, 0, 6, 6, 8);

            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageBox, HitType.Enemy, 2));
            AddComponent(BodyComponent.Index, body);
            AddComponent(PushableComponent.Index, new PushableComponent(body.BodyBox, OnPush));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(_sprite, Values.LayerTop));
        }

        private void OnCollision(Values.BodyCollision collision)
        {
            Despawn();
        }

        private void Update()
        {
            _liveTime -= Game1.DeltaTime;

            if (_liveTime <= 125)
                _sprite.Color = Color.White * ((float)_liveTime / 125f);

            if (_liveTime < 0)
                Map.Objects.DeleteObjects.Add(this);
        }

        private Values.HitCollision OnHit(GameObject originObject, Vector2 direction, HitType type, int damage, bool pieceOfPower)
        {
            Despawn();

            return Values.HitCollision.Repelling;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type == PushableComponent.PushType.Impact)
                Despawn();

            return true;
        }

        private void Despawn()
        {
            Map.Objects.DeleteObjects.Add(this);
            Map.Objects.SpawnObject(new ObjAnimator(Map, (int)EntityPosition.X, (int)EntityPosition.Y, Values.LayerTop, "Particles/despawn", "run", true));
        }
    }
}