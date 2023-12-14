using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.Controls;
using ProjectZ.InGame.Interface;

namespace ProjectZ.InGame.Pages
{
    class CopyConfirmationPage : InterfacePage
    {
        private InterfaceListLayout _confirmLayout;

        public CopyConfirmationPage(int width, int height)
        {
            // delete save confirm
            {
                _confirmLayout = new InterfaceListLayout { Size = new Point(width, height), Selectable = true };
                _confirmLayout.AddElement(new InterfaceButton(new Point(170, 36), new Point(5, 5), "main_menu_copy_confirmation_header", null) { Selectable = false });

                var yesNoLayout = new InterfaceListLayout { Size = new Point(200, 30), HorizontalMode = true, Selectable = true };
                yesNoLayout.AddElement(new InterfaceButton(new Point(82, 26), new Point(3, 0), "main_menu_copy_confirmation_header_yes", element => OnClickDeleteYes()));
                yesNoLayout.AddElement(new InterfaceButton(new Point(82, 26), new Point(3, 0), "main_menu_copy_confirmation_header_no", element => OnClickDeleteNo()));
                _confirmLayout.AddElement(yesNoLayout);
            }

            PageLayout = _confirmLayout;
        }

        public override void OnLoad(Dictionary<string, object> intent)
        {
            base.OnLoad(intent);

            _confirmLayout.Deselect(false);
            _confirmLayout.Select(InterfaceElement.Directions.Right, false);
        }

        public override void Update(CButtons pressedButtons, GameTime gameTime)
        {
            base.Update(pressedButtons, gameTime);

            if (ControlHandler.ButtonPressed(CButtons.B))
                Abort();
        }

        private void OnClickDeleteYes()
        {
            var intent = new Dictionary<string, object>();
            intent.Add("copyFile", true);

            Game1.UiPageManager.PopPage(intent, PageManager.TransitionAnimation.Fade, PageManager.TransitionAnimation.Fade);
        }

        private void OnClickDeleteNo()
        {
            Abort();
        }

        private void Abort()
        {
            var intent = new Dictionary<string, object>();
            intent.Add("copyFile", false);

            Game1.UiPageManager.PopPage(intent, PageManager.TransitionAnimation.Fade, PageManager.TransitionAnimation.Fade);
        }
    }
}
