using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.NPCs
{
    internal class ObjDancingFish : GameObject
    {
        public BodyComponent Body;
        public readonly Animator Animator;

        public ObjDancingFish(Map.Map map, Vector2 position) : base(map)
        {
            EntityPosition = new CPosition(position.X, position.Y, 0);
            EntitySize = new Rectangle(-12, -14, 24, 18);

            Animator = AnimatorSaveLoad.LoadAnimator("NPCs/dance fish");
            Animator.Play("idle");

            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(Animator, sprite, Vector2.Zero);

            Body = new BodyComponent(EntityPosition, -8, -8, 16, 8, 8)
            {
                IgnoresZ = true
            };

            AddComponent(BodyComponent.Index, Body);
            AddComponent(CollisionComponent.Index, new BodyCollisionComponent(Body, Values.CollisionTypes.Normal));
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new BodyDrawComponent(Body, sprite, Values.LayerBottom));
        }

        private void Update()
        {
            
        }
    }
}