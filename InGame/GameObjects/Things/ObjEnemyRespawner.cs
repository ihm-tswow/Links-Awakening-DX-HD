using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjEnemyRespawner : GameObject
    {
        private GameObject _gameObject;

        private readonly string _strSpawnObjectId;
        private readonly object[] _objParameter;

        private int _lastFieldTime;

        public ObjEnemyRespawner() : base("editor object respawner") { }

        public ObjEnemyRespawner(Map.Map map, int posX, int posY, string strSpawnObjectId, string strSpawnParameter) : base(map)
        {
            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 16, 16);

            string[] parameter = null;
            if (strSpawnParameter != null)
            {
                parameter = strSpawnParameter.Split('.');
                // @HACK: some objects have stings with dots in them...
                for (var i = 0; i < parameter.Length; i++)
                    parameter[i] = parameter[i].Replace("$", ".");
            }

            _strSpawnObjectId = strSpawnObjectId;
            _objParameter = MapData.GetParameter(strSpawnObjectId, parameter);
            if (_objParameter != null)
            {
                _objParameter[1] = posX;
                _objParameter[2] = posY;
            }

            _lastFieldTime = Map.GetUpdateState(EntityPosition.Position);

            // add key change listener
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));

            SpawnObject();
        }

        private void Update()
        {
            // field went out of the update range?
            var updateState = Map.GetUpdateState(EntityPosition.Position);

            // gameobject was removed from the map?
            if (_gameObject == null || _gameObject.Map != null || _lastFieldTime >= updateState)
            {
                _lastFieldTime = updateState;
                return;
            }

            SpawnObject();

        }

        private void SpawnObject()
        {
            _gameObject = ObjectManager.GetGameObject(Map, _strSpawnObjectId, _objParameter);

            if (_gameObject != null)
                Map.Objects.SpawnObject(_gameObject);
        }
    }
}