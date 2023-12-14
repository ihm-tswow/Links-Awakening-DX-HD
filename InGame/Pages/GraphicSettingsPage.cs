using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.Controls;
using ProjectZ.InGame.Interface;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.Pages
{
    class GraphicSettingsPage : InterfacePage
    {
        private readonly InterfaceListLayout _bottomBar;
        private readonly InterfaceListLayout _toggleFullscreen;

        //private InterfaceSlider _uiScaleSlider;

        public GraphicSettingsPage(int width, int height)
        {
            // graphic settings layout
            var _graphicSettingsLayout = new InterfaceListLayout { Size = new Point(width, height), Selectable = true };

            var buttonWidth = 240;

            _graphicSettingsLayout.AddElement(new InterfaceLabel(Resources.GameHeaderFont, "settings_graphics_header",
                new Point(buttonWidth, (int)(height * Values.MenuHeaderSize)), new Point(0, 0)));

            var contentLayout = new InterfaceListLayout { Size = new Point(width, (int)(height * Values.MenuContentSize)), Selectable = true, ContentAlignment = InterfaceElement.Gravities.Top };

            contentLayout.AddElement(new InterfaceSlider(Resources.GameFont, "settings_graphics_game_scale",
                buttonWidth, new Point(1, 2), -1, 11, 1, GameSettings.GameScale + 1,
                number =>
                {
                    GameSettings.GameScale = number;
                    Game1.ScaleSettingChanged = true;
                })
            { SetString = number => GameSettings.GameScale == 11 ? "auto" : " x" + (number < 1 ? "1/" + (2 - number) : number.ToString()) });

            //contentLayout.AddElement(_uiScaleSlider = new InterfaceSlider(Resources.GameFont, "settings_graphics_ui_scale",
            //    buttonWidth, new Point(1, 2), 1, Game1.ScreenScale + 1, 1, GameSettings.UiScale - 1,
            //    number =>
            //    {
            //        GameSettings.UiScale = number >= Game1.ScreenScale + 1 ? 0 : number;
            //        Game1.ScaleSettingChanged = true;
            //    })
            //{ SetString = number => GameSettings.UiScale == 0 ? "auto" : " x" + number });

            _toggleFullscreen = InterfaceToggle.GetToggleButton(new Point(buttonWidth, 18), new Point(5, 2),
                "settings_game_fullscreen_mode", GameSettings.IsFullscreen, newState => { Game1.ToggleFullscreen(); });
            contentLayout.AddElement(_toggleFullscreen);

            var toggleFullscreenWindowed = InterfaceToggle.GetToggleButton(new Point(buttonWidth, 18), new Point(5, 2),
                "settings_game_fullscreen_windowed", GameSettings.BorderlessWindowed, newState => { Game1.SwitchFullscreenWindowedSetting(); });
            contentLayout.AddElement(toggleFullscreenWindowed);

            // not sure why this should be an option; but if this should be settable then we need to still enable circular shadows  (e.g. under the player)
            //var shadowToggle = InterfaceToggle.GetToggleButton(new Point(buttonWidth, 18), new Point(5, 2),
            //    "settings_graphics_shadow", GameSettings.EnableShadows, newState => GameSettings.EnableShadows = newState);
            //contentLayout.AddElement(shadowToggle);

            var toggleFpsLock = InterfaceToggle.GetToggleButton(new Point(buttonWidth, 18), new Point(5, 2),
                "settings_graphics_fps_lock", GameSettings.LockFps, newState =>
                {
                    GameSettings.LockFps = newState;
                    Game1.FpsSettingChanged = true;
                });
            contentLayout.AddElement(toggleFpsLock);

            var smoothCameraToggle = InterfaceToggle.GetToggleButton(new Point(buttonWidth, 18), new Point(5, 2),
                "settings_game_change_smooth_camera", GameSettings.SmoothCamera, newState => { GameSettings.SmoothCamera = newState; });
            contentLayout.AddElement(smoothCameraToggle);

            _graphicSettingsLayout.AddElement(contentLayout);

            _bottomBar = new InterfaceListLayout { Size = new Point(width, (int)(height * Values.MenuFooterSize)), Selectable = true, HorizontalMode = true };
            // back button
            _bottomBar.AddElement(new InterfaceButton(new Point(60, 20), new Point(2, 4), "settings_menu_back", element =>
            {
                Game1.UiPageManager.PopPage();
            }));

            _graphicSettingsLayout.AddElement(_bottomBar);

            PageLayout = _graphicSettingsLayout;

            //UpdateScaleSlider();
        }

        public override void Update(CButtons pressedButtons, GameTime gameTime)
        {
            base.Update(pressedButtons, gameTime);

            UpdateFullscreenState();

            //UpdateScaleSlider();

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

        private void UpdateFullscreenState()
        {
            var toggle = ((InterfaceToggle)_toggleFullscreen.Elements[1]);
            if (toggle.ToggleState != GameSettings.IsFullscreen)
                toggle.SetToggle(GameSettings.IsFullscreen);
        }

        //private void UpdateScaleSlider()
        //{
        //    if (GameSettings.UiScale == 0)
        //    {
        //        _uiScaleSlider.UpdateStepSize(1, Game1.ScreenScale + 1, Game1.ScreenScale + 1);
        //        _uiScaleSlider.CurrentStep = Game1.ScreenScale;
        //    }
        //    else
        //    {
        //        _uiScaleSlider.UpdateStepSize(1, Game1.ScreenScale, Game1.ScreenScale + 1);
        //        GameSettings.UiScale = _uiScaleSlider.CurrentStep + 1;
        //    }
        //}
    }
}
