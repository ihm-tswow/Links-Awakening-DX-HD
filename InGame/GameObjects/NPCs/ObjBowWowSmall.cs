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
    class ObjBowWowSmall : GameObject
    {
        private readonly Animator _animator;
        private readonly AiComponent _aiComponent;
        private readonly BodyComponent _body;
        private readonly AiTriggerSwitch _changeDirectionSwitch;

        private string _traded;
        private string _name;
        private int _direction;

        public ObjBowWowSmall() : base("smallBowWow") { }

        public ObjBowWowSmall(Map.Map map, int posX, int posY, string name) : base(map)
        {
            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            _name = name;

            _body = new BodyComponent(EntityPosition, -5, -8, 10, 8, 8)
            {
                MoveCollision = OnCollision,
                CollisionTypes = Values.CollisionTypes.Normal |
                                 Values.CollisionTypes.NPCWall,
                FieldRectangle = map.GetField(posX, posY),
                Gravity = -0.15f
            };

            OnKeyChange();

            _animator = AnimatorSaveLoad.LoadAnimator("NPCs/bowWowSmall");
            _animator.Play("walk_" + _traded + "0");

            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, sprite, Vector2.Zero);

            var stateIdle = new AiState();
            stateIdle.Trigger.Add(new AiTriggerRandomTime(() => _aiComponent.ChangeState("walking"), 500, 1500));
            var stateWalking = new AiState(UpdateWalking) { Init = InitWalk };
            stateWalking.Trigger.Add(new AiTriggerRandomTime(ToIdle, 750, 1500));
            stateWalking.Trigger.Add(_changeDirectionSwitch = new AiTriggerSwitch(250));

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("walking", stateWalking);
            _aiComponent.ChangeState(Game1.RandomNumber.Next(0, 10) < 5 ? "idle" : "walking");

            if (!string.IsNullOrEmpty(_name))
            {
                var interactionBox = new CBox(EntityPosition, -8, -16, 16, 16, 8);
                AddComponent(InteractComponent.Index, new InteractComponent(interactionBox, Interact));
            }
            AddComponent(BodyComponent.Index, _body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(CollisionComponent.Index, new BoxCollisionComponent(_body.BodyBox, Values.CollisionTypes.Normal));
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new DrawShadowCSpriteComponent(sprite));
            AddComponent(PushableComponent.Index, new PushableComponent(_body.BodyBox, OnPush));
            AddComponent(HittableComponent.Index, new HittableComponent(_body.BodyBox, OnHit));
            AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(OnKeyChange));
        }

        private void OnKeyChange()
        {
            // show the ribbon on the head?
            _traded = (_name == "bowWow3" && Game1.GameManager.SaveManager.GetString("trade1") == "1") ? "r_" : "";
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            _body.Velocity.X += direction.X;
            _body.Velocity.Y += direction.Y;

            return Values.HitCollision.Blocking;
        }

        private void ToIdle()
        {
            _aiComponent.ChangeState("idle");

            // stop and wait
            _body.VelocityTarget.X = 0;
            _body.VelocityTarget.Y = 0;
        }

        private void InitWalk()
        {
            // change the direction
            var rotation = Game1.RandomNumber.Next(0, 628) / 100f;
            _body.VelocityTarget = new Vector2(
                                       (float)Math.Sin(rotation),
                                       (float)Math.Cos(rotation)) * Game1.RandomNumber.Next(25, 40) / 50f;
            _direction = _body.VelocityTarget.X < 0 ? 0 : 1;

            _animator.Play("walk_" + _traded + _direction);
        }

        private void UpdateWalking()
        {
            // jump up and down while walking
            if (_body.IsGrounded)
                _body.Velocity.Z = 0.85f;
        }

        private void OnCollision(Values.BodyCollision moveCollision)
        {
            // rotate after wall collision
            // top collision
            if (moveCollision.HasFlag(Values.BodyCollision.Horizontal))
            {
                if (!_changeDirectionSwitch.State)
                    return;
                _changeDirectionSwitch.Reset();

                _body.VelocityTarget.X = -_body.VelocityTarget.X * 0.5f;
                _direction = (_direction + 1) % 2;
                _animator.Play("walk_" + _traded + _direction);
            }
            // vertical collision
            else if (moveCollision.HasFlag(Values.BodyCollision.Vertical))
            {
                _body.VelocityTarget.Y = -_body.VelocityTarget.Y * 0.5f;
            }
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            // push bowwow away
            _body.Velocity = new Vector3(direction.X, direction.Y, 0) * 0.65f;

            if (_aiComponent.CurrentStateId == "walking")
                return false;

            _aiComponent.ChangeState("walking");

            var offsetAngle = MathHelper.ToRadians(Game1.RandomNumber.Next(45, 85) * (_direction * 2 - 1));
            var newDirection = new Vector2(
                                   direction.X * (float)Math.Cos(offsetAngle) - direction.Y * (float)Math.Sin(offsetAngle),
                                   direction.X * (float)Math.Sin(offsetAngle) + direction.Y * (float)Math.Cos(offsetAngle)) * 0.5f;
            _body.VelocityTarget = newDirection;

            _direction = _body.VelocityTarget.X < 0 ? 0 : 1;
            _animator.Play("walk_" + _traded + _direction);

            return true;
        }

        private bool Interact()
        {
            Game1.GameManager.PlaySoundEffect("D370-24-18");
            Game1.GameManager.StartDialogPath(_name);

            return true;
        }
    }
}