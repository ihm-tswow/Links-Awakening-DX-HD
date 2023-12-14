using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ProjectZ.Base;
using ProjectZ.Base.UI;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Screens;
using ProjectZ.InGame.Things;
using System;

namespace ProjectZ.Editor
{
    internal class MapEditorScreen : Screen
    {
        public enum EditorModes
        {
            TileMode,
            ObjectMode,
            DigMode,
            MusicMode
        }

        public Vector2 MousePixelPosition => new Vector2(
            (InputHandler.MousePosition().X - _camera.Location.X) / _camera.Scale,
            (InputHandler.MousePosition().Y - _camera.Location.Y) / _camera.Scale);

        public Point MouseMapPosition => new Point(
            (InputHandler.MousePosition().X - _camera.Location.X) / (int)(Values.TileSize * _camera.Scale),
            (InputHandler.MousePosition().Y - _camera.Location.Y) / (int)(Values.TileSize * _camera.Scale));

        public bool ShowGrid;

        private EditorModes _currentMode = EditorModes.ObjectMode;

        private readonly EditorCamera _camera = new EditorCamera();

        private readonly TileEditorScreen _tileEditorScreen;
        private readonly ObjectEditorScreen _objectEditorScreen;
        private readonly DigMapEditor _digMapEditor;
        private readonly MusicTileEditor _musicTileEditor;

        private UiNumberInput _niOffsetX;
        private UiNumberInput _niOffsetY;

        private Point _mousePosition;

        private int _mapOffsetX = 0;
        private int _mapOffsetY = 0;

        private string _currentMapName;
        private int _toolBarWidth = 200;
        private bool _showTiles = true;
        private bool _showObjects = true;
        private bool _shiftDown;

        public MapEditorScreen(string screenId) : base(screenId)
        {
            _tileEditorScreen = new TileEditorScreen(_camera);
            _objectEditorScreen = new ObjectEditorScreen(_camera);
            _digMapEditor = new DigMapEditor(_camera);
            _musicTileEditor = new MusicTileEditor(_camera);
        }

        public override void Load(ContentManager content)
        {
            _tileEditorScreen.Load(content);
            _objectEditorScreen.Load();

            SetupUi();
        }

        public void SetupUi()
        {
            var buttonWidth = _toolBarWidth - 10;
            var buttonWidthHalf = (buttonWidth - 4) / 2;
            var buttonHeight = 30;
            var lableHeight = 20;
            var buttonQWidth = _toolBarWidth - 15 - buttonHeight;
            var posY = Values.ToolBarHeight + 5;
            var dist = 4;
            var bigDist = 16;

            var strScreenName = $"{Values.EditorUiTileEditor}:{Values.EditorUiObjectEditor}:{Values.EditorUiDigTileEditor}:{Values.EditorUiMusicTileEditor}";

            // left background
            Game1.EditorUi.AddElement(new UiRectangle(Rectangle.Empty, "left", strScreenName,
                Values.ColorBackgroundLight, Color.White,
                ui =>
                {
                    ui.Rectangle = new Rectangle(0, Values.ToolBarHeight, _toolBarWidth,
                        Game1.WindowHeight - Values.ToolBarHeight);
                }));

            Game1.EditorUi.AddElement(new UiButton(new Rectangle(5, posY, buttonWidth, buttonHeight),
                Resources.EditorFont,
                "Load", "", strScreenName, null, ui => SaveLoadMap.LoadMap(Game1.GameManager.MapManager.CurrentMap)));

            Game1.EditorUi.AddElement(new UiButton(new Rectangle(5, posY += buttonHeight + dist, buttonWidth, buttonHeight),
                Resources.EditorFont,
                "Save as...", "", strScreenName, null, ui => SaveLoadMap.SaveMapDialog(Game1.GameManager.MapManager.CurrentMap)));

            Game1.EditorUi.AddElement(new UiButton(new Rectangle(5, posY += buttonHeight + dist, buttonWidth, buttonHeight),
                Resources.EditorFont,
                "Save...", "", strScreenName, null, ui => SaveLoadMap.SaveMap(Game1.GameManager.MapManager.CurrentMap)));

            Game1.EditorUi.AddElement(new UiButton(new Rectangle(5, posY += buttonHeight + dist, buttonWidth, buttonHeight),
                Resources.EditorFont,
                "Update Maps", "", strScreenName, null, ui => SaveLoadMap.UpdateMaps()));

            // map offset
            Game1.EditorUi.AddElement(_niOffsetX = new UiNumberInput(
                new Rectangle(5, posY += buttonHeight + dist, buttonWidthHalf, buttonHeight),
                Resources.EditorFont, _mapOffsetX, -16, 16, 1, "", strScreenName, null, NumberInputChangeMapOffsetX));
            Game1.EditorUi.AddElement(_niOffsetY = new UiNumberInput(
                new Rectangle(5 + buttonWidthHalf + dist, posY, buttonWidthHalf, buttonHeight),
                Resources.EditorFont, _mapOffsetX, -16, 16, 1, "", strScreenName, null, NumberInputChangeMapOffsetY));

            Game1.EditorUi.AddElement(new UiNumberInput(
                new Rectangle(5, posY += buttonHeight + dist, buttonWidthHalf, buttonHeight),
                Resources.EditorFont, _mapOffsetX, -16, 16, 1, "", strScreenName, null, NumberInputChangeOffsetX));
            Game1.EditorUi.AddElement(new UiNumberInput(
                new Rectangle(5 + buttonWidthHalf + dist, posY, buttonWidthHalf, buttonHeight),
                Resources.EditorFont, _mapOffsetX, -16, 16, 1, "", strScreenName, null, NumberInputChangeOffsetY));
            Game1.EditorUi.AddElement(new UiButton(new Rectangle(5, posY += buttonHeight + dist, buttonWidth, buttonHeight),
                Resources.EditorFont, "Offset Map", "", strScreenName, null, ButtonPressedOffsetMap));

            // show grid button
            Game1.EditorUi.AddElement(new UiCheckBox(
                new Rectangle(5, posY += buttonHeight + bigDist, buttonWidth, buttonHeight), Resources.EditorFont,
                "show grid", "cb", strScreenName, false, null,
                ui => { ShowGrid = ((UiCheckBox)ui).CurrentState; }));

            // tile/object mode switch
            Game1.EditorUi.AddElement(new UiButton(new Rectangle(5, posY += buttonHeight + bigDist, buttonQWidth, buttonHeight),
                Resources.EditorFont, "Tiles", "", strScreenName,
                element => ((UiButton)element).Marked = _currentMode == EditorModes.TileMode,
                element => _currentMode = EditorModes.TileMode));

            Game1.EditorUi.AddElement(new UiButton(new Rectangle(5 + buttonQWidth + dist, posY, buttonHeight, buttonHeight),
                    Resources.EditorFont, "", "bt1", strScreenName, null, ButtonUpdateTilesVisibility)
            { ButtonIcon = Resources.EditorEyeOpen });

            Game1.EditorUi.AddElement(new UiButton(new Rectangle(5, posY += buttonHeight + dist, buttonQWidth, buttonHeight),
                Resources.EditorFont, "Objects", "", strScreenName,
                element => ((UiButton)element).Marked = _currentMode == EditorModes.ObjectMode,
                element => _currentMode = EditorModes.ObjectMode));

            Game1.EditorUi.AddElement(new UiButton(new Rectangle(5 + buttonQWidth + dist, posY, buttonHeight, buttonHeight),
                    Resources.EditorFont, "", "bt1", strScreenName, null, ButtonUpdateObjectsVisibility)
            { ButtonIcon = Resources.EditorEyeOpen });

            Game1.EditorUi.AddElement(new UiButton(new Rectangle(5, posY += buttonHeight + dist, buttonWidth, buttonHeight),
                Resources.EditorFont, "Dig Map", "", strScreenName,
                element => ((UiButton)element).Marked = _currentMode == EditorModes.DigMode,
                element => _currentMode = EditorModes.DigMode));

            Game1.EditorUi.AddElement(new UiButton(new Rectangle(5, posY += buttonHeight + dist, buttonWidth, buttonHeight),
                Resources.EditorFont, "Music", "", strScreenName,
                element => ((UiButton)element).Marked = _currentMode == EditorModes.MusicMode,
                element => _currentMode = EditorModes.MusicMode));

            posY += buttonHeight + bigDist;

            // load the ui of the tile editor
            _tileEditorScreen.SetupUi(posY);

            // load the ui of the object editor
            _objectEditorScreen.SetupUi(posY);

            _digMapEditor.SetUpUi(posY);

            // set up music ui
            _musicTileEditor.SetUpUi(posY);
        }
        
        public override void Update(GameTime gameTime)
        {
            _shiftDown = InputHandler.KeyDown(Keys.LeftControl) & InputHandler.MousePosition().X < Game1.WindowWidth - _toolBarWidth;

            // update the selection screen or the editor screen
            if (_shiftDown)
                UpdateSelectionScreen(gameTime);
            else
                UpdateEditorScreen(gameTime);
        }

        public void UpdateSelectionScreen(GameTime gameTime)
        {
            if (_currentMode == EditorModes.TileMode)
                _tileEditorScreen.UpdateTileSelection(gameTime);
            else if (_currentMode == EditorModes.ObjectMode)
                _objectEditorScreen.UpdateObjectSelection(gameTime);
        }

        public void UpdateEditorScreen(GameTime gameTime)
        {
            _mousePosition = InputHandler.MousePosition();

            // move the tileset
            if (!InputHandler.MouseMiddleStart() && InputHandler.MouseMiddleDown())
                _camera.Location += _mousePosition - InputHandler.LastMousePosition();

            // center camera after map change
            if (Game1.GameManager.MapManager.CurrentMap.MapName != _currentMapName)
            {
                _currentMapName = Game1.GameManager.MapManager.CurrentMap.MapName;
                CenterCamera();
            }

            _musicTileEditor.Map = Game1.GameManager.MapManager.CurrentMap;
            _tileEditorScreen.Map = Game1.GameManager.MapManager.CurrentMap;

            _niOffsetX.Value = Game1.GameManager.MapManager.CurrentMap.MapOffsetX;
            _niOffsetY.Value = Game1.GameManager.MapManager.CurrentMap.MapOffsetY;

            // update the tile or the object editor screen
            if (_currentMode == EditorModes.TileMode)
                _tileEditorScreen.Update(gameTime);
            else if (_currentMode == EditorModes.ObjectMode)
                _objectEditorScreen.Update(gameTime);
            else if (_currentMode == EditorModes.DigMode)
                _digMapEditor.Update(gameTime);
            else if (_currentMode == EditorModes.MusicMode)
                _musicTileEditor.Update(gameTime);

            // update tileset scale
            if (InputHandler.MouseWheelUp())
                _camera.Zoom(1, _mousePosition);
            if (InputHandler.MouseWheelDown())
                _camera.Zoom(-1, _mousePosition);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            // show the tile or the object selection screen
            if (_shiftDown)
                DrawSelectionScreen(spriteBatch);
            else
                // draw the editor screen
                DrawEditorScreen(spriteBatch);
        }

        private void DrawSelectionScreen(SpriteBatch spriteBatch)
        {
            if (_currentMode == EditorModes.TileMode)
                _tileEditorScreen.DrawTileSelection(spriteBatch);
            else if (_currentMode == EditorModes.ObjectMode)
                _objectEditorScreen.DrawObjectSelection(spriteBatch);
        }

        private void DrawEditorScreen(SpriteBatch spriteBatch)
        {
            spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointWrap, null, null, null, _camera.TransformMatrix);

            // draw the background
            spriteBatch.Draw(Resources.SprTiledBlock, new Rectangle(0, 0,
                    Game1.GameManager.MapManager.CurrentMap.MapWidth * Values.TileSize,
                    Game1.GameManager.MapManager.CurrentMap.MapHeight * Values.TileSize),
                new Rectangle(0, 0,
                    Game1.GameManager.MapManager.CurrentMap.MapWidth * 2,
                    Game1.GameManager.MapManager.CurrentMap.MapHeight * 2),
                Color.White);

            // draw the tile layers
            if (_showTiles)
                _tileEditorScreen.Draw(spriteBatch, _currentMode == EditorModes.TileMode);

            // draw the object layer
            if (_showObjects)
                _objectEditorScreen.Draw(spriteBatch);

            if (_currentMode == EditorModes.DigMode)
                _digMapEditor.Draw(spriteBatch);
            else if (_currentMode == EditorModes.MusicMode)
                _musicTileEditor.Draw(spriteBatch);

            // draw the grid
            var currentMap = Game1.GameManager.MapManager.CurrentMap;
            if (ShowGrid)
            {
                var countX = MathF.Ceiling(currentMap.TileMap.ArrayTileMap.GetLength(0) / 10.0f);
                var countY = MathF.Ceiling(currentMap.TileMap.ArrayTileMap.GetLength(1) / 8.0f);

                for (var y = 0; y < countY; y++)
                    for (var x = 0; x < countX; x++)
                        if ((y + x) % 2 == 0)
                        {
                            var sizeX = Math.Min(10, currentMap.TileMap.ArrayTileMap.GetLength(0) - x * 10);
                            var sizeY = Math.Min(8, currentMap.TileMap.ArrayTileMap.GetLength(1) - y * 8);

                            spriteBatch.Draw(Resources.SprWhite, new Rectangle(
                                    (x * 10 + currentMap.MapOffsetX) * Values.TileSize,
                                    (y * 8 + currentMap.MapOffsetY) * Values.TileSize,
                                    Values.TileSize * sizeX, Values.TileSize * sizeY), Color.White * 0.5f);
                        }
            }

            spriteBatch.End();
        }

        public override void DrawTop(SpriteBatch spriteBatch)
        {
            if (_shiftDown)
                return;

            if (_currentMode == EditorModes.TileMode)
                _tileEditorScreen.DrawTop(spriteBatch);
            else if (_currentMode == EditorModes.ObjectMode)
                _objectEditorScreen.DrawTop(spriteBatch);
        }

        public void CenterCamera()
        {
            _camera.Location.X = (int)(Game1.WindowWidth - Values.TileSize * Game1.GameManager.MapManager.CurrentMap.MapWidth * _camera.Scale) / 2;
            _camera.Location.Y = (int)(Game1.WindowHeight - Values.TileSize * Game1.GameManager.MapManager.CurrentMap.MapHeight * _camera.Scale) / 2;
        }

        public bool InsideField()
        {
            return InputHandler.MouseIntersect(new Rectangle(
                _toolBarWidth, Values.ToolBarHeight,
                Game1.WindowWidth - _toolBarWidth * 2,
                Game1.WindowHeight - Values.ToolBarHeight));
        }

        private void ButtonUpdateTilesVisibility(UiElement ui)
        {
            _showTiles = !_showTiles;
            ((UiButton)ui).ButtonIcon = _showTiles ? Resources.EditorEyeOpen : Resources.EditorEyeClosed;
        }

        private void ButtonUpdateObjectsVisibility(UiElement ui)
        {
            _showObjects = !_showObjects;
            ((UiButton)ui).ButtonIcon = _showObjects ? Resources.EditorEyeOpen : Resources.EditorEyeClosed;
        }

        private void NumberInputChangeMapOffsetX(UiElement uiElement)
        {
            Game1.GameManager.MapManager.CurrentMap.MapOffsetX = (int)((UiNumberInput)uiElement).Value;
        }

        private void NumberInputChangeMapOffsetY(UiElement uiElement)
        {
            Game1.GameManager.MapManager.CurrentMap.MapOffsetY = (int)((UiNumberInput)uiElement).Value;
        }

        private void NumberInputChangeOffsetX(UiElement uiElement)
        {
            _mapOffsetX = (int)((UiNumberInput)uiElement).Value;
        }

        private void NumberInputChangeOffsetY(UiElement uiElement)
        {
            _mapOffsetY = (int)((UiNumberInput)uiElement).Value;
        }

        private void ButtonPressedOffsetMap(UiElement uiElement)
        {
            // offset the tilemap
            _tileEditorScreen.OffsetTileMap(_mapOffsetX, _mapOffsetY);
            // offset the objects
            ObjectEditorScreen.OffsetObjects(
                Game1.GameManager.MapManager.CurrentMap, _mapOffsetX * Values.TileSize, _mapOffsetY * Values.TileSize);
        }
    }
}