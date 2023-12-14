using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.Base;
using ProjectZ.Base.UI;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;
#if WINDOWS
using System.Windows.Forms;
#endif

namespace ProjectZ.Editor
{
    internal class TilesetEdit : InGame.Screens.Screen
    {
        private class MapData
        {
            public string FilePath;
            public Map Map;
        }

        private class Tile : IComparable<Tile>
        {
            public Texture2D SprTile;
            public Color[] Data;
            public uint Value;

            // needed for mapping after sorting the tile list
            public int Position;

            public int CompareTo(Tile other) => (int)(other.Value - Value);
        }

        private readonly List<MapData> _loadedMaps = new List<MapData>();
        private readonly List<Tile> _tileSetData = new List<Tile>();
        private readonly EditorCamera _camera = new EditorCamera();

        private RenderTarget2D _renderTarget;

        private int _toolBarWidth = 200;
        private int _currentSelection;
        private int _selctionEnd;
        private int _tileSize = 16;

        private int _selectionStart;
        public int[,] SelectedTiles;

        private bool _selecting;
        private int _outputWidth = 15;
        private int _outputHeight;

        public TilesetEdit(string screenId) : base(screenId) { }

        public override void Load(ContentManager content)
        {
            var buttonDist = 5;
            var buttonWidth = _toolBarWidth - buttonDist * 2;
            var buttonWidthHalf = _toolBarWidth - buttonDist * 3;
            var buttonHeight = 30;
            var posY = Values.ToolBarHeight + buttonDist;

            Game1.EditorUi.AddElement(new UiRectangle(new Rectangle(0, Values.ToolBarHeight, _toolBarWidth, 0), "left", Values.EditorUiTilesetEditor, Color.Transparent, Color.Black * 0.5f,
                ui => { ui.Rectangle = new Rectangle(0, Values.ToolBarHeight, _toolBarWidth, Game1.WindowHeight - Values.ToolBarHeight); }));

            Game1.EditorUi.AddElement(new UiButton(new Rectangle(buttonDist, posY, buttonWidth, buttonHeight), Resources.EditorFont,
                "add map", "bt1", Values.EditorUiTilesetEditor, null, ui => { LoadMaps(); }));

            Game1.EditorUi.AddElement(new UiButton(new Rectangle(buttonDist, posY += buttonHeight + buttonDist, buttonWidth, buttonHeight), Resources.EditorFont,
                "save", "bt1", Values.EditorUiTilesetEditor, null, ui => { SaveChanges(); }));

            Game1.EditorUi.AddElement(new UiButton(new Rectangle(buttonDist, posY += buttonHeight + buttonDist, buttonWidth, buttonHeight), Resources.EditorFont,
                "remove all", "bt1", Values.EditorUiTilesetEditor, null, ui => { RemoveAll(); }));

            _camera.Location = new Point(400, 250);
        }

        public override void Update(GameTime gameTime)
        {
            Game1.EditorUi.CurrentScreen = Values.EditorUiTilesetEditor;

            var position = InputHandler.MousePosition();

            // update tileset scale
            if (InputHandler.MouseWheelUp() && _camera.Scale < 10)
            {
                _camera.Scale += 0.25f;
                var scale = _camera.Scale / (_camera.Scale - 0.25f);
                _camera.Location.X = InputHandler.MousePosition().X - (int)((InputHandler.MousePosition().X - _camera.Location.X) * scale);
                _camera.Location.Y = InputHandler.MousePosition().Y - (int)((InputHandler.MousePosition().Y - _camera.Location.Y) * scale);
            }
            if (InputHandler.MouseWheelDown() && _camera.Scale > 0.25f)
            {
                _camera.Scale -= 0.25f;
                var scale = _camera.Scale / (_camera.Scale + 0.25f);
                _camera.Location.X = InputHandler.MousePosition().X - (int)((InputHandler.MousePosition().X - _camera.Location.X) * scale);
                _camera.Location.Y = InputHandler.MousePosition().Y - (int)((InputHandler.MousePosition().Y - _camera.Location.Y) * scale);
            }

            // move the tileset
            if (!InputHandler.MouseMiddleStart() && InputHandler.MouseMiddleDown())
                _camera.Location += position - InputHandler.LastMousePosition();

            // update currentSelection
            if (InputHandler.MouseIntersect(new Rectangle(_camera.Location.X, _camera.Location.Y,
                (int)(_outputWidth * _tileSize * _camera.Scale),
                (int)(_outputHeight * _tileSize * _camera.Scale))))
            {
                _currentSelection =
                    ((position.X - _camera.Location.X) / (int)(_tileSize * _camera.Scale)) % _outputWidth +
                    ((position.Y - _camera.Location.Y) / (int)(_tileSize * _camera.Scale)) * _outputWidth;
                _selctionEnd = _currentSelection;

                if (_currentSelection >= _tileSetData.Count)
                    _currentSelection = -1;
            }
            else
                _currentSelection = -1;

            //if (InputHandler.MouseRightStart() && currentSelection != -1)
            //{
            //    selectionStart = currentSelection;
            //}

            //if (InputHandler.MouseRightPressed())
            //{
            //    if (_currentSelection != -1)
            //    {
            //        for (var y = 0; y < SelectedTiles.GetLength(1); y++)
            //        {
            //            for (var x = 0; x < SelectedTiles.GetLength(0); x++)
            //            {
            //                var pos = SelectedTiles[x, y];
            //                var dir = pos - SelectedTiles[0, 0] + _currentSelection;

            //                var temp = _tiles[pos];
            //                _tiles[pos] = _tiles[dir];
            //                _tiles[dir] = temp;

            //                // UpdatePosition(pos, dir);
            //            }
            //        }
            //    }
            //}

            //// select a tile
            //if (InputHandler.MouseLeftStart() && _currentSelection != -1)
            //{
            //    _selecting = true;
            //    _selectionStart = _currentSelection;
            //}
            //if (InputHandler.MouseLeftReleased() && _selecting)
            //{
            //    _selecting = false;

            //    // select multiple tiles
            //    var start = Math.Min(_selectionStart, _selctionEnd);
            //    var end = Math.Max(_selectionStart, _selctionEnd);
            //    SelectedTiles = new int[Math.Abs(end % _world.TileMap.TileCountX - start % _world.TileMap.TileCountX) + 1,
            //        end / _world.TileMap.TileCountX - start / _world.TileMap.TileCountX + 1];

            //    for (var y = start / _world.TileMap.TileCountX; y <= end / _world.TileMap.TileCountX; y++)
            //        for (var x = Math.Min(start % _world.TileMap.TileCountX, end % _world.TileMap.TileCountX);
            //            x <= Math.Max(start % _world.TileMap.TileCountX, end % _world.TileMap.TileCountX); x++)
            //        {
            //            SelectedTiles[x - Math.Min(start % _world.TileMap.TileCountX, end % _world.TileMap.TileCountX),
            //                y - start / _world.TileMap.TileCountX] = x + y * _world.TileMap.TileCountX;
            //        }
            //}
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointWrap, null, null, null, _camera.TransformMatrix);

            // draw the tiled background
            spriteBatch.Draw(Resources.SprTiledBlock, new Rectangle(0, 0,
                    _outputWidth * _tileSize, _outputHeight * _tileSize),
                new Rectangle(0, 0,
                    (_outputWidth * _tileSize) / _tileSize * 2,
                    (_outputHeight * _tileSize) / _tileSize * 2), Color.White);

            // draw the tileset
            //spriteBatch.Draw(Resources.sprTileset, new Rectangle(drawPosition.X, drawPosition.Y, (int)(Resources.sprTileset.Width * drawScale), (int)(Resources.sprTileset.Height * drawScale)), Color.White);

            // draw all the tiles
            for (var i = 0; i < _tileSetData.Count; i++)
                spriteBatch.Draw(_tileSetData[i].SprTile, new Rectangle(
                    i % _outputWidth * _tileSize, i / _outputWidth * _tileSize,
                    _tileSetData[i].SprTile.Width, _tileSetData[i].SprTile.Height), Color.White);

            // draw the current selection
            if (_currentSelection >= 0)
                spriteBatch.Draw(Resources.SprWhite, new Rectangle(
                        _currentSelection % _outputWidth * _tileSize,
                        _currentSelection / _outputWidth * _tileSize,
                    _tileSize, _tileSize), Color.Red * 0.5f);


            // draw all the loaded tilemap
            var posY = 0;
            foreach (var map in _loadedMaps)
            {
                var tileMap = map.Map.TileMap;

                for (var z = 0; z < tileMap.ArrayTileMap.GetLength(2) - (tileMap.BlurLayer ? 1 : 0); z++)
                    for (var y = 0; y < tileMap.ArrayTileMap.GetLength(1); y++)
                        for (var x = 0; x < tileMap.ArrayTileMap.GetLength(0); x++)
                            if (tileMap.ArrayTileMap[x, y, z] >= 0)
                                spriteBatch.Draw(_tileSetData[tileMap.ArrayTileMap[x, y, z]].SprTile,
                                    new Vector2(x * _tileSize + _outputWidth * 16 + 8, posY + y * _tileSize), Color.White);

                posY += tileMap.ArrayTileMap.GetLength(1) * 16 + 8;
            }
            //// draw the selection
            //if (SelectedTiles != null)
            //{
            //    for (var y = 0; y < SelectedTiles.GetLength(1); y++)
            //        for (var x = 0; x < SelectedTiles.GetLength(0); x++)
            //        {
            //            spriteBatch.Draw(Resources.SprWhite, new Rectangle(_drawPosition.X + (SelectedTiles[x, y] % _world.TileMap.TileCountX) * (int)(_tileSize * _drawScale),
            //                _drawPosition.Y + (SelectedTiles[x, y] / _world.TileMap.TileCountX) * (int)(_tileSize * _drawScale),
            //                (int)(_tileSize * _drawScale), (int)(_tileSize * _drawScale)), Color.Red * 0.5f);
            //        }
            //}

            //if (_selecting)
            //{
            //    var start = Math.Min(_selectionStart, _selctionEnd);
            //    var end = Math.Max(_selectionStart, _selctionEnd);

            //    spriteBatch.Draw(Resources.SprWhite, new Rectangle(
            //        _drawPosition.X + Math.Min(_selectionStart % _world.TileMap.TileCountX, _selctionEnd % _world.TileMap.TileCountX) * (int)(_tileSize * _drawScale),
            //        _drawPosition.Y + Math.Min(_selectionStart / _world.TileMap.TileCountX, _selctionEnd / _world.TileMap.TileCountX) * (int)(_tileSize * _drawScale),
            //        (Math.Abs(_selectionStart % _world.TileMap.TileCountX - _selctionEnd % _world.TileMap.TileCountX) + 1) * (int)(_tileSize * _drawScale),
            //        (Math.Abs(_selectionStart / _world.TileMap.TileCountX - _selctionEnd / _world.TileMap.TileCountX) + 1) * (int)(_tileSize * _drawScale)), Color.PaleVioletRed * 0.5f);
            //}

            spriteBatch.End();
        }

        public void RemoveAll()
        {
            _loadedMaps.Clear();
            _tileSetData.Clear();
        }

        public void LoadMaps()
        {
#if WINDOWS
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Map file (*.map)|*.map",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() != DialogResult.OK) return;

            // add the selected maps
            foreach (var fileName in openFileDialog.FileNames)
                AddMap(fileName);

            _outputHeight = (int)Math.Ceiling(_tileSetData.Count / (float)_outputWidth);

            // sort the tiles
            _tileSetData.Sort();

            RemapTiles();
#endif
        }

        public void SaveChanges()
        {
#if WINDOWS
            var saveFileDialog = new SaveFileDialog()
            {
                Filter = "Map file (*.png)|*.png"
            };

            if (saveFileDialog.ShowDialog() != DialogResult.OK) return;

            var filePath = saveFileDialog.FileName;
            var fileName = Path.GetFileName(filePath);

            // save the tileset
            SaveTileset(saveFileDialog.FileName);

            // save the changes to the map
            foreach (var map in _loadedMaps)
            {
                // set the path of the new tileset
                map.Map.TileMap.TilesetPath = fileName;
                // save the map to the original path
                SaveLoadMap.SaveMapFile(map.FilePath, map.Map);
            }
#endif
        }

        public void SaveTileset(string path)
        {
            _renderTarget = new RenderTarget2D(Game1.Graphics.GraphicsDevice, _outputWidth * _tileSize, _outputHeight * _tileSize);

            Game1.Graphics.GraphicsDevice.SetRenderTarget(_renderTarget);
            Game1.Graphics.GraphicsDevice.Clear(Color.Transparent);
            Game1.SpriteBatch.Begin();

            for (var i = 0; i < _tileSetData.Count; i++)
                Game1.SpriteBatch.Draw(_tileSetData[i].SprTile, new Rectangle(
                    i % _outputWidth * _tileSize, i / _outputWidth * _tileSize,
                    _tileSetData[i].SprTile.Width, _tileSetData[i].SprTile.Height), Color.White);

            Game1.SpriteBatch.End();
            Game1.Graphics.GraphicsDevice.SetRenderTarget(null);

            using Stream stream = File.Create(path);
            _renderTarget.SaveAsPng(stream, _renderTarget.Width, _renderTarget.Height);
        }

        public void RemapTiles()
        {
            var tileMapping = new int[_tileSetData.Count];

            for (var i = 0; i < _tileSetData.Count; i++)
                tileMapping[_tileSetData[i].Position] = i;

            // map all tiles to the new order
            foreach (var maps in _loadedMaps)
            {
                var tileMap = maps.Map.TileMap;
                MapTileArray(tileMap, tileMapping);
            }

            // reset the tile position
            for (var i = 0; i < _tileSetData.Count; i++)
                _tileSetData[i].Position = i;
        }

        public void AddMap(string strPath)
        {
            var map = new Map();

            SaveLoadMap.LoadMapFile(strPath, map);

            var mapData = new MapData() { FilePath = strPath, Map = map };

            // get the Color[] data of each tile
            var tiles = SplitTileset(map.TileMap.SprTileset);
            var tileMapping = new int[tiles.Count];

            // add the tiles
            for (var i = 0; i < tiles.Count; i++)
            {
                // remove tile if it is not used in the image
                if (IsTileUsed(map.TileMap, i))
                    tileMapping[i] = AddTexture(tiles[i]);
            }

            // map the tiles from the tilemap to the tiles of the output tileset
            MapTileArray(map.TileMap, tileMapping);

            _loadedMaps.Add(mapData);
        }

        private bool IsTileUsed(TileMap tileMap, int index)
        {
            for (var z = 0; z < tileMap.ArrayTileMap.GetLength(2) - (tileMap.BlurLayer ? 1 : 0); z++)
                for (var y = 0; y < tileMap.ArrayTileMap.GetLength(1); y++)
                    for (var x = 0; x < tileMap.ArrayTileMap.GetLength(0); x++)
                        if (tileMap.ArrayTileMap[x, y, z] == index)
                            return true;

            return false;
        }

        public void MapTileArray(TileMap tileMap, int[] mapping)
        {
            for (var z = 0; z < tileMap.ArrayTileMap.GetLength(2) - (tileMap.BlurLayer ? 1 : 0); z++)
                for (var y = 0; y < tileMap.ArrayTileMap.GetLength(1); y++)
                    for (var x = 0; x < tileMap.ArrayTileMap.GetLength(0); x++)
                        if (tileMap.ArrayTileMap[x, y, z] >= 0)
                            tileMap.ArrayTileMap[x, y, z] = mapping[tileMap.ArrayTileMap[x, y, z]];
        }

        public List<Color[]> SplitTileset(Texture2D sprTexture)
        {
            var colorData = new Color[sprTexture.Width * sprTexture.Height];
            sprTexture.GetData(colorData);

            var tileDataList = new List<Color[]>();

            for (var y = 0; y < sprTexture.Height / _tileSize; y++)
                for (var x = 0; x < sprTexture.Width / _tileSize; x++)
                {
                    var data = new Color[_tileSize * _tileSize];

                    for (var iy = 0; iy < _tileSize; iy++)
                    {
                        for (var ix = 0; ix < _tileSize; ix++)
                        {
                            data[ix + iy * _tileSize] = colorData[y * _tileSize * sprTexture.Width + x * _tileSize + ix + iy * sprTexture.Width];
                        }
                    }

                    tileDataList.Add(data);
                }

            return tileDataList;
        }

        /// <summary>
        /// returns the index of the tile inside the new tileset
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public int AddTexture(Color[] data)
        {
            var texture = new Texture2D(Game1.Graphics.GraphicsDevice, _tileSize, _tileSize);

            // check if the tile is already in use
            for (var i = 0; i < _tileSetData.Count; i++)
                if (ColorEquals(_tileSetData[i].Data, data))
                    return i;

            uint r = 0;
            uint g = 0;
            uint b = 0;
            uint a = 0;

            foreach (var color in data)
            {
                r += color.R;
                g += color.G;
                b += color.B;
                a += color.A;
            }

            // only works when the tiles are 16x16
            uint max = 255 * 16 * 16;
            uint value =
                ((uint)(r / (float)max * 256) << 0) +
                ((uint)(g / (float)max * 256) << 8) +
                ((uint)(b / (float)max * 256) << 16) +
                ((uint)(a / (float)max * 256) << 24);

            texture.SetData(data);
            _tileSetData.Add(new Tile { Data = data, SprTile = texture, Value = value, Position = _tileSetData.Count });

            return _tileSetData.Count - 1;
        }

        public bool ColorEquals(Color[] first, Color[] second)
        {
            if (first.Length != second.Length)
                return false;

            for (var i = 0; i < first.Length; i++)
            {
                var diff = ColorDiff(first[i], second[i]);
                // why is this 30 and not 0?
                if (diff > 30)
                    return false;
            }

            return true;
        }

        public int ColorDiff(Color first, Color second)
        {
            return Math.Abs(first.R - second.R) +
                   Math.Abs(first.G - second.G) +
                   Math.Abs(first.B - second.B) +
                   Math.Abs(first.A - second.A);
        }
    }
}
