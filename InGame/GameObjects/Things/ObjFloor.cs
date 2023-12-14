using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjFloor : GameObject
    {
        public ObjFloor() : base("editor floor")
        {
            EditorColor = Color.YellowGreen * 0.65f;
        }

        public ObjFloor(Map.Map map, int posX, int posY, int depth) : base(map)
        {
            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 16, 16);

            AddComponent(CollisionComponent.Index, 
                new BoxCollisionComponent(new CBox(posX, posY, -10 + depth, 16, 16, 10), Values.CollisionTypes.Normal));
        }
    }
}