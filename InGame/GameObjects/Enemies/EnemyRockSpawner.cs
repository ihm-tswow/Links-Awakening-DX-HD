using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.Map;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyRockSpawner : GameObject
    {
        private readonly Rectangle _field;

        private float _spawnCounter;

        public EnemyRockSpawner() : base("rock") { }

        public EnemyRockSpawner(Map.Map map, int posX, int posY, int width, int height) : base(map)
        {
            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, width, height);

            _field = new Rectangle(posX, posY, width, height);
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
        }

        private void Update()
        {
            if (_field.Contains(MapManager.ObjLink.EntityPosition.Position))
            {
                _spawnCounter -= Game1.DeltaTime;

                if (_spawnCounter < 0)
                {
                    _spawnCounter += Game1.RandomNumber.Next(750, 1500);

                    var playerPosition = MathHelper.Clamp(MapManager.ObjLink.EntityPosition.X, _field.Left + 80, _field.Right - 80);

                    // spawn the rocks around the player inside the field
                    var posX = playerPosition - 80 + Game1.RandomNumber.Next(0, 160);
                    var posY = _field.Y - Game1.RandomNumber.Next(0, 16);
                    var objRock = new EnemyRock(Map, new Vector2(posX, posY));
                    Map.Objects.SpawnObject(objRock);
                }
            }
        }
    }
}