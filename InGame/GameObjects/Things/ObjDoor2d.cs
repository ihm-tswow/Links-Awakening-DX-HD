using Microsoft.Xna.Framework;
using ProjectZ.InGame.Controls;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameSystems;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    public class ObjDoor2d : GameObject
    {
        private Rectangle _collisionRectangle;
        private Vector2 _transitionPosition;

        private string _entryId;
        private string _nextMap;
        private string _exitId;

        private bool _isColliding;
        private bool _wasColliding;
        private bool _isTransitioning;

        public ObjDoor2d() : base("editor door")
        {
            EditorColor = Color.Orange * 0.65f;
        }

        public ObjDoor2d(Map.Map map, int posX, int posY, int width, int height, string entryId, string nextMapId, string exitId) : base(map)
        {
            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, width, height);

            _transitionPosition = new Vector2(posX + 8, posY + 16);
            _collisionRectangle = new Rectangle(posX + 6, posY, 4, height);

            _entryId = entryId;

            // has the player just entered this door?
            if (_entryId != null && MapManager.ObjLink.NextMapPositionId == _entryId)
                PlacePlayer();

            _nextMap = nextMapId;
            _exitId = exitId;

            if (!string.IsNullOrEmpty(_nextMap) && !string.IsNullOrEmpty(_exitId))
            {
                AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
                AddComponent(ObjectCollisionComponent.Index,
                    new ObjectCollisionComponent(_collisionRectangle, OnCollision));
            }
        }

        private void Update()
        {
            if (_isTransitioning)
                return;

            _wasColliding = _isColliding;
            _isColliding = false;

            // first step on the door?
            if (MapManager.ObjLink.IsGrounded() && !MapManager.ObjLink.IsTransitioning && _wasColliding && ControlHandler.GetMoveVector2().Y < 0)
            {
                _isTransitioning = true;

                Game1.GameManager.PlaySoundEffect("D378-06-06");

                MapManager.ObjLink.MapTransitionStart = MapManager.ObjLink.EntityPosition.Position;
                MapManager.ObjLink.MapTransitionEnd = _transitionPosition;
                MapManager.ObjLink.TransitionOutWalking = true;
                MapManager.ObjLink.Direction = 1;

                // append a map change
                var transitionSystem = (MapTransitionSystem)Game1.GameManager.GameSystems[typeof(MapTransitionSystem)];
                transitionSystem.AppendMapChange(_nextMap, _exitId, false, false, Values.MapTransitionColor, false);
            }
        }

        private void OnCollision(GameObject gameObject)
        {
            _isColliding = true;
        }

        private void PlacePlayer()
        {
            _isColliding = true;
            _wasColliding = true;

            MapManager.ObjLink.NextMapPositionStart = _transitionPosition;
            MapManager.ObjLink.NextMapPositionEnd = _transitionPosition;
            MapManager.ObjLink.TransitionInWalking = false;
            MapManager.ObjLink.DirectionEntry = 3;
        }
    }
}