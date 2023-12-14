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

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyRiverZora : GameObject
    {
        private readonly AiComponent _aiComponent;
        private readonly AiDamageState _damageState;
        private readonly BodyComponent _body;
        private readonly Animator _animator;
        private readonly CSprite _sprite;

        private readonly Rectangle _fieldPosition;

        private float _floatCount;

        public EnemyRiverZora() : base("river zora") { }

        public EnemyRiverZora(Map.Map map, int posX, int posY) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY - 2 + 8, 0);
            EntitySize = new Rectangle(-8, -8, 16, 16);

            _fieldPosition = map.GetField(posX, posY);

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/river zora");
            _animator.Play("idle");

            _sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, _sprite, new Vector2(-8, -8));

            _body = new BodyComponent(EntityPosition, -6, -5, 12, 10, 8) { DragWater = 0.9f };

            var stateWaiting = new AiState();
            stateWaiting.Trigger.Add(new AiTriggerCountdown(4000, null, () => _aiComponent.ChangeState("positioning")));
            var statePositioning = new AiState(UpdatePositioning);
            var stateSpawning = new AiState() { Init = InitSpawning };
            stateSpawning.Trigger.Add(new AiTriggerCountdown(2000, null, ToIdle));
            var stateIdle = new AiState(UpdateIdle);
            stateIdle.Trigger.Add(new AiTriggerCountdown(500, null, ToAttacking));
            var stateAttacking = new AiState(UpdateAttacking);
            stateAttacking.Trigger.Add(new AiTriggerCountdown(600, null, ToDespawning));
            var stateDespawning = new AiState(UpdateDespawning);
            stateDespawning.Trigger.Add(new AiTriggerCountdown(500, null, ToWait));

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("waiting", stateWaiting);
            _aiComponent.States.Add("positioning", statePositioning);
            _aiComponent.States.Add("spawning", stateSpawning);
            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("attacking", stateAttacking);
            _aiComponent.States.Add("despawning", stateDespawning);
            _damageState = new AiDamageState(this, _body, _aiComponent, _sprite, 1) { HitMultiplierX = 1.5f, HitMultiplierY = 1.5f, FlameOffset = new Point(0, 2) };

            ToWait();

            AddComponent(BodyComponent.Index, _body);
            AddComponent(HittableComponent.Index, new HittableComponent(_body.BodyBox, _damageState.OnHit));
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, _sprite, Values.LayerPlayer) { WaterOutline = false });
        }

        private void ToWait()
        {
            _aiComponent.ChangeState("waiting");

            _floatCount = 0;
            _sprite.DrawOffset.Y = -8;

            _sprite.IsVisible = false;
            _body.IsGrounded = false;
            _damageState.IsActive = false;

            // splash effect
            var splashAnimator = new ObjAnimator(Map, 0, 0, 0, 3, Values.LayerPlayer, "Particles/splash", "idle", true);
            splashAnimator.EntityPosition.Set(new Vector2(
                _body.Position.X + _body.OffsetX + _body.Width / 2f,
                _body.Position.Y + _body.OffsetY + _body.Height - _body.Position.Z - 3));
            Map.Objects.SpawnObject(splashAnimator);
        }

        private void UpdatePositioning()
        {
            // try to find a new position
            for (var i = 0; i < 25; i++)
            {
                // find new position
                var newPosition = new Vector2(
                    _fieldPosition.X + Game1.RandomNumber.Next(0, 10) * 16 + 8,
                    _fieldPosition.Y + Game1.RandomNumber.Next(0, 8) * 16 + 8 - 2);

                var fieldState = Map.GetFieldState(newPosition);
                if ((fieldState & MapStates.FieldStates.DeepWater) != 0)
                {
                    EntityPosition.Set(newPosition);
                    _aiComponent.ChangeState("spawning");
                    return;
                }
            }
        }

        private void InitSpawning()
        {
            _sprite.IsVisible = true;
            _animator.Play("spawn");
        }

        private void ToIdle()
        {
            _aiComponent.ChangeState("idle");

            _animator.Play("idle");
            _damageState.IsActive = true;
        }

        private void UpdateIdle()
        {
            UpdateOffset();
        }

        private void ToAttacking()
        {
            var distance = EntityPosition.Position - MapManager.ObjLink.EntityPosition.Position;
            if (distance.Length() > 90)
            {
                ToDespawning();
                return;
            }

            _aiComponent.ChangeState("attacking");
            _animator.Play("attack");

            // spawn a fireball
            Map.Objects.SpawnObject(new EnemyFireball(Map, (int)EntityPosition.X, (int)EntityPosition.Y, 1.5f));
        }

        private void UpdateAttacking()
        {
            UpdateOffset();
        }

        private void ToDespawning()
        {
            _aiComponent.ChangeState("despawning");

            _animator.Play("idle");
        }

        private void UpdateDespawning()
        {
            UpdateOffset();
        }

        private void UpdateOffset()
        {
            _floatCount += Game1.DeltaTime;
            _sprite.DrawOffset.Y = -8 - (float)Math.Sin(_floatCount / 200f);
        }
    }
}