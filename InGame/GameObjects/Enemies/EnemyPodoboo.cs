using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using Microsoft.Xna.Framework.Graphics;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyPodoboo : GameObject
    {
        private readonly BodyComponent _body;
        private readonly AnimationComponent _animationComponent;
        private readonly AiComponent _aiComponent;
        private readonly DamageFieldComponent _damageField;
        private readonly CSprite _sprite;

        private Vector2 _startPosition;

        public EnemyPodoboo() : base("podoboo") { }

        public EnemyPodoboo(Map.Map map, int posX, int posY, int timeOffset) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-32, -8 - 32, 64, 64);

            _startPosition = EntityPosition.Position;

            var animator = AnimatorSaveLoad.LoadAnimator("Enemies/podoboo");
            animator.Play("idle");

            _sprite = new CSprite(EntityPosition);
            _animationComponent = new AnimationComponent(animator, _sprite, new Vector2(0, -8));

            _body = new BodyComponent(EntityPosition, -5, -8 - 5, 10, 10, 8)
            {
                Gravity2D = 0.05f,
                CollisionTypes = Values.CollisionTypes.None,
                SplashEffect = false
            };

            _aiComponent = new AiComponent();

            var stateFlying = new AiState(UpdateFlying) { Init = InitFlying };
            stateFlying.Trigger.Add(new AiTriggerCountdown(250, null, SpawnParticle) { ResetAfterEnd = true });
            var stateHidden = new AiState() { Init = InitHidden };
            var hiddenCountdown = new AiTriggerCountdown(2000, null, () => _aiComponent.ChangeState("flying"));
            stateHidden.Trigger.Add(hiddenCountdown);

            _aiComponent.States.Add("flying", stateFlying);
            _aiComponent.States.Add("hidden", stateHidden);

            var damageCollider = new CBox(EntityPosition, -5, -8 - 5, 0, 10, 10, 4);

            AddComponent(DamageFieldComponent.Index, _damageField = new DamageFieldComponent(damageCollider, HitType.Enemy, 2));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BaseAnimationComponent.Index, _animationComponent);
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(_sprite, Values.LayerBottom));
            AddComponent(LightDrawComponent.Index, new LightDrawComponent(DrawLight));

            _aiComponent.ChangeState("hidden");
            hiddenCountdown.CurrentTime = timeOffset;
        }

        private void InitFlying()
        {
            _sprite.IsVisible = true;
            _damageField.IsActive = true;
            _body.IsActive = true;
            _body.Velocity.Y = -2.7f;

            SpawnSplash();
        }

        private void UpdateFlying()
        {
            _sprite.SpriteShader = Game1.TotalGameTime % (8000 / 60f) >= (4000 / 60f) ? Resources.DamageSpriteShader0 : null;

            if (_body.Velocity.Y > 0 && !_animationComponent.MirroredV)
            {
                _animationComponent.MirroredV = true;
            }

            if (EntityPosition.Y > _startPosition.Y)
            {
                SpawnSplash();
                _aiComponent.ChangeState("hidden");
            }
        }

        private void InitHidden()
        {
            EntityPosition.Set(_startPosition);
            _body.IsActive = false;
            _sprite.IsVisible = false;
            _damageField.IsActive = false;
            _animationComponent.MirroredV = false;
        }

        private void SpawnParticle()
        {
            var particle = new EnemyPodobooParticle(Map, new Vector2(EntityPosition.X, EntityPosition.Y), _animationComponent.MirroredV);
            Map.Objects.SpawnObject(particle);
        }

        private void SpawnSplash()
        {
            // left splash
            Map.Objects.SpawnObject(new EnemyPodobooSplash(Map, new Vector2(_startPosition.X, _startPosition.Y), new Vector2(-0.5f, -0.85f)));
            // right splash
            Map.Objects.SpawnObject(new EnemyPodobooSplash(Map, new Vector2(_startPosition.X, _startPosition.Y), new Vector2(0.5f, -0.85f)));
        }

        private void DrawLight(SpriteBatch spriteBatch)
        {
            DrawHelper.DrawLight(spriteBatch, new Rectangle((int)EntityPosition.X - 32, (int)EntityPosition.Y - 32, 64, 64), new Color(255, 200, 200) * 0.75f);
        }
    }
}