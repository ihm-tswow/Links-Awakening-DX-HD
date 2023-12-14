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
    internal class EnemyZombie : GameObject
    {
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly Animator _animator;
        private readonly AiDamageState _damageState;
        private readonly DamageFieldComponent _damageField;

        public EnemyZombie() : base("zombie") { }

        public EnemyZombie(Map.Map map, int posX, int posY) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/zombie");

            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, sprite, new Vector2(-8, -16));

            _body = new BodyComponent(EntityPosition, -6, -10, 12, 10, 8)
            {
                MoveCollision = OnCollision,
                HoleAbsorb = OnHoleAbsorb,
                AvoidTypes = Values.CollisionTypes.Hole | Values.CollisionTypes.NPCWall,
                Drag = 0.8f
            };

            // ai states
            var stateSpawn = new AiState(UpdateSpawn) { Init = InitSpawn };
            var walkingState = new AiState() { Init = InitWalking };
            walkingState.Trigger.Add(new AiTriggerRandomTime(() => _aiComponent.ChangeState("despawn"), 1000, 4000));
            var stateDespawn = new AiState(UpdateDespawning) { Init = InitDespawning };

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("spawn", stateSpawn);
            _aiComponent.States.Add("walking", walkingState);
            _aiComponent.States.Add("despawn", stateDespawn);
            _damageState = new AiDamageState(this, _body, _aiComponent, sprite, 1) { OnBurn = () => _animator.Pause(), IsActive = false };
            new AiFallState(_aiComponent, _body, OnHoleAbsorb);
            _aiComponent.ChangeState("spawn");

            var hittableBox = new CBox(EntityPosition, -6, -15, 0, 12, 15, 8);
            var damageBox = new CBox(EntityPosition, -6, -14, 0, 12, 14, 4);
            var pushableBox = new CBox(EntityPosition, -6, -13, 0, 12, 13, 8);

            AddComponent(PushableComponent.Index, new PushableComponent(pushableBox, OnPush));
            AddComponent(DamageFieldComponent.Index, _damageField = new DamageFieldComponent(damageBox, HitType.Enemy, 2));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new DrawShadowCSpriteComponent(sprite));
        }

        private void InitSpawn()
        {
            _animator.Play("spawn");
        }

        private void UpdateSpawn()
        {
            if (!_animator.IsPlaying)
                _aiComponent.ChangeState("walking");
        }

        private void InitWalking()
        {
            if (_animator.CurrentFrameIndex > 0)
                _damageState.IsActive = true;

            _damageField.IsActive = true;

            _animator.Play("walk");

            // start walking towards the player
            var walkDirection = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;

            if (walkDirection != Vector2.Zero)
                walkDirection.Normalize();

            _body.VelocityTarget = walkDirection * Game1.RandomNumber.Next(50, 80) / 100f;
        }

        private void InitDespawning()
        {
            // start despawn animation and stop moving
            _animator.Play("despawn");
            _body.Velocity = Vector3.Zero;
            _body.VelocityTarget = Vector2.Zero;
            _damageField.IsActive = false;
        }

        private void UpdateDespawning()
        {
            if (_animator.CurrentFrameIndex > 0)
                _damageState.IsActive = false;

            if (!_animator.IsPlaying)
                Map.Objects.DeleteObjects.Add(this);
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (_aiComponent.CurrentStateId == "despawn")
                return false;

            if (type == PushableComponent.PushType.Impact)
                _body.Velocity = new Vector3(direction.X * 1.75f, direction.Y * 1.75f, _body.Velocity.Z);

            return true;
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            return _damageState.OnHit(gameObject, direction, damageType, damage, pieceOfPower);
        }

        private void OnCollision(Values.BodyCollision direction)
        {
            if (_aiComponent.CurrentStateId == "walking")
            {
                if ((direction & Values.BodyCollision.Horizontal) != 0 &&
                   Math.Abs(_body.VelocityTarget.X) > Math.Abs(_body.VelocityTarget.Y) * 3 ||
                   (direction & Values.BodyCollision.Vertical) != 0 &&
                   Math.Abs(_body.VelocityTarget.Y) > Math.Abs(_body.VelocityTarget.X) * 3)
                    _aiComponent.ChangeState("despawn");
            }
        }

        public void OnHoleAbsorb()
        {
            _animator.SpeedMultiplier = 2f;
        }
    }
}