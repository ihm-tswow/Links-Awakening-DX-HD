using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.Things;
using ProjectZ.InGame.GameObjects.Things;
using System.Collections.Generic;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyFloorLayerFloor : GameObject
    {
        private readonly List<GameObject> _underlyingObjects = new List<GameObject>();

        public EnemyFloorLayerFloor(Map.Map map, int posX, int posY) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 16, 16);

            // remove the underlying objects
            Map.Objects.GetObjectsOfType(_underlyingObjects, typeof(ObjHole), posX, posY, 16, 16);
            Map.Objects.GetObjectsOfType(_underlyingObjects, typeof(ObjLava), posX, posY, 16, 16);
            SetHoleState(false);

            AddComponent(DrawComponent.Index, new DrawSpriteComponent("d8 floor", EntityPosition, Vector2.Zero, Values.LayerBottom));
        }

        public void SetHoleState(bool active)
        {
            foreach (var gameObject in _underlyingObjects)
            {
                if (gameObject is ObjHole && gameObject.EntityPosition.Position == EntityPosition.Position)
                    gameObject.IsActive = active;
                if (gameObject is ObjLava objLava && gameObject.EntityPosition.Position == EntityPosition.Position)
                    objLava.SetActive(active);
            }
        }
    }
}