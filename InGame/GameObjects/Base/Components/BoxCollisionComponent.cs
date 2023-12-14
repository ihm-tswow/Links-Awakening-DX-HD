using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Base.Components
{
    class BoxCollisionComponent : CollisionComponent
    {
        public CBox CollisionBox;
        public int DirectionFlag = 15;
        public bool IsActive = true;

        public BoxCollisionComponent(CBox collisionBox, Values.CollisionTypes collisionType)
        {
            CollisionBox = collisionBox;
            CollisionType = collisionType;
            Collision = IsColliding;
        }
        
        public bool IsColliding(Box box, int dir, int level, ref Box collidingBox)
        {
            if (!IsActive || ((0x01 << dir) & DirectionFlag) == 0 || !box.Intersects(CollisionBox.Box))
                return false;

            collidingBox = CollisionBox.Box;
            return true;
        }
    }
}
