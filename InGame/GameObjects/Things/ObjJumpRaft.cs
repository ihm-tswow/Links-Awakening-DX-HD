using Microsoft.Xna.Framework;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjJumpRaft : GameObject
    {
        private readonly RectangleF _collisionRectangle;
        private readonly int _offsetY;

        public ObjJumpRaft() : base("editor jump")
        {
            EditorColor = Color.Blue * 0.5f;
        }

        public ObjJumpRaft(Map.Map map, int posX, int posY, int width, int offsetY) : base(map)
        {
            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, width, 16);

            _collisionRectangle = new RectangleF(posX, posY, width, 16);
            _offsetY = offsetY;

            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
        }

        private void Update()
        {
            if (_collisionRectangle.Intersects(MapManager.ObjLink.BodyRectangle))
            {
                var goalPosition = new Vector2(MapManager.ObjLink.EntityPosition.X, EntityPosition.Y + _offsetY);
                MapManager.ObjLink.RaftJump(goalPosition);
            }
        }
    }
}