using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjObjectSpawner : GameObject
    {
        private readonly GameObject _spawnObject;

        private readonly string _strKey;
        private readonly string _strValue;
        private readonly string _strSpawnObjectId;
        private readonly object[] _objParameter;

        private readonly bool _canDespawn;
        private bool _isSpawned;

        public ObjObjectSpawner() : base("editor object spawner") { }

        public ObjObjectSpawner(Map.Map map, int posX, int posY, string strKey, string strValue, string strSpawnObjectId, string strSpawnParameter, bool canDespawn = true) : base(map)
        {
            _strKey = strKey;
            _strValue = string.IsNullOrEmpty(strValue) ? "0" : strValue;

            _strSpawnObjectId = strSpawnObjectId;
            string[] parameter = null;
            if (strSpawnParameter != null)
            {
                parameter = strSpawnParameter.Split('.');
                // @HACK: some objects have stings with dots in them...
                for (var i = 0; i < parameter.Length; i++)
                    parameter[i] = parameter[i].Replace("$", ".");
            }

            _canDespawn = canDespawn;

            _objParameter = MapData.GetParameter(strSpawnObjectId, parameter);
            if (_objParameter != null)
            {
                _objParameter[1] = posX;
                _objParameter[2] = posY;
            }

            if (_strSpawnObjectId != null)
                _spawnObject = ObjectManager.GetGameObject(map, _strSpawnObjectId, _objParameter);

            if (_spawnObject == null)
            {
                IsDead = true;
                return;
            }

            // spawn object deactivated
            Map.Objects.SpawnObject(_spawnObject);

            // add key change listener
            if (!string.IsNullOrEmpty(_strKey))
            {
                _spawnObject.IsActive = false;
                AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(KeyChanged));
            }
        }

        private void KeyChanged()
        {
            var value = Game1.GameManager.SaveManager.GetString(_strKey, "0");

            if (!_isSpawned && value == _strValue)
            {
                // activate the object
                _spawnObject.IsActive = true;

                _isSpawned = true;

                // remove the spawner if it does not despawn the object
                if (!_canDespawn)
                    Map.Objects.DeleteObjects.Add(this);
            }
            else if (_isSpawned && value != _strValue)
            {
                // despawn the object
                if (_canDespawn)
                    _spawnObject.IsActive = false;
                else
                    Map.Objects.DeleteObjects.Add(_spawnObject);

                _isSpawned = false;
            }
        }
    }
}