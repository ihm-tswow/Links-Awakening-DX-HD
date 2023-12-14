using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.Controls;
using ProjectZ.InGame.Interface;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.Pages
{
    class GameMenuPage : InterfacePage
    {
        public GameMenuPage(int width, int height)
        {
            // main layout
            var mainLayout = new InterfaceListLayout() { Size = new Point(width, height), Selectable = true };

            mainLayout.AddElement(new InterfaceLabel(Resources.GameHeaderFont, "game_menu_header",
                new Point(150, (int)(height * Values.MenuHeaderSize)), new Point(0, 0))
            { TextColor = Color.White });

            // Size = new Point(width, (int)(height * Values.MenuContentSize))
            var contentLayout = new InterfaceListLayout { AutoSize = true, Selectable = true };

            contentLayout.AddElement(new InterfaceButton(new Point(150, 25), Point.Zero, "game_menu_back_to_game", e => ClosePage()) { Margin = new Point(0, 2) });
            contentLayout.AddElement(new InterfaceButton(new Point(150, 25), Point.Zero, "game_menu_settings", OnClickSettings) { Margin = new Point(0, 2) });
            contentLayout.AddElement(new InterfaceButton(new Point(150, 25), Point.Zero, "game_menu_exit_to_the_menu", OnClickBackToMenu) { Margin = new Point(0, 2) });

            mainLayout.AddElement(contentLayout);

            mainLayout.AddElement(new InterfaceListLayout { Size = new Point(width, (int)(height * Values.MenuFooterSize)) });

            PageLayout = mainLayout;
            PageLayout.Select(InterfaceElement.Directions.Top, false);
        }

        public override void OnLoad(Dictionary<string, object> intent)
        {
            Game1.GbsPlayer.Pause();

            // select the "Back to Game" button
            PageLayout.Deselect(false);
            PageLayout.Select(InterfaceElement.Directions.Top, false);
        }

        public override void OnPop(Dictionary<string, object> intent)
        {
            Game1.GbsPlayer.Resume();
        }

        public override void Update(CButtons pressedButtons, GameTime gameTime)
        {
            base.Update(pressedButtons, gameTime);

            // close the page
            if (ControlHandler.ButtonPressed(CButtons.Start) ||
                ControlHandler.ButtonPressed(CButtons.Left) ||
                ControlHandler.ButtonPressed(CButtons.B))
                ClosePage();
        }

        private void ClosePage()
        {
            Game1.GameManager.InGameOverlay.CloseOverlay();
        }

        public void OnClickSettings(InterfaceElement element)
        {
            Game1.UiPageManager.ChangePage(typeof(SettingsPage));
        }

        public void OnClickBackToMenu(InterfaceElement element)
        {
            // show the yes no layout
            Game1.UiPageManager.ChangePage(typeof(ExitGamePage));
        }
    }
}
