using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.Controls;
using ProjectZ.InGame.Interface;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.Pages
{
    class QuitGamePage : InterfacePage
    {
        public QuitGamePage(int width, int height)
        {
            var margin = 6;

            // yes no layout
            var yesNoLayoutInside = new InterfaceListLayout() { Size = new Point(150 + margin * 2, 55), Selectable = true };
            yesNoLayoutInside.AddElement(new InterfaceLabel(Resources.GameHeaderFont, "game_menu_exit_header", new Point(150, 30), new Point(1, 2)) { TextColor = Color.White });
            var hLayout = new InterfaceListLayout() { Size = new Point(150, 25), Margin = new Point(0, 2), Selectable = true, HorizontalMode = true };
            hLayout.AddElement(new InterfaceButton(new Point(74, 25), Point.Zero, "game_menu_exit_yes", OnClickYes) { Margin = new Point(2, 0) });
            hLayout.AddElement(new InterfaceButton(new Point(74, 25), Point.Zero, "game_menu_exit_no", OnClickNo) { Margin = new Point(2, 0) });
            yesNoLayoutInside.AddElement(hLayout);

            var yesNoLayout = new InterfaceListLayout() { Size = new Point(width, height), Selectable = true };
            yesNoLayout.AddElement(yesNoLayoutInside);

            PageLayout = yesNoLayout;
        }

        public override void Update(CButtons pressedButtons, GameTime gameTime)
        {
            base.Update(pressedButtons, gameTime);

            // close the page
            if (ControlHandler.ButtonPressed(CButtons.B))
                Game1.UiPageManager.PopPage();
        }

        public override void OnLoad(Dictionary<string, object> intent)
        {
            // select the "Back to Game" button
            PageLayout.Deselect(false);
            PageLayout.Select(InterfaceElement.Directions.Right, false);
        }

        public void OnClickNo(InterfaceElement element)
        {
            // go to the previous page
            Game1.UiPageManager.PopPage();
        }

        public void OnClickYes(InterfaceElement element)
        {
            // quit the game
            Application.Exit();
        }
    }
}
