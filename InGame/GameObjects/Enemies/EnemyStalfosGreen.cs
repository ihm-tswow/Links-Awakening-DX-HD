using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyStalfosGreen : GameObject
    {
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly Animator _animator;
        private readonly AnimationComponent _animatorComponent;

        private readonly Rectangle _fieldRectangle;

        private float _walkSpeed = 0.5f;
        private float _changeDirCount;
        private int _dir;

        private bool _jumpMoving;

        public EnemyStalfosGreen() : base("stalfos green") { }

        public EnemyStalfosGreen(Map.Map map, int posX, int posY) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -40, 16, 40);

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/stalfos green");
            _animator.Play("walk");

            var sprite = new CSprite(EntityPosition);
            _animatorComponent = new AnimationComponent(_animator, sprite, new Vector2(-8, -16));

            _fieldRectangle = map.GetField(posX, posY);

            _body = new BodyComponent(EntityPosition, -6, -10, 11, 10, 8)
            {
                MoveCollision = OnCollision,
                Gravity = -0.075f,
                AvoidTypes = Values.CollisionTypes.Hole | Values.CollisionTypes.NPCWall,
                FieldRectangle = _fieldRectangle
            };

            _aiComponent = new AiComponent();

            var stateWalking = new AiState(UpdateWalking);
            var stateMoveUp = new AiState(UpdateMoveUp);
            var stateWait = new AiState();
            stateWait.Trigger.Add(new AiTriggerCountdown(250, null, ToMoveDown));
            var stateMoveDown = new AiState(UpdateMoveDown);
            var stateWaitFloor = new AiState();
            stateWaitFloor.Trigger.Add(new AiTriggerCountdown(250, null, ToWalk));

            _aiComponent.States.Add("walking", stateWalking);
            _aiComponent.States.Add("moveUp", stateMoveUp);
            _aiComponent.States.Add("wait", stateWait);
            _aiComponent.States.Add("moveDown", stateMoveDown);
            _aiComponent.States.Add("waitFloor", stateWaitFloor);
            new AiFallState(_aiComponent, _body, null, null, 300);
            var damageState = new AiDamageState(this, _body, _aiComponent, sprite, 2) { OnBurn = () => _animator.Pause() };
            _aiComponent.ChangeState("walking");

            var damageBox = new CBox(EntityPosition, -7, -15, 2, 13, 15, 4);
            var hittableBox = new CBox(EntityPosition, -7, -15, 2, 13, 15, 8);
            var pushableBox = new CBox(EntityPosition, -6, -14, 2, 12, 14, 4);

            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageBox, HitType.Enemy, 2));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, damageState.OnHit));
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BodyComponent.Index, _body);
            AddComponent(BaseAnimationComponent.Index, _animatorComponent);
            AddComponent(PushableComponent.Index, new PushableComponent(pushableBox, OnPush));
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(_body, sprite) { ShadowWidth = 10 });
        }

        public void SetAirPosition(int posZ)
        {
            EntityPosition.SetZ(posZ);
            _animator.Play("jump");
            ToMoveDown();

            // randomize the walk speed so that when two are spawned at the same position they will not
            // stay at the same position
            _walkSpeed = Game1.RandomNumber.Next(45, 55) / 100f;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type == PushableComponent.PushType.Impact)
                _body.Velocity = new Vector3(direction.X * 1.75f, direction.Y * 1.75f, _body.Velocity.Z);

            return true;
        }

        private void ToWalk()
        {
            _aiComponent.ChangeState("walking");
        }

        private void UpdateWalking()
        {
            _animator.Play("walk");

            // jump away when the player is pressing the use key
            var direction = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            var distance = direction.Length();

            if (_fieldRectangle.Contains(MapManager.ObjLink.EntityPosition.Position) && distance < 56)
            {
                if (distance < 24)
                    ToJumping();
                else if (distance < 56)
                {
                    // move towards the player
                    direction.Normalize();
                    _body.VelocityTarget = direction * _walkSpeed;
                }
            }
            else
            {
                _changeDirCount -= Game1.DeltaTime;

                // change direction
                if (_changeDirCount <= 0)
                    ChangeDirection();
            }
        }

        private void ToJumping()
        {
            _aiComponent.ChangeState("moveUp");

            Game1.GameManager.PlaySoundEffect("D360-36-24");

            _animator.Play("jump");

            _body.Velocity.Z = 2;
            _jumpMoving = true;
        }

        private void UpdateMoveUp()
        {
            // start waiting in the air
            if (EntityPosition.Z > 26 || _body.Velocity.Z <= 0)
            {
                ToWait();
                return;
            }

            // move towards the player
            if (_jumpMoving)
            {
                var vecDirection = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
                if (vecDirection.Length() < 2)
                {
                    _jumpMoving = false;
                    return;
                }

                vecDirection.Normalize();
                _body.VelocityTarget = vecDirection * _walkSpeed * 2;
            }
        }

        private void ToWait()
        {
            _aiComponent.ChangeState("wait");

            _body.VelocityTarget = Vector2.Zero;
            _body.IgnoresZ = true;
        }

        private void ToMoveDown()
        {
            _aiComponent.ChangeState("moveDown");

            _body.Velocity.Z = -3.5f;
            _body.IgnoresZ = false;
        }

        private void UpdateMoveDown()
        {
            if (_body.IsGrounded)
                ToWaitFloor();
        }

        private void ToWaitFloor()
        {
            _aiComponent.ChangeState("waitFloor");

            Game1.GameManager.PlaySoundEffect("D360-07-07");

            // this is green in the original
            var animation = new ObjAnimator(Map, 0, 0, Values.LayerTop, "Particles/swordPoke", "run", true);
            animation.EntityPosition.Set(new Vector2(EntityPosition.X, EntityPosition.Y + 4));
            Map.Objects.SpawnObject(animation);
        }

        private void OnCollision(Values.BodyCollision collision)
        {
            if ((collision & Values.BodyCollision.Floor) != 0 && _aiComponent.CurrentStateId == "moveDown")
                ToWaitFloor();
            if ((collision & (Values.BodyCollision.Horizontal | Values.BodyCollision.Vertical)) != 0)
                ChangeDirection();
        }

        private void ChangeDirection()
        {
            _changeDirCount = Game1.RandomNumber.Next(200, 600);
            _dir = Game1.RandomNumber.Next(0, 4);
            _body.VelocityTarget = AnimationHelper.DirectionOffset[_dir] * _walkSpeed;
        }
    }
}