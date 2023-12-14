using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameSystems;
using ProjectZ.InGame.Interface;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.Pages
{
    class GameOverPage : InterfacePage
    {
        private InterfaceListLayout _pageLayout;
        private InterfaceListLayout _layout0;

        public GameOverPage(int width, int height)
        {
            _pageLayout = new InterfaceListLayout { Size = new Point(width, height), Selectable = true };

            _layout0 = new InterfaceListLayout { Size = new Point(width, 75), ContentAlignment = InterfaceElement.Gravities.Bottom };
            var layout1 = new InterfaceListLayout { Size = new Point(width - 10, 55) };
            var layout2 = new InterfaceListLayout { Size = new Point(width, 75), ContentAlignment = InterfaceElement.Gravities.Top, Selectable = true };

            _pageLayout.AddElement(_layout0);
            _pageLayout.AddElement(layout1);
            _pageLayout.AddElement(layout2);

            // yes no layout
            _layout0.AddElement(new InterfaceImage(Resources.GetSprite("ui game over"), Point.Zero));
            layout2.AddElement(new InterfaceButton(new Point(85, 20), Point.Zero, "gameover_continue", OnClickContinue) { Margin = new Point(2, 2) });
            layout2.AddElement(new InterfaceButton(new Point(85, 20), Point.Zero, "gameover_quit", OnClickQuit) { Margin = new Point(2, 2) });

            PageLayout = _pageLayout;
        }

        public override void OnLoad(Dictionary<string, object> intent)
        {
            // select the "Back to Game" button
            PageLayout.Deselect(false);
            PageLayout.Select(InterfaceElement.Directions.Top, false);

            Game1.GameManager.ResetMusic();
            Game1.GameManager.SetMusic(2, 0);

            Game1.GbsPlayer.SetVolumeMultiplier(1.0f);
            Game1.GbsPlayer.Play();

            _pageLayout.Recalculate = true;

            _layout0.Recalculate = true;
            _layout0.Size.Y = 75 - (int)(MapManager.Camera.Scale * 2);
        }

        public void OnClickContinue(InterfaceElement element)
        {
            Game1.UiPageManager.ClearStack();
            Game1.ScreenManager.ChangeScreen(Values.ScreenNameGame);

            ((GameOverSystem)Game1.GameManager.GameSystems[typeof(GameOverSystem)]).EndSystem();

            Game1.GameManager.RespawnPlayer();
        }

        public void OnClickQuit(InterfaceElement element)
        {
            ((GameOverSystem)Game1.GameManager.GameSystems[typeof(GameOverSystem)]).EndSystem();
            Game1.ScreenManager.ChangeScreen(Values.ScreenNameMenu);
        }

        public override void Draw(SpriteBatch spriteBatch, Vector2 position, int scale, float transparency)
        {
            PageLayout?.Draw(spriteBatch, position, scale, transparency);
        }
    }
}
