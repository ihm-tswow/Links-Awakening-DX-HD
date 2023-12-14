using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Dungeon
{
    internal class ObjDungeonDoor : GameObject
    {
        private enum DoorStates { Opening, Closing, Open, Closed }
        private DoorStates _currentState;

        private readonly BoxCollisionComponent _collisionComponent;
        private readonly Rectangle _sourceRectangle;
        private readonly CSprite _sprite;

        private readonly string _strKey;
        private readonly string _strPushKey;
        private readonly string _pushItem;

        private float _doorState;
        private bool _wasUpdated;

        public ObjDungeonDoor() : base("dungeon_door") { }

        public ObjDungeonDoor(Map.Map map, int posX, int posY, int mode, string strKey, int direction, string strPushKey) : base(map)
        {
            _sourceRectangle = Resources.SourceRectangle("dungeon_door");
            
            _strKey = strKey;
            _strPushKey = strPushKey;

            if (string.IsNullOrEmpty(_strKey))
            {
                IsDead = true;
                return;
            }

            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 16, 16);

            _collisionComponent = new BoxCollisionComponent(new CBox(posX, posY, 0, 16, 16, 16), Values.CollisionTypes.Normal);
            _sprite = new CSprite(Resources.SprObjects, EntityPosition, Rectangle.Empty, new Vector2(8, 8));
            _sprite.Center = new Vector2(8, 8);
            _sprite.Rotation = (float)(Math.PI / 2 * (direction + 1));

            if (!string.IsNullOrEmpty(_strKey))
                AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(KeyChanged));
            AddComponent(CollisionComponent.Index, _collisionComponent);
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(_sprite, Values.LayerBottom));

            _sourceRectangle.X += mode * 16;

            if (mode == 1)
                _pushItem = "smallkey";
            else if (mode == 3)
                _pushItem = "nightmarekey";

            if (mode == 1 || mode == 3)
            {
                var pushBox = new CBox(EntityPosition, 0, 0, 16, 16, 8);
                AddComponent(PushableComponent.Index, new PushableComponent(pushBox, OnPush) { InertiaTime = 100 });
            }

            _sprite.SourceRectangle = _sourceRectangle;
        }

        private void Update()
        {
            _wasUpdated = true;

            if (_currentState == DoorStates.Opening)
            {
                _doorState -= Game1.TimeMultiplier * 0.05f;

                if (_doorState <= 0.5f)
                    _collisionComponent.IsActive = false;

                if (_doorState <= 0)
                {
                    _doorState = 0;
                    _currentState = DoorStates.Open;
                }
            }
            else if (_currentState == DoorStates.Closing)
            {
                _doorState += Game1.TimeMultiplier * 0.1f;
                if (_doorState >= 1)
                {
                    _doorState = 1;
                    _currentState = DoorStates.Closed;
                }
            }

            _sprite.SourceRectangle.Height = (int)Math.Round(16 * _doorState);
            _sprite.SourceRectangle.Y = _sourceRectangle.Y + 16 - _sprite.SourceRectangle.Height;

            _sprite.SpriteEffect = SpriteEffects.FlipHorizontally;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type == PushableComponent.PushType.Impact || _currentState != DoorStates.Closed)
                return false;

            // remove one key
            if (!Game1.GameManager.RemoveItem(_pushItem, 1))
            {
                // start a dialog if the player does not have the required item
                Game1.GameManager.StartDialogPath("door_" + _pushItem);
                return false;
            }

            // only play the sound effect when the player uses a key to open the door
            Game1.GameManager.PlaySoundEffect("D378-04-04", false);

            if (!string.IsNullOrEmpty(_strPushKey))
                Game1.GameManager.SaveManager.SetString(_strPushKey, "1");

            return true;
        }

        private void Open()
        {
            _currentState = DoorStates.Opening;
        }

        private void Close()
        {
            _currentState = DoorStates.Closing;
            _collisionComponent.IsActive = true;

            Game1.GameManager.PlaySoundEffect("D378-16-10", false);
        }

        private void KeyChanged()
        {
            // open/close the door if it is not already in the right state
            // 1: open, 0: closed
            var value = Game1.GameManager.SaveManager.GetString(_strKey);
            var openDoor = value != null && value != "0";

            if (_wasUpdated)
            {
                if (_currentState != DoorStates.Open && openDoor)
                    Open();
                else if (_currentState != DoorStates.Closed && _currentState != DoorStates.Closing && !openDoor)
                    Close();
            }
            else
            {
                // set the door to open or closed
                if (openDoor)
                {
                    _currentState = DoorStates.Open;
                    _collisionComponent.IsActive = false;
                    _doorState = 0;
                }
                else
                {
                    _currentState = DoorStates.Closed;
                    _collisionComponent.IsActive = true;
                    _doorState = 1;
                }
            }
        }
    }
}