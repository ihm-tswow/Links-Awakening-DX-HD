using System;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameSystems;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    public class ObjDoor : GameObject
    {
        private Rectangle _collisionRectangle;

        public string _entryId;
        public string _nextMap;
        private string _exitId;

        private int _direction;

        // 0: normal door
        // 1: stairs
        // 2: no walk in transition + fall
        // 3: swim in transition
        // 4: no walk in transition
        // 5: final stairs transition
        // 6: fall + rotate
        // 7: fall
        private int _mode;
        private int _positionOffset;

        private bool _isColliding;
        private bool _wasColliding;
        public bool _savePosition;
        private bool _isTransitioning;

        public ObjDoor() : base("editor door")
        {
            EditorColor = Color.Yellow * 0.65f;
        }

        public ObjDoor(Map.Map map, int posX, int posY, int width, int height,
            string entryId, string nextMapId, string exitId, int direction, int mode, bool savePosition) : base(map)
        {
            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, width, height);

            // stairs have a smaller entry
            if (Map.Is2dMap || mode != 1)
                _collisionRectangle = new Rectangle(posX, posY, width, height);
            else
            {
                _collisionRectangle = new Rectangle(posX + 6, posY + 6, width - 12, height - 12);
                _positionOffset = 4;
            }

            if (mode == 4)
            {
                _collisionRectangle.Height = 10;
            }

            _entryId = entryId;
            _direction = direction;
            _mode = mode;
            _savePosition = savePosition;

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
            _wasColliding = _isColliding;
            _isColliding = false;
        }

        private void OnCollision(GameObject gameObject)
        {
            // can jump over stair entries
            if (_mode == 1 && !MapManager.ObjLink.IsGrounded() && !Map.Is2dMap)
                return;

            if (_mode == 3 && !MapManager.ObjLink.IsDiving() && !Map.Is2dMap)
                return;

            if (MapManager.ObjLink.IsRailJumping())
                return;

            _isColliding = true;

            // first step on the door?
            if (MapManager.ObjLink.WasHoleReset || MapManager.ObjLink.CurrentState == ObjLink.State.Dying || _wasColliding || _isTransitioning)
                return;

            _isTransitioning = true;

            var transitionEnd = new Vector2(
                _collisionRectangle.X + _collisionRectangle.Width / 2f,
                _collisionRectangle.Y + _collisionRectangle.Height / 2f + MapManager.ObjLink._body.Height / 2f);
            var color = Values.MapTransitionColor;
            var colorMode = false;

            if (_mode == 0)
            {
                if (_direction == 1)
                    transitionEnd.Y = _collisionRectangle.Y + 8;
                else if (_direction == 3)
                    transitionEnd.Y = _collisionRectangle.Y + 16;

                if (!Map.Is2dMap)
                    MapManager.ObjLink.Direction = (_direction + 2) % 4;

                // walk on the ground
                if (Map.Is2dMap && (_direction % 2) == 0)
                    transitionEnd.Y = _collisionRectangle.Bottom;
            }
            else if (_mode == 3)
            {
                if (Map.Is2dMap)
                {
                    if (_direction == 0)
                        transitionEnd = MapManager.ObjLink.EntityPosition.Position + new Vector2(8, 0);
                    else if (_direction == 2)
                        transitionEnd = MapManager.ObjLink.EntityPosition.Position + new Vector2(-8, 0);
                    else if (_direction == 3)
                        transitionEnd = MapManager.ObjLink.EntityPosition.Position +
                                        new Vector2(MapManager.ObjLink.GetSwimVelocity().X * 8, -8);

                    // look at the camera
                    MapManager.ObjLink.Direction = 3;
                }
                else
                    // do not move while transitioning out
                    transitionEnd = MapManager.ObjLink.EntityPosition.Position;
            }
            else if (_mode == 5)
            {
                transitionEnd = MapManager.ObjLink.EntityPosition.Position + MapManager.ObjLink._body.VelocityTarget * 60 * (MapTransitionSystem.ChangeMapTime / 1000f);
                color = Color.White;
                colorMode = true;
            }

            Game1.GameManager.PlaySoundEffect("D378-06-06");

            MapManager.ObjLink.MapTransitionStart = MapManager.ObjLink.EntityPosition.Position;
            MapManager.ObjLink.MapTransitionEnd = transitionEnd;
            MapManager.ObjLink.TransitionOutWalking = MapManager.ObjLink.EntityPosition.Position != transitionEnd;

            // append a map change
            var transitionSystem = (MapTransitionSystem)Game1.GameManager.GameSystems[typeof(MapTransitionSystem)];
            transitionSystem.AppendMapChange(_nextMap, _exitId, false, false, color, colorMode);
        }

        private void PlacePlayer()
        {
            _isColliding = true;
            _wasColliding = true;

            var transitionStart = new Vector2(
                _collisionRectangle.X + _collisionRectangle.Width / 2f,
                _collisionRectangle.Y + _collisionRectangle.Height / 2f + MapManager.ObjLink._body.Height / 2f);
            var transitionEnd = transitionStart;

            if (_mode == 0 || _mode == 1)
            {
                if (_direction == 0)
                    transitionEnd.X = _collisionRectangle.X - MathF.Ceiling(MapManager.ObjLink._body.Width / 2f) - _positionOffset;
                else if (_direction == 1)
                    transitionEnd.Y = _collisionRectangle.Y - _positionOffset;
                else if (_direction == 2)
                    transitionEnd.X = _collisionRectangle.X + _collisionRectangle.Width + MathF.Ceiling(MapManager.ObjLink._body.Width / 2f) + _positionOffset;
                else if (_direction == 3)
                    transitionEnd.Y = _collisionRectangle.Y + _collisionRectangle.Height + MapManager.ObjLink._body.Height + _positionOffset;

                // walk on the ground
                if (Map.Is2dMap && (_direction % 2) == 0)
                {
                    transitionStart.Y = _collisionRectangle.Bottom;
                    transitionEnd.Y = _collisionRectangle.Bottom;
                }
            }
            else if (_mode == 2)
            {
                MapManager.ObjLink.NextMapFallStart = true;
            }
            else if (_mode == 3)
            {
                if (_direction == 0)
                    transitionEnd.X = _collisionRectangle.X - MathF.Ceiling(MapManager.ObjLink._body.Width / 2f) - _positionOffset;
                else if (_direction == 1)
                    transitionEnd.Y = _collisionRectangle.Y - _positionOffset;
                else if (_direction == 2)
                    transitionEnd.X = _collisionRectangle.X + _collisionRectangle.Width + MathF.Ceiling(MapManager.ObjLink._body.Width / 2f) + _positionOffset;
                else if (_direction == 3)
                    transitionEnd.Y = _collisionRectangle.Y + _collisionRectangle.Height + MapManager.ObjLink._body.Height + _positionOffset;
            }
            else if (_mode == 4)
            {
                // why was this here?
                //transitionEnd.Y = _collisionRectangle.Y + _collisionRectangle.Height + MapManager.ObjLink._body.Height - _positionOffset;
            }
            else if (_mode == 5)
            {
                transitionEnd = transitionStart + new Vector2(0, -0.5f) * 60 * (MapTransitionSystem.ChangeMapTime / 1000f);
            }
            else if (_mode == 6)
            {
                MapManager.ObjLink.NextMapFallRotateStart = true;
            }

            if (_savePosition)
            {
                MapManager.ObjLink.SaveMap = Map.MapName;
                MapManager.ObjLink.SavePosition = transitionEnd;
                MapManager.ObjLink.SaveDirection = _direction;

                // save settings?
                if (GameSettings.Autosave)
                {
                    SaveGameSaveLoad.SaveGame(Game1.GameManager);
                    Game1.GameManager.InGameOverlay.InGameHud.ShowSaveIcon();
                }
            }

            MapManager.ObjLink.NextMapPositionStart = transitionStart;
            MapManager.ObjLink.NextMapPositionEnd = transitionEnd;
            MapManager.ObjLink.TransitionInWalking = transitionStart != transitionEnd;
            MapManager.ObjLink.DirectionEntry = _direction;

            // no transition (eg. fall into a 2d room)
            if (_mode == 7)
            {
                MapManager.ObjLink.Fall2DEntry = true;
                MapManager.ObjLink.NextMapPositionEnd = null;
            }
        }
    }
}