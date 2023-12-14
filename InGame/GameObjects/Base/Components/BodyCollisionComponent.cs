using ProjectZ.Base;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Base.Components
{
    class BodyCollisionComponent : CollisionComponent
    {
        public BodyComponent Body;

        public bool IsActive = true;

        public BodyCollisionComponent(BodyComponent body, Values.CollisionTypes collisionType)
        {
            Body = body;
            CollisionType = collisionType;
            Collision = IsColliding;
        }

        public bool IsColliding(Box box, int dir, int level, ref Box collidingBox)
        {
            if (!IsActive || !box.Intersects(Body.BodyBox.Box))
                return false;

            collidingBox = Body.BodyBox.Box;
            return true;
        }
    }
}
