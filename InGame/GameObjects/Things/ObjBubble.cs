using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjBubble : GameObject
    {
        private readonly Animator _animator;

        public ObjBubble(Map.Map map, Vector3 position, Vector3 velocity) : base(map)
        {
            EntityPosition = new CPosition(position.X, position.Y, position.Z);
            EntitySize = new Rectangle(-3, -32, 6, 35);

            _animator = AnimatorSaveLoad.LoadAnimator("NPCs/walrus particle");
            _animator.Play("idle");

            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, sprite, Vector2.Zero);

            var body = new BodyComponent(EntityPosition, -3, -3, 6, 6, 3)
            {
                Velocity = velocity,
                Gravity = 0,
                DragAir = 0.975f
                //IgnoresZ = true
            };

            AddComponent(BodyComponent.Index, body);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(sprite, Values.LayerPlayer));
        }

        private void Update()
        {
            if (!_animator.IsPlaying)
                Map.Objects.DeleteObjects.Add(this);
        }
    }
}