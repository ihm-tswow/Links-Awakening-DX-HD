using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjHoleResetPoint : GameObject
    {
        private readonly int _direction;

        public ObjHoleResetPoint(Map.Map map, int posX, int posY, int direction) : base(map)
        {
            SprEditorImage = Resources.SprWhite;
            EditorIconSource = new Rectangle(0, 0, 16, 16);
            EditorColor = Color.Yellow * 0.75f;

            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 16, 16);

            _direction = direction;

            var collisionRectangle = new Rectangle(posX, posY, 16, 16);
            AddComponent(ObjectCollisionComponent.Index, new ObjectCollisionComponent(collisionRectangle, OnCollision));
        }

        private void OnCollision(GameObject gameObject)
        {
            MapManager.ObjLink.SetHoleResetPosition(EntityPosition.Position, _direction);
        }
    }
}