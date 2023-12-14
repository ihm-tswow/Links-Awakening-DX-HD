using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;
using ProjectZ.InGame.GameObjects.Dungeon;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyAntiFairy : GameObject
    {
        private readonly BodyComponent _body;
        private readonly AiDamageState _aiDamageState;

        private readonly Color _lightColor = new Color(255, 255, 255);

        public EnemyAntiFairy() : base("antiFairy") { }

        public EnemyAntiFairy(Map.Map map, int posX, int posY) : base(map)
        {
            // not used for the enemy trigger
            Tags = Values.GameObjectTag.Damage;

            EntityPosition = new CPosition(posX + 8, posY + 8, 0);
            EntitySize = new Rectangle(-32, -32, 64, 64);

            var animator = AnimatorSaveLoad.LoadAnimator("Enemies/anti-fairy");
            animator.Play("idle");

            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(animator, sprite, new Vector2(-8, -8));

            _body = new BodyComponent(EntityPosition, -6, -6, 12, 12, 8)
            {
                IgnoreHeight = true,
                IgnoreHoles = true,
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
            aiComponent.ChangeState("idle");

            _aiDamageState = new AiDamageState(this, _body, aiComponent, sprite, 1, false)
            {
                IgnoreZeroDamage = true,
                FlameOffset = new Point(0, 2),
                OnDeath = OnDeath
            };

            var hittableBox = new CBox(EntityPosition, -8, -8, 0, 16, 16, 8);
            var damageBox = new CBox(EntityPosition, -7, -7, 0, 14, 14, 4);

            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(AiComponent.Index, aiComponent);
            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageBox, HitType.Enemy, 2));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, sprite, Values.LayerPlayer) { WaterOutline = false });
            AddComponent(DrawShadowComponent.Index, new DrawShadowCSpriteComponent(sprite));
            AddComponent(LightDrawComponent.Index, new LightDrawComponent(DrawLight));
        }

        private Values.HitCollision OnHit(GameObject originObject, Vector2 direction, HitType type, int damage, bool pieceOfPower)
        {
            if (type == HitType.Boomerang || type == HitType.MagicPowder)
                return _aiDamageState.OnHit(originObject, direction, type, damage, pieceOfPower);

            return Values.HitCollision.Blocking;
        }

        private void OnCollision(Values.BodyCollision collider)
        {
            if ((collider & Values.BodyCollision.Horizontal) != 0)
                _body.VelocityTarget.X = -_body.VelocityTarget.X;
            if ((collider & Values.BodyCollision.Vertical) != 0)
                _body.VelocityTarget.Y = -_body.VelocityTarget.Y;
        }

        private void DrawLight(SpriteBatch spriteBatch)
        {
            DrawHelper.DrawLight(spriteBatch, new Rectangle((int)EntityPosition.X - 25, (int)EntityPosition.Y - 25, 50, 50), _lightColor * 0.5f);
        }

        private void OnDeath(bool pieceOfPower)
        {
            // spawn fairy? ~50% cance
            // not sure how this is calculated in the original game
            if (Game1.RandomNumber.Next(0, 100) < 50)
                Map.Objects.SpawnObject(new ObjDungeonFairy(Map, (int)EntityPosition.X, (int)EntityPosition.Y + 4, 0));

            _aiDamageState.BaseOnDeath(pieceOfPower);
        }
    }
}