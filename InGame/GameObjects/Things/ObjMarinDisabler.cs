using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.Map;

namespace ProjectZ.InGame.GameObjects.Things
{
    public class ObjMarinDisabler : GameObject
    {
        public ObjMarinDisabler() : base("marin") { }

        public ObjMarinDisabler(Map.Map map, int posX, int posY) : base(map)
        {
            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 16, 16);

            var marin = MapManager.ObjLink.GetMarin();
            if (marin != null)
                marin.IsHidden = true;
        }
    }
}