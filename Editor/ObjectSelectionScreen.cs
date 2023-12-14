using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.Editor
{
    internal class ObjectSelectionScreen
    {
        private readonly EditorCamera _camera = new EditorCamera();
        private readonly EditorCamera _singleObjectCamera = new EditorCamera();
        private Point _drawSize;

        public object[] SelectedObjectParameter;
        public string SelectedObjectIndex;

        private GameObject _selectedObject;
        private Vector2 _objectNameTextSize;
        private int _columns = 30;

        private List<string> _objectList = new List<string>();

        public ObjectSelectionScreen()
        {
            _camera.Location = new Point(400, 250);
        }

        public void Load()
        {
            foreach (var gameObjItem in GameObjectTemplates.ObjectTemplates)
            {
                if (gameObjItem.Value != null)
                    _objectList.Add(gameObjItem.Key);
                else
                {
                    // create "line break" for the object selection
                    var count = _columns - _objectList.Count % _columns;
                    for (var i = 0; i < count; i++)
                        _objectList.Add("");
                }
            }

            _drawSize = new Point(_columns * 32, (int)Math.Ceiling(_objectList.Count / (float)_columns) * 32);
        }

        public void Update(GameTime gameTime)
        {
            Game1.EditorUi.CurrentScreen = Values.EditorUiObjectSelection;

            var position = InputHandler.MousePosition();

            // update tileset scale
            if (InputHandler.MouseWheelUp() && _camera.Scale < 10)
            {
                _camera.Scale += 0.25f;
                var scale = _camera.Scale / (_camera.Scale - 0.25f);
                _camera.Location.X = InputHandler.MousePosition().X - (int)((InputHandler.MousePosition().X - _camera.Location.X) * scale);
                _camera.Location.Y = InputHandler.MousePosition().Y - (int)((InputHandler.MousePosition().Y - _camera.Location.Y) * scale);
            }
            if (InputHandler.MouseWheelDown() && _camera.Scale > 1)
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
            if (InputHandler.MouseLeftPressed(new Rectangle(
                _camera.Location.X, _camera.Location.Y,
                (int)(_drawSize.X * _camera.Scale),
                (int)(_drawSize.Y * _camera.Scale))))
            {
                var selectionIndex = (int)((position.X - _camera.Location.X) / (32 * _camera.Scale)) +
                                     (int)((position.Y - _camera.Location.Y) / (32 * _camera.Scale)) * _columns;

                if (_objectList[selectionIndex] != "" && selectionIndex < _objectList.Count)
                    SelectObject(_objectList[selectionIndex]);
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointWrap, null, null, null, _camera.TransformMatrix);

            // draw the tiled background
            spriteBatch.Draw(Resources.SprTiledBlock,
                new Rectangle(0, 0, _drawSize.X, _drawSize.Y),
                new Rectangle(0, 0, _drawSize.X / Values.TileSize * 2,
                                    _drawSize.Y / Values.TileSize * 2), Color.LightGray);

            // draw the objects
            var objIndex = 0;
            foreach (var objKey in _objectList)
            {
                if (objKey == "")
                {
                    objIndex++;
                    continue;
                }

                // draw the selection
                if (SelectedObjectIndex == objKey)
                    spriteBatch.Draw(Resources.SprWhite, new Rectangle(
                            objIndex % _columns * 32,
                            objIndex / _columns * 32, 32, 32), Color.Red * 0.5f);

                var objTemplate = ObjectEditorScreen.EditorObjectTemplates[objKey];
                objTemplate.DrawEditor(spriteBatch, new Vector2(
                    objIndex % _columns * 32 + 16 - objTemplate.EditorIconSource.Width * objTemplate.EditorIconScale / 2,
                    objIndex / _columns * 32 + 16 - objTemplate.EditorIconSource.Height * objTemplate.EditorIconScale / 2));

                objIndex++;
            }

            spriteBatch.End();

            spriteBatch.Begin();

            // draw the name of the selected object
            if (SelectedObjectIndex != null)
            {
                spriteBatch.DrawString(Resources.EditorFont, SelectedObjectIndex,
                    new Vector2(5, Game1.WindowHeight - Resources.EditorFontHeight - 5), Color.Red);
            }

            spriteBatch.End();
        }

        public void SelectObject(string objIndex)
        {
            SelectedObjectIndex = objIndex;
            SelectedObjectParameter = MapData.GetParameterArray(SelectedObjectIndex);
            _selectedObject = ObjectEditorScreen.EditorObjectTemplates[SelectedObjectIndex];

            // measure the size
            _objectNameTextSize = Resources.EditorFont.MeasureString(SelectedObjectIndex);
        }

        public void DrawSelectedObject(SpriteBatch spriteBatch, Rectangle rectangle)
        {
            rectangle.Y -= Resources.EditorFontHeight + 10;

            // draw the background
            spriteBatch.Draw(Resources.SprWhite, rectangle, Color.White * 0.25f);

            if (_selectedObject == null) return;

            // draw the name
            spriteBatch.DrawString(Resources.EditorFont, SelectedObjectIndex,
                new Vector2(rectangle.Right - _objectNameTextSize.X - 10, rectangle.Bottom + 5), Color.White);

            var scale = MathHelper.Min(
                rectangle.Width / (_selectedObject.EditorIconSource.Width * _selectedObject.EditorIconScale),
                rectangle.Height / (_selectedObject.EditorIconSource.Height * _selectedObject.EditorIconScale));

            var drawWidth = (int)(scale * _selectedObject.EditorIconSource.Width * _selectedObject.EditorIconScale);
            var drawHeight = (int)(scale * _selectedObject.EditorIconSource.Height * _selectedObject.EditorIconScale);

            _singleObjectCamera.Scale = scale;
            _singleObjectCamera.Location = new Point(
                rectangle.X + rectangle.Width / 2 - drawWidth / 2,
                rectangle.Y + rectangle.Height / 2 - drawHeight / 2);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointWrap, null, null, null, _singleObjectCamera.TransformMatrix);

            // draw the selected object
            _selectedObject.DrawEditor(spriteBatch, new Vector2(0, 0));

            spriteBatch.End();
            spriteBatch.Begin();
        }
    }
}
