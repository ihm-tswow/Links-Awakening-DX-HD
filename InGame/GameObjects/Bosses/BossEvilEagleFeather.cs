using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class BossEvilEagleFeather : GameObject
    {
        private readonly DamageFieldComponent _damageFieldComponent;
        private readonly BodyComponent _body;

        private readonly CSprite _sprite;
        private double _liveTime = 750;
        private bool _reflected;

        public BossEvilEagleFeather(Map.Map map, Vector2 position, Vector2 velocity) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(position.X, position.Y, 0);
            EntitySize = new Rectangle(-5, -5, 10, 10);

            _sprite = new CSprite("eagle feather", EntityPosition, Vector2.Zero);
            _sprite.Center = new Vector2(12, 4);

            if (velocity.X > 0)
            {
                _sprite.Center = new Vector2(3, 4);
                _sprite.SpriteEffect = SpriteEffects.FlipHorizontally;
            }

            _body = new BodyComponent(EntityPosition, -3, -3, 6, 6, 8)
            {
                IgnoresZ = true,
                IgnoreHoles = true,
                CollisionTypes = Values.CollisionTypes.None
            };

            _body.VelocityTarget = velocity;

            var damageBox = new CBox(EntityPosition, -3, -3, 0, 6, 6, 8);
            var pushBox = new CBox(EntityPosition, -4, -3, 0, 8, 6, 8);
            var hittableBox = new CBox(EntityPosition, -6, -3, 0, 12, 6, 8);

            AddComponent(BodyComponent.Index, _body);
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(DamageFieldComponent.Index, _damageFieldComponent = new DamageFieldComponent(damageBox, HitType.Enemy, 2));
            AddComponent(PushableComponent.Index, new PushableComponent(pushBox, OnPush) { RepelMultiplier = 0.075f });
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(_sprite, Values.LayerBottom));
        }

        private void Update()
        {
            _liveTime -= Game1.DeltaTime;

            if (_liveTime <= 75)
                _sprite.Color = Color.White * ((float)_liveTime / 75f);

            if (_liveTime < 0)
                Map.Objects.DeleteObjects.Add(this);
        }

        private Values.HitCollision OnHit(GameObject originObject, Vector2 direction, HitType type, int damage, bool pieceOfPower)
        {
            Reflect();

            return Values.HitCollision.Enemy;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type == PushableComponent.PushType.Impact)
            {
                Reflect();
                return true;
            }

            return false;
        }

        private void Reflect()
        {
            if (_reflected)
                return;

            Game1.GameManager.PlaySoundEffect("D360-22-16");

            _reflected = true;
            _damageFieldComponent.IsActive = false;
            _body.VelocityTarget.Y = -_body.VelocityTarget.Y;
            _sprite.SpriteEffect |= SpriteEffects.FlipVertically;
        }
    }
}