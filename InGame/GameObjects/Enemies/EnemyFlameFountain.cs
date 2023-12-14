using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    class EnemyFlameFountain : GameObject
    {
        private readonly Animator _animator;
        
        private int _lastFrameIndex;

        public EnemyFlameFountain() : base("flame fountain") { }

        public EnemyFlameFountain(Map.Map map, int posX, int posY) : base(map)
        {
            EntityPosition = new CPosition(posX + 8, posY + 8, 0);
            EntitySize = new Rectangle(-8, -8, 16, 16);

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/flame fountain");
            _animator.Play("idle");

            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, sprite, new Vector2(-8, -8));

            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(sprite, Values.LayerBottom));
        }

        private void Update()
        {
            // spawn a fireball
            if (_animator.CurrentFrameIndex == 1 && _lastFrameIndex == 0)
            {
                Map.Objects.SpawnObject(
                    new EnemyFlameFountainFireball(Map, new Vector2(EntityPosition.X, EntityPosition.Y + 8), new Vector2(0, 1)));
            }

            _lastFrameIndex = _animator.CurrentFrameIndex;

        }
    }
}
