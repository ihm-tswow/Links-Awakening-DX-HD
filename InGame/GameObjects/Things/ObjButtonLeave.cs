using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjButtonLeave : GameObject
    {
        private readonly Rectangle _collisionRectangle;

        private readonly string _strKey;
        private readonly int _buttonDir;
        private readonly bool _negate;

        private bool _isColliding;
        private bool _wasColliding;

        public ObjButtonLeave(Map.Map map, int posX, int posY, string strKey, int direction, int buttonWidth, int buttonHeight, bool negate) : base(map)
        {
            SprEditorImage = Resources.SprWhite;
            EditorIconSource = new Rectangle(0, 0, 16, 16);
            EditorColor = Color.Yellow * 0.5f;

            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, buttonWidth, buttonHeight);

            _strKey = strKey;
            _buttonDir = direction;

            _negate = negate;

            if (string.IsNullOrEmpty(_strKey))
            {
                IsDead = true;
                return;
            }

            _collisionRectangle = new Rectangle(posX, posY, buttonWidth, buttonHeight);
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
        }

        private void Update()
        {
            _isColliding = MapManager.ObjLink.BodyRectangle.Intersects(_collisionRectangle);

            // check if player leaved the collision field
            if (_wasColliding && !_isColliding &&
                (_buttonDir == 0 && _collisionRectangle.X >= MapManager.ObjLink.BodyRectangle.Right ||
                _buttonDir == 2 && _collisionRectangle.X + _collisionRectangle.Width <= MapManager.ObjLink.BodyRectangle.Left ||
                _buttonDir == 1 && _collisionRectangle.Y >= MapManager.ObjLink.BodyRectangle.Bottom ||
                _buttonDir == 3 && _collisionRectangle.Y + _collisionRectangle.Height <= MapManager.ObjLink.BodyRectangle.Top))
            {
                Activate();
            }

            _wasColliding = _isColliding;
            _isColliding = false;
        }

        private void Activate()
        {
            // set the key
            Game1.GameManager.SaveManager.SetString(_strKey, _negate ? "0" : "1");
        }
    }
}