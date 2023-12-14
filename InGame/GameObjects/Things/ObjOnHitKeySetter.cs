using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjOnHitKeySetter : GameObject
    {
        private readonly string _strKey;
        private readonly HitType _weaponType;

        public ObjOnHitKeySetter() : base("signpost_0") { }

        public ObjOnHitKeySetter(Map.Map map, int posX, int posY, string strKey, int weaponType, bool reset, int width, int height) : base(map)
        {
            if (string.IsNullOrEmpty(strKey))
            {
                IsDead = true;
                return;
            }

            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, width, height);

            _strKey = strKey;
            _weaponType = (HitType)weaponType;

            if (reset)
                Game1.GameManager.SaveManager.SetString(_strKey, "0");

            var box = new CBox(EntityPosition, 0, 0, width, height, 16);
            AddComponent(HittableComponent.Index, new HittableComponent(box, OnHit));
        }

        private Values.HitCollision OnHit(GameObject originObject, Vector2 direction, HitType type, int damage, bool pieceOfPower)
        {
            if((type & _weaponType) != 0)
            {
                Game1.GameManager.SaveManager.SetString(_strKey, "1");
                Map.Objects.DeleteObjects.Add(this);
            }

            return Values.HitCollision.None;
        }
    }
}