using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjRollBandEdge : GameObject
    {
        private readonly List<GameObject> _collidingObjects = new List<GameObject>();
        private readonly Box _itemDetectionBox;

        private Box _collisionBox;

        public ObjRollBandEdge(Map.Map map, int posX, int posY) : base(map)
        {
            SprEditorImage = Resources.SprWhite;
            EditorIconSource = new Rectangle(0, 0, 16, 16);
            EditorColor = Color.Sienna * 0.5f;

            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 16, 8);

            _collisionBox = new Box(posX, posY, 0, 16, 8, 8);
            _itemDetectionBox = new Box(posX, posY, 0, 16, 10, 8);

            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(CollisionComponent.Index, new CollisionComponent(Collision));
        }

        public void Update()
        {
            // get and move the components colliding with the rollband
            _collidingObjects.Clear();
            Map.Objects.GetComponentList(_collidingObjects,
                (int)_itemDetectionBox.Left, (int)_itemDetectionBox.Back, (int)_itemDetectionBox.Width, (int)_itemDetectionBox.Height, BodyComponent.Mask);

            foreach (var gameObject in _collidingObjects)
            {
                var gameObjectBody = ((BodyComponent)gameObject.Components[BodyComponent.Index]);
                if (_itemDetectionBox.Contains(gameObjectBody.BodyBox.Box))
                {
                    // stop moving and fall from the edge
                    gameObjectBody.AdditionalMovementVT = Vector2.Zero;
                    gameObjectBody.Position.Set(new Vector3(
                        gameObjectBody.Position.X, EntityPosition.Y + gameObjectBody.Height + 8, (EntityPosition.Y + gameObjectBody.Height + 8) - gameObjectBody.Position.Y));
                }
            }
        }

        private bool Collision(Box box, int dir, int level, ref Box collidingBox)
        {
            if (dir != 1 || !_collisionBox.Intersects(box)) return false;

            collidingBox = _collisionBox;
            return true;
        }
    }
}