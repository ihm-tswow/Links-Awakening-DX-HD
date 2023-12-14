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
    internal class EnemyBloober : GameObject
    {
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly Animator _animator;

        private readonly Vector2 _startPosition;

        public EnemyBloober() : base("bloober") { }

        public EnemyBloober(Map.Map map, int posX, int posY) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            _startPosition = EntityPosition.Position;

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/bloober");

            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, sprite, new Vector2(-8, -16));

            _body = new BodyComponent(EntityPosition, -6, -13, 12, 10, 8)
            {
                MoveCollision = OnCollision,
                AvoidTypes = Values.CollisionTypes.NPCWall,
                CollisionTypes = Values.CollisionTypes.Normal,
                Gravity2DWater = 0.035f,
                DeepWaterOffset = -9
            };

            var stateUp = new AiState(UpdateUp);
            stateUp.Trigger.Add(new AiTriggerRandomTime(ToMoveDown, 650, 750));
            var stateDown = new AiState(UpdateDown);
            stateDown.Trigger.Add(new AiTriggerRandomTime(ToMoveUp, 550, 650));

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("moveUp", stateUp);
            _aiComponent.States.Add("moveDown", stateDown);
            var damageState = new AiDamageState(this, _body, _aiComponent, sprite, 1) { HitMultiplierX = 2.0f, HitMultiplierY = 2.0f, FlameOffset = new Point(0, 3) };

            ToMoveUp();

            var hittableBox = new CBox(EntityPosition, -7, -14, 0, 14, 12, 8);
            var damageBox = new CBox(EntityPosition, -7, -14, 0, 14, 12, 4);
            
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, damageState.OnHit));
            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageBox, HitType.Enemy, 2));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, sprite, Values.LayerPlayer));
        }

        private void ToMoveUp()
        {
            var playerDistance = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;

            // move towards the start position or the player depending on the distance to the player
            if (playerDistance.Length() < 80)
                MoveTowardPosition(MapManager.ObjLink.EntityPosition.Position, 0.45f);
            else
                MoveTowardPosition(_startPosition, 0.25f);
        }

        private void MoveTowardPosition(Vector2 position, float speed)
        {
            // do not change to move up state if the player is below the enemy
            if (EntityPosition.Y - 6 < position.Y &&
                (_body.LastVelocityCollision & Values.BodyCollision.Bottom) == 0)
            {
                _aiComponent.ChangeState("moveDown");
                return;
            }

            _aiComponent.ChangeState("moveUp");
            _animator.Play("up");

            // move towards the player
            var dir = position.X < EntityPosition.X ? -1 : 1;
            _body.VelocityTarget.X = dir * speed;
        }

        private void UpdateUp()
        {
            _body.DisableVelocityTargetMultiplier = true;

            // swim up if in deep water
            if (_body.CurrentFieldState.HasFlag(MapStates.FieldStates.DeepWater))
                _body.Velocity.Y = -0.65f;
            else
            {
                ToMoveDown();
            }
        }

        private void ToMoveDown()
        {
            _aiComponent.ChangeState("moveDown");
            _animator.Play("down");
            _body.Velocity.X = _body.VelocityTarget.X;
            _body.VelocityTarget.X = 0;
        }

        private void UpdateDown()
        {

        }

        private void OnCollision(Values.BodyCollision collision)
        {

        }
    }
}