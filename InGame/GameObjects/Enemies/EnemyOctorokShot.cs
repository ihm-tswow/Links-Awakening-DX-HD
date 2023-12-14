using Microsoft.Xna.Framework;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyOctorokShot : GameObject
    {
        private readonly AiComponent _aiComponent;
        private readonly ShadowBodyDrawComponent _shadowBody;
        private readonly DamageFieldComponent _damageField;
        private readonly CSprite _drawComponent;
        private readonly BodyComponent _body;
        private readonly PushableComponent _pushableComponent;

        private float _lifeCounter = 950;
        private float _despawnPercentage = 1;
        private int _despawnTime = 750;
        private bool _repelledPlayer;

        public EnemyOctorokShot(Map.Map map, float posX, float posY, Vector2 velocity) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX, posY, 2);
            EntitySize = new Rectangle(-5, -12, 10, 12);

            // abort spawn in a wall
            var box = Box.Empty;
            if (Map.Objects.Collision(new Box(EntityPosition.X - 4, EntityPosition.Y - 8, 0, 8, 8, 8),
                Box.Empty, Values.CollisionTypes.Normal, 0, 0, ref box))
            {
                IsDead = true;
                return;
            }

            var animator = AnimatorSaveLoad.LoadAnimator("Enemies/octorok shot");
            animator.Play("idle");

            _drawComponent = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(animator, _drawComponent, new Vector2(-5, -10));

            _body = new BodyComponent(EntityPosition, -4, -8, 8, 8, 8)
            {
                CollisionTypes = Values.CollisionTypes.Normal,
                MoveCollision = OnCollision,
                VelocityTarget = velocity,
                Bounciness = 0.35f,
                Drag = 0.75f,
                IgnoreHeight = true,
                IgnoresZ = true,
                IgnoreInsideCollision = false,
            };

            var stateIdle = new AiState(UpdateIdle);
            var stateDespawn = new AiState() { Init = InitDespawn };
            stateDespawn.Trigger.Add(new AiTriggerCountdown(_despawnTime, Despawn, () => Despawn(0)));

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("despawn", stateDespawn);
            _aiComponent.ChangeState("idle");

            var damageCollider = new CBox(EntityPosition, -5, -10, 0, 10, 10, 4);

            AddComponent(DamageFieldComponent.Index, _damageField = new DamageFieldComponent(damageCollider, HitType.Enemy, 2) { OnDamage = OnDamage });
            AddComponent(HittableComponent.Index, new HittableComponent(_body.BodyBox, OnHit));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(PushableComponent.Index, _pushableComponent = new PushableComponent(_body.BodyBox, OnPush) { RepelMultiplier = 0.35f });
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, _drawComponent, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, _shadowBody = new ShadowBodyDrawComponent(EntityPosition));
        }

        private void InitDespawn()
        {
            _pushableComponent.IsActive = false;
            _body.IgnoresZ = false;
            _damageField.IsActive = false;
            Game1.GameManager.PlaySoundEffect("D360-07-07");
        }

        private void UpdateIdle()
        {
            _lifeCounter -= Game1.DeltaTime;
            if (_lifeCounter < 0)
            {
                _body.IsGrounded = false;
                _body.IgnoresZ = false;
                _body.Gravity = -0.125f;
                _body.Bounciness = 0.75f;
                _body.Drag = 0.9f;
                _body.Velocity = new Vector3(_body.VelocityTarget.X, _body.VelocityTarget.Y, 0);
                _body.VelocityTarget = Vector2.Zero;
                _aiComponent.ChangeState("despawn");
            }
        }

        private void Despawn(double time)
        {
            _despawnPercentage = (float)(time / (_despawnTime / 3));
            if (_despawnPercentage > 1)
                _despawnPercentage = 1;

            _drawComponent.Color = Color.White * _despawnPercentage;
            _shadowBody.Transparency = _despawnPercentage;

            if (time <= 0)
                Map.Objects.DeleteObjects.Add(this);
        }

        private bool OnDamage()
        {
            _aiComponent.ChangeState("despawn");
            _body.Velocity = new Vector3(-_body.VelocityTarget.X * 0.25f, -_body.VelocityTarget.Y * 0.25f, 1.5f);
            _body.VelocityTarget = Vector2.Zero;

            return _damageField.DamagePlayer();
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (_aiComponent.CurrentStateId != "despawn")
                _aiComponent.ChangeState("despawn");
            else if (_repelledPlayer)
                return false;
            else
            {
                // it is possible that we despawn because of OnDamage in the same frame
                // we need to make sure to still repell the player
                _repelledPlayer = true;
                return _repelledPlayer;
            }

            _body.Velocity = new Vector3(direction.X * 0.25f, direction.Y * 0.25f, 1.5f);
            _body.VelocityTarget = Vector2.Zero;

            return true;
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (_aiComponent.CurrentStateId != "despawn")
                _aiComponent.ChangeState("despawn");
            else
                return Values.HitCollision.None;

            _body.Velocity = new Vector3(direction.X, direction.Y, 1.5f);
            _body.VelocityTarget = Vector2.Zero;

            return Values.HitCollision.Enemy;
        }

        private void OnCollision(Values.BodyCollision direction)
        {
            if (direction == Values.BodyCollision.Floor)
                return;

            if (_aiComponent.CurrentStateId != "despawn")
                _aiComponent.ChangeState("despawn");

            _body.Velocity = new Vector3(-_body.VelocityTarget.X * 0.25f, -_body.VelocityTarget.Y * 0.25f, 1.5f);
            _body.VelocityTarget = Vector2.Zero;
        }
    }
}