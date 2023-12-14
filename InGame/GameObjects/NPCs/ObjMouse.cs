using System;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.NPCs
{
    internal class ObjMouse : GameObject
    {
        private readonly Animator _animator;
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly AiTriggerSwitch _changeDirectionSwitch;
        private readonly AiTriggerSwitch _hitCooldown;

        private int _direction;

        public ObjMouse(Map.Map map, int posX, int posY) : base(map)
        {
            SprEditorImage = Resources.SprNpCs;
            EditorIconSource = new Rectangle(63, 280, 15, 14);

            EntityPosition = new CPosition(posX + 8, posY + 8 + 4, 0);
            EntitySize = new Rectangle(-9, -16, 18, 16);

            _body = new BodyComponent(EntityPosition, -5, -8, 10, 8, 8)
            {
                MoveCollision = OnCollision,
                CollisionTypes = Values.CollisionTypes.Normal |
                                 Values.CollisionTypes.Player |
                                 Values.CollisionTypes.NPCWall,
                FieldRectangle = map.GetField(posX, posY),
                DragAir = 0.85f,
                Drag = 0.85f,
                Gravity = -0.15f
            };

            _animator = AnimatorSaveLoad.LoadAnimator("NPCs/mouse");
            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, sprite, new Vector2(-9, -_animator.FrameHeight));

            var stateIdle = new AiState(StateIdle);
            stateIdle.Trigger.Add(_hitCooldown = new AiTriggerSwitch(250));
            var stateWalking = new AiState(StateWalking) { Init = InitWalk };
            stateWalking.Trigger.Add(new AiTriggerRandomTime(ToIdle, 750, 1500));
            stateWalking.Trigger.Add(_changeDirectionSwitch = new AiTriggerSwitch(250));

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("walking", stateWalking);
            _aiComponent.ChangeState(Game1.RandomNumber.Next(0, 2) == 0 ? "idle" : "walking");

            AddComponent(BodyComponent.Index, _body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(AnimationComponent.Index, animationComponent);
            AddComponent(CollisionComponent.Index, new BodyCollisionComponent(_body, Values.CollisionTypes.Normal));
            AddComponent(PushableComponent.Index, new PushableComponent(_body.BodyBox, OnPush));
            AddComponent(HittableComponent.Index, new HittableComponent(_body.BodyBox, OnHit));
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new DrawShadowCSpriteComponent(sprite));
        }

        private void ToIdle()
        {
            _aiComponent.ChangeState("idle");

            // stop and wait
            _body.VelocityTarget.X = 0;
            _body.VelocityTarget.Y = 0;

            _animator.Play("stand_" + _direction);
        }

        private void InitWalk()
        {
            // change the direction
            var rotation = Game1.RandomNumber.Next(0, 628) / 100f;
            _body.VelocityTarget = new Vector2(
                                       (float)Math.Sin(rotation),
                                       (float)Math.Cos(rotation)) * Game1.RandomNumber.Next(25, 40) / 50f;
            _direction = _body.VelocityTarget.X < 0 ? 0 : 1;

            _animator.Play("walk_" + _direction);
        }

        private void StateIdle()
        {
            if (!_animator.IsPlaying)
                _aiComponent.ChangeState("walking");
        }

        private void StateWalking()
        {
            // jump while walking
            if (_body.IsGrounded)
                _body.Velocity.Z = 1f;
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (_aiComponent.CurrentStateId != "idle")
                ToIdle();

            if (!_hitCooldown.State)
                return Values.HitCollision.None;

            _hitCooldown.Reset();

            _body.Velocity.X += direction.X * 4.0f;
            _body.Velocity.Y += direction.Y * 4.0f;

            return Values.HitCollision.Blocking;
        }

        private void OnCollision(Values.BodyCollision moveCollision)
        {
            if (_aiComponent.CurrentStateId != "walking")
                return;

            // rotate after wall collision
            // top collision
            if (moveCollision.HasFlag(Values.BodyCollision.Horizontal))
            {
                if (!_changeDirectionSwitch.State)
                    return;
                _changeDirectionSwitch.Reset();

                _body.VelocityTarget.X *= -0.5f;
                _direction = (_direction + 1) % 2;
                _animator.Play("walk_" + _direction);
            }
            // vertical collision
            else if (moveCollision.HasFlag(Values.BodyCollision.Vertical))
            {
                _body.VelocityTarget.Y *= -0.5f;
            }
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            // push the bird away
            _body.Velocity = new Vector3(direction.X, direction.Y, 0) * 1.0f;

            if (_aiComponent.CurrentStateId == "walking")
                return true;

            _aiComponent.ChangeState("walking");

            var offsetAngle = MathHelper.ToRadians(Game1.RandomNumber.Next(45, 85) * (_direction * 2 - 1));
            var newDirection = new Vector2(
                                   direction.X * (float)Math.Cos(offsetAngle) - direction.Y * (float)Math.Sin(offsetAngle),
                                   direction.X * (float)Math.Sin(offsetAngle) + direction.Y * (float)Math.Cos(offsetAngle)) * 0.5f;
            _body.VelocityTarget = newDirection;

            _direction = _body.VelocityTarget.X < 0 ? 0 : 1;
            _animator.Play("walk_" + _direction);

            return true;
        }
    }
}