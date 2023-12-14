using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyStar : GameObject
    {
        private readonly BodyComponent _body;

        public EnemyStar() : base("star") { }

        public EnemyStar(Map.Map map, int posX, int posY) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            var animator = AnimatorSaveLoad.LoadAnimator("Enemies/star");
            animator.Play("idle");

            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(animator, sprite, Vector2.Zero);

            _body = new BodyComponent(EntityPosition, -6, -11, 12, 10, 8)
            {
                FieldRectangle = map.GetField(posX, posY),
                MoveCollision = OnCollision,
                CollisionTypes =
                    Values.CollisionTypes.Normal |
                    Values.CollisionTypes.Hole |
                    Values.CollisionTypes.NPCWall
            };
            _body.VelocityTarget = new Vector2(-1, 1) * (3 / 4.0f);

            var aiComponent = new AiComponent();
            aiComponent.States.Add("idle", new AiState());
            var damageState = new AiDamageState(this, _body, aiComponent, sprite, 1, false) { OnBurn = () => animator.Pause() };

            aiComponent.ChangeState("idle");

            var damageBox = new CBox(EntityPosition, -7, -14, 0, 14, 13, 4);
            var hittableBox = new CBox(EntityPosition, -7, -14, 0, 14, 13, 8);
            var pushableBox = new CBox(EntityPosition, -6, -13, 0, 12, 12, 8);

            AddComponent(PushableComponent.Index, new PushableComponent(pushableBox, OnPush));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, damageState.OnHit));
            AddComponent(AiComponent.Index, aiComponent);
            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageBox, HitType.Enemy, 2));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new DrawShadowCSpriteComponent(sprite));
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type == PushableComponent.PushType.Impact)
            {
                _body.Velocity = new Vector3(direction.X * 1.5f, direction.Y * 1.5f, _body.Velocity.Z);

                var dir = AnimationHelper.GetDirection(direction);
                if (dir % 2 == 0)
                    _body.VelocityTarget.X = -_body.VelocityTarget.X;
                else
                    _body.VelocityTarget.Y = -_body.VelocityTarget.Y;
            }

            return true;
        }

        private void OnCollision(Values.BodyCollision collider)
        {
            if ((collider & Values.BodyCollision.Horizontal) != 0)
                _body.VelocityTarget.X = -_body.VelocityTarget.X;
            if ((collider & Values.BodyCollision.Vertical) != 0)
                _body.VelocityTarget.Y = -_body.VelocityTarget.Y;
        }
    }
}