using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.GameObjects.Base.Components.AI;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyFlameFountainFireball : GameObject
    {
        private readonly DamageFieldComponent _damageComponent;
        private readonly CSprite _sprite;
        
        private const int LiveTime = 800;
        private double _liveCounter = LiveTime;

        public EnemyFlameFountainFireball(Map.Map map, Vector2 position, Vector2 velocity) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(position.X, position.Y, 0);
            EntitySize = new Rectangle(-8, -8, 16, 18);

            var animator = AnimatorSaveLoad.LoadAnimator("Enemies/flame fountain fireball");
            animator.Play("idle");

            _sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(animator, _sprite, Vector2.Zero);

            var body = new BodyComponent(EntityPosition, -4, -6, 8, 8, 8)
            {
                IgnoresZ = true,
                IgnoreHoles = true,
                CollisionTypes = Values.CollisionTypes.None
            };

            body.VelocityTarget = velocity;

            var damageBox = new CBox(EntityPosition, -3, -3, 0, 6, 6, 16);
            var pushBox = new CBox(EntityPosition, -3, -3, 0, 6, 12, 8);

            AddComponent(DamageFieldComponent.Index, _damageComponent = new DamageFieldComponent(damageBox, HitType.Enemy, 10) { OnDamage = HitPlayer });
            AddComponent(BodyComponent.Index, body);
            AddComponent(PushableComponent.Index, new PushableComponent(pushBox, OnPush));
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(_sprite, Values.LayerPlayer));
            AddComponent(LightDrawComponent.Index, new LightDrawComponent(DrawLight));

            Game1.GameManager.PlaySoundEffect("D378-18-12", true, position);
        }

        private bool HitPlayer()
        {
            // cooldown is lower so that the player will not get through the flamse
            return MapManager.ObjLink.HitPlayer(new Vector2(0, 2), HitType.Enemy, _damageComponent.Strength, true, ObjLink.CooldownTime / 2);
        }

        private void Update()
        {
            // blink
            _sprite.SpriteShader = (Game1.TotalGameTime % (AiDamageState.BlinkTime * 2) < AiDamageState.BlinkTime) ? Resources.DamageSpriteShader0 : null;
           
            _liveCounter -= Game1.DeltaTime;

            // fade out
            if (_liveCounter <= 75)
            {
                if (_sprite.Color == Color.White)
                {
                    ((PushableComponent)Components[PushableComponent.Index]).IsActive = false;
                    ((DamageFieldComponent)Components[DamageFieldComponent.Index]).IsActive = false;
                }

                _sprite.Color = Color.White * ((float)_liveCounter / 75);
            }

            if (_liveCounter < 0)
                Map.Objects.DeleteObjects.Add(this);
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            // check if the player has the better shield and is facing up
            if (Game1.GameManager.ShieldLevel == 2 && MapManager.ObjLink.Direction == 1 && type == PushableComponent.PushType.Impact)
            {
                var distanceMultiplier = (float)(_liveCounter / LiveTime);
                // push the player back
                MapManager.ObjLink._body.Velocity += new Vector3(0, 1, 0) * (0.4f + distanceMultiplier * 0.15f);

                SpawnFlames();
                return false;
            }

            return true;
        }

        private void SpawnFlames()
        {
            ((PushableComponent)Components[PushableComponent.Index]).IsActive = false;
            
            if (_liveCounter > 75)
                _liveCounter = 75;
            _damageComponent.IsActive = false;

            var flameLeft = new EnemyFlameFountainFireballRepelled(Map, new Vector2(EntityPosition.X - 3, EntityPosition.Y - 7), new Vector2(-1, 1) * 0.75f);
            Map.Objects.SpawnObject(flameLeft);

            var flameRight = new EnemyFlameFountainFireballRepelled(Map, new Vector2(EntityPosition.X + 3, EntityPosition.Y - 7), new Vector2(1, 1) * 0.75f);
            Map.Objects.SpawnObject(flameRight);
        }

        private void DrawLight(SpriteBatch spriteBatch)
        {
            DrawHelper.DrawLight(spriteBatch, new Rectangle((int)EntityPosition.X - 16, (int)EntityPosition.Y - 16, 32, 32), 
                new Color(255, 200, 200) * 0.35f * (_sprite.Color.A / 255f));
        }
    }
}