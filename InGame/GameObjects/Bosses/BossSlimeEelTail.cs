using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Bosses
{
    class BossSlimeEelTail : GameObject
    {
        public CSprite Sprite;

        private readonly DamageFieldComponent _damageFieldComponent;

        public BossSlimeEelTail() : base("slime eel") { }

        public BossSlimeEelTail(Map.Map map, Vector2 position, int spriteIndex, HittableComponent.HitTemplate onHit) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(position.X, position.Y, 0);
            EntitySize = new Rectangle(-8, -8, 16, 16);

            Sprite = new CSprite("eel_tail_" + spriteIndex, EntityPosition, spriteIndex == 1 ? new Vector2(-7) : new Vector2(-8));

            var damageCollider = new CBox(EntityPosition, -6, -6, 12, 12, 2);

            AddComponent(DamageFieldComponent.Index, _damageFieldComponent = new DamageFieldComponent(damageCollider, HitType.Enemy, 2));

            if (onHit != null)
            {
                var hittableCollider = new CBox(EntityPosition, -6, -6, 12, 12, 8);
                AddComponent(HittableComponent.Index, new HittableComponent(hittableCollider, onHit));
            }
        }

        public void SetActive(bool state)
        {
            _damageFieldComponent.IsActive = state;
        }
    }
}