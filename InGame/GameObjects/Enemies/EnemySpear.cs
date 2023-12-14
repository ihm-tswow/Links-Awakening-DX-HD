using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;
using System;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemySpear : GameObject
    {
        private readonly Animator _animator;
        private readonly AiComponent _aiComponent;
        private readonly DamageFieldComponent _damageField;
        private readonly BodyComponent _body;
        private readonly CSprite _drawComponent;
        private readonly BodyDrawComponent _bodyDrawComponent;
        private readonly ShadowBodyDrawComponent _shadowBody;

        private Vector2 _startPosition;

        private float _despawnPercentage = 1;
        private int _despawnTime = 500;
        private int dir;

        private Point[] _collisionBoxSize = { new Point(12, 4), new Point(4, 12), new Point(12, 4), new Point(4, 12) };

        public EnemySpear(Map.Map map, Vector3 position, Vector2 velocity) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(position);
            EntitySize = new Rectangle(-8, -8, 16, 16);

            _startPosition = EntityPosition.Position;

            dir = AnimationHelper.GetDirection(velocity);
            _animator = AnimatorSaveLoad.LoadAnimator("Objects/spear");
            _animator.Play(dir.ToString());

            _drawComponent = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, _drawComponent, Vector2.Zero);

            _body = new BodyComponent(EntityPosition,
                -_collisionBoxSize[dir].X / 2, -_collisionBoxSize[dir].Y / 2,
                _collisionBoxSize[dir].X, _collisionBoxSize[dir].Y, 8)
            {
                CollisionTypes = Values.CollisionTypes.Normal,
                MoveCollision = OnCollision,
                VelocityTarget = velocity,
                Bounciness = 0.35f,
                Drag = 0.75f,
                IgnoreHeight = true,
                IgnoresZ = true,
            };

            var damageCollider = new CBox(EntityPosition, -5, -5, 0, 10, 10, 4, true);

            var stateDespawn = new AiState() { Init = InitDespawn };
            stateDespawn.Trigger.Add(new AiTriggerCountdown(_despawnTime, TickDespawn, () => TickDespawn(0)));

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("idle", new AiState(UpdateIdle));
            _aiComponent.States.Add("despawn", stateDespawn);
            _aiComponent.ChangeState("idle");

            AddComponent(DamageFieldComponent.Index, _damageField = new DamageFieldComponent(damageCollider, HitType.Enemy, 2) { OnDamage = OnDamage });
            AddComponent(HittableComponent.Index, new HittableComponent(_body.BodyBox, OnHit));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(PushableComponent.Index, new PushableComponent(_body.BodyBox, OnPush) { RepelMultiplier = 0.2f });
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, _bodyDrawComponent = new BodyDrawComponent(_body, _drawComponent, Values.LayerPlayer) { Gras = false });
            AddComponent(DrawShadowComponent.Index, _shadowBody = new ShadowBodyDrawComponent(EntityPosition));
        }

        public override void Init()
        {
            Game1.GameManager.PlaySoundEffect("D378-10-0A");
        }

        private void UpdateIdle()
        {
            // start falling down?
            var distance = _startPosition - EntityPosition.Position;
            if (MathF.Abs(distance.X) > 112 || Math.Abs(distance.Y) > 96)
                _body.IgnoresZ = false;
        }

        private void InitDespawn()
        {
            _body.IgnoresZ = false;
            _damageField.IsActive = false;
            _bodyDrawComponent.Gras = true;

            _animator.Play("rotate");
            _animator.SetFrame((dir + 1) % 4);

            Game1.GameManager.PlaySoundEffect("D360-07-07");
        }

        private void TickDespawn(double time)
        {
            _despawnPercentage = (float)(time / (_despawnTime / 2));
            if (_despawnPercentage > 1)
                _despawnPercentage = 1;

            _drawComponent.Color = Color.White * _despawnPercentage;
            _shadowBody.Transparency = _despawnPercentage;

            if (time <= 0)
                Map.Objects.DeleteObjects.Add(this);
        }

        private bool OnDamage()
        {
            Map.Objects.DeleteObjects.Add(this);
            return _damageField.DamagePlayer();
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (_despawnPercentage < 1)
                return false;

            if (_aiComponent.CurrentStateId != "despawn")
                _aiComponent.ChangeState("despawn");

            _body.Velocity = new Vector3(direction.X * 0.25f, direction.Y * 0.25f, 1.5f);
            _body.VelocityTarget = Vector2.Zero;

            return true;
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (_despawnPercentage < 1)
                return Values.HitCollision.None;

            if (_aiComponent.CurrentStateId != "despawn")
                _aiComponent.ChangeState("despawn");

            _body.Velocity = new Vector3(direction.X, direction.Y, 1.5f);
            _body.VelocityTarget = Vector2.Zero;

            return Values.HitCollision.Enemy;
        }

        private void OnCollision(Values.BodyCollision direction)
        {
            if (_aiComponent.CurrentStateId == "despawn")
                return;

            _aiComponent.ChangeState("despawn");

            if ((direction & Values.BodyCollision.Floor) != 0)
                _body.Velocity = new Vector3(_body.VelocityTarget.X * 0.75f, _body.VelocityTarget.Y * 0.75f, 1.5f);
            else
                _body.Velocity = new Vector3(-_body.VelocityTarget.X * 0.25f, -_body.VelocityTarget.Y * 0.25f, 1.5f);

            _body.VelocityTarget = Vector2.Zero;
        }
    }
}