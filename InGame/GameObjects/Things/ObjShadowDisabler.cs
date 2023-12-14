using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;

namespace ProjectZ.InGame.GameObjects.Things
{
    class ObjShadowDisabler : GameObject
    {
        public ObjShadowDisabler() : base("editor shadow disabler") { }

        public ObjShadowDisabler(Map.Map map, int posX, int posY) : base(map)
        {
            EditorColor = Color.Red;

            Map.UseShadows = false;

            IsDead = true;
        }
    }
}