using ProjectZ.InGame.GameObjects.Base;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjItemTester : GameObject
    {
        public ObjItemTester() : base("item") { }

        public ObjItemTester(Map.Map map, int posX, int posY, int width) : base(map)
        {
            IsDead = true;

            var index = 0;
            foreach (var items in Game1.GameManager.ItemManager.Items)
            {
                var objPosX = posX + (index % width) * 32;
                var objPosY = posY + (index / width) * 32;
                var objItem = new ObjItem(map, objPosX, objPosY, "", "", items.Key, "");
                Map.Objects.SpawnObject(objItem);
                index++;
            }
        }
    }
}