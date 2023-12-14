using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using ProjectZ.InGame.Controls;
using ProjectZ.InGame.Interface;
using ProjectZ.InGame.Things;
using ProjectZ.Base;

namespace ProjectZ.InGame.Pages
{
    class NewGamePage : InterfacePage
    {
        private InterfaceButton _capsLockButton;
        private InterfaceButton[,] _keyboardButtons;
        private InterfaceListLayout[] _keyboardRows;

        private readonly InterfaceButton _newGameButton;
        private readonly InterfaceLabel _labelNameInput;

        private const int MaxNameLength = 12;
        private string _strNameInput;
        private int _selectedSaveSlot;

        private const char CapsLockCharacter = '³';
        private const char BackCharacter = '°';

        private bool _upperMode;

        private char[,] _charactersUpper = new char[,]
        {
            { 'Q', 'W', 'E', 'R', 'T', 'Y', 'U', 'I', 'O', 'P' },
            { 'A', 'S', 'D', 'F', 'G', 'H', 'J', 'K', 'L', '-' },
            { CapsLockCharacter, 'Z', 'X', 'C', 'V', 'B', 'N', 'M', ' ', BackCharacter }
        };

        private char[,] _charactersLower = new char[,]
        {
            { 'q', 'w', 'e', 'r', 't', 'y', 'u', 'i', 'o', 'p' },
            { 'a', 's', 'd', 'f', 'g', 'h', 'j', 'k', 'l', '-' },
            { CapsLockCharacter, 'z', 'x', 'c', 'v', 'b', 'n', 'm', ' ', BackCharacter }
        };

        public NewGamePage(int width, int height)
        {
            // new game layout
            var newGameLayout = new InterfaceListLayout { Size = new Point(width, height), Selectable = true };
            newGameLayout.AddElement(new InterfaceLabel("new_game_menu_save_name") { Margin = new Point(0, 2) });

            _labelNameInput = new InterfaceLabel(null) { Selectable = true, Size = new Point(200, 20) };

            var layerButton = new InterfaceListLayout { Size = new Point(200, 20) };
            layerButton.AddElement(_labelNameInput);
            _newGameButton = new InterfaceButton { Size = new Point(200, 20), InsideElement = layerButton };
            newGameLayout.AddElement(_newGameButton);


            {
                var keyboardLayout = new InterfaceListLayout { AutoSize = true, Margin = new Point(0, 5), Selectable = true };

                var keyWidth = 20;
                var keyHeight = 20;

                _keyboardButtons = new InterfaceButton[_charactersUpper.GetLength(0), _charactersUpper.GetLength(1)];
                _keyboardRows = new InterfaceListLayout[_charactersUpper.GetLength(0)];

                for (var y = 0; y < _charactersUpper.GetLength(0); y++)
                {
                    _keyboardRows[y] = new InterfaceListLayout { AutoSize = true, HorizontalMode = true, Selectable = true };

                    for (int x = 0; x < _charactersUpper.GetLength(1); x++)
                    {
                        if (_charactersUpper[y, x] == '-')
                            continue;

                        var letterX = x;
                        var letterY = y;
                        //var buttonWidth = _charactersUpper[y, x] == BackCharacter ? keyWidth * 2 + 2 : keyHeight;
                        _keyboardButtons[y, x] = new InterfaceButton(new Point(keyWidth, keyHeight), new Point(1, 1), "", element => KeyPressed(letterX, letterY)) { CornerRadius = 0 };
                        ((InterfaceLabel)_keyboardButtons[y, x].InsideElement).SetText(_charactersUpper[y, x].ToString());

                        if (_charactersUpper[y, x] == CapsLockCharacter)
                            _capsLockButton = _keyboardButtons[y, x];

                        _keyboardRows[y].AddElement(_keyboardButtons[y, x]);
                    }

                    _keyboardRows[y].SetSelectionIndex(4);
                    keyboardLayout.AddElement(_keyboardRows[y]);
                }

                newGameLayout.AddElement(keyboardLayout);
            }


            var nglBottomLayout = new InterfaceListLayout { Size = new Point(200, 20), HorizontalMode = true, Selectable = true };
            nglBottomLayout.AddElement(new InterfaceButton(new Point(99, 20), new Point(1, 0), "new_game_menu_back", OnClickBackButton));
            nglBottomLayout.AddElement(new InterfaceButton(new Point(99, 20), new Point(1, 0), "new_game_menu_start_game", OnClickNewGameButton));
            nglBottomLayout.Select(InterfaceElement.Directions.Right, false);
            nglBottomLayout.Deselect(false);

            newGameLayout.AddElement(nglBottomLayout);
            newGameLayout.Select(InterfaceElement.Directions.Top, false);

            PageLayout = newGameLayout;
        }

        public override void OnLoad(Dictionary<string, object> intent)
        {
            // get the selected save slot number from the intent
            _selectedSaveSlot = (int)intent["SelectedSaveSlot"];

            // reset the name of the save slot
            _strNameInput = "Link";
            _labelNameInput.SetText(_strNameInput + " ");

            _upperMode = true;
            UpdateKeyboard();

            PageLayout.Deselect(false);
            PageLayout.Select(InterfaceElement.Directions.Top, false);

            base.OnLoad(intent);
        }

        private void UpdateKeyboard()
        {
            _capsLockButton.Color = _upperMode ? Values.MenuButtonColorSelected : Values.MenuButtonColor;

            for (var y = 0; y < _charactersUpper.GetLength(0); y++)
                for (int x = 0; x < _charactersUpper.GetLength(1); x++)
                    if (_keyboardButtons[y, x] != null)
                        ((InterfaceLabel)_keyboardButtons[y, x].InsideElement).SetText((_upperMode ? _charactersUpper[y, x] : _charactersLower[y, x]).ToString());
        }

        public override void Update(CButtons pressedButtons, GameTime gameTime)
        {
            base.Update(pressedButtons, gameTime);

            // @HACK: going up/down we select the correct button
            for (var y = 0; y < _charactersUpper.GetLength(0); y++)
                for (int x = 0; x < _charactersUpper.GetLength(1); x++)
                    if (_keyboardButtons[y, x] != null && _keyboardButtons[y, x].Selected)
                    {
                        for (var y1 = 0; y1 < _charactersUpper.GetLength(0); y1++)
                            _keyboardRows[y1].SetSelectionIndex(x);
                    }

            if (_newGameButton.Selected)
            {
                // get the keyboard input
                var strInput = InputHandler.ReturnCharacter();
                AddCharacters(strInput);

                if (InputHandler.KeyPressed(Keys.Back))
                    RemoveCharacter();
            }
            else
            {
                // close the page
                if (ControlHandler.ButtonPressed(CButtons.B))
                    Game1.UiPageManager.PopPage();
            }

            _labelNameInput.SetText(_strNameInput + ((gameTime.TotalGameTime.Milliseconds % 500) < 250 ? "_" : " "));
        }

        private void RemoveCharacter()
        {
            // remove the last letter
            if (_strNameInput.Length > 0)
                _strNameInput = _strNameInput.Remove(_strNameInput.Length - 1);
        }

        private void AddCharacters(string letter)
        {
            _strNameInput += letter;

            // cut the string off
            if (_strNameInput.Length > MaxNameLength)
                _strNameInput = _strNameInput.Remove(MaxNameLength);
        }

        private void KeyPressed(int x, int y)
        {
            var characters = _upperMode ? _charactersUpper : _charactersLower;

            // toggle caps lock
            if (characters[y, x] == CapsLockCharacter)
            {
                _upperMode = !_upperMode;
                UpdateKeyboard();
            }
            else if (characters[y, x] == BackCharacter)
                RemoveCharacter();
            else
            {
                AddCharacters(characters[y, x].ToString());
            }
        }

        private void OnClickNewGameButton(InterfaceElement element)
        {
            // change to the game screen
            Game1.ScreenManager.ChangeScreen(Values.ScreenNameGame);
            // create new save file
            Game1.GameManager.StartNewGame(_selectedSaveSlot, _strNameInput);
            // close the gameui
            Game1.UiPageManager.PopAllPages(PageManager.TransitionAnimation.TopToBottom, PageManager.TransitionAnimation.TopToBottom);
        }

        private void OnClickBackButton(InterfaceElement element)
        {
            Game1.UiPageManager.PopPage();
        }
    }
}
