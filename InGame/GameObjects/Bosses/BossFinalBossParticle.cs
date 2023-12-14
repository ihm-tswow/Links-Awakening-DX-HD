using System;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Bosses
{
    internal class BossFinalBossParticle : GameObject
    {
        private readonly BodyComponent _body;
        private readonly CSprite _sprite;

        private float _transparency = 1;

        public BossFinalBossParticle(Map.Map map, Vector2 position, Vector2 velocity) : base(map)
        {
            EntityPosition = new CPosition(position.X, position.Y, 0);
            EntitySize = new Rectangle(-8, -8, 16, 16);

            var animator = AnimatorSaveLoad.LoadAnimator("Nightmares/nightmare particle");
            animator.Play("idle");

            _sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(animator, _sprite, Vector2.Zero);

            _body = new BodyComponent(EntityPosition, -5, -5, 10, 10, 8)
            {
                Velocity = new Vector3(velocity, 0),
                Drag = 0.94f,
                CollisionTypes = Values.CollisionTypes.None
            };

            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(BodyComponent.Index, _body);
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(_sprite, Values.LayerPlayer));
        }

        private void Update()
        {
            // fade away
            if (_body.Velocity.Length() < 0.125f)
            {
                _transparency -= Game1.TimeMultiplier * 0.165f;
                _sprite.Color = Color.White * _transparency;

                if (_transparency <= 0)
                    Map.Objects.DeleteObjects.Add(this);
            }
        }
    }
}