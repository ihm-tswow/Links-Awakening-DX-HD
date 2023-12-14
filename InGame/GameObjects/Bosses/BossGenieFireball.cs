using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Bosses
{
    internal class BossGenieFireball : GameObject
    {
        private readonly BodyComponent _body;

        public BossGenieFireball(Map.Map map, Vector3 position) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(position);
            EntitySize = new Rectangle(-7, -64, 14, 64);

            var sprite = new CSprite("fireball", EntityPosition, new Vector2(-7, -16));

            _body = new BodyComponent(EntityPosition, -5, -10, 10, 10, 8)
            {
                IgnoresZ = true,
                DragAir = 1.0f,
                Gravity = 0.0f,
                CollisionTypes = Values.CollisionTypes.None,
                MoveCollision = OnCollision,
            };

            var damageCollider = new CBox(EntityPosition, -6, -12, 0, 12, 12, 8, true);
            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageCollider, HitType.Enemy, 4));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(_body, sprite));
        }

        public void ThrowFireball(Vector3 velocity)
        {
            _body.IgnoresZ = false;
            _body.Velocity = velocity;
        }

        public void SetPosition(Vector3 newPosition)
        {
            EntityPosition.Set(newPosition);
        }

        private void OnCollision(Values.BodyCollision collision)
        {
            Map.Objects.DeleteObjects.Add(this);
        }
    }
}