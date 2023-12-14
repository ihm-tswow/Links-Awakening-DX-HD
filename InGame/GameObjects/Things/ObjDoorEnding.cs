using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameSystems;

namespace ProjectZ.InGame.GameObjects.Things
{
    public class ObjDoorEnding : GameObject
    {
        private bool _collided;

        public ObjDoorEnding() : base("editor door")
        {
            EditorColor = Color.Purple * 0.65f;
        }

        public ObjDoorEnding(Map.Map map, int posX, int posY) : base(map)
        {
            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 16, 16);

            var collisionRectangle = new Rectangle(posX, posY, 16, 16);
            AddComponent(ObjectCollisionComponent.Index, new ObjectCollisionComponent(collisionRectangle, OnCollision));
        }

        private void OnCollision(GameObject gameObject)
        {
            if(_collided)
                return;
            _collided = true;

            ((EndingSystem)Game1.GameManager.GameSystems[typeof(EndingSystem)]).StartEnding();
        }
    }
}