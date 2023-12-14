using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.Controls;
using ProjectZ.InGame.Interface;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.Pages
{
    class AudioSettingsPage : InterfacePage
    {
        private readonly InterfaceListLayout _bottomBar;

        public AudioSettingsPage(int width, int height)
        {
            // graphic settings layout
            var graphicSettingsLayout = new InterfaceListLayout { Size = new Point(width, height), Selectable = true };

            var buttonWidth = 240;

            graphicSettingsLayout.AddElement(new InterfaceLabel(Resources.GameHeaderFont, "settings_audio_header",
                new Point(buttonWidth, (int)(height * Values.MenuHeaderSize)), new Point(0, 0)));

            var contentLayout = new InterfaceListLayout { Size = new Point(width, (int)(height * Values.MenuContentSize)), Selectable = true, ContentAlignment = InterfaceElement.Gravities.Top };

            contentLayout.AddElement(new InterfaceSlider(Resources.GameFont, "settings_audio_music_volume",
                buttonWidth, new Point(1, 2), 0, 100, 5, GameSettings.MusicVolume, number => { GameSettings.MusicVolume = number; })
            { SetString = number => " " + number + "%" });

            contentLayout.AddElement(new InterfaceSlider(Resources.GameFont, "settings_audio_effect_volume",
                buttonWidth, new Point(1, 2), 0, 100, 5, GameSettings.EffectVolume, number => { GameSettings.EffectVolume = number; })
            { SetString = number => " " + number + "%" });

            graphicSettingsLayout.AddElement(contentLayout);

            _bottomBar = new InterfaceListLayout() { Size = new Point(width, (int)(height * Values.MenuFooterSize)), Selectable = true, HorizontalMode = true };
            // back button
            _bottomBar.AddElement(new InterfaceButton(new Point(60, 20), new Point(2, 4), "settings_menu_back", element =>
            {
                Game1.UiPageManager.PopPage();
            }));

            graphicSettingsLayout.AddElement(_bottomBar);

            PageLayout = graphicSettingsLayout;
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
            // the left button is always the first one selected
            _bottomBar.Deselect(false);
            _bottomBar.Select(InterfaceElement.Directions.Left, false);
            _bottomBar.Deselect(false);

            PageLayout.Deselect(false);
            PageLayout.Select(InterfaceElement.Directions.Top, false);
        }
    }
}
