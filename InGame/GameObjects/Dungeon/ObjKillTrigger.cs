using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Enemies;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Dungeon
{
    class ObjKillTrigger : GameObject
    {
        private readonly List<GameObject> _enemyList = new List<GameObject>();
        private readonly Rectangle _triggerField;
        private readonly string _triggerKey;
        private int _currentState;

        public ObjKillTrigger() : base("editor kill trigger") { }

        public ObjKillTrigger(Map.Map map, int posX, int posY, string triggerKey) : base(map)
        {
            Tags = Values.GameObjectTag.None;

            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 16, 16);

            _triggerKey = triggerKey;
            _triggerField = map.GetField(posX, posY);

            if (_triggerKey == null)
            {
                IsDead = true;
                return;
            }

            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
        }

        private void Update()
        {
            // get the enemies the object should watch over
            Map.Objects.GetGameObjectsWithTag(_enemyList, Values.GameObjectTag.Enemy,
                _triggerField.X, _triggerField.Y, _triggerField.Width, _triggerField.Height);

            var enemy0Alive = false;
            var enemy1Alive = false;
            var enemy2Alive = false;

            // check which enemies are alive
            foreach (var gameObject in _enemyList)
            {
                if (gameObject is EnemyPolsVoice)
                    enemy0Alive = true;
                if (gameObject is EnemyKeese)
                    enemy1Alive = true;
                if (gameObject is EnemyShroudedStalfos)
                    enemy2Alive = true;
            }

            if (_currentState == 0 && !enemy0Alive && enemy1Alive && enemy2Alive)
                _currentState = 1;
            if (_currentState == 1 && !enemy0Alive && !enemy1Alive && enemy2Alive)
                _currentState = 2;
            if (_currentState == 2 && !enemy0Alive && !enemy1Alive && !enemy2Alive)
                _currentState = 3;

            if (_currentState < 3)
                return;

            Game1.GameManager.SaveManager.SetString(_triggerKey, "1");

            // remove the object
            Map.Objects.DeleteObjects.Add(this);
        }
    }
}
