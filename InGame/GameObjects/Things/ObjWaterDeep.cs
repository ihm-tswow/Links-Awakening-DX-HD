using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjWaterDeep : GameObject
    {
        public ObjWaterDeep() : base("editor water")
        {
            EditorColor = Color.CadetBlue * 0.65f;
        }

        public ObjWaterDeep(Map.Map map, int posX, int posY) : base(map)
        {
            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 16, 16);

            Map.AddFieldState(posX / 16, posY / 16, MapStates.FieldStates.DeepWater);

            // step that goes down a little
            AddComponent(CollisionComponent.Index,
                new BoxCollisionComponent(new CBox(posX, posY, -12, 16, 16, 10), Values.CollisionTypes.Normal));

            // collider that is used for enemies to avoid the deep water; currently this is 3 high to allow blocking a body but not jumping into it
            map.Objects.SpawnObject(new ObjCollider(map, posX, posY, 3, new Rectangle(0, 0, 16, 16), Values.CollisionTypes.DeepWater, 0));
        }
    }
}