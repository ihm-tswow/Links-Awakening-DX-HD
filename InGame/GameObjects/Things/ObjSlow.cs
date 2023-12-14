using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjSlow : GameObject
    {
        private readonly float _slowdownPercentage;

        public ObjSlow() : base("editor slow")
        {
            EditorColor = Color.Orange * 0.5f;
        }

        public ObjSlow(Map.Map map, int posX, int posY, float slowdownPercentage) : base(map)
        {
            _slowdownPercentage = slowdownPercentage;

            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 16, 16);

            AddComponent(ObjectCollisionComponent.Index,
                new ObjectCollisionComponent(new Rectangle(posX, posY, 16, 12), OnCollision));
        }

        private void OnCollision(GameObject gameObject)
        {
            // slow the player down while he is standing on this area
            // could be made more general and slow down all bodies that are touching the area
            MapManager.ObjLink.SlowDown(_slowdownPercentage);
        }
    }
}