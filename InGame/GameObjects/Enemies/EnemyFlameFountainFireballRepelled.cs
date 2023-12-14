using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;
using ProjectZ.InGame.GameObjects.Base.Components.AI;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyFlameFountainFireballRepelled : GameObject
    {
        private readonly CSprite _sprite;
        private double _liveTime = 650;

        public EnemyFlameFountainFireballRepelled(Map.Map map, Vector2 position, Vector2 velocity) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(position.X, position.Y, 0);
            EntitySize = new Rectangle(-8, 0, 16, 16);

            var animator = AnimatorSaveLoad.LoadAnimator("Enemies/flame fountain fireball");
            animator.Play(velocity.X < 0 ? "left" : "right");

            _sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(animator, _sprite, new Vector2(0, 8));

            var body = new BodyComponent(EntityPosition, -5, 3, 10, 10, 8)
            {
                IgnoresZ = true,
                IgnoreHoles = true,
                CollisionTypes = Values.CollisionTypes.None,
                VelocityTarget = velocity
            };

            AddComponent(BodyComponent.Index, body);
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(_sprite, Values.LayerPlayer));
            AddComponent(LightDrawComponent.Index, new LightDrawComponent(DrawLight));
        }

        private void Update()
        {
            // blink
            _sprite.SpriteShader = (Game1.TotalGameTime % (AiDamageState.BlinkTime * 2) < AiDamageState.BlinkTime) ? Resources.DamageSpriteShader0 : null;
            
            _liveTime -= Game1.DeltaTime;

            if (_liveTime <= 75)
                _sprite.Color = Color.White * ((float)_liveTime / 75);

            if (_liveTime < 0)
                Map.Objects.DeleteObjects.Add(this);
        }

        private void DrawLight(SpriteBatch spriteBatch)
        {
            DrawHelper.DrawLight(spriteBatch, new Rectangle((int)EntityPosition.X - 16, (int)EntityPosition.Y - 8, 32, 32),
                new Color(255, 200, 200) * 0.35f * (_sprite.Color.A / 255f));
        }
    }
}