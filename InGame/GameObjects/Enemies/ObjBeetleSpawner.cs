using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Enemies;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjBeetleSpawner : GameObject
    {
        private readonly List<GameObject> _enemyList = new List<GameObject>();
        private readonly Rectangle _triggerField;

        private const float SpawnTimer = 1250;
        private float _spawnCounter = SpawnTimer;

        public ObjBeetleSpawner() : base("beetle") { }

        public ObjBeetleSpawner(Map.Map map, int posX, int posY) : base(map)
        {
            EntityPosition = new CPosition(posX + 8, posY + 8, 0);
            EntitySize = new Rectangle(-16, -16, 32, 32);

            _triggerField = map.GetField(posX, posY);

            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
        }

        private void Update()
        {
            var playerDistance = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;

            if (playerDistance.Length() < 36)
                _spawnCounter -= Game1.DeltaTime;

            if (_spawnCounter <= 0)
            {
                _spawnCounter = SpawnTimer;

                // get the enemies the object should watch over
                Map.Objects.GetGameObjectsWithTag(_enemyList, Values.GameObjectTag.Enemy,
                    _triggerField.X, _triggerField.Y, _triggerField.Width, _triggerField.Height);

                // spawn a new beetle there are no more than 3 enemies in the area
                if (_enemyList.Count < 4)
                {
                    var newBeetle = new EnemyBeetle(Map, (int)EntityPosition.X - 8, (int)EntityPosition.Y - 8);
                    Map.Objects.SpawnObject(newBeetle);
                }
            }
        }
    }
}