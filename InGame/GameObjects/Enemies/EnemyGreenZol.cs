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
    class EnemyGreenZol : GameObject
    {
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly CSprite _sprite;
        private readonly Animator _animator;
        private readonly AiDamageState _damageState;
        private readonly DamageFieldComponent _damageField;
        private readonly AnimationComponent _animationComponent;

        private readonly bool _fallMode;

        private int _jumpsLeft;
        private bool _pushable = false;

        public EnemyGreenZol() : base("green zol") { }

        public EnemyGreenZol(Map.Map map, int posX, int posY, int posZ, bool fallMode) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 13, posZ);
            EntitySize = new Rectangle(-8, -24, 16, 24);

            _fallMode = fallMode;

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/green zol");
            _animator.Play("walk_1");

            _sprite = new CSprite(EntityPosition);
            _animationComponent = new AnimationComponent(_animator, _sprite, new Vector2(-6, -16));

            _body = new BodyComponent(EntityPosition, -6, -10, 12, 10, 8)
            {
                AbsorbPercentage = 1,
                AvoidTypes = Values.CollisionTypes.NPCWall |
                             Values.CollisionTypes.DeepWater,
                Gravity = -0.15f,
                Bounciness = 0f,
                Drag = 0.85f
            };

            var stateInit = new AiState();
            // small delay before possibility of spawning; needed for situation where the enemy is directly at the door
            stateInit.Trigger.Add(new AiTriggerCountdown(450, null, () => _aiComponent.ChangeState("notSpawned")));
            var stateHidden = new AiState();
            stateHidden.Trigger.Add(new AiTriggerCountdown(2000, null, () => _aiComponent.ChangeState("notSpawned")));
            var stateNotSpawned = new AiState(UpdateNotSpawned);
            var stateSpawning = new AiState(UpdateSpawning);
            var stateFalling = new AiState(UpdateFalling);
            var stateJumping = new AiState(UpdateJumping);
            var stateWaiting = new AiState(UpdateWaiting);
            stateWaiting.Trigger.Add(new AiTriggerRandomTime(WaitingTrigger, 750, 1250));
            var stateShaking = new AiState();
            stateShaking.Trigger.Add(new AiTriggerCountdown(500, ShakingTick, ShakingEnd));
            var stateDespawning = new AiState(UpdateDespawning);
            var stateSpawnDelay = new AiState();
            stateSpawnDelay.Trigger.Add(new AiTriggerCountdown(450, null, EndSpawnDelay));

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("init", stateInit);
            _aiComponent.States.Add("notSpawned", stateNotSpawned);
            _aiComponent.States.Add("hidden", stateHidden);
            _aiComponent.States.Add("spawning", stateSpawning);
            _aiComponent.States.Add("fall", stateFalling);
            _aiComponent.States.Add("jumping", stateJumping);
            _aiComponent.States.Add("waiting", stateWaiting);
            _aiComponent.States.Add("shaking", stateShaking);
            _aiComponent.States.Add("despawning", stateDespawning);
            _aiComponent.States.Add("spawnDelay", stateSpawnDelay);
            _damageState = new AiDamageState(this, _body, _aiComponent, _sprite, 1);
            new AiFallState(_aiComponent, _body, null, null, 250);
            new AiDeepWaterState(_body);

            _aiComponent.ChangeState("init");

            var damageBox = new CBox(EntityPosition, -6, -11, 0, 12, 11, 4);
            var hittableBox = new CBox(EntityPosition, -6, -11, 0, 12, 11, 8, true);

            AddComponent(DamageFieldComponent.Index, _damageField = new DamageFieldComponent(damageBox, HitType.Enemy, 2));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, _damageState.OnHit));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(PushableComponent.Index, new PushableComponent(_body.BodyBox, OnPush));
            AddComponent(BaseAnimationComponent.Index, _animationComponent);
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, _sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(_body, _sprite));

            if (_fallMode)
                InitFalling();
            else
            {
                _body.IsActive = false;
                _damageState.IsActive = false;
                _damageField.IsActive = false;
                _sprite.IsVisible = false;
            }
        }

        /// <summary>
        /// Function used by the chest to stay in the air for a little bit before falling down
        /// </summary>
        public void SpawnDelay()
        {
            _body.IgnoresZ = true;
            _animator.Play("idle");
            _sprite.IsVisible = true;
            _aiComponent.ChangeState("spawnDelay");
        }

        private void EndSpawnDelay()
        {
            _damageState.IsActive = true;
            _damageField.IsActive = true;
            _body.IsActive = true;
            _body.IgnoresZ = false;
            _body.IsGrounded = false;

            _jumpsLeft = Game1.RandomNumber.Next(3, 5);
            ToJump();
        }

        private void InitFalling()
        {
            _body.IsGrounded = false;
            _animator.Play("jump");
            _aiComponent.ChangeState("jumping");
            _sprite.Color = Color.Transparent;
        }

        private void UpdateNotSpawned()
        {
            var distVec = EntityPosition.Position - MapManager.ObjLink.EntityPosition.Position;

            // spawn?
            if (distVec.Length() < 32)
                ToSpawning();
        }

        private void ToSpawning()
        {
            _jumpsLeft = Game1.RandomNumber.Next(3, 5);
            _animator.Play("spawn");
            _sprite.IsVisible = true;

            _aiComponent.ChangeState("spawning");
        }

        private void UpdateSpawning()
        {
            // fall down
            if (!_animator.IsPlaying)
                ToFalling();
        }

        private void ToFalling()
        {
            Game1.GameManager.PlaySoundEffect("D360-36-24");

            _pushable = true;
            _body.IsActive = true;
            _damageState.IsActive = true;
            _damageField.IsActive = true;

            // fall down
            EntityPosition.Z = 5;

            _animator.Play("idle");
            _aiComponent.ChangeState("fall");
        }

        private void UpdateFalling()
        {
            if (_body.IsGrounded)
                _aiComponent.ChangeState("shaking");
        }

        private void UpdateWaiting()
        {
            _animator.Play("idle");
        }

        private void WaitingTrigger()
        {
            if (_jumpsLeft <= 0 && !_fallMode)
                ToDespawn();
            else
                ToJump();
        }

        private void ToJump()
        {
            _jumpsLeft--;

            _aiComponent.ChangeState("jumping");
            _animator.Play("jump");

            Game1.GameManager.PlaySoundEffect("D360-36-24");

            var vecDirection = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            if (vecDirection != Vector2.Zero)
                vecDirection.Normalize();
            vecDirection *= 0.75f;

            _body.VelocityTarget = new Vector2(vecDirection.X, vecDirection.Y);
            _body.Velocity.Z = 1.5f;
        }

        private void UpdateJumping()
        {
            _sprite.Color = Color.White * MathF.Min((100 - EntityPosition.Z) / 10f, 1);

            if (_body.IsGrounded)
            {
                _body.VelocityTarget = Vector2.Zero;
                _aiComponent.ChangeState("waiting");
            }
        }

        private void ShakingTick(double time)
        {
            var shakeState = (float)Math.Sin(time / 25f);
            _animationComponent.SpriteOffset.X = -6 + shakeState;
            _animationComponent.UpdateSprite();
        }

        private void ShakingEnd()
        {
            _animationComponent.SpriteOffset.X = -6;
            _animationComponent.UpdateSprite();
            _aiComponent.ChangeState("waiting");
        }

        private void ToDespawn()
        {
            _aiComponent.ChangeState("despawning");
            _animator.Play("despawn");

            _pushable = false;
            _body.IsActive = false;
            _damageState.IsActive = false;
            _damageField.IsActive = false;
        }

        private void UpdateDespawning()
        {
            // hide
            if (_animator.IsPlaying)
                return;

            _sprite.IsVisible = false;
            _aiComponent.ChangeState("hidden");
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (!_pushable || type != PushableComponent.PushType.Impact)
                return false;

            _body.Velocity = new Vector3(direction.X, direction.Y, _body.Velocity.Z);
            return true;
        }
    }
}