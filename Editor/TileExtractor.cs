using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.Base;
using ProjectZ.Base.UI;
using ProjectZ.InGame.Screens;
using ProjectZ.InGame.Things;

namespace ProjectZ.Editor
{
    class TileExtractor : Screen
    {
        private List<Texture2D> _sprTiled = new List<Texture2D>();
        private List<Texture2D> _sprTiledRemoved = new List<Texture2D>();

        private List<Rectangle> _untiledParts = new List<Rectangle>();
        private List<Color[]> _tiledInput;

        private RenderTarget2D _textureRenderTarget;

        private Texture2D _inputTexture;
        private Texture2D _outputTexture;
        private Texture2D _outputTextureRemoved;
        private Texture2D _outputTextureUntiled;

        private EditorCamera _camera = new EditorCamera();

        private Vector2 _tilesetPosition;

        private Point _distance;
        private Point _tileSize = new Point(16, 16);

        private string _imageName;

        private int[,,] _tileMap;

        private int _selectedInputTile;
        private int _selectedOutputTile;
        private int _maxWidth = 10;
        private int _toolBarWidth = 200;

        private int MaxWidth
        {
            get => _maxWidth;
            set
            {
                _maxWidth = value;
                _maxWidth = (int)MathHelper.Clamp(_maxWidth, 1, 100);
                LoadOutput();
            }
        }

        private Point Distance
        {
            get => _distance;
            set
            {
                _distance = value;
                _distance = new Point((int)MathHelper.Clamp(_distance.X, -TileSize.X, TileSize.X), (int)MathHelper.Clamp(_distance.Y, -TileSize.Y, TileSize.Y));
                LoadOutput();
            }
        }

        private Point TileSize
        {
            get => _tileSize;
            set
            {
                _tileSize = value;
                TileTexture();
                LoadOutput();
            }
        }

        public TileExtractor(string screenId) : base(screenId) { }

        public override void Load(ContentManager content)
        {
            var buttonDist = 5;

            var buttonWidth = _toolBarWidth - buttonDist * 2;
            var buttonWidthHalf = _toolBarWidth / 2 - (int)(buttonDist * 1.5);
            var buttonHeight = 30;

            var buttonDistY = buttonHeight + 5;
            var posY = Values.ToolBarHeight + buttonDist;

            // set the init camera position
            _camera.Location = new Point(_toolBarWidth + 10, Values.ToolBarHeight + 10 + 20);

            // left background bar
            Game1.EditorUi.AddElement(new UiRectangle(new Rectangle(0, Values.ToolBarHeight, _toolBarWidth, 0), "left", Values.EditorUiTileExtractor, Color.Transparent, Color.Black * 0.5f,
                ui => { ui.Rectangle = new Rectangle(0, Values.ToolBarHeight, _toolBarWidth, Game1.WindowHeight - Values.ToolBarHeight); }));

            Game1.EditorUi.AddElement(new UiButton(new Rectangle(buttonDist, posY, buttonWidth, buttonHeight), Resources.EditorFont, "load image", "default", Values.EditorUiTileExtractor, null, Button_LoadImage));
            Game1.EditorUi.AddElement(new UiButton(new Rectangle(buttonDist, posY += buttonDistY, buttonWidth, buttonHeight), Resources.EditorFont, "save tileSet", "default", Values.EditorUiTileExtractor, null, ButtonSaveTileset));
            Game1.EditorUi.AddElement(new UiButton(new Rectangle(buttonDist, posY += buttonDistY, buttonWidth, buttonHeight), Resources.EditorFont, "save removed", "default", Values.EditorUiTileExtractor, null, ButtonSaveRemoveTileset));
            Game1.EditorUi.AddElement(new UiButton(new Rectangle(buttonDist, posY += buttonDistY, buttonWidth, buttonHeight), Resources.EditorFont, "save .txt", "default", Values.EditorUiTileExtractor, null, ButtonSaveTilemap));

            //Game1.EditorUi.AddElement(new UiButton(new Rectangle(5, posY += buttonDistY, buttonWidth, 25), Resources.EditorFont, "reload", "default", Values.EditorUiTileExtractor, null, ui => { TileTexture(); }));

            //Game1.EditorUi.AddElement(new UiCheckBox(new Rectangle(5, posY += buttonDistY + 20, buttonWidth, 25), Resources.EditorFont, "add del txt", "checkBox", screenId, false, null, (UiElement _ui) => { addDeletedTextures = ((CheckBox)_ui).currentState; }));
            //Game1.EditorUi.AddElement(new UiButton(new Rectangle(5, posY += buttonDistY, buttonWidth, 25), Resources.EditorFont, "add to map", "default", screenId, null, (UiElement _ui) => { AddToMap(); }));

            //Game1.EditorUi.AddElement(new UiButton(new Rectangle(5, posY += buttonDistY + 20, buttonWidth, 25), Resources.EditorFont, "remove untiled", "default", screenId, null, (UiElement _ui) => { untiledParts.Clear(); LoadOutput(); }));

            //output width
            Game1.EditorUi.AddElement(new UiLabel(new Rectangle(5, posY += buttonDistY, buttonWidth, 25), Resources.EditorFont, "output width", "default", Values.EditorUiTileExtractor, null));
            Game1.EditorUi.AddElement(new UiNumberInput(new Rectangle(5, posY += buttonDistY, buttonWidth, 25), Resources.EditorFont, _maxWidth, 5, 25, 1,
                "outputWidth", Values.EditorUiTileExtractor, null, ui => { MaxWidth = (int)((UiNumberInput)ui).Value; }));

            // padding
            Game1.EditorUi.AddElement(new UiLabel(new Rectangle(5, posY += buttonDistY, buttonWidth, 25), Resources.EditorFont, "padding", "default", Values.EditorUiTileExtractor, null));
            Game1.EditorUi.AddElement(new UiLabel(new Rectangle(5, posY += 15, buttonWidthHalf, 25), Resources.EditorFont, "x", "default", Values.EditorUiTileExtractor, null));
            Game1.EditorUi.AddElement(new UiLabel(new Rectangle(10 + buttonWidthHalf, posY, buttonWidthHalf, 25), Resources.EditorFont, "y", "default", Values.EditorUiTileExtractor, null));

            Game1.EditorUi.AddElement(new UiNumberInput(new Rectangle(5, posY += buttonDistY, buttonWidthHalf, 25), Resources.EditorFont, _distance.X, -10, 10, 1,
                "outputWidth", Values.EditorUiTileExtractor, null, ui => { Distance = new Point((int)((UiNumberInput)ui).Value, _distance.Y); }));
            Game1.EditorUi.AddElement(new UiNumberInput(new Rectangle(10 + buttonWidthHalf, posY, buttonWidthHalf, 25), Resources.EditorFont, _distance.Y, -10, 10, 1,
                "outputWidth", Values.EditorUiTileExtractor, null, ui => { Distance = new Point(_distance.X, (int)((UiNumberInput)ui).Value); }));

            // tilesize
            Game1.EditorUi.AddElement(new UiLabel(new Rectangle(5, posY += buttonDistY, buttonWidth, 25), Resources.EditorFont, "tile size", "default", Values.EditorUiTileExtractor, null));
            Game1.EditorUi.AddElement(new UiLabel(new Rectangle(5, posY += 15, buttonWidthHalf, 25), Resources.EditorFont, "x", "default", Values.EditorUiTileExtractor, null));
            Game1.EditorUi.AddElement(new UiLabel(new Rectangle(10 + buttonWidthHalf, posY, buttonWidthHalf, 25), Resources.EditorFont, "y", "default", Values.EditorUiTileExtractor, null));

            Game1.EditorUi.AddElement(new UiNumberInput(new Rectangle(5, posY += buttonDistY, buttonWidthHalf, 25), Resources.EditorFont, _tileSize.X, 1, 200, 1,
                "outputWidth", Values.EditorUiTileExtractor, null, ui => { TileSize = new Point((int)((UiNumberInput)ui).Value, _tileSize.Y); }));
            Game1.EditorUi.AddElement(new UiNumberInput(new Rectangle(10 + buttonWidthHalf, posY, buttonWidthHalf, 25), Resources.EditorFont, _tileSize.Y, 1, 200, 1,
                "outputWidth", Values.EditorUiTileExtractor, null, ui => { TileSize = new Point(_tileSize.X, (int)((UiNumberInput)ui).Value); }));
        }

        public override void Update(GameTime gameTime)
        {
            Game1.EditorUi.CurrentScreen = Values.EditorUiTileExtractor;

            var mousePosition = InputHandler.MousePosition();

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
                _camera.Location += mousePosition - InputHandler.LastMousePosition();


            //select a tile from the input texture
            if (_inputTexture != null &&
                InputHandler.MouseIntersect(new Rectangle(_camera.Location.X, _camera.Location.Y,
                    (int)(_inputTexture.Width * _camera.Scale), (int)(_inputTexture.Height * _camera.Scale))))
            {
                _selectedInputTile = (int)((mousePosition.X - _camera.Location.X) / (TileSize.X * _camera.Scale)) +
                                    (int)((mousePosition.Y - _camera.Location.Y) / (TileSize.Y * _camera.Scale)) * (_inputTexture.Width / TileSize.X);
            }
            else
                _selectedInputTile = -1;

            _tilesetPosition = Vector2.Zero;

            if (_inputTexture != null && _inputTexture.Width <= _inputTexture.Height)
                _tilesetPosition.X += _inputTexture.Width + 15;
            else if (_inputTexture != null)
                _tilesetPosition.Y += _inputTexture.Height + 40;

            //select a tile from the output texture
            if (_outputTexture != null &&
                InputHandler.MouseIntersect(new Rectangle(
                    _camera.Location.X + (int)(_tilesetPosition.X * _camera.Scale),
                    _camera.Location.Y + (int)(_tilesetPosition.Y * _camera.Scale),
                    (int)(_outputTexture.Width * _camera.Scale), (int)(_outputTexture.Height * _camera.Scale))))
            {
                _selectedOutputTile = (int)((mousePosition.X - _camera.Location.X - (int)(_tilesetPosition.X * _camera.Scale)) / ((TileSize.X + Distance.X) * _camera.Scale)) +
                                     (int)((mousePosition.Y - _camera.Location.Y - (int)(_tilesetPosition.Y * _camera.Scale)) / ((TileSize.Y + Distance.Y) * _camera.Scale)) *
                                     (_outputTexture.Width / (TileSize.X + Distance.X));
            }
            else
                _selectedOutputTile = -1;

            // remove selected tile
            if (InputHandler.MouseLeftPressed() && _selectedOutputTile >= 0 && _selectedOutputTile < _sprTiled.Count)
                RemoveSelectedTile();

            //if (InputHandler.MouseLeftStart() && selectedInputTile >= 0)
            //    startPosition = selectedInputTile;
            //if (InputHandler.MouseLeftReleased() && startPosition >= 0 && selectedInputTile >= 0)
            //{
            //    int left = Math.Min(startPosition % (inputTexture.Width / tileSize.X),
            //                        selectedInputTile % (inputTexture.Width / tileSize.X));
            //    int right = Math.Max(startPosition % (inputTexture.Width / tileSize.X),
            //                        selectedInputTile % (inputTexture.Width / tileSize.X));
            //    int upper = Math.Min(startPosition / (inputTexture.Width / tileSize.X),
            //                        selectedInputTile / (inputTexture.Width / tileSize.X));
            //    int down = Math.Max(startPosition / (inputTexture.Width / tileSize.X),
            //                        selectedInputTile / (inputTexture.Width / tileSize.X));

            //    untiledParts.Add(new Rectangle(left * tileSize.X, upper * tileSize.Y,
            //        (right - left + 1) * tileSize.X, (down - upper + 1) * tileSize.Y));

            //    startPosition = -1;

            //    LoadOutput();
            //}
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointWrap, null, null, null, _camera.TransformMatrix);

            //draw input
            if (_inputTexture != null)
            {
                // draw the header
                spriteBatch.DrawString(Resources.EditorFont, "input", new Vector2(0, 0 - Resources.EditorFont.MeasureString("input").Y), Color.White);
                // draw the texture
                spriteBatch.Draw(_inputTexture, Vector2.Zero, Color.White);

                // draw selection
                if (_selectedInputTile >= 0)
                    Game1.SpriteBatch.Draw(Resources.SprWhite, new Rectangle(
                            _selectedInputTile % (_inputTexture.Width / _tileSize.X) * _tileSize.X,
                            _selectedInputTile / (_inputTexture.Width / _tileSize.Y) * _tileSize.Y, _tileSize.X, _tileSize.Y), Color.Red * 0.5f);
            }

            //draw output as texture
            if (_outputTexture != null)
            {
                // draw the header
                spriteBatch.DrawString(Resources.EditorFont, "tileset", _tilesetPosition - new Vector2(0, Resources.EditorFont.MeasureString("A").Y), Color.White);
                // draw the texture
                spriteBatch.Draw(_outputTexture, _tilesetPosition, Color.White);

                // draw output selection
                if (_selectedOutputTile >= 0)
                {
                    var rectangle = new Rectangle(
                        (int)_tilesetPosition.X + _selectedOutputTile % (_outputTexture.Width / (_tileSize.X + _distance.X)) * (_tileSize.X + _distance.X),
                        (int)_tilesetPosition.Y + _selectedOutputTile / (_outputTexture.Width / (_tileSize.X + _distance.X)) * (_tileSize.Y + _distance.Y),
                        (_tileSize.X + _distance.X), (_tileSize.Y + _distance.Y));
                    Game1.SpriteBatch.Draw(Resources.SprWhite, rectangle, new Rectangle(0, 0, 1, 1), Color.Red * 0.5f);
                }

                _tilesetPosition.Y += _outputTexture.Height + 10;
            }

            // draw output as texture
            if (_outputTextureUntiled != null)
            {
                // draw the header
                spriteBatch.DrawString(Resources.EditorFont, "tileset untiled", _tilesetPosition, Color.White);
                _tilesetPosition.Y += Resources.EditorFont.MeasureString("A").Y;
                // draw the untiled output
                spriteBatch.Draw(_outputTextureUntiled, _tilesetPosition, Color.White);
                _tilesetPosition.Y += _outputTextureUntiled.Height + 10;
            }

            // draw removed tile texture
            if (_outputTextureRemoved != null)
            {
                // draw the header
                spriteBatch.DrawString(Resources.EditorFont, "removed", _tilesetPosition, Color.White);
                _tilesetPosition.Y += Resources.EditorFont.MeasureString("A").Y;
                // draw the removed tiles
                spriteBatch.Draw(_outputTextureRemoved, _tilesetPosition, Color.White);
                _tilesetPosition.Y += _outputTextureRemoved.Height;
            }

            //draw the back darker
            //Basics.DrawRectangle(new Rectangle(0, upperPos, leftPos, Values.windowSize.Height - upperPos), Color.Black * 0.25f);

            spriteBatch.End();
        }

        private void Button_LoadImage(UiElement element)
        {
#if WINDOWS
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.Filter = "(*.png)|*.png";
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var path = openFileDialog.FileName;

                //try to load the texture
                try
                {
                    Resources.LoadTexture(out _inputTexture, path);

                    // get the file name
                    var info = new FileInfo(path);
                    _imageName = info.Name.Replace(".png", "");

                }
                catch { }

                //could not load the texture
                if (_inputTexture == null)
                    return;

                _tileMap = TileTexture();

                LoadOutput();
            }
#endif
        }

        private int[,,] TileTexture()
        {
            if (_inputTexture == null)
                return null;

            var tileMap = new int[_inputTexture.Width / _tileSize.X, _inputTexture.Height / _tileSize.Y, 2];

            var colorTexture = new Color[_inputTexture.Width * _inputTexture.Height];
            _inputTexture.GetData(colorTexture);

            _sprTiledRemoved = new List<Texture2D>();

            _tiledInput = new List<Color[]>();
            _tiledInput = ExtractTiles(colorTexture, _inputTexture.Width, _inputTexture.Height, _tileSize.X, _tileSize.Y);
            _tiledInput = RemoveDuplicates(_tiledInput, ref tileMap);

            _sprTiled = new List<Texture2D>();
            for (var i = 0; i < _tiledInput.Count; i++)
            {
                var spr = new Texture2D(Game1.Graphics.GraphicsDevice, _tileSize.X, _tileSize.Y);
                spr.SetData(_tiledInput[i]);
                _sprTiled.Add(spr);
            }

            return tileMap;
        }

        private List<Color[]> ExtractTiles(Color[] colorInput, int textureWidth, int textureHeight, int sizeX, int sizeY)
        {
            var tiledOutput = new List<Color[]>();

            for (var y = 0; y <= textureHeight - sizeY; y += sizeY)
                for (var x = 0; x <= textureWidth - sizeX; x += sizeX)
                {
                    var colorTile = new Color[sizeX * sizeY];

                    for (var height = 0; height < sizeY; height++)
                        for (var width = 0; width < sizeX; width++)
                            colorTile[width + sizeX * height] = (colorInput[x + width + (y + height) * textureWidth]);

                    tiledOutput.Add(colorTile);
                }

            return tiledOutput;
        }

        private List<Color[]> RemoveDuplicates(List<Color[]> tileList, ref int[,,] tileMap)
        {
            var outputTileList = new List<Color[]>();

            for (var i = 0; i < tileList.Count; i++)
            {
                tileMap[i % tileMap.GetLength(0), i / tileMap.GetLength(0), 0] = outputTileList.Count;

                //look if there is already one
                var used = false;
                for (var y = 0; y < outputTileList.Count; y++)
                {
                    // is the tile already in the tile list
                    if (ColorArrayIsEqual(tileList[i], outputTileList[y]))
                    {
                        used = true;
                        tileMap[i % tileMap.GetLength(0), i / tileMap.GetLength(0), 0] = y;
                        break;
                    }
                }

                if (!used)
                    outputTileList.Add(tileList[i]);
            }

            return outputTileList;
        }

        private bool ColorArrayIsEqual(Color[] first, Color[] second)
        {
            if (first.Length != second.Length)
                return false;

            for (var i = 0; i < first.Length; i++)
                if (first[i] != second[i])
                    return false;

            return true;
        }

        private void LoadOutput()
        {
            _outputTexture = RenderTileTexture(Game1.Graphics.GraphicsDevice, _sprTiled, _tileSize.X, _tileSize.Y, _distance);
            _outputTextureRemoved = RenderTileTexture(Game1.Graphics.GraphicsDevice, _sprTiledRemoved, _tileSize.X, _tileSize.Y, _distance);
            RenderUntiledTexture(Game1.Graphics.GraphicsDevice);
        }

        private Texture2D RenderTileTexture(GraphicsDevice graphicDevice, List<Texture2D> tiledInput, int tileWidth, int tileHeight, Point tilePadding)
        {
            try
            {
                var newTextureWidth = tiledInput.Count <= _maxWidth ? tiledInput.Count : _maxWidth;
                var newTextureHeight = (int)Math.Ceiling((double)tiledInput.Count / (double)_maxWidth);

                var sizeWidth = (tileWidth + tilePadding.X) * newTextureWidth;
                var sizeHeight = (tileHeight + tilePadding.Y) * newTextureHeight;
                _textureRenderTarget = new RenderTarget2D(graphicDevice, sizeWidth, sizeHeight);

                Game1.Graphics.GraphicsDevice.SetRenderTarget(_textureRenderTarget);
                Game1.Graphics.GraphicsDevice.Clear(Color.Transparent);
                Game1.SpriteBatch.Begin();

                for (int y = 0; y < newTextureHeight; y++)
                    for (int x = 0; x < newTextureWidth; x++)
                    {
                        if (x + newTextureWidth * y >= tiledInput.Count)
                            break;

                        //draw the tile
                        Game1.SpriteBatch.Draw(tiledInput[x + y * newTextureWidth], new Vector2(x * (tileWidth + tilePadding.X),
                            y * (tileHeight + tilePadding.Y)), Color.White);
                    }

                Game1.SpriteBatch.End();
                Game1.Graphics.GraphicsDevice.SetRenderTarget(null);

                return _textureRenderTarget;
            }
            catch
            {
                return null;
            }
        }

        private void RenderUntiledTexture(GraphicsDevice graphicDevice)
        {
            if (_untiledParts == null || _untiledParts.Count <= 0)
                return;

            var width = 0;
            var height = 0;

            //get height
            for (var i = 0; i < _untiledParts.Count; i++)
            {
                width += _untiledParts[i].Width;

                if (height < _untiledParts[i].Height)
                    height = _untiledParts[i].Height;
            }

            _textureRenderTarget = new RenderTarget2D(graphicDevice, width, height);

            Game1.Graphics.GraphicsDevice.SetRenderTarget(_textureRenderTarget);
            Game1.Graphics.GraphicsDevice.Clear(Color.Transparent);
            Game1.SpriteBatch.Begin();

            var pos = 0;
            for (var i = 0; i < _untiledParts.Count; i++)
            {
                //draw the tile
                Game1.SpriteBatch.Draw(_inputTexture, new Rectangle(pos, 0, _untiledParts[i].Width, _untiledParts[i].Height), _untiledParts[i], Color.White);

                pos += _untiledParts[i].Width;
            }

            Game1.SpriteBatch.End();
            Game1.Graphics.GraphicsDevice.SetRenderTarget(null);

            _outputTextureUntiled = _textureRenderTarget;
        }

        private void RemoveSelectedTile()
        {
            for (var y = 0; y < _tileMap.GetLength(1); y++)
                for (var x = 0; x < _tileMap.GetLength(0); x++)
                {
                    if (_tileMap[x, y, 0] == _selectedOutputTile)
                        _tileMap[x, y, 1] = _sprTiledRemoved.Count + 1;

                    if (_tileMap[x, y, 0] >= _selectedOutputTile)
                        _tileMap[x, y, 0]--;
                }

            _sprTiledRemoved.Add(_sprTiled[_selectedOutputTile]);
            _sprTiled.Remove(_sprTiled[_selectedOutputTile]);

            LoadOutput();
        }

        public void ButtonSaveTileset(UiElement element)
        {
#if WINDOWS
            string filePath;

            var openFileDialog = new System.Windows.Forms.SaveFileDialog();
            openFileDialog.Filter = "png files (*.png)|*.png|All files (*.*)|*.*";
            openFileDialog.FileName = _imageName;
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                filePath = openFileDialog.FileName;
            else
                return;

            // save the map
            var saveFile = File.Create(filePath);
            _outputTexture.SaveAsPng(saveFile, _outputTexture.Width, _outputTexture.Height);
            saveFile.Close();
#endif
        }

        public void ButtonSaveRemoveTileset(UiElement element)
        {
#if WINDOWS
            string filePath;

            var openFileDialog = new System.Windows.Forms.SaveFileDialog();
            openFileDialog.Filter = "png files (*.png)|*.png|All files (*.*)|*.*";
            openFileDialog.FileName = _imageName + "Deleted";
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                filePath = openFileDialog.FileName;
            else
                return;

            // save the map
            var saveFile = File.Create(filePath);
            _outputTextureRemoved.SaveAsPng(saveFile, _outputTextureRemoved.Width, _outputTextureRemoved.Height);
            saveFile.Close();
#endif
        }

        public void ButtonSaveTilemap(UiElement element)
        {
#if WINDOWS
            // open save dialog
            var openFileDialog = new System.Windows.Forms.SaveFileDialog();
            openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog.FileName = _imageName;

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                SaveTilemap(openFileDialog.FileName);
#endif
        }

        private void SaveTilemap(string path)
        {
            var writer = new StreamWriter(path);
            writer.WriteLine(_tileMap.GetLength(0));
            writer.WriteLine(_tileMap.GetLength(1));

            for (var y = 0; y < _tileMap.GetLength(1); y++)
            {
                var strLine = "";
                for (var x = 0; x < _tileMap.GetLength(0); x++)
                {
                    strLine += _tileMap[x, y, 0];
                    if (x < _tileMap.GetLength(0) - 1)
                        strLine += ",";
                }
                writer.WriteLine(strLine);
            }

            writer.Close();
        }
    }
}
