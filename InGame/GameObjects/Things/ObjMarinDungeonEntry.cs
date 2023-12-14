using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;

namespace ProjectZ.InGame.GameObjects.Things
{
    class ObjMarinDungeonEntry : GameObject
    {
        private Rectangle _rectangle;

        public ObjMarinDungeonEntry(Map.Map map, int posX, int posY, int offsetX, int offsetY) : base(map)
        {
            EditorIconSource = new Rectangle(0, 0, 16, 16);
            EditorColor = Color.Blue;

            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 16, 16);

            _rectangle = new Rectangle(posX, posY, 16, 16);
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
        }

        public override void Init()
        {
            if (MapManager.ObjLink.NextMapPositionStart == null)
                return;

            var linkPosition = MapManager.ObjLink.NextMapPositionStart.Value;
            if (_rectangle.Contains(new Point((int)linkPosition.X, (int)linkPosition.Y)))
            {
                MapManager.ObjLink.GetMarin().LeaveDungeonSequence(EntityPosition.Position);
            }
        }

        private void Update()
        {
            if (MapManager.ObjLink.BodyRectangle.Intersects(_rectangle))
                MapManager.ObjLink.GetMarin().EnterDungeonMessage = true;
        }
    }
}