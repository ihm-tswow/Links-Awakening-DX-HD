using ProjectZ.InGame.GameObjects.Base;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjKeySetter : GameObject
    {
        public ObjKeySetter() : base("editor key setter") { }

        public ObjKeySetter(Map.Map map, int posX, int posY, string key, string value) : base(map)
        {
            // set the key and delete the object
            if (!string.IsNullOrEmpty(key))
            {
                Game1.GameManager.SaveManager.SetString(key, value);
                // trigger event on the right map
                Map.Objects.TriggerKeyChange();
            }

            IsDead = true;
        }
    }
}