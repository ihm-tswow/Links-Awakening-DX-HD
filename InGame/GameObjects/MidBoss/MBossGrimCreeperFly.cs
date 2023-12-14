using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;
using ProjectZ.InGame.Map;
using System;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class MBossGrimCreeperFly : GameObject
    {
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly Animator _animator;
        private readonly CSprite _sprite;
        private readonly DamageFieldComponent _damageField;
        private readonly HittableComponent _hittableComponent;
        private readonly AiDamageState _damageState;
        private readonly BodyDrawShadowComponent _shadowComponent;

        private readonly Vector2 _targetPosition;

        private const float FlySpeed = 1;
        private const float AttackSpeed = 2;

        private const float FadeTime = 100;
        private float _fadeCounter;

        private Vector2 _roomCenter;
        private Vector2 _centerPosition;

        private Vector2 _leaveStart;
        private Vector2 _leaveEnd;
        private float _leaveCounter;

        private float _circlingOffset;
        private float _circleSpeed;

        // TODO: sync animations
        public MBossGrimCreeperFly(Map.Map map, Vector2 position, Vector2 targetPosition) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(position.X, position.Y, 28);
            EntitySize = new Rectangle(-8, -42, 16, 42);

            _roomCenter = Map.GetRoomCenter(targetPosition.X, targetPosition.Y - 43);
            _targetPosition = targetPosition;

            _animator = AnimatorSaveLoad.LoadAnimator("MidBoss/grim creeper fly");
            _animator.Play("idle");

            _sprite = new CSprite(EntityPosition) { Color = Color.Transparent };
            var animatorComponent = new AnimationComponent(_animator, _sprite, Vector2.Zero);

            _body = new BodyComponent(EntityPosition, -6, -12, 12, 12, 8)
            {
                IgnoresZ = true,
                IgnoreHoles = true,
                Bounciness = 0.25f,
                Gravity = -0.175f,
                CollisionTypes = Values.CollisionTypes.None
            };

            _aiComponent = new AiComponent();

            var stateSpawn = new AiState(UpdateSpawn);
            var stateIdle = new AiState();
            var stateAttack = new AiState(UpdateAttacking) { Init = InitAttack };
            var stateFadeout = new AiState(UpdateFadeout) { Init = InitFadeout };

            // grim creeper states
            var stateCircling = new AiState(UpdateCircling);
            var stateLeave = new AiState(UpdateLeave);

            _aiComponent.States.Add("spawn", stateSpawn);
            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("attack", stateAttack);
            _aiComponent.States.Add("fadeout", stateFadeout);
            _damageState = new AiDamageState(this, _body, _aiComponent, _sprite, 1)
            {
                OnBurn = OnBurn
            };

            _aiComponent.States.Add("circling", stateCircling);
            _aiComponent.States.Add("leave", stateLeave);

            _aiComponent.ChangeState("spawn");

            var damageBox = new CBox(EntityPosition, -5, -12, 0, 10, 10, 8, true);
            var hittableBox = new CBox(EntityPosition, -7, -14, 0, 14, 14, 8, true);

            AddComponent(DamageFieldComponent.Index, _damageField = new DamageFieldComponent(damageBox, HitType.Enemy, 2) { IsActive = false });
            AddComponent(HittableComponent.Index, _hittableComponent = new HittableComponent(hittableBox, _damageState.OnHit) { IsActive = false });
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BodyComponent.Index, _body);
            AddComponent(BaseAnimationComponent.Index, animatorComponent);
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, _sprite, Values.LayerPlayer) { WaterOutline = false });
            AddComponent(DrawShadowComponent.Index, _shadowComponent = new BodyDrawShadowComponent(_body, _sprite) { Transparency = 0 });
        }

        public void StartSequenceMode()
        {
            EntityPosition.Z = 0;
            _centerPosition = EntityPosition.Position;

            _body.VelocityTarget = Vector2.Zero;
            _sprite.Color = Color.White;
            _aiComponent.ChangeState("circling");

            _circlingOffset = Game1.RandomNumber.Next(0, 20) / 5f;
            _circleSpeed = Game1.RandomNumber.Next(100, 125);
        }

        public void ToLeave()
        {
            _aiComponent.ChangeState("leave");
            _leaveStart = EntityPosition.Position;
            _leaveEnd = _targetPosition;
        }

        public void FightInit()
        {
            Game1.GameManager.PlaySoundEffect("D360-49-31");
        }

        private void OnBurn()
        {
            _body.IgnoresZ = false;
        }

        private void UpdateSpawn()
        {
            UpdateFading(false);

            // move towards the target position
            var direction = _targetPosition - EntityPosition.Position;
            if (direction.Length() > FlySpeed * Game1.TimeMultiplier)
            {
                direction.Normalize();
                _body.VelocityTarget = direction * FlySpeed;
            }
            else
            {
                // reached the target position
                _body.VelocityTarget = Vector2.Zero;
                EntityPosition.Set(_targetPosition);
                _aiComponent.ChangeState("idle");
            }
        }

        private void UpdateLeave()
        {
            _leaveCounter += Game1.DeltaTime;

            var percentage = 1 - MathF.Cos(_leaveCounter / 1000 * MathF.PI / 2);
            var newPosition = Vector2.Lerp(_leaveStart, _leaveEnd, percentage);
            EntityPosition.Set(newPosition);

            // fade out
            var transparency = Math.Clamp((1000 - _leaveCounter) / FadeTime, 0, 1);
            _sprite.Color = Color.White * transparency;

            if (percentage > 1)
            {
                Map.Objects.DeleteObjects.Add(this);
            }
        }

        private void UpdateCircling()
        {
            var newPosition = _centerPosition + new Vector2(
                MathF.Sin((float)Game1.TotalGameTime / _circleSpeed + _circlingOffset) * 1,
                MathF.Sin((float)Game1.TotalGameTime / _circleSpeed + 1.0f + _circlingOffset) * 2);
            EntityPosition.Set(newPosition);
        }

        public void StartAttack()
        {
            _aiComponent.ChangeState("attack");
        }

        public bool IsAlive()
        {
            return _damageState.CurrentLives > 0;
        }

        private void InitAttack()
        {
            Game1.GameManager.PlaySoundEffect("D360-49-31");

            _damageField.IsActive = true;
            _hittableComponent.IsActive = true;

            // fly towards the player
            var direction = MapManager.ObjLink.EntityPosition.Position - new Vector2(EntityPosition.X, EntityPosition.Y - 12);
            if (direction != Vector2.Zero)
            {
                direction.Normalize();
                _body.VelocityTarget = direction * AttackSpeed;
            }
        }

        private void UpdateAttacking()
        {
            if (EntityPosition.Z > 12)
                EntityPosition.Z -= 0.5f * Game1.TimeMultiplier;

            var direction = _roomCenter - new Vector2(EntityPosition.X, EntityPosition.Y - EntityPosition.Z);

            // fade out?
            if (direction.Length() > 120)
                _aiComponent.ChangeState("fadeout");
        }

        private void InitFadeout()
        {
            _fadeCounter = 0;
        }

        private void UpdateFadeout()
        {
            if (UpdateFading(true))
                Map.Objects.DeleteObjects.Add(this);
        }

        private bool UpdateFading(bool fadeOut)
        {
            _fadeCounter += Game1.DeltaTime;
            if (_fadeCounter > FadeTime)
                _fadeCounter = FadeTime;

            var transparency = _fadeCounter / FadeTime;
            if (fadeOut)
                transparency = 1 - transparency;

            _sprite.Color = Color.White * transparency;
            _shadowComponent.Transparency = transparency;

            return _fadeCounter == FadeTime;
        }
    }
}