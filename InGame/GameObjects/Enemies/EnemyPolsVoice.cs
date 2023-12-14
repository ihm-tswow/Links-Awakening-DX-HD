using System;
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
    internal class EnemyPolsVoice : GameObject
    {
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly Animator _animator;
        private readonly AiDamageState _damageState;
        private readonly AiStunnedState _stunnedState;

        private float _jumpVelocity = 1.0f;

        public EnemyPolsVoice() : base("pols voice") { }

        public EnemyPolsVoice(Map.Map map, int posX, int posY) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/pols voice");
            _animator.Play("jump");

            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, sprite, new Vector2(-8, -16));

            _body = new BodyComponent(EntityPosition, -6, -10, 12, 10, 8)
            {
                AvoidTypes = Values.CollisionTypes.Hole | Values.CollisionTypes.NPCWall,
                FieldRectangle = map.GetField(posX, posY),
                MaxJumpHeight = 8f,
                Gravity = -0.05f,
                Drag = 0.75f,
                DragAir = 0.8f
            };

            var stateWaiting = new AiState { Init = InitWaiting };
            stateWaiting.Trigger.Add(new AiTriggerRandomTime(EndWaiting, 500, 750));
            var stateJumping = new AiState(UpdateJumping) { Init = InitJump };

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("waiting", stateWaiting);
            _aiComponent.States.Add("jumping", stateJumping);
            _damageState = new AiDamageState(this, _body, _aiComponent, sprite, 4, true, false);
            new AiFallState(_aiComponent, _body, null, null, 100);
            _stunnedState = new AiStunnedState(_aiComponent, animationComponent, 3300, 900);

            _aiComponent.ChangeState("jumping");

            var damageBox = new CBox(EntityPosition, -7, -11, 0, 14, 11, 4);
            var hittableBox = new CBox(EntityPosition, -6, -12, 0, 12, 12, 8, true);

            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageBox, HitType.Enemy, 1));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(PushableComponent.Index, new PushableComponent(_body.BodyBox, OnPush));
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(_body, sprite) { ShadowWidth = 10 });
        }

        private void InitWaiting()
        {
            _body.VelocityTarget = Vector2.Zero;
            _animator.Play("stand");
        }

        private void EndWaiting()
        {
            var distance = EntityPosition.Position - MapManager.ObjLink.EntityPosition.Position;
            if (distance.Length() < 64 && _body.FieldRectangle.Intersects(MapManager.ObjLink.BodyRectangle))
                _aiComponent.ChangeState("jumping");
        }

        private void InitJump()
        {
            _animator.Play("jump");

            // start jumping
            _body.Velocity.Z = _jumpVelocity;
            _body.Bounciness = 0f;

            var jumpDirection = Vector2.Zero;

            if (Game1.RandomNumber.Next(0, 3) == 0)
            {
                // jump towards the player
                var direction = new Vector2(
                    MapManager.ObjLink.PosX - EntityPosition.X,
                    MapManager.ObjLink.PosY - EntityPosition.Y);

                if (direction != Vector2.Zero)
                {
                    direction.Normalize();
                    jumpDirection = direction;
                }
            }
            else
            {
                var randomDirection = Game1.RandomNumber.Next(0, 100) / 100f * Math.PI * 2;
                jumpDirection = new Vector2((float)Math.Sin(randomDirection), (float)Math.Cos(randomDirection));
            }

            _body.VelocityTarget = jumpDirection * 0.75f;
        }

        private void UpdateJumping()
        {
            // finished jumping?
            if (_body.IsGrounded)
            {
                _animator.Play("stand");
                _aiComponent.ChangeState("waiting");
            }
        }

        private void StartStun()
        {
            if (_body.Velocity.Z > 0)
                _body.Velocity.Z = 0;
            _body.VelocityTarget = Vector2.Zero;
            _body.Bounciness = 0.65f;
            _stunnedState.StartStun();
            _animator.Play("jump");
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (_damageState.IsInDamageState())
                return Values.HitCollision.None;

            if (damageType == HitType.Bow || damageType == HitType.MagicRod)
                return _damageState.OnHit(gameObject, direction, damageType, 1, pieceOfPower);
            if (damageType == HitType.ThrownObject || damageType == HitType.Bomb)
                return _damageState.OnHit(gameObject, direction, damageType, 4, pieceOfPower);

            if (damageType == HitType.MagicPowder || damageType == HitType.Hookshot || damageType == HitType.Boomerang)
            {
                direction *= 0.25f;
                StartStun();
            }

            _damageState.HitKnockBack(gameObject, direction, damageType, pieceOfPower, false);

            // play sound effect
            Game1.GameManager.PlaySoundEffect("D360-03-03");

            return Values.HitCollision.Repelling;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type == PushableComponent.PushType.Impact)
            {
                _body.VelocityTarget = Vector2.Zero;
                _body.Velocity = new Vector3(direction.X * 2f, direction.Y * 2f, _body.Velocity.Z);
            }

            return true;
        }
    }
}