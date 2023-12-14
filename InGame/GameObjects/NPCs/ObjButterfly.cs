using System;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.NPCs
{
    internal class ObjButterfly : GameObject
    {
        private Animator _animator;
        private readonly Vector2 _startPosition;
        private Vector2 _direction;

        private float _currentRotation;
        private float _directionChange;

        private float _currentSpeed;
        private float _lastSpeed;
        private float _speedGoal;

        private float _flyCounter;

        private int _flyTime;

        private const int MinSpeed = 25;
        private const int MaxSpeed = 45;

        // the butterfly will stay around this distance from the start point
        private int _startDistance;

        public ObjButterfly() : base("butterfly") { }

        public ObjButterfly(Map.Map map, int posX, int posY) : base(map)
        {
            _startPosition = new Vector2(posX, posY);
            EntityPosition = new CPosition(posX + 8, posY + 8 + 15, 15);
            EntitySize = new Rectangle(-4, -24, 8, 24);

            _startDistance = Game1.RandomNumber.Next(25, 100);
            _currentRotation = (Game1.RandomNumber.Next(0, 100) / 100f) * (float)(Math.PI * 2);

            _currentSpeed = Game1.RandomNumber.Next(MinSpeed, MaxSpeed) / 100f;
            _lastSpeed = _currentSpeed;
            _speedGoal = Game1.RandomNumber.Next(MinSpeed, MaxSpeed) / 100f;

            _animator = AnimatorSaveLoad.LoadAnimator("NPCs/butterfly");
            _animator.Play("idle");

            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, sprite, Vector2.Zero);

            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(sprite, Values.LayerTop));
        }

        public void Update()
        {
            _flyCounter -= Game1.DeltaTime;

            if (_flyCounter < 0)
            {
                _flyTime = Game1.RandomNumber.Next(500, 1000);
                _flyCounter += _flyTime;

                // set a new speed goal
                _lastSpeed = _speedGoal;
                _speedGoal = Game1.RandomNumber.Next(MinSpeed, MaxSpeed) / 100f;

                var startDifference = EntityPosition.Position - _startPosition;
                var targetRotation = Math.Atan2(startDifference.Y, startDifference.X);
                var randomDirection = ((Game1.RandomNumber.Next(0, 20) - 10) / 6f) * ((float)Math.PI / (60 * (_flyCounter / 1000f)));

                var rotationDifference = (float)targetRotation - _currentRotation;
                while (rotationDifference < 0)
                    rotationDifference += (float)Math.PI * 2;
                rotationDifference = rotationDifference % (float)(Math.PI * 2);
                rotationDifference -= (float)Math.PI;

                var newRotation = rotationDifference / (60 * (_flyCounter / 1000f));

                // calculate the new rotation direction of the butterfly
                // the farther away it is from the start position the more likely it is to rotate to face the start position
                _directionChange = MathHelper.Lerp(randomDirection, newRotation, startDifference.Length() / _startDistance);
            }

            // update the speed
            _currentSpeed = MathHelper.Lerp(_speedGoal, _lastSpeed, _flyCounter / (float)_flyTime);

            // update direction
            _currentRotation += _directionChange * Game1.TimeMultiplier;
            _currentRotation = _currentRotation % (float)(Math.PI * 2);
            _direction = new Vector2((float)Math.Cos(_currentRotation), (float)Math.Sin(_currentRotation)) *
                         (_animator.CurrentFrameIndex == 0 ? _currentSpeed : _currentSpeed * 1.125f);
            EntityPosition.Move(_direction);
        }
    }
}