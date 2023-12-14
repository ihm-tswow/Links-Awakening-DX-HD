using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Bosses
{
    class BossFinalBossTail : GameObject
    {
        public readonly CSprite Sprite;

        public BossFinalBossTail(Map.Map map, BossFinalBoss nightmare, string animationId, bool hittable) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(nightmare.EntityPosition.X, nightmare.EntityPosition.Y, 0);
            EntitySize = new Rectangle(-8, -8, 16, 16);

            var animator = AnimatorSaveLoad.LoadAnimator("Nightmares/nightmare");
            animator.Play(animationId);

            Sprite = new CSprite(EntityPosition);

            if (hittable)
            {
                var hittableBox = new CBox(EntityPosition, -6, -6, 12, 12, 8);
                AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, nightmare.HitTail));
            }

            AddComponent(BaseAnimationComponent.Index, new AnimationComponent(animator, Sprite, Vector2.Zero));
        }
    }
}