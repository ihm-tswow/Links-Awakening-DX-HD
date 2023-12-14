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
    internal class EnemyOctorok : GameObject
    {
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly Animator _animator;

        private readonly Vector2[] _shotOffset =
        {
            new Vector2(-8, -1),new Vector2(0, -6),
            new Vector2(8, -1),new Vector2(0, 11)
        };

        private float _walkSpeed = 0.5f;
        private int _direction;

        public EnemyOctorok() : base("octorok") { }

        public EnemyOctorok(Map.Map map, int posX, int posY) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 12, 0);
            EntitySize = new Rectangle(-8, -15, 16, 16);

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/octorok");

            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, sprite, new Vector2(-8, -15));

            _body = new BodyComponent(EntityPosition, -7, -12, 14, 12, 8)
            {
                MoveCollision = OnCollision,
                AbsorbPercentage = 0.9f,
                CollisionTypes =
                    Values.CollisionTypes.Normal |
                    Values.CollisionTypes.Enemy |
                    Values.CollisionTypes.Player,
                AvoidTypes =
                    Values.CollisionTypes.Hole |
                    Values.CollisionTypes.NPCWall,
                FieldRectangle = map.GetField(posX, posY),
                Bounciness = 0.25f,
                Drag = 0.85f,
            };

            var walkingState = new AiState { Init = ToWalking };
            walkingState.Trigger.Add(new AiTriggerRandomTime(() => _aiComponent.ChangeState("idle"), 750, 1000));
            var idleState = new AiState { Init = ToIdle };
            idleState.Trigger.Add(new AiTriggerRandomTime(() => _aiComponent.ChangeState("walking"), 250, 500));

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("walking", walkingState);
            _aiComponent.States.Add("idle", idleState);
            new AiFallState(_aiComponent, _body, OnHoleAbsorb, null);
            _aiComponent.ChangeState("walking");

            var damageState = new AiDamageState(this, _body, _aiComponent, sprite, 1) { OnBurn = OnBurn };
            var damageBox = new CBox(EntityPosition, -8, -13, 0, 16, 13, 4);
            var hittableBox = new CBox(EntityPosition, -7, -15, 0, 14, 15, 8);
            var pushableBox = new CBox(EntityPosition, -7, -13, 0, 14, 13, 4);

            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageBox, HitType.Enemy, 2));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, damageState.OnHit));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(PushableComponent.Index, new PushableComponent(pushableBox, OnPush));
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new DrawShadowCSpriteComponent(sprite) { Height = 1.0f, Rotation = 0.1f });
        }

        public override void Init()
        {
            base.Init();

            if (!IsActive)
                return;

            // random start position/state
            _direction = Game1.RandomNumber.Next(0, 4);
            _animator.Play("walk_" + _direction);
            _aiComponent.ChangeState(Game1.RandomNumber.Next(0, 2) == 0 ? "idle" : "walking");
        }

        private void ToIdle()
        {
            _animator.Play("stand_" + _direction);
            _body.VelocityTarget = new Vector2(0, 0);

            // shoot if the player is in the range and in the right direction
            var playerDirection = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            if (playerDirection.Length() < 80)
            {
                if (playerDirection != Vector2.Zero)
                    playerDirection.Normalize();
                var direction = AnimationHelper.GetDirection(playerDirection);

                if (direction == _direction)
                {
                    // shoot
                    var shot = new EnemyOctorokShot(Map,
                        EntityPosition.X + _shotOffset[_direction].X,
                        EntityPosition.Y + _shotOffset[_direction].Y,
                        AnimationHelper.DirectionOffset[_direction] * 2f);
                    Map.Objects.SpawnObject(shot);
                }
            }
        }

        private void ToWalking()
        {
            // random new direction
            _direction = Game1.RandomNumber.Next(0, 4);
            _animator.Play("walk_" + _direction);
            _body.VelocityTarget = AnimationHelper.DirectionOffset[_direction] * _walkSpeed;
        }

        private void OnBurn()
        {
            _animator.Pause();
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type == PushableComponent.PushType.Impact)
                _body.Velocity = new Vector3(direction.X * 1.75f, direction.Y * 1.75f, _body.Velocity.Z);

            return true;
        }

        private void OnCollision(Values.BodyCollision direction)
        {
            if (_aiComponent.CurrentStateId == "walking")
                _aiComponent.ChangeState("idle");
        }

        private void OnHoleAbsorb()
        {
            _animator.SpeedMultiplier = 3f;
            _animator.Play("walk_" + _direction);
        }
    }
}