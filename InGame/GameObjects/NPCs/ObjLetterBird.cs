using System;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.NPCs
{
    internal class ObjLetterBird : GameObject
    {
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly AiTriggerSwitch _changeDirectionSwitch;
        private readonly Animator _animator;

        private float _flyCounter;
        private int _flyTime = 850;
        private int _direction;

        public ObjLetterBird() : base("letter_bird") { }

        public ObjLetterBird(Map.Map map, int posX, int posY, string animationId) : base(map)
        {
            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            _body = new BodyComponent(EntityPosition, -6, -8, 12, 8, 8)
            {
                MoveCollision = OnCollision,
                CollisionTypes = Values.CollisionTypes.Normal |
                                 Values.CollisionTypes.NPCWall,
                Bounciness = 0.25f,
                Drag = 0.9f,
                Gravity = -0.15f,
            };

            _animator = AnimatorSaveLoad.LoadAnimator(animationId);
            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, sprite, new Vector2(-8, -16));

            var stateIdle = new AiState() { Init = InitIdle };
            stateIdle.Trigger.Add(new AiTriggerRandomTime(() => _aiComponent.ChangeState("walking"), 500, 1500));

            var stateWalking = new AiState(UpdateWalking) { Init = InitWalking };
            stateWalking.Trigger.Add(new AiTriggerRandomTime(() => _aiComponent.ChangeState("idle"), 750, 1500));
            stateWalking.Trigger.Add(_changeDirectionSwitch = new AiTriggerSwitch(250));

            var stateFly = new AiState(UpdateFlying);

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("walking", stateWalking);
            _aiComponent.States.Add("flying", stateFly);
            _aiComponent.ChangeState("idle");

            AddComponent(BodyComponent.Index, _body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(AnimationComponent.Index, animationComponent);
            AddComponent(CollisionComponent.Index, new BodyCollisionComponent(_body, Values.CollisionTypes.Normal));
            AddComponent(PushableComponent.Index, new PushableComponent(_body.BodyBox, OnPush) { RepelMultiplier = 0.5f });
            AddComponent(HittableComponent.Index, new HittableComponent(_body.BodyBox, OnHit));
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(_body, sprite));
        }

        private void InitIdle()
        {
            // stop and wait
            _body.VelocityTarget = Vector2.Zero;
            _animator.Play("idle_" + _direction);
        }

        private void InitWalking()
        {
            // change the direction
            var rotation = Game1.RandomNumber.Next(0, 628) / 100f;
            _body.VelocityTarget = new Vector2(
                (float)Math.Sin(rotation),
                (float)Math.Cos(rotation)) * Game1.RandomNumber.Next(35, 55) / 100f;
            _direction = _body.VelocityTarget.X < 0 ? 0 : 1;

            _animator.Play("idle_" + _direction);
        }

        private void UpdateWalking()
        {
            if (_body.IsGrounded)
                _body.Velocity.Z = 1.0f;
        }

        private void ToFlying()
        {
            _body.IgnoresZ = true;
            _direction = _body.VelocityTarget.X < 0 ? 0 : 1;
            _animator.Play("fly_" + _direction);
            _aiComponent.ChangeState("flying");
        }

        private void UpdateFlying()
        {
            _flyCounter += Game1.DeltaTime;
            EntityPosition.Z = (float)Math.Sin((_flyCounter / _flyTime) * Math.PI) * 8;

            // finished flying?
            if (_flyCounter >= _flyTime)
            {
                _flyCounter = 0;
                _body.IgnoresZ = false;
                _aiComponent.ChangeState("walking");
            }
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            // flee from the player
            var playerDir = EntityPosition.Position - MapManager.ObjLink.EntityPosition.Position;
            if (playerDir != Vector2.Zero)
                playerDir.Normalize();

            _body.VelocityTarget = playerDir * 0.75f;

            ToFlying();

            return Values.HitCollision.Blocking;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type == PushableComponent.PushType.Continues)
                _body.Velocity = new Vector3(direction.X, direction.Y, 0) * 0.65f;
            else if (type == PushableComponent.PushType.Impact)
                _body.Velocity = new Vector3(direction.X, direction.Y, _body.Velocity.Z) * 1.5f;

            return true;
        }

        private void OnCollision(Values.BodyCollision moveCollision)
        {
            // can only change the direction every so often
            if (!_changeDirectionSwitch.State || _aiComponent.CurrentStateId != "walking")
                return;
            _changeDirectionSwitch.Reset();

            // rotate after wall collision
            if ((moveCollision & Values.BodyCollision.Horizontal) != 0)
                _body.VelocityTarget.X = -_body.VelocityTarget.X * 0.5f;
            else if ((moveCollision & Values.BodyCollision.Vertical) != 0)
                _body.VelocityTarget.Y = -_body.VelocityTarget.Y * 0.5f;

            if ((moveCollision & (Values.BodyCollision.Vertical | Values.BodyCollision.Horizontal)) != 0)
            {
                _direction = _body.VelocityTarget.X < 0 ? 0 : 1;
                _animator.Play("idle_" + _direction);
            }
        }
    }
}