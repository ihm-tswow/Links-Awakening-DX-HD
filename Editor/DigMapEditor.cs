using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ProjectZ.Base;
using ProjectZ.Base.UI;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;
using System.Collections.Generic;

namespace ProjectZ.Editor
{
    class DigMapEditor
    {
        private readonly EditorCamera _camera;

        private Dictionary<string, Color> _colorMap = new Dictionary<string, Color>();
        private Color[,] _colorData;
        private Stack<Color> _colorStack = new Stack<Color>();

        private Map CurrentMap => Game1.GameManager.MapManager.CurrentMap;
        private string LastMap;

        private string _selection = "";

        private int _tileWidth = 16;
        private int _tileHeight = 16;

        private int _leftToolbarWidth = 200;

        public DigMapEditor(EditorCamera camera)
        {
            _camera = camera;

            _colorStack.Clear();
            _colorStack.Push(Color.Black);
            _colorStack.Push(Color.Purple);
            _colorStack.Push(Color.Orange);
            _colorStack.Push(Color.Red);
            //_colorStack.Push(Color.Blue);
            //_colorStack.Push(Color.Green);

            _colorMap.Add("", Color.White * 0.5f);
            _colorMap.Add("1", Color.Green);
            _colorMap.Add("2", Color.Blue);
        }

        public void SetUpUi(int posY)
        {
            var buttonWidth = 190;
            var buttonHeight = 35;
            var distanceY = buttonHeight + 5;

            Game1.EditorUi.AddElement(new UiTextInput(new Rectangle(5, posY, buttonWidth, 50),
                Resources.EditorFontMonoSpace, 50, "Mode", Values.EditorUiDigTileEditor,
                uiElement => ((UiTextInput)uiElement).StrValue = _selection ?? "",
                uiElement => _selection = ((UiTextInput)uiElement).StrValue));

            Game1.EditorUi.AddElement(new UiButton(new Rectangle(5, posY += 55, buttonWidth, buttonHeight),
                Resources.EditorFont, "Fill Map", "button", Values.EditorUiDigTileEditor, null, FillMap));
        }

        public void Update(GameTime gameTime)
        {
            Game1.EditorUi.CurrentScreen = Values.EditorUiDigTileEditor;

            if (LastMap != CurrentMap.MapFileName)
            {
                LastMap = CurrentMap.MapFileName;
                UpdateColor();
            }

            var cursorPosition = GetTiledCursor();
            if (cursorPosition.X >= 0)
            {
                if (InputHandler.MouseRightDown() || InputHandler.KeyPressed(Keys.Space))
                    _selection = CurrentMap.DigMap[cursorPosition.X, cursorPosition.Y];
                if (InputHandler.MouseLeftDown())
                {
                    CurrentMap.DigMap[cursorPosition.X, cursorPosition.Y] = _selection;
                    UpdateColor(cursorPosition.X, cursorPosition.Y);
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            for (var y = 0; y < CurrentMap.DigMap.GetLength(1); y++)
            {
                for (var x = 0; x < CurrentMap.DigMap.GetLength(0); x++)
                {
                    var posX = _tileWidth * x;
                    var posY = _tileHeight * y;
                    var width = _tileWidth;
                    var height = _tileHeight;

                    spriteBatch.Draw(Resources.SprWhite, new Rectangle(
                        posX + (_tileWidth - width) / 2,
                        posY + (_tileHeight - height) / 2, width, height), _colorData[x, y] * 0.75f);
                }
            }

            var cursorPosition = GetTiledCursor();
            if (cursorPosition.X >= 0 && !string.IsNullOrEmpty(CurrentMap.DigMap[cursorPosition.X, cursorPosition.Y]))
            {
                var spriteSize = Resources.EditorFont.MeasureString(CurrentMap.DigMap[cursorPosition.X, cursorPosition.Y]);
                var scale = spriteSize.X > spriteSize.Y ? (_tileWidth - 2) / spriteSize.X : 1;

                spriteBatch.DrawString(Resources.EditorFont, CurrentMap.DigMap[cursorPosition.X, cursorPosition.Y],
                    new Vector2(
                        _tileWidth * cursorPosition.X + _tileWidth / 2 - (spriteSize.X / 2 * scale),
                        _tileHeight * cursorPosition.Y + _tileHeight / 2 - (spriteSize.Y / 2 * scale)),
                    Color.White, 0, Vector2.One, new Vector2(scale), SpriteEffects.None, 0);
            }
        }

        private void UpdateColor()
        {
            _colorData = new Color[CurrentMap.DigMap.GetLength(0), CurrentMap.DigMap.GetLength(1)];

            for (var y = 0; y < CurrentMap.DigMap.GetLength(1); y++)
                for (var x = 0; x < CurrentMap.DigMap.GetLength(0); x++)
                    UpdateColor(x, y);
        }

        private void UpdateColor(int x, int y)
        {
            if (_colorMap.TryGetValue(CurrentMap.DigMap[x, y], out var color))
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
                _colorMap.Add(CurrentMap.DigMap[x, y], newColor);
            }
        }

        private void FillMap(UiElement uiElement)
        {
            for (var y = 0; y < CurrentMap.DigMap.GetLength(1); y++)
                for (var x = 0; x < CurrentMap.DigMap.GetLength(0); x++)
                    CurrentMap.DigMap[x, y] = _selection;
        }

        public Point GetTiledCursor()
        {
            var _mousePosition = InputHandler.MousePosition();

            var position = new Point(
                (int)((_mousePosition.X - _camera.Location.X) / (_tileWidth * _camera.Scale)),
                (int)((_mousePosition.Y - _camera.Location.Y) / (_tileHeight * _camera.Scale)));

            // fix
            if (_mousePosition.X - _camera.Location.X < 0)
                position.X--;
            if (_mousePosition.Y - _camera.Location.Y < 0)
                position.Y--;

            if (_mousePosition.X > _leftToolbarWidth &&
                0 <= position.X && position.X < CurrentMap.DigMap.GetLength(0) &&
                0 <= position.Y && position.Y < CurrentMap.DigMap.GetLength(1))
            {
                return position;
            }

            return new Point(-1, -1);
        }
    }
}
