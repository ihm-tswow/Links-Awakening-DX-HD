using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.Controls;
using ProjectZ.InGame.Interface;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.Pages
{
    class SettingsPage : InterfacePage
    {
        private readonly InterfaceLabel _versionLabel;

        public SettingsPage(int width, int height)
        {
            // settings layout
            var settingsLayout = new InterfaceListLayout { Size = new Point(width, height), Selectable = true };

            var headerLayout = new InterfaceListLayout { Size = new Point(width, (int)(height * Values.MenuHeaderSize)), ContentAlignment = InterfaceElement.Gravities.Left, HorizontalMode = true };
            {
                _versionLabel = new InterfaceLabel("", new Point((width - 150) / 2 - 2, headerLayout.Size.Y), new Point(1, 0))
                { Translate = false, TextAlignment = InterfaceElement.Gravities.Left | InterfaceElement.Gravities.Top };
                _versionLabel.SetText(Values.VersionString);
                headerLayout.AddElement(_versionLabel);

                headerLayout.AddElement(new InterfaceLabel(Resources.GameHeaderFont, "settings_menu_header", new Point(150, (int)(height * Values.MenuHeaderSize)), new Point(0, 0)));
            }
            settingsLayout.AddElement(headerLayout);

            var contentLayout = new InterfaceListLayout { Size = new Point(width, (int)(height * Values.MenuContentSize)), Selectable = true };

            // game settings button
            contentLayout.AddElement(new InterfaceButton(new Point(150, 25), new Point(1, 2), "settings_menu_game", element =>
            {
                Game1.UiPageManager.ChangePage(typeof(GameSettingsPage));
            }));

            //// audio settings button
            //contentLayout.AddElement(new InterfaceButton(new Point(150, 25), new Point(1, 2), "settings_menu_audio", element =>
            //{
            //    Game1.UiPageManager.ChangePage(typeof(AudioSettingsPage));
            //}));

            // controll settings button
            contentLayout.AddElement(new InterfaceButton(new Point(150, 25), new Point(1, 2), "settings_menu_controls", element =>
            {
                Game1.UiPageManager.ChangePage(typeof(ControlSettingsPage));
            }));

            // graphic settings button
            contentLayout.AddElement(new InterfaceButton(new Point(150, 25), new Point(1, 2), "settings_menu_video", element =>
            {
                Game1.UiPageManager.ChangePage(typeof(GraphicSettingsPage));
            }));

            settingsLayout.AddElement(contentLayout);

            var bottomLayout = new InterfaceListLayout { Size = new Point(width, (int)(height * Values.MenuFooterSize)), Selectable = true };
            // back button
            bottomLayout.AddElement(new InterfaceButton(new Point(60, 20), new Point(2, 4), "settings_menu_back", element =>
            {
                ExitPage();
            }));
            settingsLayout.AddElement(bottomLayout);

            PageLayout = settingsLayout;
        }

        public override void Update(CButtons pressedButtons, GameTime gameTime)
        {
            base.Update(pressedButtons, gameTime);

            // close the page
            if (ControlHandler.ButtonPressed(CButtons.B))
                ExitPage();
        }

        public override void OnLoad(Dictionary<string, object> intent)
        {
            PageLayout.Deselect(false);
            PageLayout.Select(InterfaceElement.Directions.Top, false);

            // only show the version in the main menu
            if(Game1.ScreenManager.CurrentScreenId == Values.ScreenNameGame)
                _versionLabel.TextColor = Color.Transparent;
            else
                _versionLabel.TextColor = Color.White;
        }

        private void ExitPage()
        {
            // save the new settings
            SettingsSaveLoad.SaveSettings();

            Game1.UiPageManager.PopPage();
        }
    }
}
