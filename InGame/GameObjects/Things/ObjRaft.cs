using Microsoft.Xna.Framework;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;
using System;

namespace ProjectZ.InGame.GameObjects.Things
{
    public class ObjRaft : GameObject
    {
        public readonly BodyComponent Body;

        private readonly Animator _animator;
        private readonly AiComponent _aiComponent;
        //private readonly CRectangle _collisionRectangle;

        private Vector2 _startPosition;
        private Vector2 _targetPosition;

        private float _jumpMoveTime;
        private float _jumpTime;
        private float _jumpCounter;

        private bool _isActive;
        private bool _wasColliding;
        private bool _wasMoved;

        public ObjRaft() : base("raft") { }

        public ObjRaft(Map.Map map, int posX, int posY, string strActivationKey) : base(map)
        {
            if (!string.IsNullOrEmpty(strActivationKey))
            {
                var activationValue = Game1.GameManager.SaveManager.GetString(strActivationKey);
                if (activationValue != null && activationValue == "1")
                    _isActive = true;
            }

            var offsetY = -5;
            EntityPosition = new CPosition(posX + 8, posY + 16 + offsetY + (_isActive ? 16 : 0), 0);
            EntitySize = new Rectangle(-8, -16 - offsetY, 16, 16);

            //_collisionRectangle = new CRectangle(EntityPosition, new Rectangle(-4, -offsetY - 6, 8, 4));

            Body = new BodyComponent(EntityPosition, -4, -offsetY - 14, 8, 10, 8)
            {
                IsActive = false,
                IgnoreHeight = true,
                IgnoreHoles = true,
                IsSlider = true,
                Gravity = -0.2f,
                MoveCollision = OnMoveCollision,
                CollisionTypes = Values.CollisionTypes.Normal | Values.CollisionTypes.RaftExit,
            };

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("idle", new AiState(UpdateIdle));
            _aiComponent.States.Add("moving", new AiState(UpdateMoving));
            _aiComponent.ChangeState("idle");

            _animator = AnimatorSaveLoad.LoadAnimator("Objects/raft");
            _animator.Play(_isActive ? "water" : "idle");

            var _sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, _sprite, new Vector2(-8, -offsetY - 15));

            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BodyComponent.Index, Body);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(_sprite, Values.LayerBottom));

            AddComponent(CollisionComponent.Index,
                new BoxCollisionComponent(new CBox(EntityPosition, -8, -16 - offsetY, -8, 16, 16, 8), Values.CollisionTypes.Normal));

            if (!_isActive)
                AddComponent(CollisionComponent.Index, new BoxCollisionComponent(
                    new CBox(EntityPosition, -8, -offsetY - 1, 16, 1, 8), Values.CollisionTypes.Normal | Values.CollisionTypes.ThrowWeaponIgnore));
        }

        public override void Init()
        {
            if (_isActive)
                ToggleWater();
        }

        // toggle the water field state
        private void ToggleWater()
        {
            var fieldX = (int)EntityPosition.X / 16;
            var fieldY = (int)EntityPosition.Y / 16;

            var oldState = Map.GetFieldState(fieldX, fieldY);
            Map.SetFieldState(fieldX, fieldY, oldState ^ MapStates.FieldStates.DeepWater);
        }

        private void OnMoveCollision(Values.BodyCollision collision)
        {
            // align with the raft exit
            var offset = Vector2.Zero;
            if ((collision & Values.BodyCollision.Left) != 0)
                offset.X -= 1;
            if ((collision & Values.BodyCollision.Right) != 0)
                offset.X += 1;
            if ((collision & Values.BodyCollision.Top) != 0)
                offset.Y -= 1;
            if ((collision & Values.BodyCollision.Bottom) != 0)
                offset.Y += 1;

            var box = Body.BodyBox.Box;
            box.X += offset.X;
            box.Y += offset.Y;
            var outBox = Box.Empty;
            Map.Objects.Collision(box, box, Values.CollisionTypes.RaftExit, 0, 0, ref outBox);

            if (outBox != Box.Empty)
            {
                var exitCenter = outBox.Center;
                var direction = exitCenter - EntityPosition.Position;
                var alignSpeed = 0.5f * Game1.TimeMultiplier;
                var maxAlignDist = 6;

                // align horizontally or vertically
                if ((collision & Values.BodyCollision.Vertical) != 0)
                {
                    Body.AdditionalMovementVT = Vector2.Zero;
                    Body.LastAdditionalMovementVT = Vector2.Zero;
                    if (alignSpeed < Math.Abs(direction.X) && Math.Abs(direction.X) < maxAlignDist)
                        Body.SlideOffset.X = Math.Sign(direction.X) * alignSpeed;
                    else if (Math.Abs(direction.X) < maxAlignDist)
                    {
                        EntityPosition.Set(new Vector2(exitCenter.X, EntityPosition.Y));
                        ExitRaft();
                    }
                }
                else if ((collision & Values.BodyCollision.Horizontal) != 0)
                {
                    Body.AdditionalMovementVT = Vector2.Zero;
                    Body.LastAdditionalMovementVT = Vector2.Zero;
                    if (alignSpeed < Math.Abs(direction.Y) && Math.Abs(direction.Y) < maxAlignDist)
                        Body.SlideOffset.Y = Math.Sign(direction.Y) * alignSpeed;
                    else if (Math.Abs(direction.Y) < maxAlignDist)
                    {
                        EntityPosition.Set(new Vector2(EntityPosition.X, exitCenter.Y));
                        ExitRaft();
                    }
                }
            }
        }

        public void TargetVelocity(Vector2 direction)
        {
            var targetDir = direction - Body.VelocityTarget;
            if (targetDir.Length() > 0.1f * Game1.TimeMultiplier)
            {
                targetDir.Normalize();
                Body.VelocityTarget += targetDir * 0.1f * Game1.TimeMultiplier;
            }
            else
            {
                Body.VelocityTarget = direction;
            }

            _wasMoved = true;
        }

        public void Jump(Vector2 targetPosition, int time)
        {
            targetPosition.Y -= 7;
            _startPosition = EntityPosition.Position;
            _targetPosition = targetPosition;

            var percentage = (targetPosition.Y - _startPosition.Y) / 80;
            _jumpMoveTime = 150;// * percentage;
            _jumpTime = 1000 * percentage;
            _jumpCounter = 0;

            Body.IsActive = false;
            Body.VelocityTarget = Vector2.Zero;

            MapManager.ObjLink._body.IsGrounded = false;
            MapManager.ObjLink._body.IsActive = false;
        }

        private void UpdateIdle()
        {
            var distance = MapManager.ObjLink.EntityPosition.Position - new Vector2(EntityPosition.X, EntityPosition.Y + 1);
            var isColliding = Math.Abs(distance.X) <= 3 && Math.Abs(distance.Y) <= 1;

            if (_isActive && isColliding && !_wasColliding && MapManager.ObjLink.IsGrounded())
                EnterRaft();

            _wasColliding = isColliding;
        }

        private void UpdateMoving()
        {
            if (!_wasMoved)
                TargetVelocity(Vector2.Zero);
            _wasMoved = false;

            // jump
            if (_jumpTime > 0)
            {
                var lastJumpCounter = _jumpCounter;
                _jumpCounter += Game1.DeltaTime;
                // finished jumping?
                if (MapManager.ObjLink._body.IsGrounded || _jumpCounter > _jumpTime)
                {
                    _jumpTime = 0;
                    EntityPosition.Set(_targetPosition);
                    Map.CameraTarget = null;
                }
                else
                {
                    var percentage = MathHelper.Clamp(_jumpCounter / _jumpMoveTime, 0, 1);

                    if (_jumpCounter <= _jumpMoveTime)
                    {
                        var percentageHeight = _jumpCounter / _jumpMoveTime;
                        var posZ = MathF.Sin(percentageHeight * MathF.PI * 0.5f);
                        EntityPosition.Z = posZ * 12 + percentage * (_targetPosition.Y - _startPosition.Y);
                        MapManager.ObjLink.EntityPosition.Z = posZ * 16 + percentage * (_targetPosition.Y - _startPosition.Y);
                    }
                    else if (lastJumpCounter <= _jumpMoveTime)
                    {
                        MapManager.ObjLink._body.IsActive = true;
                        Body.IsActive = true;
                    }

                    Map.CameraTarget = new Vector2(MapManager.ObjLink.EntityPosition.X, MapManager.ObjLink.EntityPosition.Y - MapManager.ObjLink.EntityPosition.Z);

                    var newPosition = Vector2.Lerp(_startPosition, _targetPosition, percentage);
                    EntityPosition.Set(newPosition);
                }
            }
        }

        private void EnterRaft()
        {
            ToggleWater();

            Body.IsActive = true;
            Body.VelocityTarget = Vector2.Zero;
            _aiComponent.ChangeState("moving");
            EntityPosition.AddPositionListener(typeof(ObjRaft), OnPositionChange);

            ((DrawComponent)Components[DrawComponent.Index]).Layer = Values.LayerPlayer;

            MapManager.ObjLink.StartRaftRiding(this);
        }

        private void ExitRaft()
        {
            ToggleWater();

            Body.IsActive = false;
            Body.VelocityTarget = Vector2.Zero;
            _aiComponent.ChangeState("idle");
            EntityPosition.RemovePositionListener(typeof(ObjRaft));

            ((DrawComponent)Components[DrawComponent.Index]).Layer = Values.LayerBottom;

            MapManager.ObjLink.ExitRaft();
        }

        private void OnPositionChange(CPosition newPosition)
        {
            MapManager.ObjLink.SetPosition(new Vector2(newPosition.X, newPosition.Y + 1));
        }
    }
}