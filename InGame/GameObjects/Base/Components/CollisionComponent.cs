using ProjectZ.Base;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Base.Components
{
    public class CollisionComponent : Component
    {
        public new static int Index = 4;
        public static int Mask = 0x01 << Index;

        public delegate bool CollisionTemplate(Box box, int direction, int level, ref Box collidingBox);
        public CollisionTemplate Collision;

        public Values.CollisionTypes CollisionType = Values.CollisionTypes.Normal;
        
        protected CollisionComponent() { }

        public CollisionComponent(CollisionTemplate collision)
        {
            Collision = collision;
        }
    }
}
