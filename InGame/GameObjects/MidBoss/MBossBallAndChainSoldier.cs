using System;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.MidBoss
{
    internal class MBossBallAndChainSoldier : GameObject
    {
        private readonly Animator _animator;
        private readonly BodyComponent _body;
        private readonly AnimationComponent _animatorComponent;
        private readonly CSprite _sprite;
        private readonly AiComponent _ai;
        private readonly MBossBallAndChain _ballAndChain;
        private readonly AiTriggerTimer _walkTimer;
        private readonly AiDamageState _damageState;
        private readonly Rectangle _fieldRectangle;

        private const string _leafSaveKey = "ow_goldLeafBalls";
        private string _strKey;

        private float _currentBallSpeed = 300;

        private const float BallDistance = 10;
        private const float BallDistanceThrow = 56;
        private float _ballState;
        private float _throwDirection;
        private int _ballCounter;
        private bool _startThrowing;
        private bool _isThrowing;

        private float _ballRadiant;
        private float _distance;
        private bool _wasBlocked;

        public MBossBallAndChainSoldier() : base("ballAndChain") { }

        public MBossBallAndChainSoldier(Map.Map map, int posX, int posY, string strKey) : base(map)
        {
            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            _strKey = strKey;

            _fieldRectangle = map.GetField(posX, posY, 16);
            _fieldRectangle.X += 16;

            // was already defeated?
            if (!string.IsNullOrEmpty(_strKey) && Game1.GameManager.SaveManager.GetString(_strKey) == "1")
            {
                IsDead = true;

                // spawn the leaf if is was not already collected
                var objLeaf = new ObjItem(Map, posX, posY, null, _leafSaveKey, "goldLeaf", null);
                if (!objLeaf.IsDead)
                    Map.Objects.SpawnObject(objLeaf);

                return;
            }

            _animator = AnimatorSaveLoad.LoadAnimator("MidBoss/ball and chain soldier");
            _animator.Play("swing1");

            _sprite = new CSprite(EntityPosition);
            _animatorComponent = new AnimationComponent(_animator, _sprite, new Vector2(-8, -16));

            _body = new BodyComponent(EntityPosition, -7, -12, 14, 12, 8);

            _ai = new AiComponent();

            var stateWalk = new AiState(UpdateWalk);
            stateWalk.Trigger.Add(_walkTimer = new AiTriggerTimer(500));
            var stateSwing = new AiState(UpdateSwing);
            var stateThrow = new AiState(UpdateThrow);

            _ai.States.Add("walk", stateWalk);
            _ai.States.Add("swing", stateSwing);
            _ai.States.Add("throw", stateThrow);
            new AiFallState(_ai, _body, null, KillBoss, 500);

            _ai.Trigger.Add(new AiTriggerUpdate(UpdateBall));

            _ai.ChangeState("walk");

            var damageCollider = new CBox(EntityPosition, -8, -16, 0, 16, 16, 8);
            AddComponent(AnimationComponent.Index, _animatorComponent);
            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageCollider, HitType.Enemy, 4));
            _damageState = new AiDamageState(this, _body, _ai, _sprite, 8, false)
            {
                OnDeath = OnDeath
            };
            AddComponent(HittableComponent.Index, new HittableComponent(_body.BodyBox, _damageState.OnHit));
            AddComponent(AiComponent.Index, _ai);
            AddComponent(BodyComponent.Index, _body);
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, _sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(_body, _sprite));

            _ballAndChain = new MBossBallAndChain(map, this);
            Map.Objects.SpawnObject(_ballAndChain);
        }

        private void OnDeath(bool pieceOfPower)
        {
            KillBoss();
            _damageState.BaseOnDeath(pieceOfPower);
        }

        private void KillBoss()
        {
            var playerDirection = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            if (playerDirection != Vector2.Zero)
                playerDirection.Normalize();
            playerDirection *= 1.75f;

            // spawn the golden leaf jumping towards the player
            var objLeaf = new ObjItem(Map, 0, 0, null, _leafSaveKey, "goldLeaf", null);
            if (!objLeaf.IsDead)
            {
                objLeaf.EntityPosition.Set(new Vector3(EntityPosition.X, EntityPosition.Y, EntityPosition.Z));
                objLeaf.SetVelocity(new Vector3(playerDirection.X, playerDirection.Y, 1.0f));
                objLeaf.Collectable = false;
                Map.Objects.SpawnObject(objLeaf);
            }

            // save
            if (!string.IsNullOrEmpty(_strKey))
                Game1.GameManager.SaveManager.SetString(_strKey, "1");

            Map.Objects.DeleteObjects.Add(_ballAndChain);
        }

        private void ToWalk()
        {
            _ai.ChangeState("walk");
            _startThrowing = false;
            _isThrowing = false;
            _ballAndChain.Deactivate();
        }

        private void UpdateWalk()
        {
            if (!_walkTimer.State)
                return;

            var playerDirection = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            if (playerDirection.Length() < 48)
            {
                ToSwing();
                return;
            }

            // do not walk towards the player if he is not in the field
            if (!_fieldRectangle.Contains(MapManager.ObjLink.EntityPosition.Position))
                return;

            // walk towards the player
            playerDirection.Normalize();
            _body.VelocityTarget = playerDirection / 4;
        }

        private void ToSwing()
        {
            _ai.ChangeState("swing");
            _body.VelocityTarget = Vector2.Zero;
            _ballCounter = 0;
        }

        private void UpdateSwing()
        {

        }

        private void ToThrow()
        {
            _ai.ChangeState("throw");
            _isThrowing = true;
            _wasBlocked = false;
            _ballAndChain.Activate();
        }

        private void UpdateThrow()
        {

        }

        private void UpdateBall()
        {
            Vector2 ballOffset;

            if (_isThrowing)
            {
                if (_wasBlocked)
                    _ballState += (Game1.DeltaTime / 500.0f) * MathF.PI;
                else
                    _ballState += (Game1.DeltaTime / 1000.0f) * MathF.PI;

                // finished animation?
                if (_ballState > _throwDirection + MathF.PI)
                {
                    ToWalk();
                }
            }
            else
            {
                var isSwinging = _ai.CurrentStateId == "swing";

                var target = isSwinging ? 300.0f : 500.0f;
                _currentBallSpeed = AnimationHelper.MoveToTarget(_currentBallSpeed, target, target * 0.1f * Game1.TimeMultiplier);

                Game1.DebugText += "\nball: " + (int)_currentBallSpeed;

                // 2 times per second or 4 if he is swinging fast
                _ballState += Game1.DeltaTime / _currentBallSpeed * MathF.PI * 2;

                if (_startThrowing && _ballState >= _throwDirection)
                {
                    _currentBallSpeed = 500.0f;
                    ToThrow();
                    _ballState = _throwDirection + ((_ballState - _throwDirection) / 1000.0f) * MathF.PI;
                }
                else if (_ballState >= MathF.PI * 2)
                {
                    _ballState -= MathF.PI * 2;

                    if (isSwinging)
                    {
                        _ballCounter++;
                        if (_ballCounter >= 2)
                        {
                            _startThrowing = true;
                            var playerDirection = MapManager.ObjLink.BodyRectangle.Center -
                                                  new Vector2(EntityPosition.X - 5, EntityPosition.Y - 15);
                            var playerAngle = MathF.Atan2(playerDirection.Y, playerDirection.X) + MathF.PI * 5 / 2;
                            if (playerAngle >= MathF.PI * 2)
                                playerAngle -= MathF.PI * 2;
                            _throwDirection = playerAngle;
                        }
                    }
                }
            }

            _animator.Play("swing" + (_ballState < MathF.PI ? "0" : "1"));

            // calculate the ball offset
            if (!_isThrowing)
            {
                ballOffset = new Vector2(-MathF.Cos(_ballState), -MathF.Sin(_ballState)) * BallDistance;
            }
            else
            {
                if (_wasBlocked)
                {
                    if (BallDistance < _distance - 2.5f * Game1.TimeMultiplier)
                        _distance -= 2.5f * Game1.TimeMultiplier;
                    else
                        _distance = BallDistance;

                    ballOffset = new Vector2(-MathF.Cos(_ballState), -MathF.Sin(_ballState)) * _distance;
                }
                else
                {
                    var throwState = MathF.Sin(_ballState - _throwDirection);
                    _distance = MathHelper.Lerp(BallDistance, BallDistanceThrow, throwState);

                    var tan = MathF.Tan((((_ballState - _throwDirection) / MathF.PI) * 2 - 1) * MathF.Atan(10)) / 10;
                    _ballRadiant = _throwDirection + (tan * 0.5f + 0.5f) * MathF.PI;

                    ballOffset = new Vector2(-MathF.Cos(_ballRadiant), -MathF.Sin(_ballRadiant)) * _distance;
                }
            }

            _ballAndChain.EntityPosition.Set(new Vector2(EntityPosition.X - 5, EntityPosition.Y - 7) + ballOffset);
        }

        public void BlockBall()
        {
            _wasBlocked = true;
            _ballState = _ballRadiant;
        }
    }
}