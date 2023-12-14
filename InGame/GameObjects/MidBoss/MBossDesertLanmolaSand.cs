using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.MidBoss
{
    internal class MBossDesertLanmolaSand : GameObject
    {
        private readonly Animator _animator;
        private Vector3 _velocity;

        public MBossDesertLanmolaSand(Map.Map map, Vector2 position, bool mirrorH) : base(map)
        {
            EntityPosition = new CPosition(position.X, position.Y, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            _animator = AnimatorSaveLoad.LoadAnimator("MidBoss/desertLanmola");

            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, sprite, new Vector2(0, 0))
            {
                MirroredH = mirrorH
            };
            
            _animator.Play("sand_up");

            _velocity = new Vector3(-0.55f, 0, 1.125f);
            if (mirrorH)
                _velocity.X = -_velocity.X;

            AddComponent(AnimationComponent.Index, animationComponent);
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(sprite, Values.LayerPlayer));
        }

        private void Update()
        {
            EntityPosition.Set(EntityPosition.Position + new Vector2(_velocity.X, _velocity.Y) * Game1.TimeMultiplier);

            EntityPosition.Z += _velocity.Z * Game1.TimeMultiplier;
            _velocity.Z -= Game1.TimeMultiplier * 0.1f;

            // despawn
            if (EntityPosition.Z < 0)
            {
                Map.Objects.DeleteObjects.Add(this);
                return;
            }

            if (_velocity.Z < 0)
                _animator.Play("sand_down");
        }
    }
}