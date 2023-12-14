using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ProjectZ.Base;
using ProjectZ.Base.UI;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;
using System.Collections.Generic;

namespace ProjectZ.Editor
{
    class MusicTileEditor
    {
        public Map Map;

        private readonly EditorCamera _camera;

        private Dictionary<string, Color> _colorMap = new Dictionary<string, Color>();
        private Color[,] _colorData;
        private Stack<Color> _colorStack = new Stack<Color>();

        private string[,] _dataArray;
        private string _selection = "";

        private int _tileWidth = 16;
        private int _tileHeight = 16;
        //private int _tileWidth = 160;
        //private int _tileHeight = 128;

        private bool _roomMode;

        private int _leftToolbarWidth = 200;

        public MusicTileEditor(EditorCamera camera)
        {
            _camera = camera;

            _dataArray = new string[16 * 10, 16 * 8];
            for (var y = 0; y < _dataArray.GetLength(1); y++)
                for (var x = 0; x < _dataArray.GetLength(0); x++)
                    _dataArray[x, y] = "";

            UpdateColor();
        }

        private void ResetColor()
        {
            _colorStack.Clear();
            _colorStack.Push(Color.Black);
            _colorStack.Push(Color.Purple);
            _colorStack.Push(Color.Orange);
            _colorStack.Push(Color.Blue);
            _colorStack.Push(Color.Green);
            _colorStack.Push(Color.Red);
        }

        public void SetUpUi(int posY)
        {
            var buttonWidth = 190;
            var buttonHeight = 35;
            var distanceY = buttonHeight + 5;

            Game1.EditorUi.AddElement(new UiButton(new Rectangle(5, posY, buttonWidth, buttonHeight),
                Resources.EditorFont, "Load", "button", Values.EditorUiMusicTileEditor, null, uiElement => LoadFile()));

            Game1.EditorUi.AddElement(new UiButton(new Rectangle(5, posY += distanceY, buttonWidth, buttonHeight),
                Resources.EditorFont, "Save", "button", Values.EditorUiMusicTileEditor, null, uiElement => DataMapSerializer.SaveDialog(_dataArray)));

            Game1.EditorUi.AddElement(new UiCheckBox(new Rectangle(5, posY += distanceY, buttonWidth, buttonHeight),
                Resources.EditorFont, "Room", "button", Values.EditorUiMusicTileEditor, _roomMode, null, element => _roomMode = ((UiCheckBox)element).CurrentState));

            Game1.EditorUi.AddElement(new UiTextInput(new Rectangle(5, posY += distanceY, buttonWidth, 50),
                Resources.EditorFontMonoSpace, 50, "Mode", Values.EditorUiMusicTileEditor,
                uiElement => ((UiTextInput)uiElement).StrValue = _selection,
                uiElement => _selection = ((UiTextInput)uiElement).StrValue));
        }

        private void LoadFile()
        {
            DataMapSerializer.LoadDialog(ref _dataArray);

            UpdateColor();

            //_tileWidth = (Game1.GameManager.MapManager.CurrentMap.MapWidth * Values.TileSize) / _dataArray.GetLength(0);
            //_tileHeight = (Game1.GameManager.MapManager.CurrentMap.MapHeight * Values.TileSize) / _dataArray.GetLength(1);
        }

        private void UpdateColor()
        {
            ResetColor();

            _colorData = new Color[_dataArray.GetLength(0), _dataArray.GetLength(1)];

            for (var y = 0; y < _dataArray.GetLength(1); y++)
                for (var x = 0; x < _dataArray.GetLength(0); x++)
                    UpdateColor(x, y);
        }

        private void UpdateColor(int x, int y)
        {
            if (_colorMap.TryGetValue(_dataArray[x, y], out var color))
                _colorData[x, y] = color;
            else
            {
                Color newColor;

                if (_colorStack.Count > 0)
                {
                    newColor = _colorStack.Pop();
                }
                else
                {
                    newColor = new Color(
                        Game1.RandomNumber.Next(0, 256),
                        Game1.RandomNumber.Next(0, 256),
                        Game1.RandomNumber.Next(0, 256));
                }

                _colorData[x, y] = newColor;
                _colorMap.Add(_dataArray[x, y], newColor);
            }
        }

        public void Update(GameTime gameTime)
        {
            Game1.EditorUi.CurrentScreen = Values.EditorUiMusicTileEditor;

            var cursorPosition = GetTiledCursor();
            if (cursorPosition.X >= 0)
            {
                if (InputHandler.MouseRightDown() || InputHandler.KeyPressed(Keys.Space))
                    _selection = _dataArray[cursorPosition.X, cursorPosition.Y];
                if (InputHandler.MouseLeftDown())
                {
                    if (_roomMode)
                    {
                        var startX = (cursorPosition.X / 10) * 10;
                        var startY = (cursorPosition.Y / 8) * 8;

                        for (var y = startY; y < startY + 8; y++)
                            if (y < _dataArray.GetLength(1))
                                for (var x = startX; x < startX + 10; x++)
                                {
                                    if (x < _dataArray.GetLength(0))
                                    {
                                        _dataArray[x, y] = _selection;
                                        UpdateColor(x, y);
                                    }
                                }
                    }
                    else
                    {
                        _dataArray[cursorPosition.X, cursorPosition.Y] = _selection;
                        UpdateColor(cursorPosition.X, cursorPosition.Y);
                    }
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            for (var y = 0; y < _dataArray.GetLength(1); y++)
            {
                for (var x = 0; x < _dataArray.GetLength(0); x++)
                {
                    var posX = _tileWidth * x + Map.MapOffsetX * Values.TileSize;
                    var posY = _tileHeight * y + Map.MapOffsetY * Values.TileSize;
                    var width = _tileWidth;
                    var height = _tileHeight;

                    spriteBatch.Draw(Resources.SprWhite, new Rectangle(
                        posX + (_tileWidth - width) / 2,
                        posY + (_tileHeight - height) / 2, width, height), _colorData[x, y] * 0.75f);
                }
            }

            var cursorPosition = GetTiledCursor();
            if (cursorPosition.X >= 0)
                spriteBatch.DrawString(Resources.EditorFontSmallMonoSpace, _dataArray[cursorPosition.X, cursorPosition.Y],
                    new Vector2(_tileWidth * cursorPosition.X + Map.MapOffsetX * Values.TileSize + _tileWidth / 2 - 1,
                                _tileHeight * cursorPosition.Y + Map.MapOffsetY * Values.TileSize + _tileHeight / 2 - 6), Color.White);
        }

        public Point GetTiledCursor()
        {
            var _mousePosition = InputHandler.MousePosition();

            var position = new Point(
                (int)((_mousePosition.X - _camera.Location.X - Map.MapOffsetX * Values.TileSize * _camera.Scale) / (_tileWidth * _camera.Scale)),
                (int)((_mousePosition.Y - _camera.Location.Y - Map.MapOffsetY * Values.TileSize * _camera.Scale) / (_tileHeight * _camera.Scale)));

            // fix
            if (_mousePosition.X - _camera.Location.X < 0)
                position.X--;
            if (_mousePosition.Y - _camera.Location.Y < 0)
                position.Y--;

            if (_mousePosition.X > _leftToolbarWidth &&
                0 <= position.X && position.X < _dataArray.GetLength(0) &&
                0 <= position.Y && position.Y < _dataArray.GetLength(1))
            {
                return position;
            }

            return new Point(-1, -1);
        }
    }
}
