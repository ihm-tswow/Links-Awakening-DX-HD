using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Bosses
{
    class BossMoldormTail : GameObject
    {
        public BossMoldormTail(Map.Map map, BossMoldorm nightmare) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;
            
            EntityPosition = new CPosition(nightmare.EntityPosition.X, nightmare.EntityPosition.Y, 0);

            var animator = AnimatorSaveLoad.LoadAnimator("Nightmares/nightmare tail");
            animator.Play("idle");

            var sprite = new CSprite(EntityPosition);
            
            var hittableBox = new CBox(EntityPosition, -6, -6, 12, 12, 8);
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, nightmare.OnHitTail));
            AddComponent(BaseAnimationComponent.Index, new AnimationComponent(animator, sprite, new Vector2(-8, -8)));
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(sprite, Values.LayerBottom));
            AddComponent(PushableComponent.Index, new PushableComponent(new CBox(EntityPosition, -6, -6, 12, 12, 8), OnPush) { RepelMultiplier = 1.5f });
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            return true;
        }
    }
}