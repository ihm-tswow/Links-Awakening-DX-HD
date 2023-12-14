using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;

namespace ProjectZ.InGame.GameObjects.Things
{
    public class ObjDialogBox : GameObject
    {
        private readonly string _dialogName;

        private bool _wasActive = false;
        private bool _isActive = true;

        public override bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                Init();
                _wasActive = _isActive;
            }
        }

        public ObjDialogBox() : base("editor dialog box")
        {
            EditorColor = Color.GreenYellow;
        }

        public ObjDialogBox(Map.Map map, int posX, int posY, string dialogName) : base(map)
        {
            _dialogName = dialogName;

            if (string.IsNullOrEmpty(_dialogName))
                IsDead = true;
        }

        public override void Init()
        {
            // execute the dialog path
            if (_isActive && !_wasActive)
            {
                Game1.GameManager.StartDialogPath(_dialogName);
                _wasActive = true;
            }
        }
    }
}