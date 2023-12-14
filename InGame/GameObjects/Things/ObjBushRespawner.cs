using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjBushRespawner : GameObject
    {
        private readonly string _spawnItem;
        private readonly string _spriteId;
        private readonly bool _hasCollider;
        private readonly bool _drawShadow;
        private readonly bool _setGrassField;
        private readonly int _drawLayer;
        private readonly string _pickupKey;

        private int _lastFieldTime;

        public ObjBushRespawner(Map.Map map, int posX, int posY, string spawnItem, string spriteId,
            bool hasCollider, bool drawShadow, bool setGrassField, int drawLayer, string pickupKey) : base(map)
        {
            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 16, 16);

            _spawnItem = spawnItem;
            _spriteId = spriteId;
            _hasCollider = hasCollider;
            _drawShadow = drawShadow;
            _setGrassField = setGrassField;
            _drawLayer = drawLayer;
            _pickupKey = pickupKey;

            _lastFieldTime = Map.GetUpdateState(EntityPosition.Position);

            // add key change listener
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
        }

        private void Update()
        {
            // field went out of the update range?
            var updateState = Map.GetUpdateState(EntityPosition.Position);
            if (_lastFieldTime < updateState)
                SpawnObject();
        }

        private void SpawnObject()
        {
            Map.Objects.DeleteObjects.Add(this);

            Map.Objects.SpawnObject(new ObjBush(Map, (int)EntityPosition.X, (int)EntityPosition.Y,
                _spawnItem, _spriteId, _hasCollider, _drawShadow, _setGrassField, _drawLayer, _pickupKey));
        }
    }
}