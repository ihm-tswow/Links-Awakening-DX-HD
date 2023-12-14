using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjSpikes : GameObject
    {
        private readonly Animator _animator;
        private readonly int _animationLength;

        public ObjSpikes() : base("spikes") { }

        public ObjSpikes(Map.Map map, int posX, int posY) : base(map)
        {
            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 16, 16);

            Tags = Values.GameObjectTag.Lamp;

            _animator = AnimatorSaveLoad.LoadAnimator("Objects/spikes");
            _animator.Play("idle");

            _animationLength = _animator.GetAnimationTime(0, _animator.CurrentAnimation.Frames.Length);

            var sprite = new CSprite(EntityPosition);

            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(new CBox(posX + 2, posY + 2, 0, 12, 12, 2), HitType.Spikes, 2));
            AddComponent(BaseAnimationComponent.Index, new AnimationComponent(_animator, sprite, Vector2.Zero));
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(sprite, Values.LayerBottom));
        }

        private void Update()
        {
            // @HACK: this is used to sync all the animations with the same length
            // otherwise they would not be in sync if they did not get updated at the same time
            _animator.SetFrame(0);
            _animator.SetTime(Game1.TotalGameTime % _animationLength);
            _animator.Update();
        }
    }
}