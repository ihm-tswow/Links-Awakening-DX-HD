using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.MidBoss
{
    internal class MBossBallAndChain : GameObject
    {
        private readonly MBossBallAndChainSoldier _owner;
        private readonly CSprite _sprite;

        private Rectangle _sourceRectangleLink = new Rectangle(179, 181, 4, 4);

        private bool _isActive;

        public MBossBallAndChain(Map.Map map, MBossBallAndChainSoldier owner) : base(map)
        {
            EntityPosition = new CPosition(owner.EntityPosition.X - 5, owner.EntityPosition.Y - 8 + 2, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            _owner = owner;
            _sprite = new CSprite(Resources.SprMidBoss, EntityPosition, new Rectangle(184, 175, 16, 16), new Vector2(-8, -16));

            var damageCollider = new CBox(EntityPosition, -6, -8 - 6, 0, 12, 12, 8);
            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageCollider, HitType.Enemy, 4));
            AddComponent(HittableComponent.Index, new HittableComponent(damageCollider, OnHit));
            AddComponent(PushableComponent.Index, new PushableComponent(damageCollider, OnPush));
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerBottom, EntityPosition));
        }

        public void Activate()
        {
            _isActive = true;
        }

        public void Deactivate()
        {
            _isActive = false;
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (!_isActive)
                return Values.HitCollision.None;

            _owner.BlockBall();
            return Values.HitCollision.RepellingParticle;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (!_isActive)
                return false;

            if (type == PushableComponent.PushType.Impact)
                _owner.BlockBall();

            return true;
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            var handPosition = new Vector2(_owner.EntityPosition.X - 5, _owner.EntityPosition.Y - 15);
            var direction = new Vector2(EntityPosition.X, EntityPosition.Y - 8) - handPosition;
            // draw the chain
            for (var i = 0; i < 3; i++)
            {
                var linkPosition = handPosition + direction * ((i + 1) / 4.0f) - new Vector2(2, 2);
                spriteBatch.Draw(Resources.SprMidBoss, linkPosition, _sourceRectangleLink, Color.White);
            }

            _sprite.Draw(spriteBatch);
        }
    }
}