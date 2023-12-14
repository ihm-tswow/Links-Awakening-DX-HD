using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjHoleTeleporter : GameObject
    {
        private readonly string _roomName;
        private readonly string _entryId;

        public ObjHoleTeleporter(Map.Map map, int posX, int posY, string roomName, string entryId) : base(map)
        {
            SprEditorImage = Resources.SprWhite;
            EditorIconSource = new Rectangle(0, 0, 16, 16);
            EditorColor = Color.HotPink * 0.75f;

            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 16, 16);

            _roomName = roomName;
            _entryId = entryId;

            var collisionRectangle = new Rectangle(posX, posY, 16, 16);
            AddComponent(ObjectCollisionComponent.Index, new ObjectCollisionComponent(collisionRectangle, OnCollision));
        }

        private void OnCollision(GameObject gameObject)
        {
            MapManager.ObjLink.HoleResetRoom = _roomName;
            MapManager.ObjLink.HoleResetEntryId = _entryId;

            MapManager.ObjLink.MapTransitionStart = null;
            MapManager.ObjLink.MapTransitionEnd = null;
        }
    }
}