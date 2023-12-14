using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;

namespace ProjectZ.InGame.GameObjects.Dungeon
{
    internal class ObjDungeon : GameObject
    {
        public ObjDungeon() : base("editor dungeon") { }

        public ObjDungeon(Map.Map map, int posX, int posY, string dungeonName, bool updatePosition, int dungeonLevel) : base(map)
        {
            if (!string.IsNullOrEmpty(dungeonName))
                Game1.GameManager.SetDungeon(dungeonName, dungeonLevel);

            // this is used in side rooms of a dungeon
            // normally this are the 2d rooms
            if (updatePosition)
                AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
        }

        private void Update()
        {
            var playerPosition = new Point(
                (int)(MapManager.ObjLink.PosX - Map.MapOffsetX * 16) / 160,
                (int)(MapManager.ObjLink.PosY - Map.MapOffsetY * 16) / 128);

            // update the player position on the dungeon map
            Game1.GameManager.DungeonUpdatePlayerPosition(playerPosition);
        }
    }
}