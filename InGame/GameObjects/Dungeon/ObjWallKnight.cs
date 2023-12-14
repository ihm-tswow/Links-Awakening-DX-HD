using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.Things;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Enemies;

namespace ProjectZ.InGame.GameObjects.Dungeon
{
    internal class ObjWallKnight : GameObject
    {
        private readonly EnemyDarknut _knight;

        public ObjWallKnight() : base("wallKnight") { }

        public ObjWallKnight(Map.Map map, int posX, int posY, bool spawnGoldLeaf) : base(map)
        {
            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 16, 16);

            var rectangle = new CBox(posX, posY, 0, 16, 16, 16);
            var sprite = new CSprite("wallKnight", EntityPosition);

            AddComponent(CollisionComponent.Index, new BoxCollisionComponent(rectangle, Values.CollisionTypes.Normal));
            AddComponent(HittableComponent.Index, new HittableComponent(rectangle, OnHit));
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(sprite, Values.LayerBottom));

            _knight = new EnemyDarknut(map, posX, posY + 10)
            {
                SpawnGoldLeaf = spawnGoldLeaf,
                IsActive = false
            };
            map.Objects.SpawnObject(_knight);
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            // gets destroyed by a bomb
            if (damageType == HitType.Bomb)
            {
                Game1.GameManager.PlaySoundEffect("D378-09-09");

                _knight.WallSpawn();
                
                Map.Objects.DeleteObjects.Add(this);

                return Values.HitCollision.Blocking;
            }

            return Values.HitCollision.None;
        }
    }
}
