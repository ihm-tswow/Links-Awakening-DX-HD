using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using ProjectZ.Base;
using ProjectZ.InGame.Controls;
using ProjectZ.InGame.Interface;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.Pages
{
    class ControlSettingsPage : InterfacePage
    {
        private readonly InterfaceListLayout[] _remapButtons;
        private readonly InterfaceListLayout _bottomBar;

        private CButtons _selectedButton;
        private bool _updateButton;

        public ControlSettingsPage(int width, int height)
        {
            // control settings layout
            var controlLayout = new InterfaceListLayout { Size = new Point(width, height), Selectable = true };

            controlLayout.AddElement(new InterfaceLabel(Resources.GameHeaderFont, "settings_controls_header",
                new Point(width - 50, (int)(height * Values.MenuHeaderSize)), new Point(0, -10)));

            var controllerHeight = (int)(height * Values.MenuContentSize);

            var buttonWidth = 65;
            var lableWidth = 90;
            var lableHeight = 12;
            var headerHeight = 14;

            var remapHeader = new InterfaceListLayout { AutoSize = true, Margin = new Point(0, 1), HorizontalMode = true, CornerRadius = 0, Color = Values.MenuButtonColor };
            remapHeader.AddElement(new InterfaceListLayout() { Size = new Point(buttonWidth, headerHeight) });
            remapHeader.AddElement(new InterfaceLabel("settings_controls_keyboad", new Point(lableWidth, headerHeight), new Point(0, 0)));
            remapHeader.AddElement(new InterfaceLabel("settings_controls_gamepad", new Point(lableWidth, headerHeight), new Point(0, 0)));
            controlLayout.AddElement(remapHeader);

            var remapButtons = new InterfaceListLayout { AutoSize = true, Margin = new Point(2, 0), Selectable = true };

            _remapButtons = new InterfaceListLayout[Enum.GetValues(typeof(CButtons)).Length - 1];
            var index = 0;

            foreach (CButtons eButton in Enum.GetValues(typeof(CButtons)))
            {
                if (eButton == CButtons.None)
                    continue;

                _remapButtons[index] = new InterfaceListLayout { Size = new Point(buttonWidth + lableWidth * 2, lableHeight), HorizontalMode = true };
                _remapButtons[index].AddElement(new InterfaceLabel("settings_controls_" + eButton, new Point(buttonWidth, lableHeight), Point.Zero)
                { CornerRadius = 0, Color = Values.MenuButtonColor });
                _remapButtons[index].AddElement(new InterfaceLabel("error", new Point(lableWidth, lableHeight), new Point(0, 0)) { Translate = false });
                _remapButtons[index].AddElement(new InterfaceLabel("error", new Point(lableWidth, lableHeight), new Point(0, 0)) { Translate = false });

                var remapButton = new InterfaceButton(new Point(buttonWidth + lableWidth * 2, lableHeight), new Point(0, 0), _remapButtons[index],
                    element =>
                    {
                        _updateButton = true;
                        _selectedButton = eButton;
                    })
                { CornerRadius = 0, Color = Color.Transparent };

                remapButtons.AddElement(remapButton);
                remapButtons.AddElement(new InterfaceListLayout() { Size = new Point(1, 1) });

                index++;
            }

            controlLayout.AddElement(remapButtons);

            _bottomBar = new InterfaceListLayout { Size = new Point(width - 50, (int)(height * Values.MenuFooterSize)), HorizontalMode = true, Selectable = true };
            // reset button
            _bottomBar.AddElement(new InterfaceButton(new Point(60, 20), new Point(2, 4), "settings_controls_reset", OnClickReset));
            // back button
            _bottomBar.AddElement(new InterfaceButton(new Point(60, 20), new Point(2, 4), "settings_menu_back", element =>
            {
                Game1.UiPageManager.PopPage();
            }));
            controlLayout.AddElement(_bottomBar);

            PageLayout = controlLayout;

            UpdateUi();
        }

        public override void OnLoad(Dictionary<string, object> intent)
        {
            // the left button is always the first one selected
            _bottomBar.Deselect(false);
            _bottomBar.Select(InterfaceElement.Directions.Right, false);
            _bottomBar.Deselect(false);

            PageLayout.Deselect(false);
            PageLayout.Select(InterfaceElement.Directions.Top, false);
        }

        public override void Update(CButtons pressedButtons, GameTime gameTime)
        {
            if (_updateButton)
            {
                // update the selected button binding
                var pressedKeys = InputHandler.GetPressedKeys();
                if (pressedKeys.Count > 0)
                {
                    _updateButton = false;
                    UpdateKeyboard(_selectedButton, pressedKeys[0]);
                    UpdateUi();
                }

                var pressedGamepadButtons = InputHandler.GetPressedButtons();
                if (pressedGamepadButtons.Count > 0)
                {
                    _updateButton = false;
                    UpdateButton(_selectedButton, pressedGamepadButtons[0]);
                    UpdateUi();
                }

                InputHandler.ResetInputState();
            }
            else
            {
                // needs to be after the update button stuff
                base.Update(pressedButtons, gameTime);

                // close the page
                if (ControlHandler.ButtonPressed(CButtons.B))
                    Game1.UiPageManager.PopPage();
            }
        }

        public void UpdateUi()
        {
            var buttonNr = 0;

            foreach (var bEntry in ControlHandler.ButtonDictionary)
            {
                var str = "";

                for (var j = 0; j < bEntry.Value.Keys.Length; j++)
                    str += bEntry.Value.Keys[j];
                ((InterfaceLabel)_remapButtons[buttonNr].Elements[1]).SetText(str);

                str = " ";
                for (var j = 0; j < bEntry.Value.Buttons.Length; j++)
                    str += bEntry.Value.Buttons[j];
                ((InterfaceLabel)_remapButtons[buttonNr].Elements[2]).SetText(str);

                buttonNr++;
            }
        }

        private void UpdateKeyboard(CButtons buttonIndex, Keys newKey)
        {
            foreach (var button in ControlHandler.ButtonDictionary)
                if (button.Value.Keys[0] == newKey && button.Key != buttonIndex)
                    button.Value.Keys[0] = ControlHandler.ButtonDictionary[_selectedButton].Keys[0];

            ControlHandler.ButtonDictionary[_selectedButton].Keys = new Keys[] { newKey };
        }

        private void UpdateButton(CButtons buttonIndex, Buttons newButton)
        {
            foreach (var button in ControlHandler.ButtonDictionary)
                if (button.Value.Buttons[0] == newButton && button.Key != buttonIndex)
                    button.Value.Buttons[0] = ControlHandler.ButtonDictionary[_selectedButton].Buttons[0];

            ControlHandler.ButtonDictionary[_selectedButton].Buttons = new Buttons[] { newButton };
        }

        private void OnClickReset(InterfaceElement element)
        {
            ControlHandler.ResetControlls();
            UpdateUi();

            InputHandler.ResetInputState();
        }
    }
}
