using Microsoft.Xna.Framework.Input;
using ProjectZ.Base;
using ProjectZ.InGame.Controls;
using ProjectZ.InGame.Map;

namespace ProjectZ.InGame.Tests
{
    public class SequenceTester
    {
        private int _currentSequence = 2;

        private string[] _sequences = new string[] { "bowWow", "weatherBird", "castle", "gravestone", "marinCliff", "marinBeach", "map", "towerCollapse", "shrine", "picture", "photo", "final" };
        private string _strCurrentSequence;

        private float _counterA;
        private float _counterB;

        private bool _isRunning;

        public void Update()
        {
            if (InputHandler.KeyPressed(Keys.Z))
            {
                _isRunning = !_isRunning;
            }

            if (_isRunning)
            {
                if (!Game1.GameManager.DialogIsRunning() &&
                    (MapManager.ObjLink.CurrentState == GameObjects.ObjLink.State.Idle || MapManager.ObjLink.CurrentState == GameObjects.ObjLink.State.Sequence) &&
                    Game1.GameManager.InGameOverlay.GetCurrentGameSequence() == null)
                {
                    StartNextSequence();
                }

                _counterA -= Game1.DeltaTime;
                _counterB -= Game1.DeltaTime;

                if (_counterA < 0 && _strCurrentSequence != "map")
                {
                    _counterA += 75;
                    ControlHandler.DebugButtons |= CButtons.A;
                }
                if (_counterB < 0)
                {
                    _counterB += 150;
                    ControlHandler.DebugButtons |= CButtons.B;
                }
            }
        }

        private void StartNextSequence()
        {
            _strCurrentSequence = _sequences[_currentSequence];
            Game1.GameManager.InGameOverlay.StartSequence(_strCurrentSequence);
            _currentSequence = (_currentSequence + 1) % _sequences.Length;
        }
    }
}
