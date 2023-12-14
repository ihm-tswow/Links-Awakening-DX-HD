using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjCastleDoor : GameObject
    {
        public ObjCastleDoor() : base("castle_door") { }

        public ObjCastleDoor(Map.Map map, int posX, int posY, string saveKey) : base(map)
        {
            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 48, 32);

            // don't spawn the door if the key was already set
            if (saveKey != null && Game1.GameManager.SaveManager.GetString(saveKey) == "1")
            {
                IsDead = true;
                return;
            }

            var sprite = Resources.GetSprite("castle_door");
            var cSprite = new CSprite(sprite, EntityPosition);

            AddComponent(CollisionComponent.Index, new BoxCollisionComponent(new CBox(posX, posY, 0, 48, 32, 16), Values.CollisionTypes.Normal));
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(cSprite, Values.LayerBottom));
        }
    }
}
