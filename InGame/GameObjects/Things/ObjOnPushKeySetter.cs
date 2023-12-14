using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjOnPushKeySetter : GameObject
    {
        private readonly string _strKey;

        public ObjOnPushKeySetter() : base("signpost_0") { }
        
        public ObjOnPushKeySetter(Map.Map map, int posX, int posY, string strKey, int inertiaTime, bool reset) : base(map)
        {
            if (string.IsNullOrEmpty(strKey))
            {
                IsDead = true;
                return;
            }
            
            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 16, 16);

            _strKey = strKey;

            if (reset)
                Game1.GameManager.SaveManager.SetString(_strKey, "0");

            var box = new CBox(EntityPosition, 0, 0, 16, 16, 16);
            AddComponent(PushableComponent.Index, new PushableComponent(box, OnPush) { InertiaTime = inertiaTime });
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            Game1.GameManager.SaveManager.SetString(_strKey, "1");
            return true;
        }
    }
}