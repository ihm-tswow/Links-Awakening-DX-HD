using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;
using ProjectZ.Base;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyBombite : GameObject
    {
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly Animator _animator;
        private readonly AiDamageState _damageState;
        private readonly AiTriggerSwitch _damageCooldown;
        private readonly CBox _pongCollider;

        private const float WalkSpeed = 0.5f;

        private int _direction;

        public EnemyBombite() : base("bombite") { }

        public EnemyBombite(Map.Map map, int posX, int posY) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/bombite");
            _animator.Play("idle");

            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, sprite, new Vector2(-7, -16));

            _body = new BodyComponent(EntityPosition, -6, -12, 12, 11, 8)
            {
                MoveCollision = OnCollision,
                AbsorbPercentage = 0.9f,
                CollisionTypes = Values.CollisionTypes.Normal,
                AvoidTypes =
                    Values.CollisionTypes.Hole |
                    Values.CollisionTypes.NPCWall,
                FieldRectangle = map.GetField(posX, posY),
                Bounciness = 0.25f,
                Drag = 0.85f,
            };

            var stateIdle = new AiState();
            stateIdle.Trigger.Add(new AiTriggerRandomTime(ChangeDirection, 250, 500));
            var statePong = new AiState(UpdatePong);
            statePong.Trigger.Add(new AiTriggerCountdown(1100, null, Explode));
            statePong.Trigger.Add(new AiTriggerCountdown(750, null, StartBlinking));

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("pong", statePong);
            new AiFallState(_aiComponent, _body, OnHoleAbsorb, null);
            _damageState = new AiDamageState(this, _body, _aiComponent, sprite, 1, false);

            _aiComponent.Trigger.Add(_damageCooldown = new AiTriggerSwitch(250));

            _aiComponent.ChangeState("idle");
            ChangeDirection();

            var damageBox = new CBox(EntityPosition, -6, -13, 0, 12, 12, 4);
            _pongCollider = new CBox(EntityPosition, -6, -12, 0, 12, 11, 8);

            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageBox, HitType.Enemy, 4));
            AddComponent(HittableComponent.Index, new HittableComponent(_body.BodyBox, OnHit));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(PushableComponent.Index, new PushableComponent(_body.BodyBox, OnPush));
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new DrawShadowCSpriteComponent(sprite) { Height = 1.0f, Rotation = 0.1f });
        }

        private void UpdatePong()
        {
            var hitReturn = Map.Objects.Hit(this, _pongCollider.Box.Center, _pongCollider.Box, HitType.Bomb, 2, false);
            if (hitReturn == Values.HitCollision.Enemy)
                Explode();
        }

        private void ChangeDirection()
        {
            _direction = Game1.RandomNumber.Next(0, 4);
            _body.VelocityTarget = AnimationHelper.DirectionOffset[_direction] * WalkSpeed;
        }

        private void StartBlinking()
        {
            _damageState.SetDamageState();
        }

        private void Explode()
        {
            // spawn explosion effect
            var objExplosion = new ObjBomb(Map, EntityPosition.X, EntityPosition.Y, false, false) { DamageEnemies = true };
            objExplosion.Explode();
            Map.Objects.SpawnObject(objExplosion);

            Map.Objects.DeleteObjects.Add(this);
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (!_damageCooldown.State || gameObject == this)
                return Values.HitCollision.None;
            _damageCooldown.Reset();

            if (damageType == HitType.Bomb && !(gameObject is EnemyBombite))
            {
                // spawn a bomb
                _damageState.SpawnItem = "bomb_1";
                return _damageState.OnHit(gameObject, direction, damageType, damage, pieceOfPower);
            }

            // hookshot/boomerang freeze

            _body.FieldRectangle = RectangleF.Empty;

            if (damageType != HitType.MagicPowder)
                _body.VelocityTarget = direction * 3;
            else
                _body.VelocityTarget = Vector2.Zero;

            _animator.Play("damage");
            _aiComponent.ChangeState("pong");

            return Values.HitCollision.Enemy;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type == PushableComponent.PushType.Impact)
                _body.Velocity = new Vector3(direction.X * 1.75f, direction.Y * 1.75f, _body.Velocity.Z);

            return true;
        }

        private void OnCollision(Values.BodyCollision direction)
        {
            if (_aiComponent.CurrentStateId == "pong")
            {
                Game1.GameManager.PlaySoundEffect("D360-09-09");

                if ((direction & Values.BodyCollision.Horizontal) != 0)
                    _body.VelocityTarget.X = -_body.VelocityTarget.X;
                else if ((direction & Values.BodyCollision.Vertical) != 0)
                    _body.VelocityTarget.Y = -_body.VelocityTarget.Y;
            }
        }

        private void OnHoleAbsorb()
        {
            _animator.SpeedMultiplier = 3f;
            _animator.Play("idle");
        }
    }
}