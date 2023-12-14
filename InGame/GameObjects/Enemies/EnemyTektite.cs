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
    internal class EnemyTektite : GameObject
    {
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly Animator _animator;

        private readonly Rectangle _fieldRectangle;

        public EnemyTektite() : base("tektite") { }

        public EnemyTektite(Map.Map map, int posX, int posY) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -32, 16, 32);

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/tektite");
            _animator.Play("idle");

            var sprite = new CSprite(EntityPosition);
            var animatorComponent = new AnimationComponent(_animator, sprite, new Vector2(-8, -16));

            _fieldRectangle = map.GetField(posX, posY);

            _body = new BodyComponent(EntityPosition, -6, -10, 12, 10, 8)
            {
                Gravity = -0.05f,
                DragAir = 0.85f,
                CollisionTypes = Values.CollisionTypes.Normal | Values.CollisionTypes.NPCWall,
                AvoidTypes = Values.CollisionTypes.Hole | Values.CollisionTypes.NPCWall,
                FieldRectangle = _fieldRectangle
            };

            _aiComponent = new AiComponent();

            var stateIdle = new AiState() { Init = InitIdle };
            stateIdle.Trigger.Add(new AiTriggerRandomTime(() => _aiComponent.ChangeState("jumping"), 750, 1250));
            var stateJumping = new AiState(UpdateJumping) { Init = InitJumping };

            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("jumping", stateJumping);
            new AiFallState(_aiComponent, _body, null, null, 300);
            var damageState = new AiDamageState(this, _body, _aiComponent, sprite, 2) { OnBurn = OnBurn };
            _aiComponent.ChangeState("idle");

            var damageBox = new CBox(EntityPosition, -6, -12, 0, 12, 12, 4);
            var hittableBox = new CBox(EntityPosition, -7, -12, 0, 14, 12, 8, true);
            var pushableBox = new CBox(EntityPosition, -7, -12, 0, 14, 12, 8, true);

            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageBox, HitType.Enemy, 2));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, damageState.OnHit));
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BodyComponent.Index, _body);
            AddComponent(BaseAnimationComponent.Index, animatorComponent);
            AddComponent(PushableComponent.Index, new PushableComponent(pushableBox, OnPush));
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(_body, sprite) { ShadowWidth = 10 });
        }

        private void OnBurn()
        {
            _body.Bounciness = 0.45f;
            _animator.Pause();
        }

        private void InitIdle()
        {
            _body.VelocityTarget = Vector2.Zero;
            _animator.Play("idle");
        }

        private void InitJumping()
        {
            Game1.GameManager.PlaySoundEffect("D360-36-24", true, EntityPosition.Position);

            _animator.Play("jump");

            // jump towards the player if he is in the range
            Vector2 vecDirection;
            if (_fieldRectangle.Contains(MapManager.ObjLink.PosX, MapManager.ObjLink.PosY) ||
                !_fieldRectangle.Contains(EntityPosition.X, EntityPosition.Y))
            {
                vecDirection = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
                vecDirection.Normalize();
                vecDirection *= 1.25f;
            }
            else
            {
                var randomDir = Game1.RandomNumber.Next(0, 100) / 100.0f;
                var directionRadius = (float)(Math.PI * 2 * randomDir);
                vecDirection = new Vector2((float)Math.Cos(directionRadius), (float)Math.Sin(directionRadius));
                vecDirection *= 0.75f;
            }

            _body.VelocityTarget = vecDirection;
            _body.Velocity.Z = 1.0f;
        }

        private void UpdateJumping()
        {
            // finished jumping
            if (_body.IsGrounded)
                _aiComponent.ChangeState("idle");
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type == PushableComponent.PushType.Impact)
                _body.Velocity = new Vector3(direction.X * 1.75f, direction.Y * 1.75f, _body.Velocity.Z);

            return true;
        }
    }
}