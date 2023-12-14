using System;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyWizzrobeProjectile : GameObject
    {
        private readonly CSprite _sprite;

        public EnemyWizzrobeProjectile(Map.Map map, Vector2 position, int direction, float speed) : base(map)
        {
            EntityPosition = new CPosition(position.X, position.Y, 0);
            EntitySize = new Rectangle(-6, -6, 12, 12);

            _sprite = new CSprite("wizzrobe shot", EntityPosition, Vector2.Zero);
            _sprite.Center = new Vector2(6, 6);
            _sprite.Rotation = MathF.PI / 2f * direction;

            var body = new BodyComponent(EntityPosition, -2, -2, 4, 4, 8)
            {
                IgnoresZ = true,
                IgnoreHoles = true,
                CollisionTypes = Values.CollisionTypes.Normal,
                MoveCollision = OnCollision
            };

            body.VelocityTarget = AnimationHelper.DirectionOffset[direction] * speed;

            var damageCollider = new CBox(EntityPosition, -3, -3, 0, 6, 6, 4);
            
            AddComponent(BodyComponent.Index, body);
            AddComponent(PushableComponent.Index, new PushableComponent(body.BodyBox, OnPush));
            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageCollider, HitType.Enemy, 4));
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(_sprite, Values.LayerTop));
        }

        private void Update()
        {
            // blink
            var blinkTime = 66.667f;
            _sprite.SpriteShader = (Game1.TotalGameTime % (blinkTime * 2) < blinkTime) ? Resources.DamageSpriteShader0 : null;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType pushType)
        {
            Despawn();

            return true;
        }

        private void OnCollision(Values.BodyCollision collision)
        {
            Despawn();
        }

        private void Despawn()
        {
            // spawn despawn effect
            var animation = new ObjAnimator(Map, (int)EntityPosition.X, (int)EntityPosition.Y, Values.LayerTop, "Particles/swordPoke", "run", true);
            Map.Objects.SpawnObject(animation);

            Map.Objects.DeleteObjects.Add(this);
        }
    }
}