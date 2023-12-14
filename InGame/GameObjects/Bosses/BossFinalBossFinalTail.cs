using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Bosses
{
    class BossFinalBossFinalTail : GameObject
    {
        private readonly BossFinalBoss _owner;

        private readonly DrawComponent _drawComponent;
        private readonly DamageFieldComponent _damageFieldComponent;

        public readonly CSprite Sprite;

        public BossFinalBossFinalTail(Map.Map map, BossFinalBoss owner, string spriteId, Vector2 position) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(position.X, position.Y, 0);
            EntitySize = new Rectangle(-8, -8, 16, 16);

            _owner = owner;

            Sprite = new CSprite(spriteId, EntityPosition);

            var damageCollider = new CBox(EntityPosition, -3, -3, 6, 6, 3);
            AddComponent(DamageFieldComponent.Index, _damageFieldComponent = new DamageFieldComponent(damageCollider, HitType.Enemy, 2));
            AddComponent(DrawComponent.Index, _drawComponent = new DrawComponent(Draw, Values.LayerBottom, EntityPosition));

            SetActive(false);
        }

        public void DeactivateDamageField()
        {
            _damageFieldComponent.IsActive = false;
        }

        public void SetActive(bool state)
        {
            _damageFieldComponent.IsActive = state;
            _drawComponent.IsActive = state;
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            if (!_drawComponent.IsActive)
                return;

            Sprite.SpriteShader = _owner.Sprite.SpriteShader;
            Sprite.Draw(spriteBatch);
        }
    }
}