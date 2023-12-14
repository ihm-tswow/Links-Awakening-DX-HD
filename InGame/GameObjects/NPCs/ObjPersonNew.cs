using System.Collections.Generic;
using System.Globalization;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.NPCs
{
    internal class ObjPersonNew : GameObject
    {
        struct MoveStep
        {
            public float MoveSpeed;
            public Vector2 Offset;
        }
        private Queue<MoveStep> _nextMoveStep = new Queue<MoveStep>();

        public BodyComponent Body;
        public readonly Animator Animator;
        private readonly CSprite _sprite;
        private readonly BodyDrawComponent _drawComponent;
        private readonly BodyDrawShadowComponent _shadowComponent;
        private readonly BodyCollisionComponent _collisionComponent;
        private readonly InteractComponent _interactionComponent;

        private readonly string _dialogId;
        private string _currentAnimation;
        private string _spawnCondition;
        private float _lookCounter;
        private int _lookRange = 32;
        private bool _directionMode = true;

        private bool _isMoving;
        private Vector2 _targetPosition;
        private float _moveSpeed;

        private float _fadeTime;
        private float _fadeCounter;

        private float _jumpTime;
        private float _jumpCounter;

        public ObjPersonNew() : base("person") { }

        public ObjPersonNew(Map.Map map, int posX, int posY, string spawnCondition, string animationId, string dialogId, string animationName, Rectangle bodyRectangle) : base(map)
        {
            if (string.IsNullOrEmpty(animationId))
            {
                IsDead = true;
                return;
            }

            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(bodyRectangle.X - bodyRectangle.Width / 2, bodyRectangle.Y - bodyRectangle.Height, bodyRectangle.Width, bodyRectangle.Height);

            _spawnCondition = spawnCondition;
            _dialogId = dialogId;
            Animator = AnimatorSaveLoad.LoadAnimator("NPCs/" + animationId);

            if (Animator == null)
            {
                IsDead = true;
                return;
            }

            _sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(Animator, _sprite, Vector2.Zero);

            Body = new BodyComponent(EntityPosition,
                bodyRectangle.X - bodyRectangle.Width / 2, bodyRectangle.Y - bodyRectangle.Height, bodyRectangle.Width, bodyRectangle.Height, bodyRectangle.Height)
            {
                Gravity = -0.15f,
            };

            AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(OnKeyChange));
            AddComponent(BodyComponent.Index, Body);
            // only the player should collide with the npc
            AddComponent(CollisionComponent.Index, _collisionComponent = new BodyCollisionComponent(Body, Values.CollisionTypes.Enemy | Values.CollisionTypes.PushIgnore));
            if (!string.IsNullOrEmpty(_dialogId))
                AddComponent(InteractComponent.Index, _interactionComponent = new InteractComponent(Body.BodyBox, Interact));
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, _drawComponent = new BodyDrawComponent(Body, _sprite, Values.LayerPlayer) { WaterOutline = false });
            AddComponent(DrawShadowComponent.Index, _shadowComponent = new BodyDrawShadowComponent(Body, _sprite));

            if (animationName == "pHidden")
            {
                SetVisibility(false);
            }
            else if (!string.IsNullOrEmpty(animationName))
            {
                _directionMode = false;
                Animator.Play(animationName);
            }
            else
            {
                Animator.Play("stand_3");
            }

            if (!string.IsNullOrEmpty(_spawnCondition))
            {
                var spawnValue = Game1.GameManager.SaveManager.GetString(_spawnCondition);
                if (spawnValue != "1")
                    SetActive(false);
            }
        }

        private void SetActive(bool isActive)
        {
            _collisionComponent.IsActive = isActive;
            _interactionComponent.IsActive = isActive;
            _drawComponent.IsActive = isActive;
            _shadowComponent.IsActive = isActive;
        }

        private void Update()
        {
            UpdateMoving();

            UpdateFade();

            JumpMode();

            _lookCounter -= Game1.DeltaTime;
            if (!_isMoving && _directionMode && _lookCounter < 0)
            {
                _lookCounter += 750;
                UpdateLookAnimation();
            }

            // finished playing
            if (_currentAnimation != null && !Animator.IsPlaying)
            {
                _currentAnimation = null;
                Game1.GameManager.SaveManager.SetString(_dialogId + "Finished", "1");
            }
        }

        private void UpdateLookAnimation()
        {
            var playerDistance = new Vector2(
                MapManager.ObjLink.EntityPosition.X - (EntityPosition.X),
                MapManager.ObjLink.EntityPosition.Y - (EntityPosition.Y - 4));

            var dir = 3;

            // rotate in the direction of the player
            if (playerDistance.Length() < _lookRange)
                dir = AnimationHelper.GetDirection(playerDistance);

            // look at the player
            if (_currentAnimation == null)
            {
                var animationIndex = Animator.GetAnimationIndex("stand_" + dir);
                if (animationIndex >= 0)
                    Animator.Play(animationIndex);
                else
                    Animator.Play("stand_" + (playerDistance.Y < 0 ? "1" : "3"));
            }
        }

        private void UpdateMoving()
        {
            if (!_isMoving)
                return;

            // move towards the target position
            var targetDistance = _targetPosition - EntityPosition.Position;
            if (targetDistance.Length() > _moveSpeed * Game1.TimeMultiplier)
            {
                targetDistance.Normalize();
                Body.VelocityTarget = targetDistance * _moveSpeed;

                if (_currentAnimation == null)
                {
                    var dir = AnimationHelper.GetDirection(targetDistance);
                    Animator.Play("walk_" + dir);
                }
            }
            // finished walking
            else
            {
                _lookCounter = 0;
                Body.VelocityTarget = Vector2.Zero;
                EntityPosition.Set(_targetPosition);

                if (_nextMoveStep.Count > 0)
                    DequeueMove();
                else
                {
                    _isMoving = false;
                    SetMovingString(false);
                }
            }
        }

        private void DequeueMove()
        {
            var move = _nextMoveStep.Dequeue();
            _moveSpeed = move.MoveSpeed;
            _targetPosition = EntityPosition.Position + move.Offset;
        }

        private void UpdateFade()
        {
            if (_fadeTime < 0)
            {
                _fadeCounter += Game1.DeltaTime;
                if (_fadeCounter >= -_fadeTime)
                    _fadeCounter = -_fadeTime;

                var percentage = _fadeCounter / -_fadeTime;
                _sprite.Color = Color.White * percentage;
                _shadowComponent.Transparency = percentage;

                if (_fadeCounter >= -_fadeTime)
                    _fadeTime = 0;
            }
            else if (_fadeTime > 0)
            {
                _fadeCounter -= Game1.DeltaTime;

                if (_fadeCounter <= 0)
                    Map.Objects.DeleteObjects.Add(this);
                else
                {
                    var percentage = _fadeCounter / _fadeTime;
                    _sprite.Color = Color.White * percentage;
                    _shadowComponent.Transparency = percentage;
                }
            }
        }

        private void JumpMode()
        {
            if (_jumpTime <= 0)
                return;

            _jumpCounter -= Game1.DeltaTime;
            if (_jumpCounter < 0)
            {
                _jumpCounter += _jumpTime;
                Body.Velocity.Z = 1.125f;
            }
        }

        public void DisableRotating()
        {
            _directionMode = false;
        }

        private bool Interact()
        {
            if (!_isMoving && _directionMode)
                UpdateLookAnimation();

            Game1.GameManager.StartDialogPath(_dialogId);
            
            return true;
        }

        private void SetVisibility(bool visible)
        {
            _sprite.IsVisible = visible;
            _shadowComponent.IsActive = visible;
        }

        private void OnKeyChange()
        {
            if (!string.IsNullOrEmpty(_spawnCondition))
            {
                var spawnValue = Game1.GameManager.SaveManager.GetString(_spawnCondition);
                if (spawnValue == "1")
                    SetActive(true);
            }

            // start new animation?
            var animationString = _dialogId + "Animation";
            var animationValues = Game1.GameManager.SaveManager.GetString(animationString);
            if (animationValues != null)
            {
                if (animationValues == "-")
                {
                    _currentAnimation = null;
                }
                else if (animationValues != "")
                {
                    SetVisibility(true);
                    _currentAnimation = animationValues;
                    Animator.Play(_currentAnimation);
                }
                else
                {
                    SetVisibility(false);
                    _currentAnimation = null;
                }

                Game1.GameManager.SaveManager.RemoveString(animationString);
            }

            // start moving?
            var moveString = _dialogId + "Move";
            var moveValue = Game1.GameManager.SaveManager.GetString(moveString);
            if (moveValue != null)
            {
                // offsetX; offsetY; movementSpeed
                var split = moveValue.Split(',');
                if (split.Length == 3)
                {
                    var offsetX = int.Parse(split[0]);
                    var offsetY = int.Parse(split[1]);
                    var moveSpeed = float.Parse(split[2], CultureInfo.InvariantCulture);

                    _nextMoveStep.Enqueue(new MoveStep() { MoveSpeed = moveSpeed, Offset = new Vector2(offsetX, offsetY) });

                    if (!_isMoving)
                    {
                        _isMoving = true;
                        DequeueMove();
                        SetMovingString(true);
                        Body.CollisionTypes = Values.CollisionTypes.None;
                    }
                }

                Game1.GameManager.SaveManager.RemoveString(moveString);
            }

            // start jumping?
            var jumpString = _dialogId + "Jump";
            var jumpValue = Game1.GameManager.SaveManager.GetString(jumpString);
            if (!string.IsNullOrEmpty(jumpValue))
            {
                var split = jumpValue.Split(',');
                if (split.Length == 1)
                {
                    _jumpTime = int.Parse(jumpValue);
                }
                else
                {
                    // jump one time
                    Body.Velocity.Z = float.Parse(split[0], CultureInfo.InvariantCulture);
                    Body.Gravity = float.Parse(split[1], CultureInfo.InvariantCulture);
                }
                Game1.GameManager.SaveManager.RemoveString(jumpString);
            }

            // change look range?
            var rangeString = _dialogId + "Range";
            var rangeValue = Game1.GameManager.SaveManager.GetString(rangeString);
            if (!string.IsNullOrEmpty(rangeValue))
            {
                _lookRange = int.Parse(rangeValue);
                Game1.GameManager.SaveManager.RemoveString(rangeString);
            }

            // start fading away?
            var fadeString = _dialogId + "Fade";
            var fadeValue = Game1.GameManager.SaveManager.GetString(fadeString);
            if (!string.IsNullOrEmpty(fadeValue))
            {
                // negative value -> fade in
                // positive value -> fade out
                _fadeTime = int.Parse(fadeValue);
                _fadeCounter = _fadeTime;
                UpdateFade();

                Game1.GameManager.SaveManager.RemoveString(fadeString);
            }
        }

        private void SetMovingString(bool state)
        {
            Game1.GameManager.SaveManager.SetString(_dialogId + "Moving", state ? "1" : "0");
        }
    }
}