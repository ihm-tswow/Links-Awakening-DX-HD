using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyCrab : GameObject
    {
        private readonly Animator _animator;
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;

        private int _currentDirection;

        public EnemyCrab() : base("crab") { }

        public EnemyCrab(Map.Map map, int posX, int posY) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/crab");
            _animator.Play("walk");

            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, sprite, new Vector2(-8, -16));

            var fieldRectangle = map.GetField(posX, posY);

            _body = new BodyComponent(EntityPosition, -7, -10, 14, 10, 8)
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
                Drag = 0.85f
            };

            var stateWalkingV = new AiState(() => { });
            stateWalkingV.Trigger.Add(new AiTriggerRandomTime(ToWalking, 250, 750));
            var stateWalkingH = new AiState(() => { });
            stateWalkingH.Trigger.Add(new AiTriggerRandomTime(ToWalking, 1000, 1500));

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("walkingV", stateWalkingV);
            _aiComponent.States.Add("walkingH", stateWalkingH);
            var damageState = new AiDamageState(this, _body, _aiComponent, sprite, 2) { OnBurn = () => _animator.Pause() };
            ToWalking();

            var hittableRectangle = new CBox(EntityPosition, -8, -15, 16, 15, 8);
            var damageCollider = new CBox(EntityPosition, -8, -11, 0, 16, 11, 4);

            AddComponent(PushableComponent.Index, new PushableComponent(_body.BodyBox, OnPush));
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageCollider, HitType.Enemy, 2));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableRectangle, damageState.OnHit));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new DrawShadowCSpriteComponent(sprite));
        }

        private void ToWalking()
        {
            _currentDirection = Game1.RandomNumber.Next(0, 4);
            _aiComponent.ChangeState("walking" + (_currentDirection % 2 == 0 ? "H" : "V"));

            // change the direction the crab is walking
            var speed = _currentDirection % 2 == 0 ? 1.0f : 0.33f;
            _body.VelocityTarget = AnimationHelper.DirectionOffset[_currentDirection] * speed;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type == PushableComponent.PushType.Impact)
                _body.Velocity = new Vector3(direction.X, direction.Y, _body.Velocity.Z);

            return true;
        }

        private void OnCollision(Values.BodyCollision direction)
        {
            // change direction after collisions
            if (direction.HasFlag(Values.BodyCollision.Horizontal))
                _body.VelocityTarget.X = -_body.VelocityTarget.X * 0.5f;
            else if (direction == Values.BodyCollision.Vertical)
                _body.VelocityTarget.Y = -_body.VelocityTarget.Y * 0.5f;
        }
    }
}