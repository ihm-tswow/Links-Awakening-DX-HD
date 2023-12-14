using Microsoft.Xna.Framework;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class ObjZombieSpawner : GameObject
    {
        private Rectangle _triggerField;

        private float _spawnTime;
        private float _spawnCounter;

        public ObjZombieSpawner() : base("zombie") { }

        public ObjZombieSpawner(Map.Map map, int posX, int posY, int spawnTime) : base(map)
        {
            _triggerField = map.GetField(posX, posY);
            _spawnTime = spawnTime;
            _spawnCounter = spawnTime;

            EntityPosition = new CPosition(_triggerField.X, _triggerField.Y, 0);
            EntitySize = new Rectangle(0, 0, _triggerField.Width, _triggerField.Height);

            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
        }

        private void Update()
        {
            // check if the player is standing in the field
            if (_triggerField.Contains(new Point((int)MapManager.ObjLink.EntityPosition.X, (int)MapManager.ObjLink.EntityPosition.Y)))
                _spawnCounter -= Game1.DeltaTime;

            if (_spawnCounter <= 0)
            {
                _spawnCounter = _spawnTime;

                // try to find a position for the new zombie
                for (var i = 0; i < 10; i++)
                {
                    var posX = _triggerField.X + Game1.RandomNumber.Next(0, 10) * Values.TileSize;
                    var posY = _triggerField.Y + Game1.RandomNumber.Next(0, 8) * Values.TileSize;

                    // found a good position?
                    var collidingRectangle = Box.Empty;
                    if (!Map.Objects.Collision(new Box(posX, posY, 0, 16, 16, 8), Box.Empty,
                        Values.CollisionTypes.Normal | Values.CollisionTypes.Enemy | Values.CollisionTypes.Player, 0, 0,
                        ref collidingRectangle))
                    {
                        var newZombie = new EnemyZombie(Map, posX, posY);
                        Map.Objects.SpawnObject(newZombie);
                        break;
                    }
                }
            }
        }
    }
}