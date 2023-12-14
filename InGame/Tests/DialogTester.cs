using Microsoft.Xna.Framework.Input;
using ProjectZ.Base;
using ProjectZ.InGame.Controls;
using System.Linq;

namespace ProjectZ.InGame.Tests
{
    public class DialogTester
    {
        private string[] _keyList;
        private int _keyIndex;

        private const int TextboxSpeed = 8; // 250
        private float _counterA;
        private bool _isRunning;

        public DialogTester()
        {
            _keyList = Game1.LanguageManager.Strings.Keys.ToArray();
        }

        public void Update()
        {
            if (InputHandler.KeyPressed(Keys.U))
                _isRunning = !_isRunning;

            if (_isRunning)
            {
                _counterA -= Game1.DeltaTime;
                if (_counterA < 0)
                {
                    _counterA += TextboxSpeed;
                    ControlHandler.DebugButtons |= CButtons.A;

                    if (!Game1.GameManager.InGameOverlay.TextboxOverlay.IsOpen)
                        StartNextDialog();
                }
            }
        }

        private void StartNextDialog()
        {
            _keyIndex++;
            if (_keyIndex >= _keyList.Length)
                return;

            Game1.GameManager.StartDialog(_keyList[_keyIndex]);
        }
    }
}
