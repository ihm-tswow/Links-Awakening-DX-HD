using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.Base;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.Editor
{
    internal class TileSelectionScreen
    {
        private readonly EditorCamera _camera = new EditorCamera();

        private TileMap _tileMap;
        public int[,] SelectedTiles;
        
        private string _currentMapFileName;

        private int _currentSelection;
        private int _selectionEnd;
        private int _selectionStart;

        private bool _selecting;

        public void Load(ContentManager content)
        {
            _camera.Scale = 5;
        }

        public void Update(GameTime gameTime)
        {
            Game1.EditorUi.CurrentScreen = Values.EditorUiTileSelection;

            _tileMap = Game1.GameManager.MapManager.CurrentMap.TileMap;

            // center the tileset
            if (Game1.GameManager.MapManager.CurrentMap.MapName != _currentMapFileName)
            {
                _currentMapFileName = Game1.GameManager.MapManager.CurrentMap.MapName;

                _camera.Location.X = (int)(Game1.WindowWidth - _tileMap.SprTileset.Width * _camera.Scale) / 2;
                _camera.Location.Y = (int)(Game1.WindowHeight - _tileMap.SprTileset.Height * _camera.Scale) / 2;
            }

            var mousePosition = InputHandler.MousePosition();

            // move the tileset
            if (!InputHandler.MouseMiddleStart() && InputHandler.MouseMiddleDown())
                _camera.Location += mousePosition - InputHandler.LastMousePosition();

            // update tileset scale
            if (InputHandler.MouseWheelUp())
                _camera.Zoom(1, mousePosition);
            if (InputHandler.MouseWheelDown())
                _camera.Zoom(-1, mousePosition);

            // clamp the position of the tileset to stay inside the _camera.Scale
            var minVisible = 48;
            _camera.Location.X = (int)MathHelper.Clamp(_camera.Location.X,
                -_tileMap.SprTileset.Width * _camera.Scale + minVisible * _camera.Scale,
                Game1.WindowWidth - minVisible * _camera.Scale);
            _camera.Location.Y = (int)MathHelper.Clamp(_camera.Location.Y,
                -_tileMap.SprTileset.Height * _camera.Scale + minVisible * _camera.Scale,
                Game1.WindowHeight - minVisible * _camera.Scale);

            // update currentSelection
            if (InputHandler.MouseIntersect(new Rectangle(
                _camera.Location.X, _camera.Location.Y,
                (int)(_tileMap.SprTileset.Width * _camera.Scale),
                (int)(_tileMap.SprTileset.Height * _camera.Scale))))
            {
                _currentSelection =
                    (mousePosition.X - _camera.Location.X) / (int)(_tileMap.TileSize * _camera.Scale) % _tileMap.TileCountX +
                    (mousePosition.Y - _camera.Location.Y) / (int)(_tileMap.TileSize * _camera.Scale) * _tileMap.TileCountX;

                _selectionEnd = _currentSelection;
            }
            else
                _currentSelection = -1;

            // select a tile
            if (InputHandler.MouseLeftStart() && _currentSelection != -1)
            {
                _selecting = true;
                _selectionStart = _currentSelection;
            }

            if (InputHandler.MouseLeftReleased() && _selecting)
            {
                _selecting = false;

                // select multiple tiles
                var start = Math.Min(_selectionStart, _selectionEnd);
                var end = Math.Max(_selectionStart, _selectionEnd);
                SelectedTiles = new int[Math.Abs(
                    end % _tileMap.TileCountX - start % _tileMap.TileCountX) + 1,
                    end / _tileMap.TileCountX - start / _tileMap.TileCountX + 1];

                for (var y = start / _tileMap.TileCountX; y <= end / _tileMap.TileCountX; y++)
                    for (var x = Math.Min(start % _tileMap.TileCountX, end % _tileMap.TileCountX);
                        x <= Math.Max(start % _tileMap.TileCountX, end % _tileMap.TileCountX); x++)
                    {
                        SelectedTiles[
                            x - Math.Min(start % _tileMap.TileCountX, end % _tileMap.TileCountX),
                            y - start / _tileMap.TileCountX] = x + y * _tileMap.TileCountX;
                    }
            }
        }
        public void Draw(SpriteBatch spriteBatch, bool blurTileset)
        {
            spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointWrap, null, null, null, _camera.TransformMatrix);

            var tileset = blurTileset ? _tileMap.SprTilesetBlur : _tileMap.SprTileset;

            // draw the tiled background
            spriteBatch.Draw(Resources.SprTiledBlock, Vector2.Zero,
                new Rectangle(0, 0,
                (tileset.Width / _tileMap.TileSize) * 16,
                (tileset.Height / _tileMap.TileSize) * 16), Color.White);

            // draw the tileset
            spriteBatch.Draw(tileset, Vector2.Zero, Color.White);

            // draw the current selection
            if (_currentSelection >= 0)
                spriteBatch.Draw(Resources.SprWhite, new Rectangle(
                    _currentSelection % _tileMap.TileCountX * _tileMap.TileSize,
                    _currentSelection / _tileMap.TileCountX * _tileMap.TileSize,
                    _tileMap.TileSize, _tileMap.TileSize), Color.White * 0.5f);

            // draw the selection
            if (SelectedTiles != null)
            {
                for (var y = 0; y < SelectedTiles.GetLength(1); y++)
                    for (var x = 0; x < SelectedTiles.GetLength(0); x++)
                    {
                        if (SelectedTiles[x, y] >= 0)
                            spriteBatch.Draw(Resources.SprWhite, new Rectangle(
                                (SelectedTiles[x, y] % _tileMap.TileCountX) * _tileMap.TileSize,
                                (SelectedTiles[x, y] / _tileMap.TileCountX) * _tileMap.TileSize,
                                _tileMap.TileSize, _tileMap.TileSize), Color.Red * 0.5f);
                    }
            }

            if (_selecting)
            {
                spriteBatch.Draw(Resources.SprWhite, new Rectangle(
                    Math.Min(_selectionStart % _tileMap.TileCountX, _selectionEnd % _tileMap.TileCountX) * _tileMap.TileSize,
                    Math.Min(_selectionStart / _tileMap.TileCountX, _selectionEnd / _tileMap.TileCountX) * _tileMap.TileSize,
                    (Math.Abs(_selectionStart % _tileMap.TileCountX - _selectionEnd % _tileMap.TileCountX) + 1) * _tileMap.TileSize,
                    (Math.Abs(_selectionStart / _tileMap.TileCountX - _selectionEnd / _tileMap.TileCountX) + 1) * _tileMap.TileSize), Color.PaleVioletRed * 0.5f);
            }

            spriteBatch.End();
        }
    }
}
