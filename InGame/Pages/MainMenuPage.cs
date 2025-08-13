using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.Controls;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.Interface;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.Pages
{
    class MainMenuPage : InterfacePage
    {
        enum State
        {
            Select,
            Delete,
            Copy
        }

        public InterfaceListLayout[] SaveEntries = new InterfaceListLayout[SaveStateManager.SaveCount];

        private float[] _playerSelectionState = new float[SaveStateManager.SaveCount];
        private InterfaceImage[] _playerImage = new InterfaceImage[SaveStateManager.SaveCount];
        private InterfaceButton[] _saveButtons = new InterfaceButton[SaveStateManager.SaveCount];

        private Dictionary<string, object> _newGameIntent = new Dictionary<string, object>();

        private Animator _playerAnimation = new Animator();
        private DictAtlasEntry _heartSprite;

        private InterfaceElement[][] _heartImage = new InterfaceElement[4][];

        private InterfaceGravityLayout[] _saveButtonLayouts = new InterfaceGravityLayout[SaveStateManager.SaveCount];
        private InterfaceLabel[] _saveNames = new InterfaceLabel[SaveStateManager.SaveCount];
        private InterfaceLabel[] _saveRuby = new InterfaceLabel[SaveStateManager.SaveCount];
        private InterfaceListLayout[] _deleteCopyLayouts = new InterfaceListLayout[SaveStateManager.SaveCount];

        private InterfaceListLayout _mainLayout;
        private InterfaceListLayout _newGameButtonLayout;

        private InterfaceListLayout _menuBottomBar;
        private InterfaceListLayout _saveFileList;

        private int _selectedSaveIndex;

        public MainMenuPage(int width, int height)
        {
            var smallButtonWidth = 80;
            var smallButtonMargin = 2;

            var saveButtonRec = new Point(186, 30);
            var sideSize = 50;

            _heartSprite = Resources.GetSprite("heart menu");

            _playerAnimation = AnimatorSaveLoad.LoadAnimator("menu_link");
            _playerAnimation.Play("idle");

            _newGameButtonLayout = new InterfaceListLayout { Size = saveButtonRec };
            _newGameButtonLayout.AddElement(new InterfaceLabel("main_menu_new_game"));

            // list of all save files
            {
                _saveFileList = new InterfaceListLayout() { Size = new Point(width, (int)(height * Values.MenuContentSize)), Selectable = true };
                for (var i = 0; i < SaveStateManager.SaveCount; i++)
                {
                    _saveButtonLayouts[i] = new InterfaceGravityLayout { Size = new Point(saveButtonRec.X, saveButtonRec.Y) };

                    var numberWidth = 17;
                    var saveSlotNumber = new InterfaceLabel(null, new Point(numberWidth, 28), Point.Zero)
                    { Gravity = InterfaceElement.Gravities.Left };
                    saveSlotNumber.SetText((i + 1).ToString());

                    _saveButtonLayouts[i].AddElement(saveSlotNumber);

                    var saveInfoLayout = new InterfaceListLayout { HorizontalMode = true, Size = new Point(saveButtonRec.X - numberWidth, saveButtonRec.Y), Gravity = InterfaceElement.Gravities.Right };

                    // hearts on the left
                    {
                        var heartsWidth = saveButtonRec.X / 2 - numberWidth - 8;
                        var hearts = new InterfaceListLayout { Size = new Point(heartsWidth, 30) };

                        var rowOne = new InterfaceListLayout { Size = new Point(heartsWidth - 4, 7), Margin = new Point(2, 1), HorizontalMode = true, ContentAlignment = InterfaceElement.Gravities.Right };
                        var rowTwo = new InterfaceListLayout { Size = new Point(heartsWidth - 4, 7), Margin = new Point(2, 1), HorizontalMode = true, ContentAlignment = InterfaceElement.Gravities.Right };

                        // hearts
                        _heartImage[i] = new InterfaceElement[14];
                        for (var j = 0; j < 7; j++)
                        {
                            // top row
                            _heartImage[i][j] = rowOne.AddElement(new InterfaceImage(Resources.SprItem, _heartSprite.ScaledRectangle, Point.Zero, new Point(1, 1)) { Gravity = InterfaceElement.Gravities.Right });
                            // bottom row
                            _heartImage[i][j + 7] =
                                rowTwo.AddElement(new InterfaceImage(Resources.SprItem, _heartSprite.ScaledRectangle, Point.Zero, new Point(1, 1)) { Gravity = InterfaceElement.Gravities.Right });
                        }

                        hearts.AddElement(rowOne);
                        hearts.AddElement(rowTwo);

                        saveInfoLayout.AddElement(hearts);
                    }

                    // name + rubys on the right
                    {
                        var rightWidth = saveButtonRec.X / 2 + 8;
                        var middle = new InterfaceListLayout { Gravity = InterfaceElement.Gravities.Left, Margin = new Point(2, 0), Size = new Point(rightWidth, 30) };

                        // name
                        middle.AddElement(_saveNames[i] = new InterfaceLabel(null, new Point(rightWidth - 3, 15), Point.Zero) { Margin = new Point(1, 0), TextAlignment = InterfaceElement.Gravities.Left | InterfaceElement.Gravities.Bottom });
                        // ruby
                        middle.AddElement(_saveRuby[i] = new InterfaceLabel(null, new Point(rightWidth - 2, 13), Point.Zero) { Margin = new Point(0, 0), TextAlignment = InterfaceElement.Gravities.Left });

                        saveInfoLayout.AddElement(middle);
                    }

                    var i1 = i;
                    _saveButtonLayouts[i].AddElement(saveInfoLayout);

                    _saveButtons[i] = new InterfaceButton
                    {
                        InsideElement = _saveButtonLayouts[i],
                        Size = new Point(saveButtonRec.X, saveButtonRec.Y),
                        Margin = new Point(0, 2),
                        ClickFunction = e => OnClickSave(i1)
                    };

                    SaveEntries[i] = new InterfaceListLayout { HorizontalMode = true, Gravity = InterfaceElement.Gravities.Right, AutoSize = true, Selectable = true };

                    // dummy layout
                    SaveEntries[i].AddElement(new InterfaceListLayout { Size = new Point(sideSize - 20, 20) });
                    SaveEntries[i].AddElement(_playerImage[i] = new InterfaceImage(_playerAnimation.SprTexture, _playerAnimation.CurrentFrame.SourceRectangle, new Point(20, 16), new Point(0, 0)));

                    // save file
                    SaveEntries[i].AddElement(_saveButtons[i]);

                    // copy/delete options
                    var currentSlot = i;
                    _deleteCopyLayouts[i] = new InterfaceListLayout
                    {
                        Gravity = InterfaceElement.Gravities.Right,
                        Size = new Point(sideSize, saveButtonRec.Y),
                        PreventSelection = true,
                        Selectable = true,
                        Visible = false
                    };

                    var insideCopy = new InterfaceListLayout() { Size = new Point(sideSize - 4, 13) };
                    insideCopy.AddElement(new InterfaceLabel("main_menu_copy") { Size = new Point(40, 12), TextAlignment = InterfaceElement.Gravities.Bottom });
                    _deleteCopyLayouts[i].AddElement(new InterfaceButton(new Point(sideSize - 4, 13), new Point(0, 1), insideCopy, element => OnClickCopy(currentSlot)));

                    var insideDelete = new InterfaceListLayout() { Size = new Point(sideSize - 4, 13) };
                    insideDelete.AddElement(new InterfaceLabel("main_menu_erase") { Size = new Point(40, 12), TextAlignment = InterfaceElement.Gravities.Bottom });
                    _deleteCopyLayouts[i].AddElement(new InterfaceButton(new Point(sideSize - 4, 13), new Point(0, 1), insideDelete, element => OnClickDelete(currentSlot)));

                    SaveEntries[i].AddElement(_deleteCopyLayouts[i]);

                    // add save button to the main layout
                    _saveFileList.AddElement(SaveEntries[i]);
                }
            }

            var buttonHeight = 18;

            // menu bottom bar
            {
                _menuBottomBar = new InterfaceListLayout
                {
                    Size = new Point(saveButtonRec.X, (int)(height * Values.MenuFooterSize)),
                    HorizontalMode = true,
                    Selectable = true
                };

                var smallButtonLayout = new InterfaceGravityLayout { Size = new Point(smallButtonWidth, buttonHeight) };
                smallButtonLayout.AddElement(new InterfaceLabel("main_menu_settings") { Gravity = InterfaceElement.Gravities.Center });
                _menuBottomBar.AddElement(new InterfaceButton
                {
                    Size = new Point(smallButtonWidth, buttonHeight),
                    InsideElement = smallButtonLayout,
                    Margin = new Point(smallButtonMargin, 2),
                    ClickFunction = element =>
                    {
                        Game1.UiPageManager.ChangePage(typeof(SettingsPage));
                    }
                });

                var smallButtonLayout2 = new InterfaceGravityLayout { Size = new Point(smallButtonWidth, buttonHeight) };
                smallButtonLayout2.AddElement(new InterfaceLabel("main_menu_quit") { Gravity = InterfaceElement.Gravities.Center });
                _menuBottomBar.AddElement(new InterfaceButton
                {
                    Size = new Point(smallButtonWidth, buttonHeight),
                    InsideElement = smallButtonLayout2,
                    Margin = new Point(smallButtonMargin, 2),
                    ClickFunction = element =>
                    {
                        Game1.UiPageManager.ChangePage(typeof(QuitGamePage));
                    }
                });
            }

            // main layout
            {
                _mainLayout = new InterfaceListLayout { Size = new Point(width, height), Gravity = InterfaceElement.Gravities.Left, Selectable = true };

                _mainLayout.AddElement(new InterfaceLabel(Resources.GameHeaderFont, "main_menu_select_header", new Point(width, (int)(height * Values.MenuHeaderSize)), new Point(0, 0)));
                _mainLayout.AddElement(_saveFileList);
                _mainLayout.AddElement(_menuBottomBar);
            }

            PageLayout = _mainLayout;
            PageLayout.Select(InterfaceElement.Directions.Top, false);
        }

        public override void OnLoad(Dictionary<string, object> intent)
        {
            // load the savestates
            SaveStateManager.LoadSaveData();

            UpdateUi();

            // select the savestate
            if (_selectedSaveIndex != -1)
            {
                _saveFileList.Elements[_selectedSaveIndex].Deselect(false);
                _saveFileList.Elements[_selectedSaveIndex].Select(InterfaceElement.Directions.Left, false);
            }

            for (var i = 0; i < _deleteCopyLayouts.Length; i++)
                _deleteCopyLayouts[i].Visible = i == 0 && SaveStateManager.SaveStates[i] != null;

            PageLayout = _mainLayout;
            PageLayout.Deselect(false);
            PageLayout.Select(InterfaceElement.Directions.Top, false);

            UpdatePlayerAnimation(25);
        }

        public override void OnReturn(Dictionary<string, object> intent)
        {
            base.OnReturn(intent);

            if (intent != null && intent.TryGetValue("deleteReturn", out var deleteReturn) && (bool)deleteReturn)
            {
                // select the savestate
                _saveFileList.Elements[_selectedSaveIndex].Deselect(false);
                _saveFileList.Elements[_selectedSaveIndex].Select(InterfaceElement.Directions.Left, false);
            }

            // delete the save state?
            if (intent != null && intent.TryGetValue("deleteSavestate", out var deleteSaveState) && (bool)deleteSaveState)
            {
                SaveGameSaveLoad.DeleteSaveFile(_selectedSaveIndex);
                ReloadSaves();
            }

            // copy save state
            if (intent != null && intent.TryGetValue("copyTargetSlot", out var targetSlot))
            {
                SaveGameSaveLoad.CopySaveFile(_selectedSaveIndex, (int)targetSlot);
                ReloadSaves();

                // select the savestate
                _saveFileList.Elements[_selectedSaveIndex].Deselect(false);
                _saveFileList.Elements[_selectedSaveIndex].Select(InterfaceElement.Directions.Left, false);
                _saveFileList.Elements[_selectedSaveIndex].Deselect(false);

                // select the target slot
                _saveFileList.Select((int)targetSlot, false);
            }
        }

        public override void Update(CButtons pressedButtons, GameTime gameTime)
        {
            base.Update(pressedButtons, gameTime);

            if (Game1.FinishedLoading && Game1.LoadFirstSave)
            {
                Game1.LoadFirstSave = false;
                LoadSave(0);
            }

            UpdatePlayerAnimation();

            // only show the copy/delete buttons for the saveslot that is currently selected
            var selectedSaveIndex = -1;
            for (var i = 0; i < _deleteCopyLayouts.Length; i++)
            {
                _deleteCopyLayouts[i].Visible = _saveFileList.Elements[i].Selected && SaveStateManager.SaveStates[i] != null;
                if (_saveFileList.Elements[i].Selected)
                    selectedSaveIndex = i;
            }

            if (ControlHandler.ButtonPressed(CButtons.B))
            {
                _selectedSaveIndex = selectedSaveIndex;

                // change to the game screen
                Game1.ScreenManager.ChangeScreen(Values.ScreenNameIntro);
                // close the menu page
                Game1.UiPageManager.PopPage(null, PageManager.TransitionAnimation.TopToBottom, PageManager.TransitionAnimation.TopToBottom);
            }
        }

        private void UpdatePlayerAnimation(float transitionSpeed = 0.25f)
        {
            // update the animation
            _playerAnimation.Update();

            for (var i = 0; i < SaveStateManager.SaveCount; i++)
            {
                _playerSelectionState[i] = AnimationHelper.MoveToTarget(_playerSelectionState[i], _saveButtons[i].Selected ? 1 : 0, transitionSpeed * Game1.TimeMultiplier);

                _playerImage[i].ImageColor = Color.Lerp(Color.Transparent, Color.White, _playerSelectionState[i]);
                _playerImage[i].SourceRectangle = _playerAnimation.CurrentFrame.SourceRectangle;
                _playerImage[i].Offset = new Vector2(
                    _playerAnimation.CurrentAnimation.Offset.X + _playerAnimation.CurrentFrame.Offset.X,
                    _playerAnimation.CurrentAnimation.Offset.Y + _playerAnimation.CurrentFrame.Offset.Y);
                _playerImage[i].Effects =
                    (_playerAnimation.CurrentFrame.MirroredV ? SpriteEffects.FlipVertically : SpriteEffects.None) |
                    (_playerAnimation.CurrentFrame.MirroredH ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
            }
        }

        private void OnClickCopy(int number)
        {
            _selectedSaveIndex = number;

            var intent = new Dictionary<string, object>();
            intent.Add("selectedSlot", number);

            Game1.UiPageManager.ChangePage(typeof(CopyPage), intent, PageManager.TransitionAnimation.Fade, PageManager.TransitionAnimation.Fade);
        }

        private void OnClickDelete(int number)
        {
            _selectedSaveIndex = number;

            Game1.UiPageManager.ChangePage(typeof(DeleteSaveSlotPage), null, PageManager.TransitionAnimation.Fade, PageManager.TransitionAnimation.Fade);
        }

        private void OnClickSave(int number)
        {
            _selectedSaveIndex = number;

            // load the save file
            LoadSave(number);
        }

        private void LoadSave(int saveIndex)
        {
            // load game or create new save
            if (SaveStateManager.SaveStates[saveIndex] != null)
            {
                // change to the game screen
                Game1.ScreenManager.ChangeScreen(Values.ScreenNameGame);
                // load the save
                Game1.GameManager.LoadSaveFile(saveIndex);
                // close the menu page
                Game1.UiPageManager.PopPage(null, PageManager.TransitionAnimation.TopToBottom, PageManager.TransitionAnimation.TopToBottom);
            }
            else
            {
                // change to the NewGamePage
                _newGameIntent["SelectedSaveSlot"] = saveIndex;
                Game1.UiPageManager.ChangePage(typeof(NewGamePage), _newGameIntent);
            }
        }

        private void ReloadSaves()
        {
            // load the savestates
            SaveStateManager.LoadSaveData();

            // update the UI
            UpdateUi();
        }

        private void UpdateUi()
        {
            for (var i = 0; i < SaveStateManager.SaveCount; i++)
            {
                if (SaveStateManager.SaveStates[i] == null)
                {
                    _saveButtons[i].InsideElement = _newGameButtonLayout;
                    continue;
                }
                else
                {
                    _saveButtons[i].InsideElement = _saveButtonLayouts[i];
                }

                _saveNames[i].SetText(SaveStateManager.SaveStates[i].Name);
                _saveRuby[i].SetText(SaveStateManager.SaveStates[i].CurrentRubee.ToString());

                for (var j = 0; j < 14; j++)
                {
                    // only draw the hearts the player has
                    _heartImage[i][j].Hidden = SaveStateManager.SaveStates[i].MaxHearth <= j;

                    var state = 4 - MathHelper.Clamp(SaveStateManager.SaveStates[i].CurrentHearth - (j * 4), 0, 4);

                    ((InterfaceImage)_heartImage[i][j]).SourceRectangle = new Rectangle(
                        _heartSprite.ScaledRectangle.X + (_heartSprite.ScaledRectangle.Width + _heartSprite.TextureScale) * state,
                        _heartSprite.ScaledRectangle.Y,
                        _heartSprite.ScaledRectangle.Width, _heartSprite.ScaledRectangle.Height);
                }
            }
        }
    }
}
