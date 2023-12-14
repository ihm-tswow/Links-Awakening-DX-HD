using ProjectZ.InGame.GameObjects.Base;

namespace ProjectZ.InGame.GameObjects.Dungeon
{
    public class Obj2DMode : GameObject
    {
        public Obj2DMode() : base("editor 2d mode") { }

        public Obj2DMode(Map.Map map, int posX, int posY) : base(map)
        {
            // @HACK: value gets set while adding the object to the ObjectList

            // set the map to be a 2d map
            // maybe this should be inside the none existent map settings?
            // this will probably lead to bugs when other objects expect this to be set in the constructor
            // Game1.GameManager.MapManager.NextMap
            //map.Is2dMap = true;

            IsDead = true;
        }
    }
}