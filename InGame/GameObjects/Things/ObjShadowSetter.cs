using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;

namespace ProjectZ.InGame.GameObjects.Things
{
    class ObjShadowSetter : GameObject
    {
        public ObjShadowSetter() : base("editor shadow setter")
        {
            EditorColor = Color.Red;
        }

        public ObjShadowSetter(Map.Map map, int posX, int posY, float height, float rotation) : base(map)
        {
            Map.ShadowHeight = height;
            Map.ShadowRotation = rotation;

            IsDead = true;
        }
    }
}