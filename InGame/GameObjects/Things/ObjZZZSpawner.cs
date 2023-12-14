using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjZZZSpawner : GameObject
    {
        private float _spawnCounter;
        private const int SpawnTime = 600;

        public ObjZZZSpawner() : base("tarin_zzz") { }

        public ObjZZZSpawner(Map.Map map, int posX, int posY) : base(map)
        {
            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 16, 16);

            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
        }

        private void Update()
        {
            _spawnCounter -= Game1.DeltaTime;
            if(_spawnCounter < 0)
            {
                _spawnCounter += SpawnTime;
                var objZzz = new ObjZZZ(Map, new Vector2(EntityPosition.X + 8, EntityPosition.Y + 8), new Vector2(1, -1));
                Map.Objects.SpawnObject(objZzz);
            }
        }
    }
}