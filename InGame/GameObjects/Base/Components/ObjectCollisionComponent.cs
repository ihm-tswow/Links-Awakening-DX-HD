using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base.CObjects;

namespace ProjectZ.InGame.GameObjects.Base.Components
{
    class ObjectCollisionComponent : Component
    {
        public delegate void ObjectCollisionTemplate(GameObject gameObject);
        public ObjectCollisionTemplate OnCollision;

        public CRectangle CollisionRectangle;
        public bool TriggerOnCollision = true;

        public new static int Index = 11;
        public static int Mask = 0x01 << Index;

        protected ObjectCollisionComponent() { }

        public ObjectCollisionComponent(Rectangle collisionRectangle, ObjectCollisionTemplate onCollision)
        {
            CollisionRectangle = new CRectangle(collisionRectangle);
            OnCollision = onCollision;
        }

        public ObjectCollisionComponent(CRectangle collisionRectangle, ObjectCollisionTemplate onCollision)
        {
            CollisionRectangle = collisionRectangle;
            OnCollision = onCollision;
        }
    }
}
