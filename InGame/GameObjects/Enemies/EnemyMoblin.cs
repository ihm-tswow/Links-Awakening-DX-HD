using Microsoft.Xna.Framework;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyMoblin : GameObject
    {
        private readonly Animator _animator;
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;

        private readonly Vector2[] _shotOffset =
        {
            new Vector2(-8, -1),new Vector2(0, -3),
            new Vector2(8, -1),new Vector2(0, 2)
        };

        private float _moveSpeed = 0.5f;
        private int _direction;

        public EnemyMoblin() : base("moblin") { }

        public EnemyMoblin(Map.Map map, int posX, int posY) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/moblin");
            _animator.Play("walk_1");

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

            var stateInit = new AiState();
            stateInit.Trigger.Add(new AiTriggerRandomTime(() => _aiComponent.ChangeState("walking"), 0, 500));
            var stateIdle = new AiState { Init = InitIdle };
            stateIdle.Trigger.Add(new AiTriggerRandomTime(() => _aiComponent.ChangeState("walking"), 300, 500));
            var stateWalking = new AiState { Init = InitWalking };
            stateWalking.Trigger.Add(new AiTriggerRandomTime(() => _aiComponent.ChangeState("idle"), 550, 850));

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("init", stateInit);
            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("walking", stateWalking);
            new AiFallState(_aiComponent, _body, OnHoleAbsorb);
            var damageState = new AiDamageState(this, _body, _aiComponent, sprite, 2) { OnBurn = () => _animator.Pause() };
            _aiComponent.ChangeState("init");

            // stand in a random direction
            _direction = Game1.RandomNumber.Next(0, 4);
            _animator.Play("stand_" + _direction);

            var damageBox = new CBox(EntityPosition, -8, -12, 0, 16, 12, 4);
            var hittableBox = new CBox(EntityPosition, -7, -15, 14, 15, 8);
            var pushableBox = new CBox(EntityPosition, -7, -11, 0, 14, 11, 4);

            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageBox, HitType.Enemy, 2));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, damageState.OnHit));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(PushableComponent.Index, new PushableComponent(pushableBox, OnPush));
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new DrawShadowCSpriteComponent(sprite));
        }

        private void InitIdle()
        {
            _animator.Play("stand_" + _direction);
            _body.VelocityTarget = Vector2.Zero;

            ThrowSpear();
        }

        private void InitWalking()
        {
            ChangeDirection();
        }

        private void ChangeDirection()
        {
            // random new direction
            _direction = Game1.RandomNumber.Next(0, 4);
            _animator.Play("walk_" + _direction);
            _body.VelocityTarget = AnimationHelper.DirectionOffset[_direction] * _moveSpeed;
        }

        private void ThrowSpear()
        {
            if (Game1.RandomNumber.Next(0, 2) == 0)
                return;

            // shoot if the player is in the range and in the right direction
            var playerDirection = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            if (playerDirection.Length() < 96)
            {
                if (playerDirection != Vector2.Zero)
                    playerDirection.Normalize();
                var direction = AnimationHelper.GetDirection(playerDirection);

                if (direction == _direction)
                {
                    var box = Box.Empty;
                    // check for collision
                    if (!Map.Objects.Collision(new Box(
                            EntityPosition.X + _shotOffset[_direction].X - 4,
                            EntityPosition.Y + _shotOffset[_direction].Y - 4, 0, 8, 8, 8),
                            Box.Empty, Values.CollisionTypes.Normal, 0, _body.Level, ref box))
                    {
                        // shoot
                        var shot = new EnemySpear(Map, new Vector3(
                            EntityPosition.X + _shotOffset[_direction].X,
                            EntityPosition.Y + _shotOffset[_direction].Y, 3),
                            AnimationHelper.DirectionOffset[_direction] * 2f);
                        Map.Objects.SpawnObject(shot);
                    }
                }
            }
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type == PushableComponent.PushType.Impact)
                _body.Velocity = new Vector3(direction.X, direction.Y, _body.Velocity.Z);

            return true;
        }

        private void OnCollision(Values.BodyCollision direction)
        {
            if (_aiComponent.CurrentStateId != "walking")
                return;

            // stop walking
            _aiComponent.ChangeState("idle");
        }

        private void OnHoleAbsorb()
        {
            _animator.SpeedMultiplier = 3f;
            _animator.Play("walk_" + _direction);
        }
    }
}