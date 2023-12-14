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
    internal class ObjFrog : GameObject
    {
        private readonly Animator _animator;
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly AiTriggerSwitch _hitCooldown;

        private int _direction;

        public ObjFrog(Map.Map map, int posX, int posY) : base(map)
        {
            SprEditorImage = Resources.SprNpCs;
            EditorIconSource = new Rectangle(84, 28, 14, 12);

            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            _body = new BodyComponent(EntityPosition, -6, -8, 12, 8, 8)
            {
                MoveCollision = OnCollision,
                CollisionTypes = Values.CollisionTypes.Normal |
                                 Values.CollisionTypes.NPCWall,
                FieldRectangle = map.GetField(posX, posY),
                MaxJumpHeight = 4f,
                DragAir = 0.99f,
                Drag = 0.85f,
                Gravity = -0.15f
            };

            _animator = AnimatorSaveLoad.LoadAnimator("NPCs/frog");
            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, sprite, new Vector2(-7, -12));

            _hitCooldown = new AiTriggerSwitch(250);

            var stateSitInit = new AiState();
            stateSitInit.Trigger.Add(new AiTriggerRandomTime(ToJump, 125, 1000));
            stateSitInit.Trigger.Add(_hitCooldown);
            var stateSit = new AiState();
            stateSit.Trigger.Add(_hitCooldown);
            stateSit.Trigger.Add(new AiTriggerRandomTime(ToJump, 750, 1500));
            var stateJump = new AiState(UpdateJump);
            stateJump.Trigger.Add(_hitCooldown);

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("sitInit", stateSitInit);
            _aiComponent.States.Add("sit", stateSit);
            _aiComponent.States.Add("jump", stateJump);

            // start by locking into a random direction
            _direction = Game1.RandomNumber.Next(0, 4);
            _animator.Play("sit_" + _direction);
            _aiComponent.ChangeState("sitInit");

            AddComponent(BodyComponent.Index, _body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(AnimationComponent.Index, animationComponent);
            //AddComponent(CollisionComponent.Index, new BodyCollisionComponent(_body, Values.CollisionTypes.Normal));
            AddComponent(PushableComponent.Index, new PushableComponent(_body.BodyBox, OnPush));
            AddComponent(HittableComponent.Index, new HittableComponent(_body.BodyBox, OnHit));
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(_body, sprite));
        }

        private void ToSit()
        {
            _aiComponent.ChangeState("sit");

            // stop and wait
            _body.Velocity = Vector3.Zero;

            _animator.Play("sit_" + _direction);
        }

        private void ToJump()
        {
            _aiComponent.ChangeState("jump");

            // change the direction
            var rotation = Game1.RandomNumber.Next(0, 628) / 100f;
            var direction = new Vector2(
                (float)Math.Sin(rotation),
                (float)Math.Cos(rotation)) * Game1.RandomNumber.Next(25, 40) / 50f;
            _direction = AnimationHelper.GetDirection(direction);

            _body.Velocity = new Vector3(direction.X * 1.5f, direction.Y * 1.5f, 1.75f);
            
            _animator.Play("jump_" + _direction);
        }

        private void UpdateJump()
        {
            // finished jumping
            if (_body.IsGrounded)
                ToSit();
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (!_hitCooldown.State)
                return Values.HitCollision.None;

            _hitCooldown.Reset();

            _body.Velocity.X += direction.X * 4.0f;
            _body.Velocity.Y += direction.Y * 4.0f;

            return Values.HitCollision.Blocking;
        }

        private void OnCollision(Values.BodyCollision moveCollision)
        {
            if (_aiComponent.CurrentStateId != "jump")
                return;

            // repel after wall collision
            if (moveCollision.HasFlag(Values.BodyCollision.Horizontal))
                _body.Velocity.X *= -0.25f;
            else if (moveCollision.HasFlag(Values.BodyCollision.Vertical))
                _body.Velocity.Y *= -0.25f;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type == PushableComponent.PushType.Impact)
                _body.Velocity = new Vector3(direction * 1.25f, _body.Velocity.Z);

            return true;
        }
    }
}