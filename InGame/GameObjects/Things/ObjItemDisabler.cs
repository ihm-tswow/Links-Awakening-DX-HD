using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;

namespace ProjectZ.InGame.GameObjects.Things
{
    public class ObjItemDisabler : GameObject
    {
        public ObjItemDisabler() : base("editor item disabler")
        {
            EditorColor = Color.Red;
        }

        public ObjItemDisabler(Map.Map map, int posX, int posY) : base(map)
        {
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
        }

        private void Update()
        {
            MapManager.ObjLink.DisableItems = true;
        }
    }
}