using System;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyHardhatBeetle : GameObject
    {
        private readonly BodyComponent _body;
        private readonly AiStunnedState _stunnedState;
        private readonly Animator _animator;
        private readonly AiDamageState _damageState;

        private Vector2 _vecDirection;

        private float _maxSpeed;
        private float _acceleration;
        private float _currentSpeed;

        private bool _isFollowing;
        private bool _wasFollowing;

        public EnemyHardhatBeetle() : base("hardHatBeetle") { }

        public EnemyHardhatBeetle(Map.Map map, int posX, int posY) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/hardhat beetle");
            _animator.Play("walk");

            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, sprite, Vector2.Zero);

            var fieldRectangle = map.GetField(posX, posY);

            _body = new BodyComponent(EntityPosition, -6, -10, 12, 9, 8)
            {
                MoveCollision = OnCollision,
                Drag = 0.875f,
                AvoidTypes = Values.CollisionTypes.Hole |
                             Values.CollisionTypes.NPCWall |
                             Values.CollisionTypes.DeepWater,
                FieldRectangle = fieldRectangle
            };

            var aiComponent = new AiComponent();

            var stateInit = new AiState();
            stateInit.Trigger.Add(new AiTriggerCountdown(350, null, () => aiComponent.ChangeState("moving")));
            var stateMoving = new AiState(UpdateMoving) { Init = InitMoving };

            aiComponent.States.Add("init", stateInit);
            aiComponent.States.Add("moving", stateMoving);
            _stunnedState = new AiStunnedState(aiComponent, animationComponent, 3300, 900) { SilentStateChange = false };
            _damageState = new AiDamageState(this, _body, aiComponent, sprite, 1);
            new AiDeepWaterState(_body);
            new AiFallState(aiComponent, _body, OnHoleAbsorb, null);

            aiComponent.ChangeState("init");

            // randomize speed and acceleration
            _maxSpeed = Game1.RandomNumber.Next(30, 60) / 100f;
            _acceleration = Game1.RandomNumber.Next(30, 60) / 2000f;

            var damageCollider = new CBox(EntityPosition, -7, -11, 0, 14, 11, 4);
            var hittableRectangle = new CBox(EntityPosition, -8, -14, 16, 14, 8);

            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageCollider, HitType.Enemy, 4));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableRectangle, OnHit));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(PushableComponent.Index, new PushableComponent(_body.BodyBox, OnPush) { RepelMultiplier = 1.25f });
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(AiComponent.Index, aiComponent);
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new DrawShadowCSpriteComponent(sprite));
        }

        private void InitMoving()
        {
            _animator.Play("walk");
        }

        private void UpdateMoving()
        {
            // accelerate
            _currentSpeed += (float)Math.Pow(_acceleration, Game1.TimeMultiplier);
            if (_currentSpeed > _maxSpeed)
                _currentSpeed = _maxSpeed;

            if (_vecDirection != Vector2.Zero)
            {
                var oldPercentage = (float)Math.Pow(0.9f, Game1.TimeMultiplier);
                var newDirection = _body.VelocityTarget * oldPercentage +
                                   _vecDirection * (1 - oldPercentage);
                newDirection.Normalize();

                _body.VelocityTarget = newDirection * _maxSpeed;
            }
            else
                _body.VelocityTarget = Vector2.Zero;

            _isFollowing = _body.FieldRectangle.Intersects(MapManager.ObjLink.BodyRectangle);

            if (_isFollowing)
            {
                _vecDirection = new Vector2(
                    MapManager.ObjLink.EntityPosition.X - EntityPosition.X,
                    MapManager.ObjLink.EntityPosition.Y - EntityPosition.Y);

                if (_vecDirection != Vector2.Zero)
                    _vecDirection.Normalize();
            }

            _wasFollowing = _isFollowing;
            _isFollowing = false;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type == PushableComponent.PushType.Impact)
                _body.Velocity = new Vector3(direction.X * 1.5f, direction.Y * 1.5f, _body.Velocity.Z);

            return true;
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (_damageState.IsInDamageState())
                return Values.HitCollision.None;
            _damageState.SetDamageState(false);

            if (damageType == HitType.Boomerang || damageType == HitType.Hookshot)
            {
                _body.VelocityTarget = Vector2.Zero;
                _animator.Play("stunned");
                _stunnedState.StartStun();
            }

            _body.Velocity.X = direction.X * 3.0f;
            _body.Velocity.Y = direction.Y * 3.0f;

            Game1.GameManager.PlaySoundEffect("D360-09-09");

            return Values.HitCollision.Enemy;
        }

        private void OnCollision(Values.BodyCollision direction)
        {
            // this is used so that the speed is not lost while sliding on a wall
            // not sure if this could be done better
            if (_wasFollowing)
            {
                if ((direction & Values.BodyCollision.Horizontal) != 0)
                {
                    var ratio = Math.Abs(_vecDirection.X) / Math.Abs(_vecDirection.Y);
                    if (1 < ratio && ratio < 25)
                    {
                        _vecDirection.X = 0;
                        _vecDirection.Y *= ratio;
                    }
                }
                else if ((direction & Values.BodyCollision.Vertical) != 0)
                {
                    var ratio = Math.Abs(_vecDirection.Y) / Math.Abs(_vecDirection.X);
                    if (1 < ratio && ratio < 25)
                    {
                        _vecDirection.X *= ratio;
                        _vecDirection.Y = 0;
                    }
                }

                return;
            }

            _body.VelocityTarget = Vector2.Zero;

            // collide with a wall
            if ((direction & Values.BodyCollision.Horizontal) != 0)
                _vecDirection.X = -_vecDirection.X;
            else if ((direction & Values.BodyCollision.Vertical) != 0)
                _vecDirection.Y = -_vecDirection.Y;
        }

        private void OnHoleAbsorb()
        {
            _animator.SpeedMultiplier = 2.0f;
            _animator.Play("walk");
        }
    }
}