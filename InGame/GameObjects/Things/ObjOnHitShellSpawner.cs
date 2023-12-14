using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Dungeon;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjOnDashSpawner : GameObject
    {
        private readonly string _saveKey;
        private readonly string _itemName;

        public ObjOnDashSpawner() : base("signpost_0") { }

        public ObjOnDashSpawner(Map.Map map, int posX, int posY, string strKey, string itemName) : base(map)
        {
            _saveKey = strKey;

            if (!string.IsNullOrEmpty(_saveKey) &&
                Game1.GameManager.SaveManager.GetString(_saveKey) == "1")
            {
                IsDead = true;
                return;
            }

            _itemName = itemName;

            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 32, 32);

            var box = new CBox(EntityPosition, 0, 0, 32, 32, 16);
            AddComponent(HittableComponent.Index, new HittableComponent(box, OnHit));
        }

        private Values.HitCollision OnHit(GameObject originObject, Vector2 direction, HitType type, int damage, bool pieceOfPower)
        {
            if ((type & HitType.PegasusBootsPush) != 0)
            {
                SpawnItem(direction);
                Map.Objects.DeleteObjects.Add(this);
            }

            return Values.HitCollision.None;
        }

        private void SpawnItem(Vector2 direction)
        {
            if(_itemName == "fairy")
            {
                var objFairy = new ObjDungeonFairy(Map, (int)EntityPosition.X + 16, (int)EntityPosition.Y + 12, 0);
                Map.Objects.SpawnObject(objFairy);
                return;
            }

            // spawn the shell
            var objItem = new ObjItem(Map, (int)EntityPosition.X + 8, (int)EntityPosition.Y + 12, null, _saveKey, _itemName, null);
            if (objItem.IsDead)
                return;

            objItem.EntityPosition.Z = 16;
            var itemBody = (BodyComponent)objItem.Components[BodyComponent.Index];
            itemBody.Velocity = new Vector3(direction.X * 1.25f, direction.Y * 1.25f, 1.0f);
            itemBody.DragAir = 0.95f;
            Map.Objects.SpawnObject(objItem);
        }
    }
}