using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Systems;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.GameSystems;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.NPCs
{
    public class ObjMarin : GameObjectFollower
    {
        private enum States { Idle, Sequence, Fade, Singing, SingingFinal, AnimalSinging, SingingDuo, PostDuo, SingingWalrus, FollowPlayer, Jumping, Saved, DungeonReturn };
        private States _currentState = States.Idle;

        struct MoveStep
        {
            public bool OffsetMode;
            public float MoveSpeed;
            public Vector2 Offset;
            public Vector2 Position;
        }
        private Queue<MoveStep> _nextMoveStep = new Queue<MoveStep>();

        public override bool IsActive
        {
            get => base.IsActive;
            set
            {
                base.IsActive = value;
                if (value)
                    Activate();
            }
        }

        public bool IsHidden;
        private bool _wasHidden;

        public bool EnterDungeonMessage;

        private bool _enterDungeonMessage;
        private bool _dungeonLeaveSequence;

        private Rectangle _field;

        private Vector2 _returnStart;
        private Vector2 _returnEnd;

        private Vector2 _railJumpStartPosition;
        private Vector2 _railJumpTargetPosition;
        private float _railJumpSpeed;
        private float _railJumpHeight;
        private float _railJumpPercentage;
        private bool _isRailJumping;
        private float _holeAbsorbCounter;
        private bool _holeAbsorb;

        private bool _fountainSequence;
        private bool _fountainMouse;
        private bool _fountainSeqInit;

        private float _returnCounter;
        private bool _returnInit;
        private bool _returnFinished;

        private double _dungeonEnterTime;
        private int _dungeonEnterLives;

        private readonly BodyComponent _body;
        private readonly Animator _animator;
        private readonly BodyDrawComponent _bodyDrawComponent;
        private readonly CSprite _sprite;
        private readonly BodyDrawShadowComponent _shadowComponent;

        private readonly DictAtlasEntry _spriteNote;

        private Vector2 _followVelocity;

        private Vector2 _targetPosition;
        private float _moveSpeed;

        private float _fadeTime;
        private float _fadeCounter;

        private float _noteCount;
        private float _noteEndTime;
        private int _lastDirection = -1;
        private int _cycleTime = 1000;

        private float _duoCounter;
        private int _duoIndex;
        private int _walkDirection;

        private bool _isSinging;
        private bool _isSingingWithSound;

        private bool _helpDialogShown;
        private bool _isPulled;
        private int _pullOffsetY;

        private bool _isMoving;

        public ObjMarin() : base("marin") { }

        public ObjMarin(Map.Map map, int posX, int posY) : base(map)
        {
            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            _spriteNote = Resources.GetSprite("note");

            _animator = AnimatorSaveLoad.LoadAnimator("NPCs/marin");

            _sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, _sprite, Vector2.Zero);

            if (map != null)
                _field = map.GetField(posX, posY);

            _body = new BodyComponent(EntityPosition, -7, -10, 14, 10, 8)
            {
                //CollisionTypes = Values.CollisionTypes.None
                MoveCollision = OnCollision,
                IgnoreHoles = true,
                Gravity = -0.15f
            };
            _bodyDrawComponent = new BodyDrawComponent(_body, _sprite, 1);

            var mariaState = Game1.GameManager.SaveManager.GetString("maria_state");

            AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(KeyChanged));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(CollisionComponent.Index, new BodyCollisionComponent(_body, Values.CollisionTypes.Normal | Values.CollisionTypes.PushIgnore));
            AddComponent(InteractComponent.Index, new InteractComponent(_body.BodyBox, Interact));
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerPlayer, EntityPosition));
            AddComponent(DrawShadowComponent.Index, _shadowComponent = new BodyDrawShadowComponent(_body, _sprite));
        }

        public override void Init()
        {
            Game1.GameManager.SaveManager.RemoveString("marin_sing_position");

            _enterDungeonMessage = false;
            EnterDungeonMessage = false;

            _sprite.IsVisible = true;

            // fall with the player from the ceiling?
            if (MapManager.ObjLink.EntityPosition.Z > 0)
            {
                EntityPosition.Z = MapManager.ObjLink.EntityPosition.Z;
                _body.Velocity.Z = 0.5f;
                _body.IsGrounded = false;
                _fountainSequence = true;
                _fountainMouse =
                    Game1.GameManager.SaveManager.GetString("photoMouseActive", "0") == "1" &&
                    Game1.GameManager.SaveManager.GetString("photo_sequence_fountain", "0") == "0";
                _fountainSeqInit = false;

                _followVelocity = Vector2.Zero;
                _walkDirection = 3;
                _animator.Play("jump_down_" + _walkDirection);
            }

            // is disabled for the current room?
            var wasHidden = IsHidden;
            if (IsHidden)
            {
                IsHidden = false;
                base.IsActive = false;
            }
            else if (_wasHidden)
            {
                base.IsActive = true;
            }
            _wasHidden = wasHidden;

            if (_dungeonLeaveSequence)
                base.IsActive = true;

            if (IsActive)
                Activate();
        }

        public void LeaveDungeonSequence(Vector2 position)
        {
            _returnInit = true;
            _returnFinished = false;
            _returnCounter = 0;

            _returnStart = new Vector2(position.X + 40, position.Y + 24);
            _returnEnd = new Vector2(position.X + 8, position.Y + 28);

            _dungeonLeaveSequence = true;
            Game1.GameManager.SaveManager.SetString("maria_dungeon", "0");

            _animator.Play("wait");
        }

        public void OnAppendMapChange()
        {
            if (_currentState == States.FollowPlayer && (_enterDungeonMessage || EnterDungeonMessage))
            {
                _body.VelocityTarget = Vector2.Zero;
                _currentState = States.Idle;
                Game1.GameManager.SaveManager.SetString("maria_dungeon", "1");
                Game1.GameManager.StartDialogPath("marin_dungeon");
                _dungeonEnterTime = Game1.TotalGameTime;
                _dungeonEnterLives = Game1.GameManager.CurrentHealth;

                // stop walking
                _animator.Stop();
            }
        }

        public override void SetPosition(Vector2 position)
        {
            if (_currentState == States.DungeonReturn)
                return;

            EntityPosition.Set(position);
        }

        private void Activate()
        {
            var mariaState = Game1.GameManager.SaveManager.GetString("maria_state");

            // singing for the animals
            // TODO: fade in music
            if (mariaState == "3")
            {
                if (Components[CollisionComponent.Index] != null)
                    RemoveComponent(CollisionComponent.Index);

                var marinDungeonState = Game1.GameManager.SaveManager.GetString("maria_dungeon");
                if (!string.IsNullOrEmpty(marinDungeonState) && marinDungeonState == "1")
                {
                    IsActive = false;
                    return;
                }

                if (_dungeonLeaveSequence)
                {
                    _dungeonLeaveSequence = false;
                    _currentState = States.DungeonReturn;
                    return;
                }

                _currentState = States.FollowPlayer;
                _followVelocity = Vector2.Zero;

                if (MapManager.ObjLink.NextMapPositionStart.HasValue &&
                    MapManager.ObjLink.NextMapPositionEnd.HasValue)
                {
                    var direction = MapManager.ObjLink.NextMapPositionEnd.Value -
                                    MapManager.ObjLink.NextMapPositionStart.Value;
                    if (direction != Vector2.Zero)
                        _walkDirection = AnimationHelper.GetDirection(direction);
                    _animator.Play("walk_" + _walkDirection);
                }
            }
            else if (mariaState == "4")
            {
                var animal0 = new ObjPersonNew(Map, (int)EntityPosition.X + 8, (int)EntityPosition.Y - 32, null, "animal_rabbit", "animals_absorbed", "dance_3", new Rectangle(0, 0, 12, 12));
                var animal1 = new ObjPersonNew(Map, (int)EntityPosition.X - 8, (int)EntityPosition.Y + 32, null, "animal_rabbit", "animals_absorbed", "dance_1", new Rectangle(0, 0, 12, 12));
                var animal2 = new ObjPersonNew(Map, (int)EntityPosition.X - 40, (int)EntityPosition.Y + 16, null, "animal_rabbit", "animals_absorbed", "dance_1", new Rectangle(0, 0, 12, 12));
                var animal3 = new ObjPersonNew(Map, (int)EntityPosition.X - 40, (int)EntityPosition.Y - 32, null, "animal 02", "animals_absorbed", "dance", new Rectangle(0, 0, 12, 12));
                var animal4 = new ObjPersonNew(Map, (int)EntityPosition.X + 24, (int)EntityPosition.Y + 16, null, "animal 03", "animals_absorbed", "dance", new Rectangle(0, 0, 12, 12));

                Map.Objects.SpawnObject(animal0);
                Map.Objects.SpawnObject(animal1);
                Map.Objects.SpawnObject(animal2);
                Map.Objects.SpawnObject(animal3);
                Map.Objects.SpawnObject(animal4);

                _currentState = States.AnimalSinging;

                StartSinging();
                // TODO: blend in with distance
                //Game1.GameManager.SetMusic(46, 2);
            }
            else if (mariaState == "5")
            {
                _currentState = States.Jumping;
                _animator.Play("land");
                ((BodyCollisionComponent)Components[BodyCollisionComponent.Index]).IsActive = false;
            }
        }

        private void Update()
        {
            // rotate towards the player
            if (_currentState == States.Idle)
            {
                var playerDistance = new Vector2(
                    MapManager.ObjLink.EntityPosition.X - (EntityPosition.X),
                    MapManager.ObjLink.EntityPosition.Y - (EntityPosition.Y - 4));

                var dir = 3;

                // rotate in the direction of the player
                if (playerDistance.Length() < 32)
                    dir = AnimationHelper.GetDirection(playerDistance);

                if (_lastDirection != dir)
                {
                    // look at the player
                    _animator.Play("stand_" + dir);
                    _lastDirection = dir;
                }
            }
            else if (_currentState == States.AnimalSinging)
            {
                // start/stop depending on the distance to the player
                var nearPlayer = _field.Contains(MapManager.ObjLink.EntityPosition.Position);
                if (!_isSingingWithSound && nearPlayer)
                {
                    _isSingingWithSound = true;
                    Game1.GameManager.SetMusic(46, 2);
                }
                else if (_isSingingWithSound && !nearPlayer)
                {
                    _isSingingWithSound = false;
                    Game1.GameManager.SetMusic(-1, 2);
                }
            }
            else if (_currentState == States.Singing)
            {
                // stop singing if the player is too far away
                var distance = EntityPosition.Position - MapManager.ObjLink.EntityPosition.Position;
                if (distance.Length() > 80)
                {
                    StopSinging();
                    _currentState = States.Idle;
                }
            }
            else if (_currentState == States.SingingDuo)
            {
                UpdateSingingDuo();
            }
            else if (_currentState == States.FollowPlayer)
            {
                UpdateFollowPlayer();
            }
            else if (_currentState == States.Jumping)
            {
                var playerPosition = MapManager.ObjLink.EntityPosition.Position;
                var playerDirection = EntityPosition.Position - playerPosition;
                var distance = playerDirection.Length();
                if (distance < 72 && !_helpDialogShown)
                {
                    _helpDialogShown = true;
                    Game1.GameManager.StartDialogPath("marin_help");
                }
                else if (distance > 128)
                {
                    _helpDialogShown = false;
                }

                if (!_isPulled && distance < 16 && playerDirection.X > 8)
                {
                    _isPulled = true;
                    _pullOffsetY = (int)playerDirection.Y;
                    _animator.Play("stand_0");
                }

                if (_isPulled)
                {
                    EntityPosition.Set(new Vector2(playerPosition.X + 14, playerPosition.Y + _pullOffsetY));
                    if (!MapManager.ObjLink.IsUsingHookshot())
                    {
                        _isPulled = false;
                        _currentState = States.Saved;
                        Game1.GameManager.StartDialogPath("marin_saved");
                    }
                }

                if (_animator.CurrentAnimation.Id == "jump" && _body.IsGrounded)
                {
                    _animator.Play("land");
                }
                if (_animator.CurrentAnimation.Id == "land" && !_animator.IsPlaying)
                {
                    _animator.Play("jump");
                    _body.Velocity.Z = 1.25f;
                }
            }
            else if (_currentState == States.DungeonReturn)
            {
                UpdateReturn();
            }

            UpdateMoving();

            UpdateFade();

            if (_isSinging || _noteCount < _noteEndTime)
                _noteCount += Game1.DeltaTime;
            else
                _noteCount = _noteEndTime;
        }

        private void UpdateReturn()
        {
            // freeze the player (need to make sure to play the transition animation)
            var transitionSystem = (MapTransitionSystem)Game1.GameManager.GameSystems[typeof(MapTransitionSystem)];
            if (!transitionSystem.IsTransitioningIn())
                MapManager.ObjLink.FreezePlayer();

            _returnCounter += Game1.DeltaTime;

            if (_returnInit)
            {
                _returnInit = false;
                EntityPosition.Set(_returnStart);
            }

            if (_returnFinished)
            {
                _walkDirection = 1;
                _animator.Play("stand_1");

                if (_returnCounter > 2500)
                {
                    // show message after having 4 or less lives and having walked into the dungeon with more than 4
                    // there are two different dialogs; the second one appear not as often as the first one
                    var heartsDialog = Game1.GameManager.CurrentHealth <= 4 && (_dungeonEnterLives >= 4 || _dungeonEnterLives == 0);
                    var randomDialog = Game1.RandomNumber.Next(0, 5);
                    Game1.GameManager.SaveManager.SetString("marin_dungeon_hearts", heartsDialog ? (randomDialog < 4 ? "1" : "2") : "0");

                    // show a different message after two minutes; does not work correctly if the savestate is loaded
                    var longTimeDialog = _dungeonEnterTime + 120000 < Game1.TotalGameTime;
                    Game1.GameManager.SaveManager.SetString("marin_dungeon_time", longTimeDialog ? "1" : "0");

                    _currentState = States.FollowPlayer;
                    Game1.GameManager.StartDialogPath("marin_dungeon_leave");
                }

                return;
            }

            // start walking
            if (_returnCounter > 1500)
            {
                _animator.Play("walk_0");
                var newPosition = AnimationHelper.MoveToTarget(EntityPosition.Position, _returnEnd, 0.75f * Game1.TimeMultiplier);
                EntityPosition.Set(newPosition);

                if (newPosition == _returnEnd)
                    _returnFinished = true;
            }
        }

        private void UpdateFade()
        {
            if (_fadeTime <= 0)
                return;

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

        private void UpdateMoving()
        {
            if (!_isMoving)
                return;

            // move towards the target position
            var targetDistance = _targetPosition - EntityPosition.Position;
            if (targetDistance.Length() > _moveSpeed * Game1.TimeMultiplier)
            {
                targetDistance.Normalize();
                _body.VelocityTarget = targetDistance * _moveSpeed;

                var dir = AnimationHelper.GetDirection(targetDistance);
                _animator.Play("walk_" + dir);
            }
            // finished walking
            else
            {
                _body.VelocityTarget = Vector2.Zero;
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

            if (!move.OffsetMode)
                _targetPosition = move.Position;
            else
                _targetPosition = EntityPosition.Position + move.Offset;
        }

        private void SetMovingString(bool state)
        {
            Game1.GameManager.SaveManager.SetString("marinMoving", state ? "1" : "0");
        }

        private void UpdateSingingDuo()
        {
            _duoCounter += Game1.DeltaTime;
            if (_duoIndex == 0 && _duoCounter > 7500)
            {
                _duoIndex++;
                _isSinging = false;
                _animator.Play("idle");
                MapManager.ObjLink.StartOcarinaDuo();
            }
            else if (_duoIndex == 1 && _duoCounter > 16000)
            {
                _duoIndex++;
                _isSinging = true;
                _animator.Play("sing");
            }
            else if (_duoIndex == 2 && _duoCounter > 32000)
            {
                _duoIndex++;
                _isSinging = false;
                _animator.Play("idle");
            }
            else if (_duoIndex == 3 && _duoCounter > 33000)
            {
                _currentState = States.PostDuo;
                MapManager.ObjLink.StopOcarinaDuo();
                Game1.GameManager.StartDialogPath("marin_singing_end");
                Game1.GameManager.SetMusic(-1, 2);

                Game1.GameManager.SaveManager.RemoveString("marin_sing_position");
            }

            if (_duoIndex == 0)
                MapManager.ObjLink.FreezePlayer();
        }

        private void OnCollision(Values.BodyCollision collision)
        {
            if ((collision & Values.BodyCollision.Vertical) != 0)
            {
                _followVelocity.X += Math.Abs(_followVelocity.Y) * MathF.Sign(_followVelocity.X);
                _followVelocity.Y = 0;
            }
            if ((collision & Values.BodyCollision.Horizontal) != 0)
            {
                _followVelocity.Y += Math.Abs(_followVelocity.X) * MathF.Sign(_followVelocity.Y);
                _followVelocity.X = 0;
            }
        }

        private void UpdateFollowPlayer()
        {
            if (((MapTransitionSystem)Game1.GameManager.GameSystems[typeof(MapTransitionSystem)]).IsTransitioningIn())
            {
                _body.VelocityTarget = Vector2.Zero;
                return;
            }

            // make sure that the player does not walk before marin hits the ground
            // he could potentially collect the heart
            if (_fountainMouse)
                MapManager.ObjLink.FreezePlayer();

            if (!_fountainSeqInit && _fountainSequence)
            {
                _fountainSeqInit = true;
                EntityPosition.Set(new Vector2(EntityPosition.X, EntityPosition.Y - 8));
            }

            if (_fountainSequence && _body.IsGrounded)
            {
                _fountainSequence = false;
                _fountainMouse = false;

                var playerDist = MapManager.ObjLink.EntityPosition.Position - new Vector2(EntityPosition.Position.X, EntityPosition.Position.Y + 4);
                var fallenOnLink = playerDist.Length() < 8;

                Game1.GameManager.SaveManager.SetString("fallen_on_link", (fallenOnLink ? "1" : "0"));

                Game1.GameManager.StartDialogPath("seq_fountain");

                if (fallenOnLink)
                {
                    Game1.GameManager.ShakeScreen(450, 0, 2, 0, 5);
                    Game1.GameManager.PlaySoundEffect("D360-11-0B");
                }
            }

            // jump
            if (MapManager.ObjLink.CurrentState == ObjLink.State.Jumping &&
                ((!MapManager.ObjLink.IsRailJumping() && MapManager.ObjLink._body.Velocity.Z < 0) ||
                MapManager.ObjLink.GetRailJumpAmount() > 0.45f) && _body.IsGrounded)
            {
                Game1.GameManager.PlaySoundEffect("D360-36-24");
                _body.Velocity.Z = 2.35f;

                if (MapManager.ObjLink.IsRailJumping())
                {
                    _isRailJumping = true;
                    _holeAbsorb = false;

                    _body.IgnoreHeight = true;
                    _body.IgnoresZ = true;
                    _body.IsGrounded = false;
                    _body.VelocityTarget = Vector2.Zero;

                    _railJumpPercentage = 0;
                    _railJumpStartPosition = EntityPosition.Position;
                    _railJumpTargetPosition = MapManager.ObjLink.RailJumpTarget();
                    _railJumpSpeed = MapManager.ObjLink.RailJumpSpeed();
                    _railJumpHeight = MapManager.ObjLink.RailJumpHeight();

                    _walkDirection = MapManager.ObjLink.Direction;
                    _animator.Play("stand_" + _walkDirection);
                }
            }

            if (_isRailJumping)
            {
                _railJumpPercentage += Game1.TimeMultiplier * _railJumpSpeed;
                var amount = MathF.Sin(_railJumpPercentage * (MathF.PI * 0.3f)) / MathF.Sin(MathF.PI * 0.3f);
                var newPosition = Vector2.Lerp(_railJumpStartPosition, _railJumpTargetPosition, amount);
                EntityPosition.Set(newPosition);

                EntityPosition.Z = MathF.Sin(_railJumpPercentage * MathF.PI) * _railJumpHeight;

                // finished rail jump?
                if (_railJumpPercentage >= 1)
                {
                    _isRailJumping = false;
                    _body.IgnoreHeight = false;
                    _body.IgnoresZ = false;
                    _body.Velocity.Z = -1f;
                    EntityPosition.Set(_railJumpTargetPosition);

                }

                if (MapManager.ObjLink.IsHoleAbsorb())
                {
                    _holeAbsorb = true;
                    _holeAbsorbCounter = 175;
                }

                return;
            }

            // fall into a hole with link?
            if (_holeAbsorb)
            {
                _holeAbsorbCounter -= Game1.DeltaTime;
                if (_holeAbsorbCounter >= 0)
                    return;

                var fallAnimation = new ObjAnimator(Map, 0, 0, Values.LayerBottom, "Particles/fall", "idle", true);
                fallAnimation.EntityPosition.Set(new Vector2(
                    _body.Position.X + _body.OffsetX + _body.Width / 2.0f - 5,
                    _body.Position.Y + _body.OffsetY + _body.Height / 2.0f - 5));
                Map.Objects.SpawnObject(fallAnimation);

                _sprite.IsVisible = false;
                _holeAbsorb = false;

                return;
            }

            if (MapManager.ObjLink.IsRailJumping())
                return;

            // landed on the ground?
            if (_body.IsGrounded && !_body.WasGrounded)
            {
                Game1.GameManager.PlaySoundEffect("D378-07-07");
            }

            var playerDirection = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            var playerDistance = Math.Abs(playerDirection.X) + Math.Abs(playerDirection.Y);
            if (playerDirection != Vector2.Zero)
                playerDirection.Normalize();
            var walkSpeedMult = playerDistance / 16;
            var targetVelocity = Vector2.Zero;

            var collisionCheckDist = 8;
            var collidingBox = Box.Empty;
            // check for future collisions
            var collisionH = SystemBody.Collision(_body,
                EntityPosition.X + playerDirection.X * collisionCheckDist,
                EntityPosition.Y, 0, _body.CollisionTypes, false, ref collidingBox);
            var collisionV = SystemBody.Collision(_body,
                EntityPosition.X,
                EntityPosition.Y + playerDirection.Y * collisionCheckDist, 0, _body.CollisionTypes, false, ref collidingBox);

            // disable the collision if we are too far away from the player; this will prevent situations where we are stuck
            var ignoreCollisions = MapManager.ObjLink.IsRailJumping() || playerDistance > 24;
            _body.CollisionTypes = ignoreCollisions ? Values.CollisionTypes.None : Values.CollisionTypes.Normal;

            if (playerDistance > 16)
            {
                targetVelocity = playerDirection * walkSpeedMult;
                // try to avoid future collisions by walking around the colliding object
                if (!ignoreCollisions && collisionH)
                {
                    targetVelocity.Y += (Math.Abs(targetVelocity.X) * MathF.Sign(targetVelocity.Y)) * 0.5f;
                    targetVelocity.X *= 0.5f;
                }
                else if (!ignoreCollisions && collisionV)
                {
                    targetVelocity.X += (Math.Abs(targetVelocity.Y) * MathF.Sign(targetVelocity.X)) * 0.5f;
                    targetVelocity.Y *= 0.5f;
                }
            }

            _followVelocity = Vector2.Lerp(_followVelocity, targetVelocity, 0.45f * Game1.TimeMultiplier);
            _body.VelocityTarget = _followVelocity;

            if (_followVelocity.Length() > 0.1f)
                _walkDirection = AnimationHelper.GetDirection(_followVelocity);

            // play walk/stand animation
            // jump animation
            if (MapManager.ObjLink.IsJumping() && _followVelocity.Length() < 0.1f)
            {
                _animator.Play("jump_up_" + _walkDirection);
            }
            else if (!_body.IsGrounded)
            {
                if (_body.Velocity.Z > 0)
                    _animator.Play("jump_up_" + _walkDirection);
                else
                    _animator.Play("jump_down_" + _walkDirection);
            }
            else if (_followVelocity.Length() > 0.1f)
            {
                _animator.Play("walk_" + _walkDirection);
                _animator.SpeedMultiplier = walkSpeedMult;
            }
            else
            {
                _animator.Play("stand_" + _walkDirection);
            }

            _enterDungeonMessage = EnterDungeonMessage;
            EnterDungeonMessage = false;
        }

        private void StartSinging()
        {
            _isSinging = true;
            _noteCount = 0;
            _noteEndTime = 0;
            _body.VelocityTarget = Vector2.Zero;
            _animator.Play("sing");

            // set the position for the dogs listening
            Game1.GameManager.SaveManager.SetString("marin_sing_position", (int)EntityPosition.X + "," + (int)EntityPosition.Y);
        }

        private void StopSinging()
        {
            Game1.GameManager.SetMusic(-1, 2);
            _isSinging = false;
            _isSingingWithSound = false;
            _lastDirection = -1;
            _noteEndTime = (int)(_noteCount / (_cycleTime * 0.5f) + 2) * (_cycleTime * 0.5f);

            Game1.GameManager.SaveManager.RemoveString("marin_sing_position");
        }

        private void KeyChanged()
        {
            if (!IsActive)
                return;

            // start fading away?
            var fadeValue = Game1.GameManager.SaveManager.GetString("maria_fade");
            if (!string.IsNullOrEmpty(fadeValue))
            {
                _fadeTime = int.Parse(fadeValue);
                _fadeCounter = _fadeTime;
                Game1.GameManager.SaveManager.RemoveString("maria_fade");
            }

            // start moving?
            var moveValue = Game1.GameManager.SaveManager.GetString("maria_walk");
            if (!string.IsNullOrEmpty(moveValue))
            {
                var split = moveValue.Split(',');

                if (split.Length == 3)
                {
                    var offsetX = int.Parse(split[0]);
                    var offsetY = int.Parse(split[1]);
                    _moveSpeed = float.Parse(split[2], CultureInfo.InvariantCulture);
                    _nextMoveStep.Enqueue(new MoveStep() { MoveSpeed = _moveSpeed, Offset = new Vector2(offsetX, offsetY), OffsetMode = true });
                }
                if (split.Length == 4)
                {
                    var positionX = int.Parse(split[0]);
                    var positionY = int.Parse(split[1]);
                    _moveSpeed = float.Parse(split[2], CultureInfo.InvariantCulture);
                    _nextMoveStep.Enqueue(new MoveStep() { MoveSpeed = _moveSpeed, Position = new Vector2(positionX, positionY) });
                }

                if (!_isMoving)
                {
                    _isMoving = true;
                    DequeueMove();
                    SetMovingString(true);
                    _body.CollisionTypes = Values.CollisionTypes.None;
                }

                _isMoving = true;
                _currentState = States.Idle;
                Game1.GameManager.SaveManager.RemoveString("maria_walk");
            }

            // start singing?
            var value = Game1.GameManager.SaveManager.GetString("maria_sing");
            if (value != null && value == "1")
            {
                StartSinging();
                Game1.GameManager.SetMusic(46, 2);
                _currentState = States.Singing;
                Game1.GameManager.SaveManager.RemoveString("maria_sing");
            }

            // start singing for the final scene?
            var singFinal = Game1.GameManager.SaveManager.GetString("maria_sing_final", "0");
            if (singFinal == "1")
            {
                StartSinging();
                _currentState = States.SingingFinal;
                Game1.GameManager.SaveManager.RemoveString("maria_sing_final");
            }

            var animalKey = Game1.GameManager.SaveManager.GetString("maria_sing_animals");
            if (animalKey != null && animalKey == "1")
            {
                StartSinging();
                Game1.GameManager.SetMusic(46, 2);
                _currentState = States.AnimalSinging;
                Game1.GameManager.SaveManager.RemoveString("maria_sing_animals");
            }

            var walrusKey = Game1.GameManager.SaveManager.GetString("maria_stop_singing");
            if (walrusKey != null && walrusKey == "1")
            {
                StopSinging();
                _currentState = States.Sequence;
                Game1.GameManager.SaveManager.RemoveString("maria_stop_singing");
            }

            var animationKey = Game1.GameManager.SaveManager.GetString("maria_play_animation");
            if (!string.IsNullOrEmpty(animationKey))
            {
                _animator.Play(animationKey);
                _currentState = States.Sequence;
                Game1.GameManager.SaveManager.RemoveString("maria_play_animation");
            }

            var stopSingingKey = Game1.GameManager.SaveManager.GetString("maria_sing_walrus");
            if (stopSingingKey != null && stopSingingKey == "1")
            {
                StartSinging();
                Game1.GameManager.SetMusic(46, 2);
                _currentState = States.SingingWalrus;
                Game1.GameManager.SaveManager.RemoveString("maria_sing_walrus");
            }

            var duoKey = Game1.GameManager.SaveManager.GetString("maria_start_duo");
            if (duoKey != null && duoKey == "1")
            {
                _duoIndex = 0;
                _duoCounter = 0;
                StartSinging();
                Game1.GameManager.SetMusic(73, 2);
                _currentState = States.SingingDuo;
                MapManager.ObjLink.FreezePlayer();
                Game1.GameManager.SaveManager.RemoveString("maria_start_duo");
            }
        }

        private bool Interact()
        {
            if (_currentState != States.Idle &&
                _currentState != States.AnimalSinging &&
                _currentState != States.PostDuo)
                return false;

            // stop singing
            if (_currentState == States.AnimalSinging)
            {
                _isSinging = false;
                _animator.Play("idle");
                Game1.GameManager.SetMusic(-1, 2);
            }

            Game1.GameManager.StartDialogPath("maria");
            return true;
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            // draw maria
            _bodyDrawComponent.Draw(spriteBatch);

            // draw the notes if maria is singing
            var leftNotePosition = new Vector2(EntityPosition.X - 8 - _spriteNote.SourceRectangle.Width / 2f, EntityPosition.Y - 16 - _spriteNote.SourceRectangle.Height / 2f);
            var leftNoteDirection = new Vector2(-0.4f, -1.0f);
            DrawNote(spriteBatch, leftNotePosition, leftNoteDirection, 0);

            var rightNotePosition = new Vector2(EntityPosition.X + 8 - _spriteNote.SourceRectangle.Width / 2f, EntityPosition.Y - 16 - _spriteNote.SourceRectangle.Height / 2f);
            var rightNoteDirection = new Vector2(0.4f, -1.0f);
            DrawNote(spriteBatch, rightNotePosition, rightNoteDirection, _cycleTime / 2);
        }

        private void DrawNote(SpriteBatch spriteBatch, Vector2 position, Vector2 direction, int timeOffset)
        {
            if (_noteCount < timeOffset ||
                !_isSinging && (int)((_noteCount - timeOffset) / _cycleTime + 1) * _cycleTime + timeOffset > _noteEndTime)
                return;

            var time = (_noteCount + timeOffset) % _cycleTime;
            position += direction * time * 0.02f + new Vector2(-direction.X, direction.Y) * (float)Math.Sin(time * 0.015) * 1.25f;

            var transparency = 1.0f;
            if (time > _cycleTime - 100)
                transparency = (_cycleTime - time) / 100f;
            else if (time < 100)
                transparency = time / 100;

            DrawHelper.DrawNormalized(spriteBatch, _spriteNote, position, Color.White * transparency);
        }
    }
}