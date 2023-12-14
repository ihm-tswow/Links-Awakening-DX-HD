using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Bosses
{
    internal class BossHardhitBeetleShot : GameObject
    {
        private readonly CSprite _sprite;
        private double _liveTime = 2000;

        public BossHardhitBeetleShot(Map.Map map, Vector2 position, float speed) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(position.X, position.Y, 0);
            EntitySize = new Rectangle(-8, -8, 16, 16);

            var animator = AnimatorSaveLoad.LoadAnimator("Nightmares/hardhit beetle shot");
            animator.Play("idle");

            _sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(animator, _sprite, Vector2.Zero);

            var body = new BodyComponent(EntityPosition, -5, -5, 10, 10, 8)
            {
                IgnoresZ = true,
                IgnoreHoles = true,
                CollisionTypes = Values.CollisionTypes.None
            };

            // move towards the player
            var velocity = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            if (velocity != Vector2.Zero)
                velocity.Normalize();
            body.VelocityTarget = velocity * speed;

            var hittableBox = new CBox(EntityPosition, -6, -6, 0, 12, 12, 8);
            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(hittableBox, HitType.Enemy, 2));
            AddComponent(BodyComponent.Index, body);
            AddComponent(PushableComponent.Index, new PushableComponent(body.BodyBox, OnPush));
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(_sprite, Values.LayerTop));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
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
            return Values.HitCollision.RepellingParticle;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            return true;
        }
    }
}