using System.Collections.Generic;
using System.Globalization;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.NPCs
{
    internal class ObjPhotoMouse : GameObject
    {
        struct MoveStep
        {
            public float MoveSpeed;
            public Vector2 Offset;
        }
        private Queue<MoveStep> _nextMoveStep = new Queue<MoveStep>();

        public readonly BodyComponent Body;
        private readonly Animator _animator;
        private readonly CSprite _sprite;
        private readonly BodyDrawShadowComponent _shadowComponent;
        private readonly InteractComponent _interactComponent;
        private readonly BodyCollisionComponent _collisionComponent;

        private readonly string _spawnCondition;
        private readonly string _dialogId;
        private string _currentAnimation;

        private static int SwimTime = 2000;
        private Vector2 _spawnPosition;
        private double _swimCounter;
        private int _swimDirection = -1;
        private bool _swimMode;
        private bool _isPulled;

        private bool _isMoving;
        private Vector2 _targetPosition;
        private float _moveSpeed;

        private float _fadeTime;
        private float _fadeCounter;

        private bool _photoMode;
        private bool _blockedExit;
        private bool _movingToPlayer;
        private bool _isActive = true;

        public ObjPhotoMouse() : base("photo_mouse") { }

        public ObjPhotoMouse(Map.Map map, int posX, int posY, string spawnCondition, string dialogId) : base(map)
        {
            EntityPosition = new CPosition(posX + 8, posY + 15, 0);
            EntitySize = new Rectangle(-8, -15, 16, 16);

            _spawnCondition = spawnCondition;
            _dialogId = dialogId;
            _animator = AnimatorSaveLoad.LoadAnimator("NPCs/photo_mouse");
            _animator.Play("stand_0");

            _sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, _sprite, new Vector2(0, 1));

            Body = new BodyComponent(EntityPosition, -7, -12, 14, 12, 8);

            if (map.Is2dMap)
            {
                Body.IgnoresZ = true;
                _spawnPosition = EntityPosition.Position;
                _swimMode = true;
                _animator.Play("swim_" + _swimDirection);
                Body.OffsetX = -5;
                Body.Width = 10;
                Body.DragAir = 0.95f;
            }

            AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(KeyChanged));
            AddComponent(BodyComponent.Index, Body);
            // only the player should collide with the npc
            AddComponent(CollisionComponent.Index, _collisionComponent = new BodyCollisionComponent(Body, Values.CollisionTypes.Enemy | Values.CollisionTypes.PushIgnore));
            AddComponent(InteractComponent.Index, _interactComponent = new InteractComponent(new CBox(EntityPosition, -7, -6 - 1, 2, 2, 8), Interact));
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new BodyDrawComponent(Body, _sprite, Values.LayerPlayer) { WaterOutline = false });
            AddComponent(DrawShadowComponent.Index, _shadowComponent = new BodyDrawShadowComponent(Body, _sprite));

            if (!string.IsNullOrEmpty(spawnCondition))
                SetActive(false);
        }

        private void SetActive(bool active)
        {
            _isActive = active;
            _collisionComponent.IsActive = active;
            _sprite.IsVisible = active;
            _shadowComponent.IsActive = active;
        }

        private void Update()
        {
            UpdateMoving();

            UpdateFade();

            UpdateSwimming();

            // finished playing
            if (_currentAnimation != null && !_animator.IsPlaying)
            {
                _currentAnimation = null;
                Game1.GameManager.SaveManager.SetString(_dialogId + "Finished", "1");
            }

            if (!_movingToPlayer && _photoMode && MapManager.ObjLink.Direction == 3)
            {
                // check if the player is standing on the correct position
                var positioned = Game1.GameManager.SaveManager.GetString("photo_house_positioned");
                if (!string.IsNullOrEmpty(positioned) && positioned == "1")
                {
                    _movingToPlayer = true;

                    // move to the player
                    _nextMoveStep.Enqueue(new MoveStep() { MoveSpeed = 1, Offset = new Vector2(80 - EntityPosition.X, 0) });
                    _nextMoveStep.Enqueue(new MoveStep() { MoveSpeed = 1, Offset = new Vector2(0, 54 - EntityPosition.Y) });

                    StartMoving();

                    Game1.GameManager.StartDialogPath("photo_mouse_photo_0");
                }
            }
        }

        private void UpdateSwimming()
        {
            if (!_swimMode || _isPulled)
                return;

            _swimCounter -= Game1.DeltaTime;
            if (_swimCounter < SwimTime - 450)
            {
                var catchMode = Game1.GameManager.SaveManager.GetString("mouse_catch", "0");
                if (EntityPosition.X < _spawnPosition.X - 8 && catchMode == "1")
                {
                    Body.Velocity = Vector3.Zero;
                    _isPulled = true;
                    _animator.Play("pulled");
                    Game1.GameManager.SaveManager.SetString("mouse_pulled", "1");
                }
            }

            if (_swimCounter < 0)
            {
                _swimCounter += SwimTime;
                // change direction
                if ((_swimDirection > 0 && _spawnPosition.X + 8 < EntityPosition.X) ||
                    (_swimDirection < 0 && EntityPosition.X < _spawnPosition.X - 8))
                {
                    _swimDirection = -_swimDirection;
                    _animator.Play("swim_" + _swimDirection);
                }

                Body.Velocity.X = _swimDirection;
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

                if (_currentAnimation == null && !_isPulled)
                {
                    var dir = AnimationHelper.GetDirection(targetDistance);
                    _animator.Play("walk_" + dir);
                }
            }
            // finished walking
            else
            {
                Body.VelocityTarget = Vector2.Zero;
                EntityPosition.Set(_targetPosition);
                _animator.Pause();

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

        private bool Interact()
        {
            // only allow interaction from the left side to allow the pushing to work
            if (!_isActive || (!_blockedExit && MapManager.ObjLink.Direction != 2))
                return false;

            Game1.GameManager.StartDialogPath(_dialogId);
            return true;
        }

        private void SetVisibility(bool visible)
        {
            _sprite.IsVisible = visible;
            _shadowComponent.IsActive = visible;
        }

        private void KeyChanged()
        {
            var photoMode = Game1.GameManager.SaveManager.GetString("photo_house_blocked");
            if (!string.IsNullOrEmpty(photoMode))
            {
                _photoMode = photoMode == "1";
                _blockedExit = true;
                _interactComponent.BoxInteractabel = Body.BodyBox;
            }

            var photoFlash = Game1.GameManager.SaveManager.GetString("photo_flash");
            if (photoFlash != null)
            {
                Map.Objects.SpawnObject(new ObjPhotoFlash(Map));
                Game1.GameManager.SaveManager.RemoveString("photo_flash");
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
                    _animator.Play(_currentAnimation);
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

                    StartMoving();
                }

                Game1.GameManager.SaveManager.RemoveString(moveString);
            }

            if (!string.IsNullOrEmpty(_spawnCondition))
            {
                var spawnValue = Game1.GameManager.SaveManager.GetString(_spawnCondition);
                if (!string.IsNullOrEmpty(spawnValue) && spawnValue == "1")
                {
                    SetActive(true);
                    Game1.GameManager.SaveManager.RemoveString(spawnValue);
                }
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

        private void StartMoving()
        {
            if (_isMoving)
                return;

            _isMoving = true;
            DequeueMove();
            SetMovingString(true);
            Body.CollisionTypes = Values.CollisionTypes.None;
        }

        private void SetMovingString(bool state)
        {
            Game1.GameManager.SaveManager.SetString(_dialogId + "Moving", state ? "1" : "0");
        }
    }
}