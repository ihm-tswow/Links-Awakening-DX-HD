using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ProjectZ.Base;
using ProjectZ.Base.UI;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.Editor
{
    class TileEditorScreen
    {
        public Map Map;
        private TileMap TileMap => Map.TileMap;

        public Point Selection;
        public Point SelectionStart;
        public Point SelectionEnd;

        public bool DrawMode;
        public bool Drawing;
        public bool MarkSelectedTiles;
        public bool MultiSelect;
        public bool IsSelecting;

        public bool[] LayerVisibility = { true, true, false };

        private readonly EditorCamera _camera;
        private readonly TileSelectionScreen _tileSelectionScreen = new TileSelectionScreen();

        private Point _mousePosition;

        private int _replaceSelections;
        private int _currentLayer;
        private int _toolBarWidth = 200;

        private bool _removedTile;

        public TileEditorScreen(EditorCamera camera)
        {
            _camera = camera;
        }

        public void Load(ContentManager content)
        {
            _tileSelectionScreen.Load(content);
        }

        public void SetupUi(int posY)
        {
            var buttonWidth = _toolBarWidth - 10;
            var buttonHalfWidth = _toolBarWidth / 2 - 15;
            var buttonHeight = 30;
            var lableHeight = 20;
            var buttonQWidth = buttonWidth - 2 * 5 - buttonHeight * 2;
            var dist = 5;
            var bigDist = 16;

            Game1.EditorUi.AddElement(new UiButton(new Rectangle(5, posY, buttonWidth, buttonHeight),
            Resources.EditorFont, "import tilemap", "bt1", Values.EditorUiTileEditor, null, ui => { SaveLoadMap.ImportTilemap(); }));

            posY += 11;
            for (var i = 0; i < 3; i++)
            {
                var layer = i;
                Game1.EditorUi.AddElement(new UiButton(
                    new Rectangle(5, posY += buttonHeight + dist, buttonQWidth, buttonHeight), Resources.EditorFont,
                    "layer " + layer, "bt1", Values.EditorUiTileEditor,
                    ui => { ((UiButton)ui).Marked = _currentLayer == layer; },
                    ui => { ButtonPressedLayer(layer); }));

                Game1.EditorUi.AddElement(new UiButton(new Rectangle(5 + buttonQWidth + dist, posY, buttonHeight, buttonHeight),
                        Resources.EditorFont, "", "bt1", Values.EditorUiTileEditor, null, ui => ButtonUpdate(ui, layer))
                { ButtonIcon = LayerVisibility[i] ? Resources.EditorEyeOpen : Resources.EditorEyeClosed });

                Game1.EditorUi.AddElement(new UiButton(new Rectangle(5 + buttonQWidth + dist * 2 + buttonHeight, posY, buttonHeight, buttonHeight),
                        Resources.EditorFont, "", "bt1", Values.EditorUiTileEditor, null, ui => RemoveTileContent(layer))
                { ButtonIcon = Resources.EditorIconDelete });
            }

            Game1.EditorUi.AddElement(new UiCheckBox(
                new Rectangle(5, posY += buttonHeight + bigDist, buttonWidth, buttonHeight), Resources.EditorFont,
                "mark selected tiles", "cb", Values.EditorUiTileEditor, false, null,
                ui => { MarkSelectedTiles = ((UiCheckBox)ui).CurrentState; }));

            Game1.EditorUi.AddElement(new UiLabel(new Rectangle(5, posY += buttonHeight + bigDist, buttonHalfWidth, buttonHeight), "from:", Values.EditorUiTileEditor));

            Game1.EditorUi.AddElement(new UiImage(null,
                new Rectangle(10 + buttonHalfWidth, posY, 16 * 2, 16 * 2),
                new Rectangle(0, 0, 16, 16), "from", Values.EditorUiTileEditor, Color.White, UpdateImageFrom));

            Game1.EditorUi.AddElement(new UiButton(new Rectangle(5, posY += buttonHeight + bigDist, buttonWidth, buttonHeight),
                Resources.EditorFont,
                "change tiles", "bt1", Values.EditorUiTileEditor, null, ui => { ReplaceTiles(); }));

            Game1.EditorUi.AddElement(new UiButton(new Rectangle(5, posY += buttonHeight + dist, buttonWidth, buttonHeight),
                Resources.EditorFont,
                "create blur map", "bt1", Values.EditorUiTileEditor, null, ui => { CreateBlurMap(); }));
            Game1.EditorUi.AddElement(new UiButton(new Rectangle(5, posY += buttonHeight + dist, buttonWidth, buttonHeight),
                Resources.EditorFont,
                "create blur sides", "bt1", Values.EditorUiTileEditor, null, ui => { CreateBlurMapSides(); }));
        }

        public void UpdateTileSelection(GameTime gameTime)
        {
            // update tile selection
            _tileSelectionScreen.Update(gameTime);
        }

        public void Update(GameTime gameTime)
        {
            Game1.EditorUi.CurrentScreen = Values.EditorUiTileEditor;

            if (TileMap.ArrayTileMap == null)
                return;

            _removedTile = false;

            _mousePosition = InputHandler.MousePosition();

            // update the tiled and the object cursor
            Selection = GetTiledCursor();

            // select the current tile
            if (InputHandler.KeyPressed(Keys.Space))
            {
                var selection = GetSelection(Selection);
                _tileSelectionScreen.SelectedTiles = new[,] { { selection } };

                _replaceSelections = selection;
            }
            if (InputHandler.KeyDown(Keys.Space))
                IsSelecting = true;

            // draw
            if (!MultiSelect)
                SelectionStart = Selection;

            SelectionEnd = Selection;

            // cursor outside the map? -> resize map
            if (InputHandler.MouseLeftDown() && InsideEditor() && !IsInsideTileMap(Selection))
            {
                ResizeMap();
                Update(gameTime);
                return;
            }

            if (InsideEditor() && !Drawing)
            {
                if (InputHandler.MouseLeftStart())
                {
                    DrawMode = true;
                    Drawing = true;
                }
                else if (InputHandler.MouseRightStart())
                {
                    DrawMode = false;
                    Drawing = true;
                }
            }

            if ((DrawMode && (InputHandler.MouseLeftDown() || InputHandler.MouseLeftReleased()) ||
                 !DrawMode && (InputHandler.MouseRightDown() || InputHandler.MouseRightReleased())) && Drawing)
            {
                if ((InputHandler.KeyDown(Keys.LeftShift) || InputHandler.KeyDown(Keys.Space)) &&
                    (DrawMode && InputHandler.MouseLeftDown() || !DrawMode && InputHandler.MouseRightDown()))
                {
                    MultiSelect = true;
                }
                else
                {
                    // no longer in multiselect
                    if (MultiSelect)
                    {
                        MultiSelect = false;

                        if (IsSelecting)
                            SelectArea();
                        else
                            FillMultiSelection();
                    }
                    else
                    {
                        if (!IsSelecting)
                            FillSelection();
                    }
                }
            }
            else
            {
                IsSelecting = false;
                MultiSelect = false;
                Drawing = false;
            }

            // delete the tiles that are marked
            if (InputHandler.KeyPressed(Keys.Delete) && MarkSelectedTiles)
            {
                if (_tileSelectionScreen.SelectedTiles != null)
                {
                    for (int z = 0; z < TileMap.ArrayTileMap.GetLength(2); z++)
                        for (var y = 0; y < TileMap.ArrayTileMap.GetLength(1); y++)
                            for (var x = 0; x < TileMap.ArrayTileMap.GetLength(0); x++)
                            {
                                if (TileMap.ArrayTileMap[x, y, z] >= 0 &&
                                    TileMap.ArrayTileMap[x, y, z] == _tileSelectionScreen.SelectedTiles[0, 0])
                                    TileMap.ArrayTileMap[x, y, z] = -1;
                            }
                }
            }

            if (_removedTile && CutCorners())
                Update(gameTime);
        }

        public void DrawTileSelection(SpriteBatch spriteBatch)
        {
            // draw the tileset
            _tileSelectionScreen.Draw(spriteBatch, _currentLayer == TileMap.ArrayTileMap.GetLength(2) - 1);
        }

        public void Draw(SpriteBatch spriteBatch, bool drawCursor)
        {
            if (TileMap.ArrayTileMap == null)
                return;

            // draw the visible layers of the map
            for (var z = 0; z < TileMap.ArrayTileMap.GetLength(2); z++)
                if (LayerVisibility[z])
                    DrawLayer(spriteBatch, z);

            // draw the cursor
            // only draw the cursor when the update function was called
            if (drawCursor)
                spriteBatch.Draw(Resources.SprWhite,
                    new Rectangle(Selection.X * Values.TileSize, Selection.Y * Values.TileSize,
                        Values.TileSize, Values.TileSize), Color.Red * 0.75f);

            // draw the selection
            if (MultiSelect)
            {
                var left = Math.Min(SelectionStart.X, SelectionEnd.X);
                var right = Math.Max(SelectionStart.X, SelectionEnd.X);
                var top = Math.Min(SelectionStart.Y, SelectionEnd.Y);
                var down = Math.Max(SelectionStart.Y, SelectionEnd.Y);

                spriteBatch.Draw(Resources.SprWhite,
                    new Rectangle(
                        left * Values.TileSize,
                        top * Values.TileSize,
                        (right - left + 1) * Values.TileSize,
                        (down - top + 1) * Values.TileSize), Color.White * 0.5f);

                // draw the preview
                for (var y = top; y <= down; y++)
                    for (var x = left; x <= right; x++)
                        if (!DrawMode)
                        {
                            spriteBatch.Draw(Resources.SprWhite, new Rectangle(
                                    x * Values.TileSize, y * Values.TileSize,
                                    Values.TileSize, Values.TileSize), Color.Red * 0.5f);
                        }
            }
        }

        private void DrawLayer(SpriteBatch spriteBatch, int layer)
        {
            // only draw the visible tiles
            var startX = Math.Max(0, (int)(-_camera.Location.X / (_camera.Scale * Values.TileSize)));
            var startY = Math.Max(0, (int)(-_camera.Location.Y / (_camera.Scale * Values.TileSize)));
            var endX = Math.Min(TileMap.ArrayTileMap.GetLength(0),
                (int)((Game1.WindowWidth - _camera.Location.X) / (_camera.Scale * Values.TileSize)) + 1);
            var endY = Math.Min(TileMap.ArrayTileMap.GetLength(1),
                (int)((Game1.WindowHeight - _camera.Location.Y) / (_camera.Scale * Values.TileSize)) + 1);

            // draw the tilemap
            for (var y = startY; y < endY; y++)
                for (var x = startX; x < endX; x++)
                    if (TileMap.ArrayTileMap[x, y, layer] >= 0)
                    {
                        var tileset = layer + 1 == TileMap.ArrayTileMap.GetLength(2) ? TileMap.SprTilesetBlur : TileMap.SprTileset;
                        spriteBatch.Draw(tileset,
                            new Rectangle(x * Values.TileSize, y * Values.TileSize, Values.TileSize, Values.TileSize),
                            new Rectangle(
                                TileMap.ArrayTileMap[x, y, layer] % (TileMap.SprTileset.Width / TileMap.TileSize) * TileMap.TileSize,
                                TileMap.ArrayTileMap[x, y, layer] / (TileMap.SprTileset.Width / TileMap.TileSize) * TileMap.TileSize, TileMap.TileSize, TileMap.TileSize), Color.White);

                        if (MarkSelectedTiles && _tileSelectionScreen.SelectedTiles != null &&
                            TileMap.ArrayTileMap[x, y, layer] == _tileSelectionScreen.SelectedTiles[0, 0])
                            spriteBatch.Draw(Resources.SprWhite,
                                new Rectangle(x * Values.TileSize, y * Values.TileSize,
                                    Values.TileSize, Values.TileSize), Color.Red * (float)(Math.Sin(Game1.TotalGameTime / 100f) * 0.25f + 0.5f));
                    }
        }

        public void DrawTop(SpriteBatch spriteBatch)
        {
            // draw the background
            spriteBatch.Draw(Resources.SprWhite, new Rectangle(
                    0, Game1.WindowHeight - _toolBarWidth, _toolBarWidth, _toolBarWidth), Color.White * 0.25f);

            if (_tileSelectionScreen.SelectedTiles == null)
                return;

            var width = _toolBarWidth - 10;
            var height = _toolBarWidth - 10;
            var max = MathHelper.Max(_tileSelectionScreen.SelectedTiles.GetLength(0), _tileSelectionScreen.SelectedTiles.GetLength(1));
            var tileWidth = width / max;
            var tileHeight = height / max;

            var posX = width / 2 - (_tileSelectionScreen.SelectedTiles.GetLength(0) * tileWidth) / 2;
            var posY = width / 2 - (_tileSelectionScreen.SelectedTiles.GetLength(1) * tileHeight) / 2;

            for (var y = 0; y < _tileSelectionScreen.SelectedTiles.GetLength(1); y++)
                for (var x = 0; x < _tileSelectionScreen.SelectedTiles.GetLength(0); x++)
                {
                    var tileset = _currentLayer + 1 == TileMap.ArrayTileMap.GetLength(2) ? TileMap.SprTilesetBlur : TileMap.SprTileset;
                    if (_tileSelectionScreen.SelectedTiles[x, y] >= 0)
                        spriteBatch.Draw(tileset, new Rectangle(
                            5 + posX + x * tileWidth, Game1.WindowHeight - _toolBarWidth + posY + 5 + y * tileHeight, tileWidth, tileHeight), new Rectangle(
                                               _tileSelectionScreen.SelectedTiles[x, y] % (TileMap.SprTileset.Width / Values.TileSize) * Values.TileSize,
                                               _tileSelectionScreen.SelectedTiles[x, y] / (TileMap.SprTileset.Width / Values.TileSize) * Values.TileSize,
                                               Values.TileSize, Values.TileSize), Color.White);
                }
        }

        private void ResizeMap()
        {
            var posX = 0;
            var posY = 0;
            var newWidth = TileMap.ArrayTileMap.GetLength(0);
            var newHeight = TileMap.ArrayTileMap.GetLength(1);

            if (Selection.X < 0)
            {
                posX = -Selection.X;
                newWidth -= Selection.X;
                _camera.Location.X += (int)(Selection.X * _camera.Scale * 16);
            }
            else if (Selection.X >= newWidth)
            {
                newWidth = Selection.X + 1;
            }
            if (Selection.Y < 0)
            {
                posY = -Selection.Y;
                newHeight -= Selection.Y;
                _camera.Location.Y += (int)(Selection.Y * _camera.Scale * 16);
            }
            else if (Selection.Y >= newHeight)
            {
                newHeight = Selection.Y + 1;
            }

            Map.ResizeMap(newWidth, newHeight, posX, posY);
        }

        private bool IsInsideTileMap(Point selection)
        {
            return 0 <= selection.X && selection.X < TileMap.ArrayTileMap.GetLength(0) &&
                   0 <= selection.Y && selection.Y < TileMap.ArrayTileMap.GetLength(1);
        }

        private bool InsideEditor()
        {
            return InputHandler.MouseIntersect(new Rectangle(
                _toolBarWidth, Values.ToolBarHeight,
                Game1.WindowWidth - _toolBarWidth * 2,
                Game1.WindowHeight - Values.ToolBarHeight));
        }

        private int GetSelection(Point position)
        {
            if (IsInsideTileMap(position))
                return TileMap.ArrayTileMap[position.X, position.Y, _currentLayer];

            return -1;
        }

        private Point GetTiledCursor()
        {
            var position = new Point(
                (int)((_mousePosition.X - _camera.Location.X) / (Values.TileSize * _camera.Scale)),
                (int)((_mousePosition.Y - _camera.Location.Y) / (Values.TileSize * _camera.Scale)));

            // fix
            if (_mousePosition.X - _camera.Location.X < 0)
                position.X--;
            if (_mousePosition.Y - _camera.Location.Y < 0)
                position.Y--;

            return position;
        }

        /// <summary>
        ///     replace a tile with a different one
        /// </summary>
        private void ReplaceTiles()
        {
            // return if no tile is select
            if (_tileSelectionScreen.SelectedTiles == null ||
               _tileSelectionScreen.SelectedTiles.GetLength(0) <= 0 ||
               _tileSelectionScreen.SelectedTiles.GetLength(1) <= 0)
                return;

            var toSelection = _tileSelectionScreen.SelectedTiles[0, 0];

            for (var z = 0; z < TileMap.ArrayTileMap.GetLength(2); z++)
                for (var y = 0; y < TileMap.ArrayTileMap.GetLength(1); y++)
                    for (var x = 0; x < TileMap.ArrayTileMap.GetLength(0); x++)
                        if (TileMap.ArrayTileMap[x, y, z] == _replaceSelections)
                            TileMap.ArrayTileMap[x, y, z] = toSelection;
        }

        private void CreateBlurMap()
        {
            for (var y = 0; y < TileMap.ArrayTileMap.GetLength(1); y++)
                for (var x = 0; x < TileMap.ArrayTileMap.GetLength(0); x++)
                {
                    if (TileMap.ArrayTileMap[x, y, 0] == -1 && (
                        TileNotEmpty(x - 1, y, 0) || TileNotEmpty(x + 1, y, 0) || TileNotEmpty(x, y - 1, 0) || TileNotEmpty(x, y + 1, 0) ||
                        TileNotEmpty(x - 1, y - 1, 0) || TileNotEmpty(x + 1, y - 1, 0) || TileNotEmpty(x - 1, y + 1, 0) || TileNotEmpty(x + 1, y + 1, 0)))
                    {
                        TileMap.ArrayTileMap[x, y, TileMap.ArrayTileMap.GetLength(2) - 1] = 0;
                    }
                }
        }

        private void CreateBlurMapSides()
        {
            for (var y = 0; y < TileMap.ArrayTileMap.GetLength(1); y++)
                for (var x = 0; x < TileMap.ArrayTileMap.GetLength(0); x++)
                {
                    if (TileMap.ArrayTileMap[x, y, 0] == -1)
                        continue;

                    if (!TileNotEmpty(x - 1, y, 0) && !TileNotEmpty(x, y - 1, 0) && TileNotEmpty(x + 1, y, 0) && TileNotEmpty(x, y + 1, 0))
                        TileMap.ArrayTileMap[x, y, TileMap.ArrayTileMap.GetLength(2) - 1] = 3;
                    if (!TileNotEmpty(x - 1, y, 0) && TileNotEmpty(x, y - 1, 0) && TileNotEmpty(x + 1, y, 0) && !TileNotEmpty(x, y + 1, 0))
                        TileMap.ArrayTileMap[x, y, TileMap.ArrayTileMap.GetLength(2) - 1] = 1;
                    if (TileNotEmpty(x - 1, y, 0) && !TileNotEmpty(x, y - 1, 0) && !TileNotEmpty(x + 1, y, 0) && TileNotEmpty(x, y + 1, 0))
                        TileMap.ArrayTileMap[x, y, TileMap.ArrayTileMap.GetLength(2) - 1] = 4;
                    if (TileNotEmpty(x - 1, y, 0) && TileNotEmpty(x, y - 1, 0) && !TileNotEmpty(x + 1, y, 0) && !TileNotEmpty(x, y + 1, 0))
                        TileMap.ArrayTileMap[x, y, TileMap.ArrayTileMap.GetLength(2) - 1] = 2;
                }
        }

        private bool TileNotEmpty(int x, int y, int z)
        {
            if (x < 0 || TileMap.ArrayTileMap.GetLength(0) <= x ||
                y < 0 || TileMap.ArrayTileMap.GetLength(1) <= y ||
                z < 0 || TileMap.ArrayTileMap.GetLength(2) <= z)
                return false;

            return TileMap.ArrayTileMap[x, y, z] >= 0;
        }

        private void SelectArea()
        {
            var left = Math.Min(SelectionStart.X, SelectionEnd.X);
            var right = Math.Max(SelectionStart.X, SelectionEnd.X);
            var top = Math.Min(SelectionStart.Y, SelectionEnd.Y);
            var down = Math.Max(SelectionStart.Y, SelectionEnd.Y);

            _tileSelectionScreen.SelectedTiles = new int[right - left + 1, down - top + 1];

            for (var y = top; y <= down; y++)
                for (var x = left; x <= right; x++)
                {
                    _tileSelectionScreen.SelectedTiles[x - left, y - top] = GetSelection(new Point(x, y));
                }
        }

        private void FillMultiSelection()
        {
            if (_tileSelectionScreen.SelectedTiles == null)
                return;

            var left = Math.Min(SelectionStart.X, SelectionEnd.X);
            var right = Math.Max(SelectionStart.X, SelectionEnd.X);
            var top = Math.Min(SelectionStart.Y, SelectionEnd.Y);
            var down = Math.Max(SelectionStart.Y, SelectionEnd.Y);

            for (var y = top; y <= down; y++)
                for (var x = left; x <= right; x++)
                {
                    var index = DrawMode ? _tileSelectionScreen.SelectedTiles[
                        (x - left) % _tileSelectionScreen.SelectedTiles.GetLength(0),
                        (y - top) % _tileSelectionScreen.SelectedTiles.GetLength(1)] : -1;

                    // do not erase stuff in draw mode
                    if (DrawMode && index < 0)
                        continue;

                    DrawTileAt(x, y, _currentLayer, index);
                }
        }

        private void DrawTileAt(int x, int y, int z, int index)
        {
            // check if the position is inside the tilemap
            if (x < 0 || x >= TileMap.ArrayTileMap.GetLength(0) ||
                y < 0 || y >= TileMap.ArrayTileMap.GetLength(1) ||
                z < 0 || z >= TileMap.ArrayTileMap.GetLength(2))
                return;

            if (index < 0)
                _removedTile = true;

            TileMap.ArrayTileMap[x, y, z] = index;
        }

        private bool CutCorners()
        {
            var posX = TileMap.ArrayTileMap.GetLength(0);
            var posY = TileMap.ArrayTileMap.GetLength(1);
            var newWidth = 0;
            var newHeight = 0;

            for (var z = 0; z < TileMap.ArrayTileMap.GetLength(2); z++)
            {
                for (var y = 0; y < TileMap.ArrayTileMap.GetLength(1); y++)
                {
                    for (var x = 0; x < TileMap.ArrayTileMap.GetLength(0); x++)
                    {
                        if (TileMap.ArrayTileMap[x, y, z] >= 0)
                        {
                            if (posX > x)
                                posX = x;
                            if (newWidth < x + 1)
                                newWidth = x + 1;
                            if (posY > y)
                                posY = y;
                            if (newHeight < y + 1)
                                newHeight = y + 1;
                        }
                    }
                }
            }

            // did not change the size?
            if (posX == 0 && posY == 0 &&
                newWidth == TileMap.ArrayTileMap.GetLength(0) &&
                newHeight == TileMap.ArrayTileMap.GetLength(1))
                return false;

            newWidth -= posX;
            newHeight -= posY;

            Map.ResizeMap(newWidth, newHeight, -posX, -posY);

            _camera.Location.X += (int)(posX * _camera.Scale * 16);
            _camera.Location.Y += (int)(posY * _camera.Scale * 16);

            return true;
        }

        private void FillSelection()
        {
            if (DrawMode && _tileSelectionScreen.SelectedTiles != null)
            {
                // draw selected tiles
                var left = Selection.X;
                var right = Selection.X + _tileSelectionScreen.SelectedTiles.GetLength(0);
                var top = SelectionEnd.Y;
                var down = SelectionEnd.Y + _tileSelectionScreen.SelectedTiles.GetLength(1);

                for (var y = top; y < down; y++)
                    for (var x = left; x < right; x++)
                        // do not erase stuff in draw mode
                        if (_tileSelectionScreen.SelectedTiles[x - left, y - top] >= 0)
                            DrawTileAt(x, y, _currentLayer, _tileSelectionScreen.SelectedTiles[x - left, y - top]);
            }
            else if (!DrawMode)
            {
                // remove tile
                DrawTileAt(Selection.X, Selection.Y, _currentLayer, -1);
            }
        }

        private void ButtonPressedLayer(int layer)
        {
            _currentLayer = layer;
        }

        private void ButtonUpdate(UiElement ui, int layer)
        {
            LayerVisibility[layer] = !LayerVisibility[layer];
            ((UiButton)ui).ButtonIcon = LayerVisibility[layer] ? Resources.EditorEyeOpen : Resources.EditorEyeClosed;
        }

        private void DeleteTilelayer(int layer)
        {
            if (layer >= TileMap.ArrayTileMap.GetLength(2) ||
                TileMap.ArrayTileMap.GetLength(2) <= 0)
                return;

            var newTilemap = new int[
                TileMap.ArrayTileMap.GetLength(0),
                TileMap.ArrayTileMap.GetLength(1),
                TileMap.ArrayTileMap.GetLength(2) - 1];

            // move the content to a new tilemap without copying the deleted layer
            for (var z = 0; z < newTilemap.GetLength(2); z++)
                for (var y = 0; y < newTilemap.GetLength(1); y++)
                    for (var x = 0; x < newTilemap.GetLength(0); x++)
                    {
                        var posZ = z == layer ? z + 1 : z;
                        newTilemap[x, y, posZ] = TileMap.ArrayTileMap[x, y, z];
                    }

            TileMap.ArrayTileMap = newTilemap;
        }

        private void RemoveTileContent(int layer)
        {
            if (layer >= TileMap.ArrayTileMap.GetLength(2) ||
                TileMap.ArrayTileMap.GetLength(2) <= 0)
                return;

            for (var y = 0; y < TileMap.ArrayTileMap.GetLength(1); y++)
                for (var x = 0; x < TileMap.ArrayTileMap.GetLength(0); x++)
                    TileMap.ArrayTileMap[x, y, layer] = -1;
        }

        private void UpdateImageFrom(UiElement ui)
        {
            if (TileMap.SprTileset == null) return;

            if (_replaceSelections < 0)
            {
                ((UiImage)ui).SprImage = null;
                return;
            }

            ((UiImage)ui).SprImage = TileMap.SprTileset;
            ((UiImage)ui).SourceRectangle =
                new Rectangle(
                    _replaceSelections % (TileMap.SprTileset.Width / Values.TileSize) * Values.TileSize,
                    _replaceSelections / (TileMap.SprTileset.Width / Values.TileSize) * Values.TileSize, Values.TileSize, Values.TileSize);
        }

        public void OffsetTileMap(int offsetX, int offsetY)
        {
            var dirY = offsetY < 0 ? 1 : -1;
            var dirX = offsetX < 0 ? 1 : -1;

            for (var z = 0; z < TileMap.ArrayTileMap.GetLength(2); z++)
            {
                for (var y = dirY == 1 ? 0 : TileMap.ArrayTileMap.GetLength(1) - 1;
                    (dirY == 1 && y < TileMap.ArrayTileMap.GetLength(1)) || (dirY == -1 && y >= 0); y += dirY)
                {
                    for (var x = dirX == 1 ? 0 : TileMap.ArrayTileMap.GetLength(0) - 1;
                        (dirX == 1 && x < TileMap.ArrayTileMap.GetLength(0)) || (dirX == -1 && x >= 0); x += dirX)
                    {
                        var newX = x - offsetX;
                        var newY = y - offsetY;

                        if (0 <= newX && newX < TileMap.ArrayTileMap.GetLength(0) &&
                            0 <= newY && newY < TileMap.ArrayTileMap.GetLength(1))
                            TileMap.ArrayTileMap[x, y, z] = TileMap.ArrayTileMap[newX, newY, z];
                        else
                            TileMap.ArrayTileMap[x, y, z] = -1;
                    }
                }
            }
        }
    }
}
