using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Pages;
using ProjectZ.InGame.Things;
using System;

namespace ProjectZ.InGame.GameSystems
{
    class GameOverSystem : GameSystem
    {
        private const int DyingTime = 500;

        private double _dyingCount;
        private float _percentage;
        private bool _isRunning;

        public void StartDeath()
        {
            _dyingCount = -850;
            _isRunning = true;
            _percentage = 0;
        }

        public void EndSystem()
        {
            _isRunning = false;
            Game1.GameManager.InGameOverlay.HudTransparency = 1;
            Game1.GameManager.DrawPlayerOnTopPercentage = 0;
        }

        public override void Update()
        {
            if (!_isRunning)
                return;

            Game1.GameManager.InGameOverlay.DisableOverlayToggle = true;

            // draw the player on top of everything
            (MapManager.ObjLink.Components[DrawComponent.Index] as DrawComponent).Layer = Values.LayerTop;

            if (_dyingCount < DyingTime)
                _dyingCount += Game1.DeltaTime;

            _percentage = MathHelper.Clamp((float)_dyingCount / DyingTime, 0, 1);

            // fade out hud
            Game1.GameManager.InGameOverlay.HudTransparency = 1 - _percentage;
            // make sure the player is drawn ontop
            Game1.GameManager.DrawPlayerOnTopPercentage = _percentage;
            //Game1.GameManager.MapManager.CurrentMap.LightState = _percentage;

            if (_dyingCount >= DyingTime && !MapManager.ObjLink.Animation.IsPlaying)
            {
                Game1.UpdateGame = false;
                _dyingCount = DyingTime;

                if (Game1.UiPageManager.GetCurrentPage() == null ||
                    Game1.UiPageManager.GetCurrentPage().GetType() != typeof(GameOverPage))
                {
                    Game1.UiPageManager.ClearStack();
                    Game1.UiPageManager.ChangePage(typeof(GameOverPage), null, PageManager.TransitionAnimation.Fade, PageManager.TransitionAnimation.Fade);
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!_isRunning && _percentage > 0)
                return;

            spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointWrap, null, null, null);
            spriteBatch.Draw(Resources.SprWhite, new Rectangle(0, 0, Game1.RenderWidth, Game1.RenderHeight), Color.White * _percentage);
            spriteBatch.End();
        }
    }
}
