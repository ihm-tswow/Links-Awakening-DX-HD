using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjButtonTouch : GameObject
    {
        private readonly Rectangle _collisionRectangle;
        private readonly string _strKey;
        private readonly string _value;
        private readonly bool _deleteOnTouch;
        private readonly bool _resetKey;

        private bool _currentState;

        public ObjButtonTouch(Map.Map map, int posX, int posY, int buttonWidth, int buttonHeight, string strKey, string value, bool deleteOnTouch, bool resetKey) : base(map, "button")
        {
            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, buttonWidth, buttonHeight);

            _strKey = strKey;
            _value = string.IsNullOrEmpty(value) ? "0" : value;
            _deleteOnTouch = deleteOnTouch;
            _resetKey = resetKey;

            if (string.IsNullOrEmpty(_strKey))
            {
                IsDead = true;
                return;
            }

            _collisionRectangle = new Rectangle(posX, posY, buttonWidth, buttonHeight);
            AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(OnKeyChange));
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
        }

        public override void Init()
        {
            // check if the player spawns on the button
            CheckNextMapPosition();
        }

        private void OnKeyChange()
        {
            var keyState = Game1.GameManager.SaveManager.GetString(_strKey, "0");
            _currentState = keyState == _value;
        }

        private void CheckNextMapPosition()
        {
            if (MapManager.ObjLink.NextMapPositionStart.HasValue &&
                _collisionRectangle.Contains(MapManager.ObjLink.NextMapPositionStart.Value))
            {
                Activate();

                // do not spawn the object
                if (_deleteOnTouch)
                    IsDead = true;

                // trigger event on the right map
                Map.Objects.TriggerKeyChange();
            }
        }

        private void Update()
        {
            var collision = MapManager.ObjLink.BodyRectangle.Intersects(_collisionRectangle);

            if (!_currentState && collision)
                Activate();
            else if (_resetKey && _currentState && !collision)
                Deactivate();
        }

        private void Activate()
        {
            // set the key
            _currentState = true;
            Game1.GameManager.SaveManager.SetString(_strKey, _value);

            if (_deleteOnTouch)
                Map.Objects.DeleteObjects.Add(this);
        }

        private void Deactivate()
        {
            // set the key
            _currentState = false;
            Game1.GameManager.SaveManager.RemoveString(_strKey);
        }
    }
}