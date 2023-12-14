using Microsoft.Xna.Framework;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjObjectRespawner : GameObject
    {
        private GameObject _spawnedObject;

        private Box _spawnBox;

        private readonly string _strDisableKey;
        private readonly string _strSpawnObjectId;
        private readonly object[] _objParameter;

        private const int SpawnTime = 250;
        private float _spawnCounter;
        private bool _isActive = true;

        public ObjObjectRespawner() : base("editor object respawner") { }

        public ObjObjectRespawner(Map.Map map, int posX, int posY, string strDisableKey, string strSpawnObjectId, string strSpawnParameter) : base(map)
        {
            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 16, 16);

            _spawnBox = new Box(posX, posY, 0, 16, 16, 8);

            _strDisableKey = strDisableKey;
            _strSpawnObjectId = strSpawnObjectId;
            string[] parameter = null;
            if (strSpawnParameter != null)
            {
                parameter = strSpawnParameter.Split('.');
                // @HACK: some objects have stings with dots in them...
                for (var i = 0; i < parameter.Length; i++)
                    parameter[i] = parameter[i].Replace("$", ".");
            }

            _objParameter = MapData.GetParameter(strSpawnObjectId, parameter);
            if (_objParameter != null)
            {
                _objParameter[1] = posX;
                _objParameter[2] = posY;
            }

            if (_strSpawnObjectId == null)
            {
                IsDead = true;
                return;
            }

            // add key change listener
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            if (!string.IsNullOrEmpty(_strDisableKey))
                AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(OnKeyChange));

            OnKeyChange();
            SpawnObject();
        }

        private void OnKeyChange()
        {
            if (string.IsNullOrEmpty(_strDisableKey))
                return;

            var state = Game1.GameManager.SaveManager.GetString(_strDisableKey, "0");
            _isActive = state != "1";
        }

        private void Update()
        {
            if (!_isActive || (_spawnedObject != null && _spawnedObject.Map != null))
            {
                _spawnCounter = SpawnTime;
                return;
            }

            _spawnCounter -= Game1.DeltaTime;
            if (_spawnCounter > 0)
                return;

            // return if there is something there
            var outBox = Box.Empty;
            if (Map.Objects.Collision(_spawnBox, Box.Empty, Values.CollisionTypes.Normal | Values.CollisionTypes.Player, 0, 0, ref outBox))
            {
                _spawnCounter = SpawnTime * 0.25f;
                return;
            }

            SpawnObject();

            Game1.GameManager.PlaySoundEffect("D360-15-0F");

            // spawn explosion effect
            Map.Objects.SpawnObject(new ObjAnimator(Map, (int)EntityPosition.X, (int)EntityPosition.Y, Values.LayerTop, "Particles/spawn", "run", true));
        }

        private void SpawnObject()
        {
            _spawnedObject = ObjectManager.GetGameObject(Map, _strSpawnObjectId, _objParameter);
            Map.Objects.SpawnObject(_spawnedObject);
        }
    }
}