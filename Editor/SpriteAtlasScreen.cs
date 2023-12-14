using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.Base;
using ProjectZ.Base.UI;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;
using Keys = Microsoft.Xna.Framework.Input.Keys;
#if WINDOWS
using System.Windows.Forms;
#endif

namespace ProjectZ.Editor
{
    internal class SpriteAtlasScreen : InGame.Screens.Screen
    {
        private readonly EditorCamera _camera = new EditorCamera();

        private readonly SpriteAtlasSerialization.SpriteAtlas _spriteAtlas = new SpriteAtlasSerialization.SpriteAtlas();
        private List<SpriteAtlasSerialization.AtlasEntry> _sourceData => _spriteAtlas.Data;
        private UiEditList<SpriteAtlasSerialization.AtlasEntry> _spriteAtlasList;

        private int _spriteIndex;
        private SpriteAtlasSerialization.AtlasEntry _selectedEntry;

        private List<int> _moveEntries = new List<int>();

        private Texture2D _sprTexture;
        private Color[] _colorData;

        private Texture2D _sprSelectionTexture;
        private Color[] _colorDataSelection;

        private Point _lastPosition;
        private Point _currentPosition;
        private Point _selectionStart;
        private Point _selectionEnd;

        private UiNumberInput _atlasScale;
        private UiNumberInput _inputSourceX;
        private UiNumberInput _inputSourceY;
        private UiNumberInput _inputSourceWidth;
        private UiNumberInput _inputSourceHeight;
        private UiNumberInput _inputSourceOriginX;
        private UiNumberInput _inputSourceOriginY;
        private UiTextInput _inputEntryName;

        private string _lastFileName;

        private const int LeftBarWidth = 200;
        private const int RightBarWidth = 250;
        private const int TileSize = 2;

        private bool _selecting;
        private bool _imageWasEdited;

        public SpriteAtlasScreen(string screenId) : base(screenId) { }

        public override void Load(ContentManager content)
        {
            var buttonDist = 5;
            var buttonWidth = LeftBarWidth - buttonDist * 2;
            var buttonWidthHalf = (LeftBarWidth - buttonDist * 3) / 2;
            var buttonHeight = 30;
            var labelHeight = Resources.EditorFontHeight;
            var buttonQWidth = LeftBarWidth - 15 - buttonHeight;
            var posY = Values.ToolBarHeight + buttonDist;

            var screenId = Values.EditorUiSpriteAtlas;

            Game1.EditorUi.AddElement(new UiRectangle(new Rectangle(0, 0, 0, 0), "leftBar", screenId, Values.ColorBackgroundLight, Color.White,
                ui => { ui.Rectangle = new Rectangle(0, Values.ToolBarHeight, LeftBarWidth, Game1.WindowHeight - Values.ToolBarHeight); }));

            Game1.EditorUi.AddElement(new UiRectangle(new Rectangle(0, 0, 0, 0), "rightBar", screenId, Values.ColorBackgroundLight, Color.White,
                ui => { ui.Rectangle = new Rectangle(Game1.WindowWidth - RightBarWidth, Values.ToolBarHeight, RightBarWidth, Game1.WindowHeight - Values.ToolBarHeight); }));

            posY = Values.ToolBarHeight + buttonDist;

            // load button
            Game1.EditorUi.AddElement(new UiButton(new Rectangle(5, posY, buttonWidth, buttonHeight), Resources.EditorFont,
                "load", "bt1", screenId, null, ui => { LoadSprite(); }));
            // save button
            Game1.EditorUi.AddElement(new UiButton(new Rectangle(5, posY += buttonHeight + buttonDist, buttonWidth, buttonHeight), Resources.EditorFont,
                "save as...", "bt1", screenId, null, ui => { SaveSpriteAtlasDialog(); }));
            Game1.EditorUi.AddElement(new UiButton(new Rectangle(5, posY += buttonHeight + buttonDist, buttonWidth, buttonHeight), Resources.EditorFont,
                "save...", "bt1", screenId, null, ui => { SaveSpriteAtlas(_lastFileName); }));

            Game1.EditorUi.AddElement(_atlasScale = new UiNumberInput(new Rectangle(5, posY += buttonHeight + buttonDist, buttonWidth, buttonHeight),
                Resources.EditorFont, 0, 1, 16, 1, "Scaling", screenId, null,
                ui =>
                {
                    _spriteAtlas.Scale = (int)((UiNumberInput)ui).Value;
                }));

            Game1.EditorUi.AddElement(new UiLabel(new Rectangle(buttonDist, posY += buttonHeight + buttonDist * 3, buttonWidth, labelHeight),
                Resources.EditorFont, "Source Rectangle", "sourceHeader", screenId, null));

            var minValue = 0;
            var maxValue = 10000;

            _inputSourceX = new UiNumberInput(new Rectangle(buttonDist, posY += labelHeight + buttonDist, buttonWidthHalf, buttonHeight),
                Resources.EditorFont, 0, minValue, maxValue, 1, "sourceX", screenId, null,
                ui =>
                {
                    FixSelectedPart();
                    _sourceData[_spriteIndex].SourceRectangle.X = (int)((UiNumberInput)ui).Value;
                });
            _inputSourceY = new UiNumberInput(new Rectangle(buttonDist * 2 + buttonWidthHalf, posY, buttonWidthHalf, buttonHeight),
                Resources.EditorFont, 0, minValue, maxValue, 1, "sourceY", screenId, null,
                ui =>
                {
                    FixSelectedPart();
                    _sourceData[_spriteIndex].SourceRectangle.Y = (int)((UiNumberInput)ui).Value;
                });
            _inputSourceWidth = new UiNumberInput(new Rectangle(buttonDist, posY += buttonHeight + buttonDist, buttonWidthHalf, buttonHeight),
                Resources.EditorFont, 0, minValue, maxValue, 1, "sourceWidth", screenId, null, ui =>
                {
                    FixSelectedPart();
                    _sourceData[_spriteIndex].SourceRectangle.Width = (int)((UiNumberInput)ui).Value;
                });
            _inputSourceHeight = new UiNumberInput(new Rectangle(buttonDist * 2 + buttonWidthHalf, posY, buttonWidthHalf, buttonHeight),
                Resources.EditorFont, 0, minValue, maxValue, 1, "sourceHeight", screenId, null, ui =>
                {
                    FixSelectedPart();
                    _sourceData[_spriteIndex].SourceRectangle.Height = (int)((UiNumberInput)ui).Value;
                });

            Game1.EditorUi.AddElement(new UiLabel(new Rectangle(buttonDist, posY += buttonHeight + buttonDist * 3, buttonWidth, labelHeight),
                Resources.EditorFont, "Origin", "originLabel", screenId, null));

            Game1.EditorUi.AddElement(_inputSourceOriginX = new UiNumberInput(new Rectangle(buttonDist, posY += labelHeight + buttonDist, buttonWidthHalf, buttonHeight),
                Resources.EditorFont, 0, minValue, maxValue, 1, "originX", screenId, null,
                ui =>
                {
                    _sourceData[_spriteIndex].Origin.X = (int)((UiNumberInput)ui).Value;
                }));
            Game1.EditorUi.AddElement(_inputSourceOriginY = new UiNumberInput(new Rectangle(buttonDist * 2 + buttonWidthHalf, posY, buttonWidthHalf, buttonHeight),
                Resources.EditorFont, 0, minValue, maxValue, 1, "originY", screenId, null,
                ui =>
                {
                    _sourceData[_spriteIndex].Origin.Y = (int)((UiNumberInput)ui).Value;
                }));

            Game1.EditorUi.AddElement(_inputSourceX);
            Game1.EditorUi.AddElement(_inputSourceY);
            Game1.EditorUi.AddElement(_inputSourceWidth);
            Game1.EditorUi.AddElement(_inputSourceHeight);

            var buttonWidthRight = RightBarWidth - buttonDist * 2;
            var buttonWidthHalfRight = (RightBarWidth - buttonDist * 3) / 2;
            var buttonHeightRight = 25;

            posY = Values.ToolBarHeight + buttonDist;
            var inputPosY = posY;
            _inputEntryName = new UiTextInput(new Rectangle(0, 0, buttonWidthRight, 35), Resources.EditorFontMonoSpace, 32, "inputSpriteName", screenId,
                element => element.Rectangle = new Rectangle(Game1.WindowWidth - RightBarWidth + buttonDist, inputPosY, buttonWidthRight, 45),
                element =>
                {
                    if (_sourceData.Count > _spriteIndex)
                        _sourceData[_spriteIndex].EntryId = ((UiTextInput)element).StrValue;
                });
            Game1.EditorUi.AddElement(_inputEntryName);

            posY += 45 + buttonDist;
            var buttonPosY0 = posY;
            Game1.EditorUi.AddElement(new UiButton(Rectangle.Empty, Resources.EditorFont, "add", "bt1", screenId,
                element => element.Rectangle = new Rectangle(Game1.WindowWidth - RightBarWidth + buttonDist, buttonPosY0, buttonWidthHalfRight, buttonHeight),
                ui => { AddAtlasEntry(); }));

            var buttonPosY1 = posY;
            Game1.EditorUi.AddElement(new UiButton(Rectangle.Empty, Resources.EditorFont, "remove", "bt2", screenId,
                element => element.Rectangle = new Rectangle(Game1.WindowWidth - RightBarWidth + buttonDist * 2 + buttonWidthHalfRight, buttonPosY1, buttonWidthHalfRight, buttonHeight),
                ui => { RemoveAtlasEntry(); }));

            posY += buttonHeight + buttonDist;
            var buttonPosY2 = posY;
            Game1.EditorUi.AddElement(new UiButton(Rectangle.Empty, Resources.EditorFont, "sort", "bt3", screenId,
                element => element.Rectangle = new Rectangle(Game1.WindowWidth - RightBarWidth + buttonDist, buttonPosY2, buttonWidthRight, buttonHeight),
                ui => { SortAtlasEntry(); }));

            posY += buttonHeight + buttonDist;
            Game1.EditorUi.AddElement(_spriteAtlasList = new UiEditList<SpriteAtlasSerialization.AtlasEntry>(
                Rectangle.Empty, Resources.EditorFontSmallMonoSpace, _sourceData, "bt4", screenId, element =>
                {
                    element.Rectangle = new Rectangle(Game1.WindowWidth - RightBarWidth, posY, RightBarWidth, Game1.WindowHeight - posY);
                    var selectedEntry = ((UiEditList<SpriteAtlasSerialization.AtlasEntry>)element).SelectedEntry;
                    if (selectedEntry != _spriteIndex)
                    {
                        // do not fix anything if just the order of the list was changed
                        if (_sourceData[selectedEntry] != _selectedEntry)
                            FixSelectedPart();

                        _spriteIndex = selectedEntry;
                        _selectedEntry = _sourceData[_spriteIndex];

                        UpdateInputUi();
                    }
                }));
        }

        public override void Update(GameTime gameTime)
        {
            Game1.EditorUi.CurrentScreen = Values.EditorUiSpriteAtlas;

            if (_sprTexture == null)
                return;

            // update the camera
            var mousePosition = InputHandler.MousePosition();

            if (InputHandler.MouseIntersect(new Rectangle(LeftBarWidth, Values.ToolBarHeight,
                Game1.WindowWidth - LeftBarWidth - RightBarWidth, Game1.WindowHeight - Values.ToolBarHeight)))
            {
                _currentPosition = new Point(
                    (int)((InputHandler.MousePosition().X - _camera.Location.X) / _camera.Scale),
                    (int)((InputHandler.MousePosition().Y - _camera.Location.Y) / _camera.Scale));

                if (InputHandler.MouseLeftStart())
                {
                    FixSelectedPart();

                    if (InputHandler.KeyDown(Keys.LeftShift))
                    {
                        SelectSprite(_currentPosition);
                    }
                    else
                    {
                        _selecting = true;
                        _selectionStart = _currentPosition;
                    }
                }
                if (InputHandler.MouseLeftDown() && _selecting)
                {
                    _selectionEnd = _currentPosition;

                    var selectionStart = new Point(Math.Min(_selectionStart.X, _selectionEnd.X), Math.Min(_selectionStart.Y, _selectionEnd.Y));
                    var selectionEnd = new Point(Math.Max(_selectionStart.X, _selectionEnd.X) + 1, Math.Max(_selectionStart.Y, _selectionEnd.Y) + 1);

                    _sourceData[_spriteIndex].SourceRectangle.X = selectionStart.X;
                    _sourceData[_spriteIndex].SourceRectangle.Y = selectionStart.Y;
                    _sourceData[_spriteIndex].SourceRectangle.Width = selectionEnd.X - selectionStart.X;
                    _sourceData[_spriteIndex].SourceRectangle.Height = selectionEnd.Y - selectionStart.Y;

                    UpdateInputUi();
                }
                if (InputHandler.MouseLeftReleased())
                {
                    _selecting = false;
                }

                if (InputHandler.MouseRightStart() && _sprSelectionTexture == null)
                {
                    var selectionSource = _sourceData[_spriteIndex].SourceRectangle;

                    if (selectionSource.Width > 0 && selectionSource.Height > 0)
                    {
                        _colorDataSelection = new Color[selectionSource.Width * selectionSource.Height];

                        // fill the color data array
                        for (var y = 0; y < selectionSource.Height; y++)
                            for (var x = 0; x < selectionSource.Width; x++)
                            {
                                var originX = selectionSource.X + x;
                                var originY = selectionSource.Y + y;

                                if (0 <= originX && originX < _sprTexture.Width &&
                                    0 <= originY && originY < _sprTexture.Height)
                                {
                                    _colorDataSelection[x + y * selectionSource.Width] =
                                        _colorData[originX + originY * _sprTexture.Width];

                                    // remove the data from the base texture
                                    _colorData[originX + originY * _sprTexture.Width] = Color.Transparent;
                                }
                                else
                                    _colorDataSelection[x + y * selectionSource.Width] = Color.Transparent;
                            }

                        _sprSelectionTexture = new Texture2D(Game1.Graphics.GraphicsDevice, selectionSource.Width, selectionSource.Height);
                        _sprSelectionTexture.SetData(_colorDataSelection);

                        _sprTexture.SetData(_colorData);

                        // get a list of entries that will be moved
                        _moveEntries.Clear();
                        for (var i = 0; i < _sourceData.Count; i++)
                        {
                            if (i != _spriteIndex &&
                                _sourceData[_spriteIndex].SourceRectangle.Contains(_sourceData[i].SourceRectangle))
                            {
                                _moveEntries.Add(i);
                            }
                        }
                    }
                }
                if (InputHandler.MouseRightDown())
                {
                    var offset = new Point(_currentPosition.X - _lastPosition.X, _currentPosition.Y - _lastPosition.Y);
                    MoveSelection(offset);
                }

                if (InputHandler.MouseWheelUp())
                    _camera.Zoom(1, mousePosition);
                if (InputHandler.MouseWheelDown())
                    _camera.Zoom(-1, mousePosition);
            }

            if (!InputHandler.MouseMiddleStart() && InputHandler.MouseMiddleDown())
                _camera.Location += mousePosition - InputHandler.LastMousePosition();

            _lastPosition = _currentPosition;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (_sprTexture == null)
                return;

            spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointWrap, null, null, null, _camera.TransformMatrix);

            // draw the tiled background
            spriteBatch.Draw(Resources.SprTiledBlock, new Rectangle(0, 0, _sprTexture.Width, _sprTexture.Height),
                new Rectangle(0, 0, (int)(_sprTexture.Width / (float)TileSize * 2), (int)(_sprTexture.Height / (float)TileSize * 2)), Color.White);

            // draw the sprite
            spriteBatch.Draw(_sprTexture, Vector2.Zero, Color.White);

            // draw the sprite selection
            if (_sprSelectionTexture != null)
                spriteBatch.Draw(_sprSelectionTexture,
                    new Vector2(_sourceData[_spriteIndex].SourceRectangle.X,
                        _sourceData[_spriteIndex].SourceRectangle.Y), Color.White);

            // draw current position of the mouse
            spriteBatch.Draw(Resources.SprWhite,
                new Rectangle(_currentPosition.X, _currentPosition.Y, 1, 1), Color.Red * 0.5f);

            for (var i = 0; i < _sourceData.Count; i++)
            {
                spriteBatch.Draw(Resources.SprWhite, new Rectangle(
                    _sourceData[i].SourceRectangle.X, _sourceData[i].SourceRectangle.Y,
                    _sourceData[i].SourceRectangle.Width, _sourceData[i].SourceRectangle.Height),
                    _spriteIndex == i ? Color.Red * (0.5f + MathF.Sin((float)Game1.TotalTime / 100) * 0.125f) : Color.Red * 0.25f);
            }

            spriteBatch.End();
            spriteBatch.Begin();

            // draw the origin xy axis
            if (0 <= _spriteIndex && _spriteIndex <= _sourceData.Count)
            {
                var originPosition = new Vector2(
                    _camera.Location.X + (_sourceData[_spriteIndex].SourceRectangle.X + _sourceData[_spriteIndex].Origin.X) * _camera.Scale,
                    _camera.Location.Y + (_sourceData[_spriteIndex].SourceRectangle.Y + _sourceData[_spriteIndex].Origin.Y) * _camera.Scale);
                spriteBatch.Draw(Resources.SprWhite, new Vector2(originPosition.X - 1, originPosition.Y - 10), new Rectangle(0, 0, 2, 20), Color.Green);
                spriteBatch.Draw(Resources.SprWhite, new Vector2(originPosition.X - 10, originPosition.Y - 1), new Rectangle(0, 0, 20, 2), Color.Red);
            }

            spriteBatch.End();
        }

        private void MoveSelection(Point offset)
        {
            foreach (var entryIndex in _moveEntries)
            {
                _sourceData[entryIndex].SourceRectangle.X += offset.X;
                _sourceData[entryIndex].SourceRectangle.Y += offset.Y;
            }

            _sourceData[_spriteIndex].SourceRectangle.X += offset.X;
            _sourceData[_spriteIndex].SourceRectangle.Y += offset.Y;
        }

        private void FixSelectedPart()
        {
            if (_sprSelectionTexture == null)
                return;

            _imageWasEdited = true;

            var selectionSource = _sourceData[_spriteIndex].SourceRectangle;

            // need to resize the texture?
            var offset = Point.Zero;
            if (selectionSource.X < 0)
                offset.X = -selectionSource.X;
            if (selectionSource.Y < 0)
                offset.Y = -selectionSource.Y;

            var newSizeX = Math.Max(offset.X + selectionSource.Right, offset.X + _sprTexture.Width);
            var newSizeY = Math.Max(offset.Y + selectionSource.Bottom, offset.Y + _sprTexture.Height);

            // clamp the max texture size
            var maxSize = 4096;
            if (newSizeX > maxSize)
            {
                if (offset.X > 0)
                    offset.X = Math.Clamp(offset.X, 0, maxSize - _sprTexture.Width);
                newSizeX = maxSize;
            }
            if (newSizeY > maxSize)
            {
                if (offset.Y > 0)
                    offset.X = Math.Clamp(offset.Y, 0, maxSize - _sprTexture.Height);
                newSizeY = maxSize;
            }

            if (newSizeX != _sprTexture.Width || newSizeY != _sprTexture.Height)
            {
                var newColorData = new Color[newSizeX * newSizeY];
                for (var y = 0; y < _sprTexture.Height; y++)
                    for (var x = 0; x < _sprTexture.Width; x++)
                    {
                        newColorData[(x + offset.X) + (y + offset.Y) * newSizeX] = _colorData[x + y * _sprTexture.Width];
                    }

                _colorData = newColorData;
                _sprTexture = new Texture2D(Game1.Graphics.GraphicsDevice, newSizeX, newSizeY);
                _sprTexture.SetData(_colorData);

                _currentPosition += offset;
                _camera.Location -= new Point((int)(offset.X * _camera.Scale), (int)(offset.Y * _camera.Scale));
            }

            // fill the color data array
            for (var y = 0; y < _sprSelectionTexture.Height; y++)
                for (var x = 0; x < _sprSelectionTexture.Width; x++)
                {
                    var originX = selectionSource.X + x + offset.X;
                    var originY = selectionSource.Y + y + offset.Y;

                    if (0 <= originX && originX < _sprTexture.Width &&
                        0 <= originY && originY < _sprTexture.Height &&
                        _colorDataSelection[x + y * selectionSource.Width] != Color.Transparent)
                        _colorData[originX + originY * _sprTexture.Width] = _colorDataSelection[x + y * selectionSource.Width];
                }
            _sprTexture.SetData(_colorData);

            _sprSelectionTexture = null;

            // move the source data if the texture gets expanded on the left or top
            foreach (var source in _sourceData)
            {
                source.SourceRectangle.X += offset.X;
                source.SourceRectangle.Y += offset.Y;
            }
        }

        private void SelectSprite(Point position)
        {
            for (var i = 0; i < _sourceData.Count; i++)
                if (_sourceData[i].SourceRectangle.Contains(position))
                {
                    _spriteIndex = i;
                    _selectedEntry = _sourceData[_spriteIndex];
                    _spriteAtlasList.SelectedEntry = _spriteIndex;
                    UpdateInputUi();
                    return;
                }
        }

        private void UpdateInputUi()
        {
            _inputSourceX.Value = _sourceData[_spriteIndex].SourceRectangle.X;
            _inputSourceY.Value = _sourceData[_spriteIndex].SourceRectangle.Y;
            _inputSourceWidth.Value = _sourceData[_spriteIndex].SourceRectangle.Width;
            _inputSourceHeight.Value = _sourceData[_spriteIndex].SourceRectangle.Height;
            _inputSourceOriginX.Value = _sourceData[_spriteIndex].Origin.X;
            _inputSourceOriginY.Value = _sourceData[_spriteIndex].Origin.Y;
            _inputEntryName.StrValue = _sourceData[_spriteIndex].EntryId;
        }

        private void AddAtlasEntry()
        {
            _sourceData.Add(new SpriteAtlasSerialization.AtlasEntry { EntryId = "" });
        }

        private void RemoveAtlasEntry()
        {
            if (_spriteIndex >= 0 && _sourceData.Count > _spriteIndex)
                _sourceData.RemoveAt(_spriteIndex);

            // move the selection up if we are at the bottom
            if (_spriteIndex >= _sourceData.Count)
            {
                _spriteIndex--;
                _spriteAtlasList.SelectedEntry = _spriteIndex;
            }
        }

        private void SortAtlasEntry()
        {
            _sourceData.Sort((x, y) => x.EntryId.CompareTo(y.EntryId));
        }

        private void LoadSprite()
        {
#if WINDOWS
            var openFileDialog = new OpenFileDialog()
            {
                Filter = "sprite file (*.png)|*.png"
            };

            if (openFileDialog.ShowDialog() != DialogResult.OK)
                return;

            LoadSpriteEditor(openFileDialog.FileName);
#endif
        }

        public void LoadSpriteEditor(string filePath)
        {
            _imageWasEdited = false;

            // load the sprite
            using var stream = File.OpenRead(filePath);
            _sprTexture = Texture2D.FromStream(Game1.Graphics.GraphicsDevice, stream);
            _lastFileName = filePath;

            _colorData = new Color[_sprTexture.Width * _sprTexture.Height];
            _sprTexture.GetData(_colorData);

            _spriteAtlas.Scale = 1;
            _sourceData.Clear();

            // load the sprite atlas if there is one
            var atlasFileName = _lastFileName.Replace(".png", ".atlas");
            if (!SpriteAtlasSerialization.LoadSpriteAtlas(atlasFileName, _spriteAtlas))
                AddAtlasEntry();

            // need to be scaled to actually have the source rectangle
            foreach (var entry in _spriteAtlas.Data)
            {
                entry.SourceRectangle.X *= _spriteAtlas.Scale;
                entry.SourceRectangle.Y *= _spriteAtlas.Scale;
                entry.SourceRectangle.Width *= _spriteAtlas.Scale;
                entry.SourceRectangle.Height *= _spriteAtlas.Scale;
                entry.Origin.X *= _spriteAtlas.Scale;
                entry.Origin.Y *= _spriteAtlas.Scale;
            }

            _atlasScale.Value = _spriteAtlas.Scale;

            _spriteIndex = 0;
            _selectedEntry = _sourceData[_spriteIndex];
            UpdateInputUi();
        }

        private void SaveSpriteAtlasDialog()
        {
#if WINDOWS
            var saveFileDialog = new SaveFileDialog()
            {
                RestoreDirectory = true,
                Filter = "sprite file (*.png)|*.png",
            };

            if (_lastFileName != null)
            {
                saveFileDialog.FileName = Path.GetFileName(_lastFileName);
                saveFileDialog.InitialDirectory = Path.GetFullPath(Path.GetDirectoryName(_lastFileName));
            }

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
                SaveSpriteAtlas(saveFileDialog.FileName);
#endif
        }

        private void SaveSpriteAtlas(string filePath)
        {
            FixSelectedPart();

            // save the texture?
            if (_imageWasEdited)
            {
                using Stream stream = File.Create(filePath);
                _sprTexture.SaveAsPng(stream, _sprTexture.Width, _sprTexture.Height);
            }

            // save the sprite atlas
            var atlasFileName = filePath.Replace(".png", ".atlas");
            SpriteAtlasSerialization.SaveSpriteAtlas(atlasFileName, _spriteAtlas);
        }
    }
}
