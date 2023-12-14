using Microsoft.Xna.Framework;

namespace ProjectZ.InGame.Overlay.Sequences
{
    class TowerCollapseSequence : GameSequence
    {
        private SeqSprite _sprTower;
        private SeqAnimation _aniDust;

        private int _state;

        public override void OnStart()
        {
            Sprites.Clear();
            SpriteDict.Clear();

            Sprites.Add(new SeqSprite("tower_background", new Vector2(0, 0), 0));
            Sprites.Add(new SeqSprite("tower_bottom", new Vector2(48, 96), 1));
            Sprites.Add(_sprTower = new SeqSprite("tower_top", new Vector2(64, 17), 0));

            _sequenceCounter = 0;
            _state = 0;
        }

        public override void Update()
        {
            base.Update();

            var collapseTime = 800;
            var shakeTime = 600;
            var shakePeriode = 4.5f;

            if (_sequenceCounter > 250 && _state == 0)
            {
                Game1.GameManager.ShakeScreen(2300, 0, 1, 0, shakePeriode);
                _state = 1;
            }
            else if (_sequenceCounter > 2500 && _state == 1)
            {
                Game1.GameManager.PlaySoundEffect("D378-12-0C");
                Game1.GameManager.ShakeScreen(shakeTime, 0, 1, 0, shakePeriode);
                Sprites.Add(_aniDust = new SeqAnimation("Sequences/tower dust", "idle", new Vector2(56, 88), 1));
                _sprTower.Position.Y += 8;
                _state = 2;
            }
            else if (_sequenceCounter > 2500 + collapseTime && _state == 2)
            {
                Game1.GameManager.PlaySoundEffect("D378-12-0C");
                Game1.GameManager.ShakeScreen(shakeTime, 0, 1, 0, shakePeriode);
                _sprTower.Position.Y += 8;
                _state = 3;
            }
            else if (_sequenceCounter > 2500 + collapseTime * 2 && _state == 3)
            {
                Game1.GameManager.PlaySoundEffect("D378-12-0C");
                Game1.GameManager.ShakeScreen(shakeTime, 0, 1, 0, shakePeriode);
                _sprTower.Position.Y += 8;
                _state = 4;
            }
            else if (_sequenceCounter > 2500 + collapseTime * 3 && _state == 4)
            {
                Game1.GameManager.PlaySoundEffect("D378-12-0C");
                Game1.GameManager.ShakeScreen(shakeTime, 0, 1, 0, shakePeriode);
                _sprTower.Position.Y += 8;
                _state = 5;
            }
            else if (_sequenceCounter > 2500 + collapseTime * 3 + 800 && _state == 5)
            {
                Sprites.Remove(_aniDust);
                _state = 6;
            }
            else if (_sequenceCounter > 2500 + collapseTime * 4 + 800 && _state == 6)
            {
                Game1.GameManager.InGameOverlay.CloseOverlay();
            }

            // @HACK
            Game1.GameManager.UpdateShake();
        }
    }
}
