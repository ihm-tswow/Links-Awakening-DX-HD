using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyPodobooParticle : GameObject
    {
        private readonly Animator _animator;
        private readonly CSprite _sprite;

        public EnemyPodobooParticle(Map.Map map, Vector2 position, bool mirrored) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(position.X, position.Y - 5, 0);
            EntitySize = new Rectangle(-5, 0, 10, 10);

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/podoboo");
            _animator.Play("particle");

            _sprite = new CSprite(EntityPosition) { Color = Color.White * 0.85f };
            var animationComponent = new AnimationComponent(_animator, _sprite, new Vector2(0, 5));
            animationComponent.MirroredV = mirrored;

            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(_sprite, Values.LayerBottom));
        }

        private void Update()
        {
            // 4 frame blink effect
            _sprite.SpriteShader = Game1.TotalGameTime % (8000 / 60f) >= (4000 / 60f) ? Resources.DamageSpriteShader0 : null;

            // delete after finishing the animation
            if (!_animator.IsPlaying)
                Map.Objects.DeleteObjects.Add(this);
        }
    }
}