using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.Controls;
using ProjectZ.InGame.Interface;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.Pages
{
    class CopyPage : InterfacePage
    {
        private InterfaceListLayout[] _fromSaveSlot = new InterfaceListLayout[SaveStateManager.SaveCount];
        private InterfaceLabel[] _saveFromNames = new InterfaceLabel[SaveStateManager.SaveCount];

        private InterfaceListLayout[] _toSaveSlot = new InterfaceListLayout[SaveStateManager.SaveCount];
        private InterfaceLabel[] _saveToNames = new InterfaceLabel[SaveStateManager.SaveCount];

        private int _sourceSlotIndex;
        private int _targetSlotIndex;

        public CopyPage(int width, int height)
        {
            var saveButtonRec = new Point(120, 22);

            // main layout
            var _mainLayout = new InterfaceListLayout { Size = new Point(width, height), Gravity = InterfaceElement.Gravities.Left, Selectable = true };
            _mainLayout.AddElement(new InterfaceLabel(Resources.GameHeaderFont, "main_menu_copy_header", new Point(width, (int)(height * Values.MenuHeaderSize)), new Point(0, 0)));

            var fromToLayout = new InterfaceListLayout() { Size = new Point(width, (int)(height * Values.MenuContentSize)), HorizontalMode = true, Selectable = true };

            // list of all save files
            {
                var fromSaveSlot = new InterfaceListLayout() { Size = new Point(saveButtonRec.X, (int)(height * Values.MenuContentSize)) };
                fromSaveSlot.AddElement(new InterfaceLabel("main_menu_copy_from"));
                for (var i = 0; i < SaveStateManager.SaveCount; i++)
                {
                    var _saveButtonLayouts = new InterfaceGravityLayout { Size = new Point(saveButtonRec.X, saveButtonRec.Y) };

                    var numberWidth = 17;
                    var saveSlotNumber = new InterfaceLabel(null, new Point(numberWidth, 12), Point.Zero)
                    { Gravity = InterfaceElement.Gravities.Left, TextAlignment = InterfaceElement.Gravities.Center | InterfaceElement.Gravities.Bottom };
                    saveSlotNumber.SetText((i + 1).ToString());

                    _saveButtonLayouts.AddElement(saveSlotNumber);

                    var saveInfoLayout = new InterfaceListLayout { HorizontalMode = true, Size = new Point(saveButtonRec.X - numberWidth, saveButtonRec.Y), Gravity = InterfaceElement.Gravities.Right };

                    // name on the right
                    {
                        var rightWidth = saveButtonRec.X - numberWidth;
                        var middle = new InterfaceListLayout { Gravity = InterfaceElement.Gravities.Left, Margin = new Point(2, 0), Size = new Point(rightWidth, 30) };

                        // name
                        middle.AddElement(_saveFromNames[i] = new InterfaceLabel(null, new Point(rightWidth - 3, 12), Point.Zero) { Margin = new Point(1, 0), TextAlignment = InterfaceElement.Gravities.Left | InterfaceElement.Gravities.Bottom });

                        saveInfoLayout.AddElement(middle);
                    }

                    _saveButtonLayouts.AddElement(saveInfoLayout);

                    var _saveButtons = new InterfaceButton
                    {
                        InsideElement = _saveButtonLayouts,
                        Size = new Point(saveButtonRec.X, saveButtonRec.Y),
                        Margin = new Point(0, 2)
                    };

                    _fromSaveSlot[i] = new InterfaceListLayout { HorizontalMode = true, Gravity = InterfaceElement.Gravities.Right, AutoSize = true };

                    // save file
                    _fromSaveSlot[i].AddElement(_saveButtons);

                    // add save button to the main layout
                    fromSaveSlot.AddElement(_fromSaveSlot[i]);
                }
                fromToLayout.AddElement(fromSaveSlot);

                fromToLayout.AddElement(new InterfaceLabel("main_menu_copy_arrow") { Size = new Point(6, 7), Margin = new Point(10, 10) });

                var toSaveSlot = new InterfaceListLayout() { Size = new Point(saveButtonRec.X, (int)(height * Values.MenuContentSize)), Selectable = true };
                toSaveSlot.AddElement(new InterfaceLabel("main_menu_copy_to"));
                for (var i = 0; i < SaveStateManager.SaveCount; i++)
                {
                    var _saveButtonLayouts = new InterfaceGravityLayout { Size = new Point(saveButtonRec.X, saveButtonRec.Y) };

                    var numberWidth = 17;
                    var saveSlotNumber = new InterfaceLabel(null, new Point(numberWidth, 12), Point.Zero)
                    { Gravity = InterfaceElement.Gravities.Left, TextAlignment = InterfaceElement.Gravities.Center | InterfaceElement.Gravities.Bottom };
                    saveSlotNumber.SetText((i + 1).ToString());

                    _saveButtonLayouts.AddElement(saveSlotNumber);

                    var saveInfoLayout = new InterfaceListLayout { HorizontalMode = true, Size = new Point(saveButtonRec.X - numberWidth, saveButtonRec.Y), Gravity = InterfaceElement.Gravities.Right };

                    // name on the right
                    {
                        var rightWidth = saveButtonRec.X - numberWidth;
                        var middle = new InterfaceListLayout { Gravity = InterfaceElement.Gravities.Left, Margin = new Point(2, 0), Size = new Point(rightWidth, 30) };

                        // name
                        middle.AddElement(_saveToNames[i] = new InterfaceLabel(null, new Point(rightWidth - 3, 12), Point.Zero) { Margin = new Point(1, 0), TextAlignment = InterfaceElement.Gravities.Left | InterfaceElement.Gravities.Bottom });

                        saveInfoLayout.AddElement(middle);
                    }

                    _saveButtonLayouts.AddElement(saveInfoLayout);

                    var slotIndex = i;
                    var _saveButtons = new InterfaceButton
                    {
                        InsideElement = _saveButtonLayouts,
                        Size = new Point(saveButtonRec.X, saveButtonRec.Y),
                        Margin = new Point(0, 2),
                        ClickFunction = (InterfaceElement element) => OnSelectSave(slotIndex)
                    };

                    _toSaveSlot[i] = new InterfaceListLayout { HorizontalMode = true, Gravity = InterfaceElement.Gravities.Right, AutoSize = true, Selectable = true };

                    // save file
                    _toSaveSlot[i].AddElement(_saveButtons);

                    // add save button to the main layout
                    toSaveSlot.AddElement(_toSaveSlot[i]);
                }
                fromToLayout.AddElement(toSaveSlot);

                _mainLayout.AddElement(fromToLayout);
            }

            // menu bottom bar
            {
                var menuBottomBar = new InterfaceListLayout
                {
                    Size = new Point(saveButtonRec.X, (int)(height * Values.MenuFooterSize)),
                    HorizontalMode = true,
                    Selectable = true
                };

                // back button
                menuBottomBar.AddElement(new InterfaceButton(new Point(60, 20), new Point(2, 4), "main_menu_copy_back", element => Abort()));

                _mainLayout.AddElement(menuBottomBar);
            }

            PageLayout = _mainLayout;
        }

        public override void OnLoad(Dictionary<string, object> intent)
        {
            if (intent != null && intent.TryGetValue("selectedSlot", out var selectedSlot))
            {
                _sourceSlotIndex = (int)selectedSlot;
            }

            UpdateUi();

            PageLayout.Deselect(false);
            PageLayout.Select(InterfaceElement.Directions.Top, false);
        }

        public override void OnReturn(Dictionary<string, object> intent)
        {
            base.OnReturn(intent);

            // copy file?
            if (intent != null && intent.TryGetValue("copyFile", out var copyFile) && (bool)copyFile)
            {
                var popIntent = new Dictionary<string, object>();
                popIntent.Add("copyTargetSlot", _targetSlotIndex);

                Game1.UiPageManager.PopPage(popIntent, PageManager.TransitionAnimation.Fade, PageManager.TransitionAnimation.Fade);
            }
        }

        public override void Update(CButtons pressedButtons, GameTime gameTime)
        {
            base.Update(pressedButtons, gameTime);

            if (ControlHandler.ButtonPressed(CButtons.B))
                Abort();
        }

        private void Abort()
        {
            Game1.UiPageManager.PopPage(null, PageManager.TransitionAnimation.Fade, PageManager.TransitionAnimation.Fade);
        }

        private void OnSelectSave(int slotIndex)
        {
            _targetSlotIndex = slotIndex;

            Game1.UiPageManager.ChangePage(typeof(CopyConfirmationPage), null, PageManager.TransitionAnimation.Fade, PageManager.TransitionAnimation.Fade);
        }

        private void UpdateUi()
        {
            for (var i = 0; i < SaveStateManager.SaveCount; i++)
            {
                var slotName = SaveStateManager.SaveStates[i] == null ?
                    Game1.LanguageManager.GetString("main_menu_copy_empty", "error") : SaveStateManager.SaveStates[i].Name;

                // only show the selected slot
                if (i == _sourceSlotIndex)
                {
                    _fromSaveSlot[i].Select(InterfaceElement.Directions.Left, false);
                    _saveFromNames[i].SetText(slotName);
                }
                _fromSaveSlot[i].Visible = i == _sourceSlotIndex;

                _toSaveSlot[i].Visible = i != _sourceSlotIndex;
                _saveToNames[i].SetText(slotName);
            }
        }
    }
}
