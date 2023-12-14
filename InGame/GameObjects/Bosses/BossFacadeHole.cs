using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Bosses
{
    internal class BossFacadeHole : GameObject
    {
        private readonly Animator _animator;
        private readonly BoxCollisionComponent _collisionComponent;

        private bool _playedSoundEffect;

        public BossFacadeHole(Map.Map map, Vector2 position) : base(map)
        {
            Tags = Values.GameObjectTag.Hole;

            EntityPosition = new CPosition(position.X, position.Y - 8, 0);
            EntitySize = new Rectangle(-8, 0, 16, 16);

            var sprite = new CSprite(EntityPosition);

            _animator = AnimatorSaveLoad.LoadAnimator("Nightmares/facade hole");
            _animator.Play("idle");

            var animationComponent = new AnimationComponent(_animator, sprite, new Vector2(0, 8));

            _collisionComponent = new BoxCollisionComponent(new CBox(EntityPosition, -7, -7 + 8, 0, 14, 14, 16), Values.CollisionTypes.Hole);
            AddComponent(CollisionComponent.Index, _collisionComponent);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(sprite, Values.LayerBottom));
        }

        private void Update()
        {
            if (!_playedSoundEffect && _animator.CurrentFrameIndex == 1)
            {
                _playedSoundEffect = true;
                Game1.GameManager.PlaySoundEffect("D360-64-40");
            }

            // hole is only active while at the x frame
            if (_animator.CurrentFrameIndex == 2 || _animator.CurrentFrameIndex == 3)
                _collisionComponent.IsActive = true;
            else
                _collisionComponent.IsActive = false;

            // finished animation => delete the object
            if(!_animator.IsPlaying)
                Map.Objects.DeleteObjects.Add(this);
        }
    }
}