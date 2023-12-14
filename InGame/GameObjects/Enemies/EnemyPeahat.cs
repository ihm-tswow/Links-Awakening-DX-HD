using System;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyPeahat : GameObject
    {
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly Animator _animator;
        private readonly AiTriggerCountdown _flyCounter;
        private readonly AiDamageState _damageState;

        private const int StartTime = 2500;
        private const int FlyTime = 7500;
        private const int LandTime = 1500;

        private float _flyState;
        private float _turnSpeed;
        private int _dir = 1;

        public EnemyPeahat() : base("peahat") { }

        public EnemyPeahat(Map.Map map, int posX, int posY) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -32, 16, 32);

            _turnSpeed = 0.02f;
            _flyState = (float)Math.PI / 2 * Game1.RandomNumber.Next(0, 4);
            _dir = Game1.RandomNumber.Next(0, 2) * 2 - 1;

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/peahat");
            _animator.SpeedMultiplier = 0;
            _animator.Play("idle");

            var sprite = new CSprite(EntityPosition);
            var animatorComponent = new AnimationComponent(_animator, sprite, new Vector2(-8, -16));

            var fieldRectangle = map.GetField(posX, posY, 8);

            _body = new BodyComponent(EntityPosition, -6, -12, 12, 10, 8)
            {
                MoveCollision = OnCollision,
                IgnoresZ = true,
                FieldRectangle = fieldRectangle,
                MaxJumpHeight = 4
            };

            _aiComponent = new AiComponent();

            var stateStart = new AiState();
            stateStart.Trigger.Add(new AiTriggerCountdown(StartTime, StartTick, ToFlying));
            var stateFly = new AiState(UpdateFlying);
            stateFly.Trigger.Add(new AiTriggerRandomTime(ChangeFlyingDirection, 500, 1500));
            stateFly.Trigger.Add(_flyCounter = new AiTriggerCountdown(FlyTime, null, ToLand));
            var stateLand = new AiState();
            stateLand.Trigger.Add(new AiTriggerCountdown(LandTime, LandTick, ToIdle));
            var stateIdle = new AiState();
            stateIdle.Trigger.Add(new AiTriggerRandomTime(() => _aiComponent.ChangeState("start"), 3000, 6000));
            var stateStunned = new AiState();
            stateStunned.Trigger.Add(new AiTriggerCountdown(200, null, EndStunned));

            _aiComponent.States.Add("start", stateStart);
            _aiComponent.States.Add("fly", stateFly);
            _aiComponent.States.Add("land", stateLand);
            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("stunned", stateStunned);
            _damageState = new AiDamageState(this, _body, _aiComponent, sprite, 1, false);

            _aiComponent.ChangeState("start");

            var hittableBox = new CBox(EntityPosition, -6, -14, 0, 12, 14, 8, true);
            var damageBox = new CBox(EntityPosition, -6, -14, 0, 12, 14, 4, true);

            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageBox, HitType.Enemy, 2));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(PushableComponent.Index, new PushableComponent(damageBox, OnPush));
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BodyComponent.Index, _body);
            AddComponent(BaseAnimationComponent.Index, animatorComponent);
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, sprite, Values.LayerPlayer) { DeepWaterOutline = true });
            AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(_body, sprite));
        }

        private Values.HitCollision OnHit(GameObject originObject, Vector2 direction, HitType type, int damage, bool pieceOfPower)
        {
            if (_aiComponent.CurrentStateId == "stunned")
                return Values.HitCollision.None;

            if (_animator.SpeedMultiplier > 0.25f)
            {
                _aiComponent.ChangeState("stunned");
                _animator.SpeedMultiplier = 0;
                _body.VelocityTarget = Vector2.Zero;

                return Values.HitCollision.RepellingParticle;
            }

            return _damageState.OnHit(originObject, direction, type, damage, pieceOfPower);
        }

        private void OnCollision(Values.BodyCollision collision)
        {
            // speed up the collision time
            _flyState += (_dir == -1 ? -1 : 1) * MathF.PI * 0.025f * Game1.TimeMultiplier;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type == PushableComponent.PushType.Impact)
                _body.Velocity = new Vector3(direction.X * 1.5f, direction.Y * 1.5f, _body.Velocity.Z);

            return true;
        }

        private void EndStunned()
        {
            _aiComponent.ChangeState(_aiComponent.LastStateId, true);
        }

        private void StartTick(double count)
        {
            // speed up animation
            var mult = (StartTime - (float)count + 250f) / 1500f;
            mult = Math.Clamp(mult, 0, 1);
            _animator.SpeedMultiplier = mult;

            // start moving up
            if (count < StartTime - 1250)
                EntityPosition.Z = (float)(1 - count / (StartTime - 1250)) * 16;
        }

        private void ToFlying()
        {
            _aiComponent.ChangeState("fly");
            EntityPosition.Z = 16;
        }

        private void ChangeFlyingDirection()
        {
            _dir = Game1.RandomNumber.Next(0, 3) - 1;
        }

        private void UpdateFlying()
        {
            _animator.SpeedMultiplier = 1;

            _flyState += _dir * _turnSpeed * Game1.TimeMultiplier;
            var vecDirection = new Vector2((float)Math.Sin(_flyState), (float)Math.Cos(_flyState));

            // speeds up / slows down
            float speedMult;
            if (_flyCounter.CurrentTime >= FlyTime - 250)
                speedMult = Math.Clamp((float)(FlyTime - _flyCounter.CurrentTime) / 250, 0, 1);
            else
                speedMult = Math.Min(1, (float)_flyCounter.CurrentTime / 250);

            // fly
            _body.VelocityTarget = vecDirection * 0.5f * speedMult;
        }

        private void ToLand()
        {
            _aiComponent.ChangeState("land");
            _body.VelocityTarget = Vector2.Zero;
        }

        private void LandTick(double count)
        {
            // speed up animation
            var mult = (float)count / LandTime;
            mult = Math.Clamp(mult, 0, 1);
            _animator.SpeedMultiplier = mult;

            // move down
            var posZ = (float)((count - 500) / (LandTime - 500));
            posZ = Math.Clamp(posZ, 0, 1);
            EntityPosition.Z = posZ * 16;
        }

        private void ToIdle()
        {
            _aiComponent.ChangeState("idle");
            EntityPosition.Z = 0;
        }
    }
}