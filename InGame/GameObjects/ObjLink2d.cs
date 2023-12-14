using System;
using Microsoft.Xna.Framework;
using ProjectZ.Base;
using ProjectZ.InGame.Controls;
using ProjectZ.InGame.GameObjects.Base.Systems;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects
{
    public partial class ObjLink
    {
        public bool Fall2DEntry;

        private Vector2 _moveVector2D;

        private bool Is2DMode;

        // swim stuff
        private const float MaxSwimSpeed2D = 0.65f; // speed in the original is 0.5f
        private float _swimAnimationMult;
        private int _swimDirection;
        private bool _inWater;
        private bool _wasInWater;

        // climb stuff
        private const float ClimbSpeed = 0.7f;
        private bool _isClimbing;
        private bool _wasClimbing;
        private bool _tryClimbing;
        private bool _ladderCollision;

        // jump stuff
        private double _jumpStartTime;
        private bool _playedJumpAnimation;
        private bool _waterJump;

        private bool _init;
        private bool _spikeDamage;

        private void MapInit2D()
        {
            // start climbing it the player is touching a ladder at the init position
            var box = Box.Empty;
            if (Map.Objects.Collision(_body.BodyBox.Box, Box.Empty, Values.CollisionTypes.Ladder, 3, 0, ref box))
            {
                _isWalking = true;
                _isClimbing = true;
                DirectionEntry = 1;
                UpdateAnimation2D();
            }
            else if (Fall2DEntry)
            {
                Fall2DEntry = false;
                CurrentState = State.Jumping;
                _body.Velocity.Y = 1.5f;
                _playedJumpAnimation = true;
                if (Direction != 0 && Direction != 2)
                    Direction = 2;
                DirectionEntry = Direction;
                Animation.Play("fall_" + Direction);
            }

            // move down a little bit after coming from the top
            if (DirectionEntry == 3)
                _swimVelocity.Y = 0.4f;

            _init = true;
            _swimAnimationMult = 0.75f;
            EntityPosition.Z = 0;
            _body.DeepWaterOffset = -9;
            _jumpStartTime = 0;

            _swimDirection = DirectionEntry;
            // look towards the middle of the map
            if (DirectionEntry % 2 != 0)
                _swimDirection = EntityPosition.X < Map.MapWidth * Values.TileSize / 2f ? 2 : 0;
        }

        private void Update2DFrozen()
        {
            // make sure to not fall down while frozen
            if (_isClimbing)
                _body.Velocity.Y = 0;
        }

        private void Update2D()
        {
            var initState = CurrentState;

            var box = Box.Empty;
            // is the player touching a ladder?
            _ladderCollision = Map.Objects.Collision(_body.BodyBox.Box, Box.Empty, Values.CollisionTypes.Ladder, 1, 0, ref box);

            if (!_ladderCollision && _isClimbing)
            {
                _isClimbing = false;

                if (CurrentState != State.Carrying)
                {
                    _body.Velocity.Y = 0;
                    CurrentState = State.Idle;
                }
            }

            if (!_body.IsGrounded && !_isClimbing && (!_tryClimbing || !_ladderCollision) &&
                (CurrentState == State.Idle || CurrentState == State.Blocking) &&
                !_bootsRunning)
            {
                CurrentState = State.Jumping;
                _waterJump = false;

                // if we get pushed down we change the direction in the push direction
                // this does not work for all cases but we only need if for the evil eagle boss where it should work correctly
                if (_body.LastAdditionalMovementVT.X != 0)
                    Direction = _body.LastAdditionalMovementVT.X < 0 ? 0 : 2;

                if (_wasClimbing)
                {
                    // not ontop of a ladder
                    if (SystemBody.MoveBody(_body, new Vector2(0, 2), _body.CollisionTypes | Values.CollisionTypes.LadderTop, false, false, true) == Values.BodyCollision.None)
                    {
                        SystemBody.MoveBody(_body, new Vector2(0, -2), _body.CollisionTypes | Values.CollisionTypes.LadderTop, false, false, true);

                        if (Math.Abs(_moveVector2D.X) >= Math.Abs(_moveVector2D.Y))
                            Direction = _moveVector2D.X < 0 ? 0 : 2;
                        else
                            Direction = 1;
                    }
                    // aligned with the top of the ladder
                    else
                    {
                        _body.IsGrounded = true;
                        _body.Velocity.Y = _body.Gravity2D;
                        CurrentState = initState;
                    }
                }
            }

            if (_isClimbing &&
                CurrentState != State.Attacking && CurrentState != State.PickingUp &&
                CurrentState != State.Dying && CurrentState != State.Blocking &&
                CurrentState != State.PreCarrying && CurrentState != State.Carrying &&
                CurrentState != State.Hookshot && CurrentState != State.MagicRod &&
                CurrentState != State.Powdering && CurrentState != State.Throwing)
                CurrentState = State.Idle;

            var inLava = (_body.CurrentFieldState & MapStates.FieldStates.Lava) != 0;
            _inWater = (_body.CurrentFieldState & MapStates.FieldStates.DeepWater) != 0 || inLava;

            if (_init)
                _wasInWater = _inWater;

            // need to make sure to play the animation when the player walks over a cliff
            if (_body.IsGrounded || _isClimbing)
                _playedJumpAnimation = false;

            // is the player in deep water?
            if (_inWater)
            {
                if (!_wasInWater)
                {
                    _swimDirection = Direction;
                    if (_swimDirection % 2 != 0)
                        _swimDirection = 0;
                }

                // start swimming if the player has flippers
                if (HasFlippers && !inLava)
                {
                    if (!_wasInWater)
                    {
                        _swimVelocity.X = _body.VelocityTarget.X * 0.35f;
                        _swimVelocity.Y = _isClimbing ? _body.VelocityTarget.Y * 0.35f : _body.Velocity.Y;
                        _body.Velocity = Vector3.Zero;
                    }

                    if (CurrentState != State.Attacking &&
                        CurrentState != State.PickingUp &&
                        CurrentState != State.Hookshot &&
                        CurrentState != State.Bombing &&
                        CurrentState != State.Powdering &&
                        CurrentState != State.MagicRod &&
                        CurrentState != State.Dying &&
                        CurrentState != State.PreCarrying)
                        CurrentState = State.Swimming;

                    _isClimbing = false;
                }
                else
                {
                    if (CurrentState != State.Drowning && CurrentState != State.Drowned)
                    {
                        _body.Velocity = Vector3.Zero;
                        _body.Velocity.X = _lastMoveVelocity.X * 0.25f;

                        if (CurrentState != State.Dying)
                        {
                            Game1.GameManager.PlaySoundEffect("D370-03-03");

                            CurrentState = State.Drowning;
                            _isClimbing = false;
                            _drownCounter = 650;

                            // blink in lava
                            _hitCount = inLava ? CooldownTime : 0;
                        }
                    }
                }
            }
            // jump a little bit out of the water
            else if (CurrentState == State.Swimming)
            {
                Direction = _swimDirection;
                _lastMoveVelocity.X = _body.VelocityTarget.X;

                // jump out of the water?
                if (_swimVelocity.Y < -MaxSwimSpeed2D)
                {
                    CurrentState = State.Idle;
                    Jump2D();
                }
                // just jump up a little out of the water
                else
                {
                    CurrentState = State.Jumping;
                    _body.Velocity.Y = -0.75f;
                    _playedJumpAnimation = true;
                    _waterJump = true;
                }
            }

            if (CurrentState == State.Drowning)
            {
                if (_drownCounter < 300)
                {
                    _body.Velocity = Vector3.Zero;
                    // align the player to the pixel grid
                    EntityPosition.Set(new Vector2(
                        MathF.Round(EntityPosition.X), MathF.Round(EntityPosition.Y)));
                }

                _drownCounter -= Game1.DeltaTime;
                if (_drownCounter <= 0)
                {
                    IsVisible = false;
                    CurrentState = State.Drowned;
                    _drownResetCounter = 500;
                }
            }
            else if (CurrentState == State.Drowned)
            {
                _body.Velocity = Vector3.Zero;

                _drownResetCounter -= Game1.DeltaTime;
                if (_drownResetCounter <= 0)
                {
                    CurrentState = State.Idle;
                    CanWalk = true;
                    IsVisible = true;

                    _hitCount = CooldownTime;
                    Game1.GameManager.CurrentHealth -= 2;

                    _body.CurrentFieldState = MapStates.FieldStates.None;
                    EntityPosition.Set(_drownResetPosition);
                }
            }

            _body.IgnoresZ = _inWater || _hookshotPull;

            // walk
            UpdateWalking2D();

            // swimming
            UpdateSwimming2D();

            // update the animation
            UpdateAnimation2D();

            if (_isClimbing)
                _body.Velocity.Y = 0;

            // first frame getting hit
            if (_hitCount == CooldownTime)
            {
                if (_hitVelocity != Vector2.Zero)
                    _hitVelocity.Normalize();

                _hitVelocity *= 1.75f;

                _swimVelocity *= 0.25f;

                // repell the player up and in the direction the player came from
                if (_spikeDamage)
                {
                    _hitVelocity *= 0.85f;

                    if (_moveVector2D.X < 0)
                        _hitVelocity += new Vector2(2, 0);
                    else if (_moveVector2D.X > 0)
                        _hitVelocity += new Vector2(-2, 0);

                    _body.Velocity.X = _hitVelocity.X;
                    _body.Velocity.Y = _hitVelocity.Y;
                    _hitVelocity = Vector2.Zero;
                }
            }

            _spikeDamage = false;

            if (_hitCount > 0)
                _hitVelocity *= (float)Math.Pow(0.9f, Game1.TimeMultiplier);
            else
                _hitVelocity = Vector2.Zero;

            // slows down the walk movement when the player is hit
            var moveMultiplier = MathHelper.Clamp(1f - _hitVelocity.Length(), 0, 1);

            // move the player
            if (CurrentState != State.Hookshot)
                _body.VelocityTarget = _moveVector2D * moveMultiplier + _hitVelocity;

            // remove ladder collider while climbing
            if (_isClimbing || _tryClimbing)
                _body.CollisionTypes &= ~(Values.CollisionTypes.LadderTop);
            else if (CurrentState == State.Jumping)
            {
                // only collide with the top of a ladder block
                _body.CollisionTypes |= Values.CollisionTypes.LadderTop;
            }
            else
                _body.CollisionTypes |= Values.CollisionTypes.LadderTop;

            // save the last position the player is grounded to use for the reset position if the player drowns
            if (_body.IsGrounded)
            {
                var bodyCenter = new Vector2(EntityPosition.X, EntityPosition.Y);
                // center the position
                // can lead to the position being inside something
                bodyCenter.X = (int)(bodyCenter.X / 16) * 16 + 8;

                // found new reset position?
                var bodyBox = new Box(bodyCenter.X + _body.OffsetX, bodyCenter.Y + _body.OffsetY, 0, _body.Width, _body.Height, _body.Depth);
                var bodyBoxFloor = new Box(bodyCenter.X + _body.OffsetX, bodyCenter.Y + _body.OffsetY + 1, 0, _body.Width, _body.Height, _body.Depth);
                var cBox = Box.Empty;

                // check it the player is not standing inside something; why???
                if (//!Game1.GameManager.MapManager.CurrentMap.Objects.Collision(bodyBox, Box.Empty, _body.CollisionTypes, 0, 0, ref cBox) &&
                    Map.Objects.Collision(bodyBoxFloor, Box.Empty, _body.CollisionTypes, Values.CollisionTypes.MovingPlatform, 0, 0, ref cBox))
                    _drownResetPosition = bodyCenter;
            }

            _wasClimbing = _isClimbing;
            _wasInWater = _inWater;
            _init = false;
        }

        private void UpdateAnimation2D()
        {
            var shieldString = Game1.GameManager.ShieldLevel == 2 ? "ms_" : "s_";
            if (!CarryShield)
                shieldString = "_";

            // start the jump animation
            if (CurrentState == State.Jumping && !_playedJumpAnimation)
            {
                Animation.Play("jump_" + Direction);
                _playedJumpAnimation = true;
            }

            if (_bootsHolding || _bootsRunning)
            {
                if (!_bootsRunning)
                    Animation.Play("walk" + shieldString + Direction);
                else
                {
                    // run while blocking with the shield
                    Animation.Play((CarryShield ? "walkb" : "walk") + shieldString + Direction);
                }

                Animation.SpeedMultiplier = 2.0f;
                return;
            }

            Animation.SpeedMultiplier = 1.0f;

            if ((CurrentState != State.Jumping || !Animation.IsPlaying || _waterJump) && CurrentState != State.Attacking)
            {
                if (CurrentState == State.Jumping)
                    Animation.Play("fall_" + Direction);
                else if (CurrentState == State.Idle)
                {
                    if (_isWalking || _isClimbing)
                    {
                        var newAnimation = "walk" + shieldString + Direction;

                        if (Animation.CurrentAnimation.Id != newAnimation)
                            Animation.Play(newAnimation);
                        else if (_isClimbing)
                            // continue/pause the animation
                            Animation.IsPlaying = _isWalking;
                    }
                    else
                        Animation.Play("stand" + shieldString + Direction);
                }
                else if (!_isWalking && CurrentState == State.Charging)
                    Animation.Play("stand" + shieldString + Direction);
                else if (CurrentState == State.Carrying)
                    Animation.Play((_isWalking ? "walkc_" : "standc_") + Direction);
                else if (_isWalking && CurrentState == State.Charging)
                    Animation.Play("walk" + shieldString + Direction);
                else if (CurrentState == State.Blocking)
                    Animation.Play((!_isWalking ? "standb" : "walkb") + shieldString + Direction);
                else if (CurrentState == State.Grabbing)
                    Animation.Play("grab_" + Direction);
                else if (CurrentState == State.Pulling)
                    Animation.Play("pull_" + Direction);
                else if (CurrentState == State.Swimming)
                {
                    Animation.Play("swim_2d_" + _swimDirection);
                    Animation.SpeedMultiplier = _swimAnimationMult;
                }
                // TODO: create a different sprite for 2d drowning
                else if (CurrentState == State.Drowning)
                    Animation.Play(_drownCounter > 300 ? "swim_" + _swimDirection : "dive");
            }
        }

        private void UpdateWalking2D()
        {
            _isWalking = false;

            if ((CurrentState != State.Idle && CurrentState != State.Jumping &&
                CurrentState != State.Carrying && CurrentState != State.Blocking &&
                CurrentState != State.Charging && CurrentState != State.Attacking &&
                (CurrentState != State.MagicRod || _body.IsGrounded || _isClimbing)) || _inWater)
            {
                _moveVector2D = Vector2.Zero;
                _lastBaseMoveVelocity = _moveVector2D;
                return;
            }

            var walkVelocity = Vector2.Zero;
            if (!_isLocked && (CurrentState != State.Attacking || !_body.IsGrounded))
                walkVelocity = ControlHandler.GetMoveVector2();

            var walkVelLength = walkVelocity.Length();
            var vectorDirection = ToDirection(walkVelocity);

            // start climbing?
            if (_ladderCollision && ((walkVelocity.Y != 0 && Math.Abs(walkVelocity.X) <= Math.Abs(walkVelocity.Y)) || _tryClimbing) && _jumpStartTime + 175 < Game1.TotalGameTime)
            {
                _isClimbing = true;
                _tryClimbing = false;
            }
            // try climbing down?
            else if (walkVelocity.Y > 0 && Math.Abs(walkVelocity.X) <= Math.Abs(walkVelocity.Y) && !_bootsRunning)
            {
                if (_tryClimbing && !_isHoldingSword)
                    Direction = 3;

                _tryClimbing = true;
            }
            else
                _tryClimbing = false;

            if (_isClimbing && _ladderCollision)
            {
                _moveVector2D = walkVelocity * ClimbSpeed;
                _lastMoveVelocity = new Vector2(_moveVector2D.X, 0);

                if (_isClimbing)
                    Direction = 1;
            }
            // boot running; stop if the player tries to move in the opposite direction
            else if (_bootsRunning && (walkVelLength < Values.ControllerDeadzone || vectorDirection != (Direction + 2) % 4))
            {
                if (!_bootsStop)
                    _moveVector2D = AnimationHelper.DirectionOffset[Direction] * 2;

                _lastMoveVelocity = _moveVector2D;
            }
            // normally walking on the floor
            else if (walkVelLength > Values.ControllerDeadzone)
            {
                // if the player is walking he is walking left or right
                if (walkVelocity.X != 0)
                    walkVelocity.Y = 0;

                // update the direction if not attacking/charging
                var newDirection = AnimationHelper.GetDirection(walkVelocity);

                // reset boot counter if the player changes the direction
                if (newDirection != Direction)
                    _bootsCounter %= _bootsParticleTime;
                _bootsRunning = false;

                if (CurrentState != State.Charging && CurrentState != State.Attacking && CurrentState != State.Jumping && newDirection != 3)
                    Direction = newDirection;

                if (_body.IsGrounded)
                {
                    _moveVector2D = new Vector2(walkVelocity.X, 0);
                    _lastMoveVelocity = _moveVector2D;
                }
            }
            else if (_body.IsGrounded)
            {
                _moveVector2D = Vector2.Zero;
                _lastMoveVelocity = Vector2.Zero;
            }

            // the player has momentum when he is in the air and can not be controlled directly like on the ground
            if (!_body.IsGrounded && !_isClimbing)
            {
                walkVelocity.Y = 0;

                var distance = (_lastMoveVelocity - walkVelocity * _currentWalkSpeed).Length();

                if (distance > 0 && walkVelocity != Vector2.Zero)
                {
                    // we make sure that when walkVelocity is pointing in the same direction as _lastMoveVelocity we do not decrease the velocity if walkVelocity is smaller
                    var direction = walkVelocity;
                    direction.Normalize();
                    var speed = Math.Max(walkVelocity.Length(), _lastMoveVelocity.Length());
                    _lastMoveVelocity = AnimationHelper.MoveToTarget(_lastMoveVelocity, direction * speed, 0.05f * Game1.TimeMultiplier);
                }

                _moveVector2D = _lastMoveVelocity;

                // update the direction if the player goes left or right in the air
                // only update the animation after the jump animation was played
                if (CurrentState == State.Jumping && _moveVector2D != Vector2.Zero && _playedJumpAnimation)
                {
                    var newDirection = AnimationHelper.GetDirection(_moveVector2D);
                    if (newDirection % 2 == 0)
                        Direction = newDirection;
                }
            }

            if (_moveVector2D.X != 0 || (_isClimbing && _moveVector2D.Y != 0))
                _isWalking = true;

            _lastBaseMoveVelocity = _moveVector2D;
        }

        private void UpdateSwimming2D()
        {
            if (!_inWater || CurrentState == State.Drowning || CurrentState == State.Drowned)
                return;

            // direction can only be 0 or 2 while swimming
            if (Direction % 2 != 0)
                Direction = _swimDirection;

            var moveVector = Vector2.Zero;
            if (!_isLocked && CurrentState != State.Attacking)
                moveVector = ControlHandler.GetMoveVector2();

            var moveVectorLength = moveVector.Length();
            moveVectorLength = Math.Clamp(moveVectorLength, 0, MaxSwimSpeed2D);

            if (moveVectorLength > Values.ControllerDeadzone)
            {
                moveVector.Normalize();
                moveVector *= moveVectorLength;

                // accelerate to the target velocity
                var distance = (moveVector - _swimVelocity).Length();
                var lerpPercentage = MathF.Min(1, (0.0225f * Game1.TimeMultiplier) / distance);
                _swimVelocity = Vector2.Lerp(_swimVelocity, moveVector, lerpPercentage);

                Game1.DebugText += "\n" + lerpPercentage;

                _swimAnimationMult = moveVector.Length() / MaxSwimSpeed2D;

                Direction = AnimationHelper.GetDirection(moveVector);
                if (moveVector.X != 0)
                    _swimDirection = moveVector.X < 0 ? 0 : 2;
            }
            else
            {
                // slows down and stop
                var distance = _swimVelocity.Length();
                var lerpPercentage = MathF.Min(1, (0.0225f / distance) * Game1.TimeMultiplier);
                _swimVelocity = Vector2.Lerp(_swimVelocity, Vector2.Zero, lerpPercentage);

                _swimAnimationMult = Math.Max(0.35f, _swimVelocity.Length() / MaxSwimSpeed2D);
            }

            _moveVector2D = _swimVelocity;
            _lastMoveVelocity.X = _swimVelocity.X;
        }

        private void Jump2D()
        {
            // TODO: In 2d you can adjust the jump height by pressing shorter/longer

            // swim faster
            if (CurrentState == State.Swimming)
                _swimVelocity.Y = -0.9f;

            if (CurrentState == State.Carrying ||
                (CurrentState != State.Idle &&
                 CurrentState != State.Attacking &&
                 CurrentState != State.Charging))
                return;


            if (!_body.IsGrounded && !_wasInWater && !_isClimbing)
                return;

            if (_isClimbing)
            {
                if (Math.Abs(_moveVector2D.X) > Math.Abs(_moveVector2D.Y))
                    Direction = _moveVector2D.X < 0 ? 0 : 2;
                else
                    Direction = 1;
            }

            Game1.GameManager.PlaySoundEffect("D360-13-0D");

            _jumpStartTime = Game1.TotalGameTime;

            _body.IsGrounded = false;
            _body.Velocity.Y = _isClimbing ? -1.5f : -1.9f;
            _moveVector2D = Vector2.Zero;


            _isClimbing = false;
            _waterJump = false;

            // while attacking the player can still jump but without the animation
            if (CurrentState != State.Attacking &&
                CurrentState != State.Charging)
            {
                _playedJumpAnimation = false;
                CurrentState = State.Jumping;
            }
            else
            {
                _playedJumpAnimation = true;
            }
        }

        private void OnMoveCollision2D(Values.BodyCollision collision)
        {
            // prevent the body from trying to move up and directly falling down in the next step
            if ((collision & Values.BodyCollision.Horizontal) != 0 && !_isClimbing)
                _body.SlideOffset = Vector2.Zero;

            // collision with the ground
            if ((collision & Values.BodyCollision.Bottom) != 0)
            {
                // we cant use the check because the player can attack while jumping and avoid the jump animation the next time
                if (CurrentState == State.Jumping ||
                    CurrentState == State.BootKnockback)
                {
                    CurrentState = State.Idle;
                    Game1.GameManager.PlaySoundEffect("D378-07-07");
                }
            }
            // collision with the ceiling
            else if ((collision & Values.BodyCollision.Top) != 0)
            {
                _body.Velocity.Y = 0;
            }
            else if ((collision & Values.BodyCollision.Horizontal) != 0)
            {
                _lastMoveVelocity = Vector2.Zero;
                _swimVelocity.X = 0;
            }

            if ((collision & Values.BodyCollision.Vertical) != 0)
            {
                _hitVelocity.Y = 0;
                _swimVelocity.Y = 0;
            }
        }

        public bool IsClimbing()
        {
            return _isClimbing;
        }

        public bool IsInWater2D()
        {
            return _inWater;
        }

        public void InflictSpikeDamage2D()
        {
            _spikeDamage = true;
        }
    }
}
