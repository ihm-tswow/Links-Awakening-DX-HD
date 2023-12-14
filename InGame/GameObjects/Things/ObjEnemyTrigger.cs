using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjEnemyTrigger : GameObject
    {
        private readonly List<GameObject> _enemyList = new List<GameObject>();
        private readonly Rectangle _triggerField;
        private readonly string _triggerKey;

        private bool _enemiesAlive;
        private bool _init;

        public ObjEnemyTrigger() : base("editor enemy trigger") { }

        public ObjEnemyTrigger(Map.Map map, int posX, int posY, string triggerKey) : base(map)
        {
            if (string.IsNullOrEmpty(triggerKey))
            {
                IsDead = true;
                return;
            }

            _triggerKey = triggerKey;
            _triggerField = map.GetField(posX, posY);

            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
        }

        private void Update()
        {
            // this gets called the first time update is run so it can capture enemies spawned by an ObjObjectSpawner
            if (!_init)
            {
                _init = true;
                // get the enemies the object should watch over
                Map.Objects.GetGameObjectsWithTag(_enemyList, Values.GameObjectTag.Enemy,
                    _triggerField.X, _triggerField.Y, _triggerField.Width, _triggerField.Height);
            }

            _enemiesAlive = false;
            // check if the enemies where deleted from the map
            foreach (var gameObject in _enemyList)
                if (gameObject.Map != null)
                    _enemiesAlive = true;

            if (_enemiesAlive)
                return;

            Game1.GameManager.SaveManager.SetString(_triggerKey, "1");

            //RemoveComponent(UpdateComponent.Index);
            // remove the object
            Map.Objects.DeleteObjects.Add(this);
        }
    }
}