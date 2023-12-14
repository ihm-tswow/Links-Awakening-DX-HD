using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyBeetle : GameObject
    {
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly Animator _animator;
        private readonly AiFallState _aiFallState;
        private readonly AiTriggerRandomTime _directionChangeCounter;
        private readonly AiDamageState _damageState;

        private float _walkSpeed = 0.5f;
        private int _direction;

        private bool _finishedSpawning;

        public EnemyBeetle() : base("beetle") { }

        public EnemyBeetle(Map.Map map, int posX, int posY) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 14, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/beetle");
            _animator.Play("idle");

            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, sprite, new Vector2(-8, -11));

            _body = new BodyComponent(EntityPosition, -5, -8, 10, 8, 8)
            {
                AbsorbPercentage = 0.9f,
                CollisionTypes =
                    Values.CollisionTypes.Normal |
                    Values.CollisionTypes.Enemy |
                    Values.CollisionTypes.Player,
                AvoidTypes =
                    Values.CollisionTypes.Hole |
                    Values.CollisionTypes.NPCWall,
                HoleOnPull = OnHolePull,
                AbsorbStop = 0,
                FieldRectangle = map.GetField(posX, posY),
                Bounciness = 0.25f,
                Drag = 0.85f,
                MoveCollision = OnMoveCollision
            };

            var stateMoving = new AiState();
            stateMoving.Trigger.Add(_directionChangeCounter = new AiTriggerRandomTime(ChangeDirection, 500, 750));

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("moving", stateMoving);

            // AiFallState sets the HoleAbsorb function
            _aiFallState = new AiFallState(_aiComponent, _body, OnHoleAbsorb, null);
            _body.HoleAbsorb = OnHoleAbsorb;

            var playerDirection = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            playerDirection.Normalize();
            _body.VelocityTarget = playerDirection * _walkSpeed;

            _aiComponent.ChangeState("moving");

            var damageCollider = new CBox(EntityPosition, -6, -10, 0, 12, 10, 4);
            _damageState = new AiDamageState(this, _body, _aiComponent, sprite, 1);
            
            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageCollider, HitType.Enemy, 2));
            AddComponent(HittableComponent.Index, new HittableComponent(_body.BodyBox, OnHit));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(PushableComponent.Index, new PushableComponent(_body.BodyBox, OnPush));
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new DrawShadowCSpriteComponent(sprite) { Height = 1.0f, Rotation = 0.1f });
        }

        private void OnMoveCollision(Values.BodyCollision collision)
        {
            _directionChangeCounter.CurrentTime *= 0.5f;
            _body.VelocityTarget = Vector2.Zero;
        }

        private void OnHolePull(Vector2 direction, float percentage)
        {
            if (!_finishedSpawning)
            {
                _body.HoleAbsorption *= 0.25f;
                _body.SpeedMultiply = 1.0f;

                if (percentage == 0)
                {
                    _finishedSpawning = true;
                    ChangeDirection();
                }
            }
        }

        private void OnHoleAbsorb()
        {
            if (_finishedSpawning)
            {
                _animator.SpeedMultiplier = 2f;
                _aiFallState.OnHoleAbsorb();
            }
            else
            {
                _body.HoleAbsorption *= 0.25f;
                _body.SpeedMultiply = 1.0f;
            }
        }

        private void ChangeDirection()
        {
            // random start position/state
            _direction = Game1.RandomNumber.Next(0, 4);
            _body.VelocityTarget = AnimationHelper.DirectionOffset[_direction] * _walkSpeed;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type == PushableComponent.PushType.Impact)
                _body.Velocity = new Vector3(direction.X * 1.75f, direction.Y * 1.75f, _body.Velocity.Z);

            _finishedSpawning = true;

            return true;
        }

        public Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            // make sure to fall into the hole after getting burned while coming out
            _finishedSpawning = true;

            return _damageState.OnHit(gameObject, direction, damageType, damage, pieceOfPower);
        }
    }
}