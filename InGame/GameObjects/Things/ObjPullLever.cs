using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjPullLever : GameObject
    {
        private readonly DictAtlasEntry _dictLever;
        private readonly DictAtlasEntry _dictLeverTop;

        // currently only supports one lever per map
        public static float LeverState;

        private CPosition _position;
        private Rectangle _field;
        private Point _startPosition;

        private string _openStrKey;

        private const int MinLeverLength = 4;
        private const int MaxLeverLength = 47;
        private const float PullSpeed = 0.24f;
        private readonly float RetractingSpeed;
        private const float OpenSpeed = 0.25f;

        private float _length = MinLeverLength; // 4 - 47

        private bool _isOpening;
        private bool _isGrabbed;
        private bool _wasPulled;

        public ObjPullLever() : base("pull_lever") { }

        public ObjPullLever(Map.Map map, int posX, int posY, float retractingSpeed, string openStrKey) : base(map)
        {
            _startPosition = new Point(posX + 8, posY);

            _position = new CPosition(posX + 8, posY + _length + 16, 0);
            //EntitySize = new Rectangle(-8, -56, 16, 56);

            RetractingSpeed = retractingSpeed;
            _openStrKey = openStrKey;

            LeverState = 1;

            _field = map.GetField(posX, posY, 12);

            _dictLever = Resources.GetSprite("pull_lever");
            _dictLeverTop = Resources.GetSprite("pull_lever_top");

            var collisionBox = new CBox(_position, -2, -56, 0, 4, 54, 8);

            if (!string.IsNullOrEmpty(_openStrKey))
                AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(OnKeyChange));
            AddComponent(CarriableComponent.Index, new CarriableComponent(
                new CRectangle(_position, new Rectangle(-3, -4, 6, 2)), null, null, null)
            { StartGrabbing = StartGrabbing, Pull = OnPull });
            AddComponent(CollisionComponent.Index, new BoxCollisionComponent(collisionBox, Values.CollisionTypes.Normal));
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerBottom, _position));
        }

        private void OnKeyChange()
        {
            var openState = Game1.GameManager.SaveManager.GetString(_openStrKey, "0");
            if (openState == "1")
            {
                _isOpening = true;
                Game1.GameManager.SaveManager.SetString(_openStrKey, "0");
            }
        }

        private void StartGrabbing()
        {
            if (MapManager.ObjLink.Direction != 1)
                return;

            UpdatePlayerPosition();
        }

        private bool OnPull(Vector2 direction)
        {
            if (MapManager.ObjLink.Direction != 1)
                return false;

            _isGrabbed = true;

            if (direction.X != 0 || direction.Y < 0)
                return true;

            // play soundeffect on pull
            if (!_wasPulled && direction.Y > 0)
                Game1.GameManager.PlaySoundEffect("D378-17-11");
            _wasPulled = direction.Y > 0;

            _position.Move(direction * PullSpeed);

            _length = _position.Y - 16 - _startPosition.Y;

            if (_length > MaxLeverLength)
            {
                _length = MaxLeverLength;
                _position.Set(new Vector2(_startPosition.X, _startPosition.Y + 16 + _length));
            }

            UpdateLeverState(direction.Y * (PullSpeed + 0.01f) * Game1.TimeMultiplier);

            UpdatePlayerPosition();

            // this is used to not continuously play the pull animation
            if (_length == MaxLeverLength)
                return false;

            return true;
        }

        private void UpdatePlayerPosition()
        {
            // set the position of the player
            MapManager.ObjLink.EntityPosition.Set(new Vector2(
                _position.X, _position.Y + MapManager.ObjLink.BodyRectangle.Height - 2));
        }

        private void UpdateLeverState(float offset)
        {
            LeverState += offset / (MaxLeverLength - MinLeverLength);
            LeverState = Math.Clamp(LeverState, 0, 1);
        }

        private void Update()
        {
            var insideRoom = _field.Contains(MapManager.ObjLink.EntityPosition.Position);
            if (insideRoom)
                _isOpening = false;

            if (_isOpening)
                UpdateLeverState(OpenSpeed * Game1.TimeMultiplier);
            else if (!_isGrabbed && insideRoom)
                UpdateLeverState(-RetractingSpeed * Game1.TimeMultiplier);

            // move the lever back to the starting position
            if (!_isGrabbed)
            {
                _position.Move(new Vector2(0, -RetractingSpeed));
                if (_position.Y < _startPosition.Y + 16 + MinLeverLength)
                    _position.Set(new Vector2(_startPosition.X, _startPosition.Y + 16 + MinLeverLength));

                _length = _position.Y - 16 - _startPosition.Y;
            }

            _isGrabbed = false;
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            // thing
            spriteBatch.Draw(_dictLeverTop.Texture,
                new Rectangle(_startPosition.X - 2, _startPosition.Y, 4, (int)_length + 1), _dictLeverTop.ScaledRectangle, Color.White);

            // pull thing
            spriteBatch.Draw(_dictLever.Texture,
                new Vector2(_position.X - 8, _position.Y - 16), _dictLever.ScaledRectangle, Color.White);
        }
    }
}