using Microsoft.Xna.Framework;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjColliderOneWay : GameObject
    {
        private readonly Box _collisionBox;
        private readonly int _direction;

        public ObjColliderOneWay(Map.Map map, int posX, int posY, Rectangle collisionRectangle, Values.CollisionTypes type, int direction) : base(map)
        {
            SprEditorImage = Resources.SprWhite;
            EditorIconSource = new Rectangle(0, 0, collisionRectangle.Width, collisionRectangle.Height);
            EditorColor = Color.DeepPink;

            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = collisionRectangle;

            _collisionBox = new Box(posX + collisionRectangle.X, posY + collisionRectangle.Y, 0, collisionRectangle.Width, collisionRectangle.Height, 8);
            _direction = direction;

            AddComponent(CollisionComponent.Index, new CollisionComponent(CollisionCheck) { CollisionType = type });
        }

        private bool CollisionCheck(Box box, int dir, int level, ref Box collidingBox)
        {
            if (dir != _direction || !_collisionBox.Intersects(box))
                return false;

            collidingBox = _collisionBox;
            return true;
        }
    }
}