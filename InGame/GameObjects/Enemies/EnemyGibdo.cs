using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyGibdo : GameObject
    {
        private readonly Animator _animator;
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly AiDamageState _damageState;
        private readonly AiStunnedState _aiStunnedState;

        private const float MoveSpeed = 0.5f;

        private int _direction;

        public EnemyGibdo() : base("gibdo") { }

        public EnemyGibdo(Map.Map map, int posX, int posY) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/gibdo");
            _animator.Play("idle");

            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, sprite, new Vector2(-8, -16));

            _body = new BodyComponent(EntityPosition, -6, -10, 12, 10, 8)
            {
                MoveCollision = OnCollision,
                CollisionTypes = Values.CollisionTypes.Normal |
                                 Values.CollisionTypes.Enemy,
                AvoidTypes = Values.CollisionTypes.Hole | Values.CollisionTypes.NPCWall,
                FieldRectangle = map.GetField(posX, posY),
                Bounciness = 0.25f,
                Drag = 0.85f
            };

            var stateWalking = new AiState { Init = InitWalking };
            stateWalking.Trigger.Add(new AiTriggerRandomTime(() => _aiComponent.ChangeState("walk"), 550, 850));

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("walk", stateWalking);
            _damageState = new AiDamageState(this, _body, _aiComponent, sprite, 6, false)
            {
                HitMultiplierX = 1.0f,
                HitMultiplierY = 1.0f,
                OnDeath = OnDeath,
                OnBurn = () => _animator.Pause()
            };
            _aiStunnedState = new AiStunnedState(_aiComponent, animationComponent, 3300, 900);
            new AiFallState(_aiComponent, _body, OnHoleAbsorb);

            _aiComponent.ChangeState("walk");

            var damageBox = new CBox(EntityPosition, -7, -14, 0, 14, 14, 4);
            var pushableBox = new CBox(EntityPosition, -6, -13, 0, 12, 13, 4);
            var hittableBox = new CBox(EntityPosition, -7, -15, 14, 15, 8);

            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageBox, HitType.Enemy, 4));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(PushableComponent.Index, new PushableComponent(pushableBox, OnPush));
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new DrawShadowCSpriteComponent(sprite));
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (damageType == HitType.Bomb)
                damage = 3;
            if (damageType == HitType.Boomerang)
                damage = 2;
            if (damageType == HitType.Bow)
                damage = 1;

            if (damageType == HitType.Hookshot)
            {
                _body.VelocityTarget = Vector2.Zero;
                _body.Velocity.X += direction.X * 0.75f;
                _body.Velocity.Y += direction.Y * 0.75f;

                _aiStunnedState.StartStun();
                _animator.Pause();

                return Values.HitCollision.Enemy;
            }

            return _damageState.OnHit(gameObject, direction, damageType, damage, pieceOfPower);
        }

        private void OnDeath(bool pieceOfPower)
        {
            if (Map == null)
                return;

            if (_aiComponent.CurrentStateId == "burning")
            {
                Map.Objects.DeleteObjects.Add(this);
                // spawn the stalfos orange
                Map.Objects.SpawnObject(new EnemyStalfosOrange(Map, (int)EntityPosition.X - 8, (int)EntityPosition.Y - 16, true));
            }
            else
            {
                _damageState.BaseOnDeath(pieceOfPower);
            }
        }

        private void InitWalking()
        {
            _animator.Play("idle");

            // walk into a random direction
            _direction = Game1.RandomNumber.Next(0, 4);
            _body.VelocityTarget = AnimationHelper.DirectionOffset[_direction] * MoveSpeed;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type == PushableComponent.PushType.Impact)
            {
                _body.Velocity.X = direction.X * 0.75f;
                _body.Velocity.Y = direction.Y * 0.75f;
            }

            return true;
        }

        private void OnCollision(Values.BodyCollision direction)
        {
            if ((direction & Values.BodyCollision.Horizontal) != 0)
                _body.VelocityTarget.X = -_body.VelocityTarget.X;
            if ((direction & Values.BodyCollision.Vertical) != 0)
                _body.VelocityTarget.Y = -_body.VelocityTarget.Y;
        }

        private void OnHoleAbsorb()
        {
            _animator.SpeedMultiplier = 3f;
        }
    }
}