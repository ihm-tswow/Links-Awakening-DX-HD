using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.Controls;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.Overlay.Sequences
{
    class MapOverlaySequence : GameSequence
    {
        private MapOverlay _mapOverlay;

        public MapOverlaySequence()
        {
            _sequenceWidth = 144;
            _sequenceHeight = 144;

            _mapOverlay = new MapOverlay(_sequenceWidth, _sequenceHeight, 0, true);
            _mapOverlay.Load();
            _mapOverlay.IsSelected = true;
            _mapOverlay.UpdateRenderTarget();
        }

        public override void OnStart()
        {
            base.OnStart();

            _mapOverlay.OnFocus();
        }

        public override void Update()
        {
            base.Update();

            _mapOverlay.UpdateRenderTarget();
            _mapOverlay.Update();

            // can close the overlay if the dialog isn't running anymore
            if (ControlHandler.ButtonPressed(CButtons.B) &&
               !Game1.GameManager.InGameOverlay.TextboxOverlay.IsOpen)
                Game1.GameManager.InGameOverlay.CloseOverlay();
        }

        public override void DrawRT(SpriteBatch spriteBatch)
        {
            _mapOverlay.DrawRenderTarget(spriteBatch);
            Game1.Graphics.GraphicsDevice.SetRenderTarget(null);
        }

        public override void Draw(SpriteBatch spriteBatch, float transparency)
        {
            spriteBatch.End();

            var width = _sequenceWidth * Game1.UiScale;
            var height = _sequenceHeight * Game1.UiScale;

            _mapOverlay.Draw(spriteBatch, new Rectangle(
                Game1.WindowWidth / 2 - width / 2,
                Game1.WindowHeight / 2 - height / 2, width, height), Color.White * transparency, Game1.GetMatrix);

            spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointClamp, null, null, null, null);

            // draw close text
            {
                var selectStr = "";
                if (ControlHandler.LastKeyboardDown && ControlHandler.ButtonDictionary[CButtons.B].Keys.Length > 0)
                    selectStr = ControlHandler.ButtonDictionary[CButtons.B].Keys[0].ToString();
                if (!ControlHandler.LastKeyboardDown && ControlHandler.ButtonDictionary[CButtons.B].Buttons.Length > 0)
                    selectStr = ControlHandler.ButtonDictionary[CButtons.B].Buttons[0].ToString();
                var inputHelper = selectStr + ": " + Game1.LanguageManager.GetString("map_overlay_close", "error");

                spriteBatch.DrawString(Resources.GameFont, inputHelper,
                    new Vector2(8 * Game1.UiScale, Game1.WindowHeight - 16 * Game1.UiScale), Color.White * transparency, 0, Vector2.Zero, Game1.UiScale, SpriteEffects.None, 0);
            }
        }
    }
}
