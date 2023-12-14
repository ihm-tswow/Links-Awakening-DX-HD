using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Dungeon
{
    internal class ObjDungeonBlacker : GameObject
    {
        public ObjDungeonBlacker() : base("editor dungeon blacker")
        {
            EditorColor = Color.DarkRed * 0.75f;
        }

        public ObjDungeonBlacker(Map.Map map, int posX, int posY, int colorR, int colorG, int colorB, int colorA) : base(map)
        {
            map.UseLight = true;
            map.LightColor = new Color(colorR, colorG, colorB) * (colorA / 255f);

            IsDead = true;
        }
    }
}