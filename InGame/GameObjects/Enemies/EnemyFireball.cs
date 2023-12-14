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
    internal class EnemyFireball : GameObject
    {
        private readonly CSprite _sprite;
        private readonly BodyComponent _body;
        private readonly Rectangle _fieldRectangle;
        private double _liveTime = 2250;

        public EnemyFireball(Map.Map map, int posX, int posY, float speed, bool hittable = true) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(-5, -5, 10, 10);

            var animator = AnimatorSaveLoad.LoadAnimator("Enemies/fireball");
            animator.Play("idle");

            _sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(animator, _sprite, new Vector2(-5, -5));

            _body = new BodyComponent(EntityPosition, -5, -5, 10, 10, 8)
            {
                IgnoresZ = true,
                IgnoreHoles = true,
                CollisionTypes = Values.CollisionTypes.None
            };

            _fieldRectangle = Map.GetField(posX, posY);

            var playerDirection = new Vector2(MapManager.ObjLink.EntityPosition.X, MapManager.ObjLink.EntityPosition.Y - 4) - EntityPosition.Position;
            if (playerDirection != Vector2.Zero)
                playerDirection.Normalize();
            _body.VelocityTarget = playerDirection * speed;

            var damageBox = new CBox(EntityPosition, -3, -3, 0, 6, 6, 4);
            var hittableBox = new CBox(EntityPosition, -4, -4, 0, 8, 8, 8);

            AddComponent(BodyComponent.Index, _body);
            if (hittable)
            {
                AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageBox, HitType.Enemy, 2));
                AddComponent(PushableComponent.Index, new PushableComponent(damageBox, OnPush));
                AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            }
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(_sprite, Values.LayerTop));
        }

        public void SetVelocity(Vector2 velocity)
        {
            _body.VelocityTarget = velocity;
        }

        private void Update()
        {
            _liveTime -= Game1.DeltaTime;

            if (_liveTime <= 125)
                _sprite.Color = Color.White * ((float)_liveTime / 125f);
            // start despawning if we get outside of the current room
            else if (!_fieldRectangle.Contains(EntityPosition.Position))
                _liveTime = 125;

            if (_liveTime < 0)
                Map.Objects.DeleteObjects.Add(this);
        }

        private Values.HitCollision OnHit(GameObject originObject, Vector2 direction, HitType type, int damage, bool pieceOfPower)
        {
            if ((type & HitType.Sword) == 0)
                return Values.HitCollision.None;

            Game1.GameManager.PlaySoundEffect("D360-03-03");

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