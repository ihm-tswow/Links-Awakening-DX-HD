using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;
using ProjectZ.InGame.GameObjects.Base.Components.AI;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyPodobooSplash : GameObject
    {
        private readonly BodyComponent _body;
        private readonly AnimationComponent _animationComponent;
        private readonly AiComponent _aiComponent;
        private readonly CSprite _sprite;

        public EnemyPodobooSplash(Map.Map map, Vector2 position, Vector2 velocity) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(position.X, position.Y, 0);
            EntitySize = new Rectangle(-5, -5, 10, 10);

            var animator = AnimatorSaveLoad.LoadAnimator("Enemies/podoboo");
            animator.Play("splash");

            _sprite = new CSprite(EntityPosition);
            _animationComponent = new AnimationComponent(animator, _sprite, Vector2.Zero);

            _body = new BodyComponent(EntityPosition, -5, -5, 10, 10, 8)
            {
                Gravity2D = 0.065f,
                CollisionTypes = Values.CollisionTypes.None,
                Velocity = new Vector3(velocity.X, velocity.Y, 0),
                SplashEffect = false
            };

            _aiComponent = new AiComponent();

            var stateFlying = new AiState();
            stateFlying.Trigger.Add(new AiTriggerCountdown(500, DespawnTick, () => Map.Objects.DeleteObjects.Add(this)));

            _aiComponent.States.Add("flying", stateFlying);

            _aiComponent.ChangeState("flying");

            AddComponent(BodyComponent.Index, _body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BaseAnimationComponent.Index, _animationComponent);
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(_sprite, Values.LayerBottom));
        }

        private void DespawnTick(double time)
        {
            // 4 frame blink effect
            _sprite.SpriteShader = Game1.TotalGameTime % (8000 / 60f) >= (4000 / 60f) ? Resources.DamageSpriteShader0 : null;

            // fade out
            if (time < 75)
                _sprite.Color = Color.White * (float)(time / 75);
        }
    }
}