using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjPositionDialog : GameObject
    {
        public static Map.Map CurrentMap;

        private readonly GameObject _spawnObject;

        private readonly string _strKey;
        private readonly string _strValue;

        private bool _isSpawned;

        public ObjPositionDialog() : base("editor position dialog") { }

        public ObjPositionDialog(Map.Map map, int posX, int posY, string strKey, string strValue, string dialogName) : base(map)
        {
            _strKey = strKey;
            _strValue = strValue;

            CurrentMap = map;
            Game1.GameManager.SaveManager.SetInt(dialogName + "posX", posX);
            Game1.GameManager.SaveManager.SetInt(dialogName + "posY", posY);

            _spawnObject = new ObjDialogBox(map, posX, posY, dialogName);

            // spawn object deactivated
            Map.Objects.SpawnObject(_spawnObject);
            _spawnObject.IsActive = false;

            // add key change listener
            if (!string.IsNullOrEmpty(_strKey))
            {
                AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(KeyChanged));
                // if we spawn a door we need to use this here for objects refering to the player enter position in the init method
                KeyChanged();
            }
            else
            {
                _spawnObject.IsActive = true;
                IsDead = true;
            }
        }

        //public override void Init()
        //{
        //    KeyChanged();
        //}

        private void KeyChanged()
        {
            var value = Game1.GameManager.SaveManager.GetString(_strKey);

            if (!_isSpawned && value == _strValue)
            {
                // activate the object
                _spawnObject.IsActive = true;

                _isSpawned = true;

                Map.Objects.DeleteObjects.Add(this);
            }
        }
    }
}