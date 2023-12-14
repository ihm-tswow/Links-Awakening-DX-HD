using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;
using System;

namespace ProjectZ.InGame.GameObjects.NPCs
{
    internal class ObjBees : GameObject
    {
        private readonly BodyComponent _body;
        private readonly Animator _animator;
        private readonly GameObject _targetObject;
        private readonly CSprite _sprite;
        private Vector2 _targetOffset;
        private Vector2 _directionOffset;

        private float _acceleration = 0.065f;
        private float _moveSpeed = 1;
        private float _offsetSpeed = 0.025f;
        private float _targetDist = 12;
        private int _offsetDist = 4;

        private float _fadeTime;
        private float _fadeCounter;
        private float _soundCounter;

        private bool _nearTarget;
        private bool _followMode;
        private bool _angryMode;
        private bool _playSound;

        public ObjBees(Map.Map map, Vector2 position, GameObject targetObject, bool playSound) : base(map)
        {
            EntityPosition = new CPosition(position.X, position.Y, 0);
            EntitySize = new Rectangle(-3, -5, 7, 7);

            _targetObject = targetObject;
            _playSound = playSound;

            _animator = AnimatorSaveLoad.LoadAnimator("NPCs/bee");
            _animator.Play("idle");

            _body = new BodyComponent(EntityPosition, -3, -5, 7, 7, 8)
            {
                CollisionTypes = Values.CollisionTypes.None,
                VelocityTarget = new Vector2(-0.75f, 0.65f)
            };

            _sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, _sprite, new Vector2(0, 0));

            AddComponent(BodyComponent.Index, _body);
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(AnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(_sprite, Values.LayerTop));
        }

        public void FadeAway(float fadeTime)
        {
            _fadeTime = fadeTime;
        }

        public void SetAngryMode()
        {
            _angryMode = true;
            _acceleration = 0.065f * 3.5f;
            _moveSpeed = 3;
            _targetDist = 8;
            _offsetSpeed = 0.05f;
            _offsetDist = 1;
        }

        public void SetFollowMode(Vector2 offset)
        {
            _followMode = true;
            _targetOffset = offset;
            _acceleration = 0.35f + Game1.RandomNumber.Next(0, 11) / 200f;
            _moveSpeed = 2.1f + Game1.RandomNumber.Next(0, 21) / 100f;
            _targetDist = 8;
            _offsetSpeed = 0.05f;
            _offsetDist = 1;
        }

        private void Update()
        {
            UpdateFade();

            if (_angryMode && _playSound)
            {
                _soundCounter -= Game1.DeltaTime;
                if(_soundCounter < 0)
                {
                    _soundCounter += 25;
                    Game1.GameManager.PlaySoundEffect("D360-34-22", true, MathF.Sin((float)(Game1.TotalGameTime / 65)) * 0.125f + 0.875f, -MathF.Sin((float)(Game1.TotalGameTime / 65)) * 0.25f - 0.125f);
                }
            }

            // move towards the target
            var targetDirection = (_targetObject.EntityPosition.Position + _targetOffset) - EntityPosition.Position;
            var targetDistance = targetDirection.Length();
            if (targetDistance > _targetDist)
            {
                targetDirection.Normalize();

                if (!_nearTarget)
                {
                    _nearTarget = true;
                    _directionOffset = new Vector2(-targetDirection.Y, targetDirection.X);
                }
                _directionOffset = new Vector2(-targetDirection.Y, targetDirection.X);

                _body.VelocityTarget = AnimationHelper.MoveToTarget(_body.VelocityTarget, targetDirection * _moveSpeed, _acceleration * Game1.TimeMultiplier);
                _body.VelocityTarget += _directionOffset * _offsetSpeed * Game1.TimeMultiplier;
            }
            else if (targetDirection != Vector2.Zero)
            {
                if (_nearTarget)
                {
                    _nearTarget = false;
                    if (!_followMode)
                        _targetOffset = new Vector2(
                            Game1.RandomNumber.Next(0, _offsetDist * 2 + 1) - _offsetDist,
                            Game1.RandomNumber.Next(0, _offsetDist * 2 + 1) - _offsetDist);
                }
            }
        }

        private void UpdateFade()
        {
            if (_fadeTime > 0)
            {
                _fadeCounter += Game1.DeltaTime;
                if (_fadeCounter >= _fadeTime)
                    Map.Objects.DeleteObjects.Add(this);

                var percentage = _fadeCounter / _fadeTime;
                _sprite.Color = Color.White * (1 - percentage);
            }
        }
    }
}