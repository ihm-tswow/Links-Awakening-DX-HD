using System;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;
using ProjectZ.InGame.GameObjects.Dungeon;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyBonePutter : GameObject
    {
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly Animator _animator;
        private readonly AnimationComponent _animatorComponent;
        private readonly AiDamageState _damageState;

        private readonly Vector2 _roomCenter;
        private Vector2 _startPosition;
        private Vector2 _targetPosition;
        private float _flyCounter;
        private float _flyTime;
        private int _bombThrowCounter;

        private const float JumpSpeed = 0.25f;

        public EnemyBonePutter() : base("bone putter") { }

        public EnemyBonePutter(Map.Map map, int posX, int posY, bool hasWings) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 16, 16);
            EntitySize = new Rectangle(-8, -32, 16, 32);

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/bone putter");

            var sprite = new CSprite(EntityPosition);
            _animatorComponent = new AnimationComponent(_animator, sprite, Vector2.Zero);

            _body = new BodyComponent(EntityPosition, -4, -8, 8, 8, 8)
            {
                MoveCollision = OnCollision,
                HoleOnPull = OnHolePull,
                IgnoresZ = true,
                Gravity = -0.075f,
                DragAir = 0.875f,
                AvoidTypes = Values.CollisionTypes.Hole | Values.CollisionTypes.NPCWall,
                FieldRectangle = map.GetField(posX, posY)
            };

            var roomRectangle = map.GetField(posX, posY);
            _roomCenter = new Vector2(roomRectangle.Center.X, roomRectangle.Center.Y + 8);

            _aiComponent = new AiComponent();

            var stateFlying = new AiState(UpdateFlying) { Init = InitFlying };
            var stateJumping = new AiState(UpdateJumping) { Init = InitJumping };
            var stateHole = new AiState();

            _aiComponent.States.Add("flying", stateFlying);
            _aiComponent.States.Add("jumping", stateJumping);
            _aiComponent.States.Add("holePull", stateHole);
            new AiFallState(_aiComponent, _body, null, null, 200);
            _damageState = new AiDamageState(this, _body, _aiComponent, sprite, hasWings ? 3 : 1, false);
            _aiComponent.ChangeState(hasWings ? "flying" : "jumping");

            var hittableBox = new CBox(EntityPosition, -6, -15, 2, 12, 14, 8, true);
            var damageBox = new CBox(EntityPosition, -6, -15, 2, 12, 14, 4, true);
            var pushBox = new CBox(EntityPosition, -6, -15, 2, 12, 14, 4, true);

            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageBox, HitType.Enemy, 2));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BodyComponent.Index, _body);
            AddComponent(BaseAnimationComponent.Index, _animatorComponent);
            AddComponent(PushableComponent.Index, new PushableComponent(pushBox, OnPush));
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(_body, sprite));
        }

        public bool StartJump()
        {
            return _aiComponent.CurrentStateId == "jumping" && _body.IsGrounded && EntityPosition.Z <= 0;
        }

        private void SpawnFairy()
        {
            // remove the enemy
            Map.Objects.DeleteObjects.Add(this);

            // spawn explosion effect that ends in a fairy spawning
            var animationExplosion = new ObjAnimator(Map, (int)EntityPosition.X - 8, (int)(EntityPosition.Y - EntityPosition.Z - 16), Values.LayerTop, "Particles/spawn", "run", true);
            animationExplosion.Animator.OnAnimationFinished = () =>
            {
                // remove the explosion animation
                animationExplosion.Map.Objects.DeleteObjects.Add(animationExplosion);
                // spawn fairy
                animationExplosion.Map.Objects.SpawnObject(new ObjDungeonFairy(animationExplosion.Map, (int)EntityPosition.X, (int)(EntityPosition.Y - EntityPosition.Z - 4), 0));
            };

            Map.Objects.SpawnObject(animationExplosion);
        }

        private void InitFlying()
        {
            _animator.Play("fly");
            _startPosition = EntityPosition.Position;
            _targetPosition = _startPosition;
            _flyCounter = 0;
        }

        private void UpdateFlying()
        {
            _flyCounter -= Game1.DeltaTime;

            if (_flyCounter > 0)
            {
                var lerpPercentage = 0.5f - MathF.Sin(_flyCounter / _flyTime * MathF.PI - MathF.PI / 2) * 0.5f;
                var newPosition = Vector2.Lerp(_startPosition, _targetPosition, lerpPercentage);
                EntityPosition.Set(newPosition);
            }
            else
            {
                ThrowBomb();

                EntityPosition.Set(_targetPosition);
                _startPosition = _targetPosition;

                // the target direction will be in the direction of the center if we are farther away from the center
                var centerDirection = EntityPosition.Position - _roomCenter;
                var centerDistance = centerDirection.Length();
                if (centerDirection != Vector2.Zero)
                    centerDirection.Normalize();
                var centerRadian = MathF.Atan2(centerDirection.Y, centerDirection.X);

                // set a new target position
                var centerOffset = Math.Clamp((50 - centerDistance) / 25, 0, 1);
                var randomRotation = centerRadian - (MathF.PI + Game1.RandomNumber.Next(0, 628) / 100f) * centerOffset;
                var randomDistance = Game1.RandomNumber.Next(12, 20);
                _targetPosition.X = _startPosition.X - MathF.Cos(randomRotation) * randomDistance;
                _targetPosition.Y = _startPosition.Y - MathF.Sin(randomRotation) * randomDistance;

                _animatorComponent.MirroredH = _targetPosition.X > _startPosition.X;
                _animatorComponent.UpdateSprite();

                // random fly time
                _flyTime = Game1.RandomNumber.Next(600, 800);
                _flyCounter = _flyTime;
            }
        }

        private void ThrowBomb()
        {
            _bombThrowCounter--;
            if (_bombThrowCounter > 0)
                return;

            var playerDistance = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            if (playerDistance.Length() > 64)
                return;

            _bombThrowCounter = Game1.RandomNumber.Next(2, 6);

            // spawn a bomb
            var bomb = new ObjBomb(Map, 0, 0, false, true);
            bomb.EntityPosition.Set(new Vector3(EntityPosition.X, EntityPosition.Y + 1, 18));
            bomb.Body.Velocity = new Vector3(0, 0, 0.25f);
            bomb.Body.Gravity = -0.1f;
            bomb.Body.Bounciness = 0.4f;
            Map.Objects.SpawnObject(bomb);
        }

        private void InitJumping()
        {
            _animator.Play("jump");
            _body.IgnoresZ = false;
            _body.IsGrounded = false;
            _body.JumpStartHeight = 0;
        }

        private void UpdateJumping()
        {
            if (_body.IsGrounded)
                Jump();
        }

        private void Jump()
        {
            // jump into a random direction
            var randomRotation = Game1.RandomNumber.Next(0, 628) / 100f;
            _body.VelocityTarget.X = -MathF.Cos(randomRotation) * JumpSpeed;
            _body.VelocityTarget.Y = -MathF.Sin(randomRotation) * JumpSpeed;
            _body.Velocity.Z = 1.5f;

            _animatorComponent.MirroredH = _body.VelocityTarget.X > 0;
            _animatorComponent.UpdateSprite();
        }

        private Values.HitCollision OnHit(GameObject originObject, Vector2 direction, HitType type, int damage, bool pieceOfPower)
        {
            if (type == HitType.Bow)
                damage = 3;

            if (_aiComponent.CurrentStateId == "flying" && (type == HitType.MagicPowder || type == HitType.Boomerang))
            {
                SpawnFairy();
                return Values.HitCollision.Enemy;
            }

            if (_aiComponent.CurrentStateId == "flying")
                _aiComponent.ChangeState("jumping");

            return _damageState.OnHit(originObject, direction, type, damage, pieceOfPower);
        }

        private void OnHolePull(Vector2 direction, float percentage)
        {
            if (percentage < 0.5f)
                return;

            _aiComponent.ChangeState("holePull");
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type == PushableComponent.PushType.Impact)
                _body.Velocity = new Vector3(direction.X * 1.75f, direction.Y * 1.75f, _body.Velocity.Z);

            return true;
        }

        private void OnCollision(Values.BodyCollision collision)
        {
            if ((collision & Values.BodyCollision.Horizontal) != 0)
                _body.Velocity.X = -_body.Velocity.X * 0.5f;
            if ((collision & Values.BodyCollision.Vertical) != 0)
                _body.Velocity.Y = -_body.Velocity.Y * 0.5f;
        }
    }
}