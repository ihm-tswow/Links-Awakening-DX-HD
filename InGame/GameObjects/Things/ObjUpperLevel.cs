using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.Map;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjUpperLevel : GameObject
    {
        // this object was created because some objects (hookshot, boomerang, arrow, etc.) care about the height
        // the can be thrown down if the player is standing on top of the platform, but can't get thrown up the same platform if the player is standing on the bottom
        // this was added late in the development but hopefully it still works good enough

        public ObjUpperLevel() : base("editor upper level") { }

        public ObjUpperLevel(Map.Map map, int posX, int posY, int level) : base(map)
        {
            Map.AddFieldState(posX / 16, posY / 16, level == 1 ? MapStates.FieldStates.Level1 : MapStates.FieldStates.Level2);
            IsDead = true;
        }
    }
}