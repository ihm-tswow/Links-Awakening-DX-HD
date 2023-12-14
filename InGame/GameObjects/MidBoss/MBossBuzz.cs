using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.MidBoss
{
    class MBossBuzz : GameObject
    {
        private readonly CSprite _sprite;
        private float _liveCounter = 2000;

        public MBossBuzz(Map.Map map, Vector2 position, Vector2 velocity, string spriteId, float spriteRotation) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(position.X, position.Y, 0);
            EntitySize = new Rectangle(-8, -8, 16, 16);

            _sprite = new CSprite(spriteId, EntityPosition, Vector2.Zero);

            var objWidth = _sprite.SourceRectangle.Width * _sprite.Scale;
            var objHeight = _sprite.SourceRectangle.Height * _sprite.Scale;

            _sprite.Center = new Vector2(objWidth / 2, objHeight / 2);
            _sprite.Rotation = spriteRotation;

            var body = new BodyComponent(EntityPosition, -5, -5, 10, 10, 8)
            {
                IgnoresZ = true,
                IgnoreHoles = true,
                CollisionTypes = Values.CollisionTypes.None
            };

            body.VelocityTarget = velocity;

            var damageCollider = new CBox(EntityPosition, -5, -5, 10, 10, 8);
            var hittableBox = new CBox(EntityPosition, -5, -5, 10, 10, 8);

            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageCollider, HitType.Enemy, 2));
            AddComponent(BodyComponent.Index, body);
            //AddComponent(PushableComponent.Index, new PushableComponent(body.BodyBox, OnPush));
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(_sprite, Values.LayerTop));
        }

        private void Update()
        {
            // blink 8 frame interval
            _sprite.SpriteShader = (Game1.TotalGameTime % (8 / 60f * 1000) < 4 / 60f * 1000) ? Resources.DamageSpriteShader0 : null;

            // fade out/ delete object
            _liveCounter -= Game1.DeltaTime;
            if (_liveCounter < 100)
                _sprite.Color = Color.White * (_liveCounter / 100);
            if (_liveCounter <= 0)
                Map.Objects.DeleteObjects.Add(this);
        }

        private Values.HitCollision OnHit(GameObject originObject, Vector2 direction, HitType type, int damage, bool pieceOfPower)
        {


            return Values.HitCollision.RepellingParticle;
        }
    }
}
