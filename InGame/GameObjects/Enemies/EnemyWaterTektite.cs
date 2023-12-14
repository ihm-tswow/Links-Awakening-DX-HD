using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyWaterTektite : GameObject
    {
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly AiTriggerRandomTime _movementTimer;

        private float _currentSpeed;
        private int _currentDir;

        public static Vector2[] Directions =
        {
            new Vector2(-1, -1), new Vector2(-1, 1), new Vector2(1, 1), new Vector2(1, -1)
        };

        public EnemyWaterTektite() : base("water tektite") { }

        public EnemyWaterTektite(Map.Map map, int posX, int posY) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            var animator = AnimatorSaveLoad.LoadAnimator("Enemies/water tektite");
            animator.Play("idle");

            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(animator, sprite, new Vector2(-8, -16));

            var fieldRectangle = map.GetField(posX, posY);

            _body = new BodyComponent(EntityPosition, -7, -14, 14, 12, 8)
            {
                MoveCollision = OnCollision,
                CollisionTypes =
                    Values.CollisionTypes.Normal |
                    Values.CollisionTypes.Enemy |
                    Values.CollisionTypes.Player,
                AvoidTypes = Values.CollisionTypes.Hole |
                             Values.CollisionTypes.NPCWall,
                FieldRectangle = fieldRectangle,
                Bounciness = 0.25f,
                Drag = 0.85f,
                IgnoreHeight = true,
            };

            var hittableBox = new CBox(EntityPosition, -7, -15, 14, 14, 8);
            var damageBox = new CBox(EntityPosition, -7, -15, 0, 14, 13, 4);

            var stateMoving = new AiState(UpdateMoving);
            stateMoving.Trigger.Add(_movementTimer = new AiTriggerRandomTime(ToStop, 400, 750));
            var stateWaiting = new AiState();
            stateWaiting.Trigger.Add(new AiTriggerRandomTime(ToMoving, 300, 500));

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("moving", stateMoving);
            _aiComponent.States.Add("waiting", stateWaiting);
            var damageState = new AiDamageState(this, _body, _aiComponent, sprite, 1) { OnBurn = () => animator.Pause() };//, false);

            AddComponent(PushableComponent.Index, new PushableComponent(_body.BodyBox, OnPush));
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageBox, HitType.Enemy, 2));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, damageState.OnHit));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, sprite, Values.LayerPlayer) { WaterOutline = false });
            AddComponent(DrawShadowComponent.Index, new DrawShadowCSpriteComponent(sprite));

            ToMoving();
        }

        private void OnCollision(Values.BodyCollision collision)
        {
            ToStop();
        }

        private void ToMoving()
        {
            _aiComponent.ChangeState("moving");

            // TODO: should not be a constant velocity
            _currentDir = Game1.RandomNumber.Next(0, 4);
        }

        private void UpdateMoving()
        {
            // speed up or slow down
            if (_movementTimer.CurrentTime > 100)
            {
                _currentSpeed += 0.05f * Game1.TimeMultiplier;
                if (_currentSpeed > 0.75f)
                    _currentSpeed = 0.75f;
            }
            else
            {
                _currentSpeed -= 0.05f * Game1.TimeMultiplier;
                if (_currentSpeed < 0)
                    _currentSpeed = 0;
            }

            _body.VelocityTarget = Directions[_currentDir] * _currentSpeed;
        }

        private void ToStop()
        {
            _currentSpeed = 0;
            _body.VelocityTarget = Vector2.Zero;

            if (_aiComponent.CurrentStateId == "moving")
                _aiComponent.ChangeState("waiting");
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type == PushableComponent.PushType.Impact)
                _body.Velocity = new Vector3(direction.X, direction.Y, _body.Velocity.Z);

            return true;
        }
    }
}