using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.Base;
using ProjectZ.Base.UI;
using ProjectZ.InGame.GameObjects;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;
using Keys = Microsoft.Xna.Framework.Input.Keys;

namespace ProjectZ.Editor
{
    class ObjectEditorScreen
    {
        enum Mode
        {
            Draw,
            Edit
        }

        enum EditMode
        {
            Idle,
            Moving
        }

        enum DrawMode
        {
            Draw,
            Erase,
            Nothing
        }

        public static Dictionary<string, GameObject> EditorObjectTemplates = new Dictionary<string, GameObject>();

        private Mode _currentMode = Mode.Edit;
        private EditMode _currentEditMode = EditMode.Idle;
        private DrawMode _currentDrawMode = DrawMode.Nothing;

        private readonly ObjectSelectionScreen _objectSelectionSelectionScreen = new ObjectSelectionScreen();

        private const int MaxParameter = 15;
        private readonly UiLabel[] _uiParameterHeader = new UiLabel[MaxParameter];
        private readonly UiTextInput[] _uiParameterTextInput = new UiTextInput[MaxParameter];
        private readonly string[] _strParameter = new string[MaxParameter];

        private UiNumberInput _bezier0;
        private UiNumberInput _bezier1;
        private UiNumberInput _bezier2;
        private UiNumberInput _bezier3;
        private CubicBezier _cublicBezier;

        private GameObjectItem _selectedGameObjectItem;

        private EditorCamera _camera;

        public Point ObjectCursor;
        public Point SelectionStart;
        public Point SelectionEnd;

        private Point? LastObjectCursor;
        private Point mousePosition, mouseMapPosition;

        private Point _moveOffset;
        private Point _startMovePosition;

        public Point MouseMapPosition => new Point(
            (InputHandler.MousePosition().X - _camera.Location.X) / (int)(Values.TileSize * _camera.Scale),
            (InputHandler.MousePosition().Y - _camera.Location.Y) / (int)(Values.TileSize * _camera.Scale));

        private string _currentMapPath;

        private int _currentLayer;
        private int _replaceSelection;

        private int _leftToolbarWidth = 200;
        private int _rightToolbarWidth = 250;

        private int gridSize = 1;

        public bool DrawObjectLayer = true;
        public bool MultiSelect;

        public ObjectEditorScreen(EditorCamera camera)
        {
            _camera = camera;
        }

        public void Load()
        {
            _objectSelectionSelectionScreen.Load();
        }

        public void SetupUi(int posY)
        {
            {
                var buttonWidth = _leftToolbarWidth - 10;
                var halfButtonWidth = buttonWidth / 2 - 2;
                var buttonHeight = 30;
                var lableHeight = 20;

                //Game1.EditorUi.AddElement(new UiButton(
                //    new Rectangle(5, posY += (int)(buttonHeight * 1.5f) + 5, buttonWidth, buttonHeight),
                //    Resources.EditorFont,
                //    "add obj at selected tile", "bt1", Values.EditorUiObjectEditor, null, ui => { AddObjectsAt(); }));

                Game1.EditorUi.AddElement(new UiLabel(new Rectangle(5, posY, buttonWidth, buttonHeight),
                    Resources.EditorFont, "Mode", "bt1", Values.EditorUiObjectEditor, null));

                Game1.EditorUi.AddElement(new UiButton(
                        new Rectangle(5, posY += buttonHeight, halfButtonWidth, buttonHeight), Resources.EditorFont,
                        "Draw", "bt1", Values.EditorUiObjectEditor,
                        ui => { ((UiButton)ui).Marked = _currentMode == Mode.Draw; },
                        ui => { _currentMode = Mode.Draw; })
                { ButtonIcon = Resources.EditorIconEdit });

                Game1.EditorUi.AddElement(new UiButton(
                        new Rectangle(5 + halfButtonWidth + 4, posY, halfButtonWidth, buttonHeight),
                        Resources.EditorFont,
                        "Edit", "bt1", Values.EditorUiObjectEditor,
                        ui => { ((UiButton)ui).Marked = _currentMode == Mode.Edit; },
                        ui => { _currentMode = Mode.Edit; })
                { ButtonIcon = Resources.EditorIconSelect });

                Game1.EditorUi.AddElement(new UiLabel(
                    new Rectangle(5, posY += buttonHeight + 5, buttonWidth, buttonHeight),
                    Resources.EditorFont, "Grid", "bt1", Values.EditorUiObjectEditor, null));

                // grid size
                posY += buttonHeight;
                var gridValue = 1;
                var gridButtonWidth = (buttonWidth - 3 * 4) / 5;
                for (var i = 0; i < 5; i++)
                {
                    var gridValueLocal = gridValue;

                    Game1.EditorUi.AddElement(new UiButton(
                        new Rectangle(5 + (gridButtonWidth + 3) * i, posY, gridButtonWidth, buttonHeight),
                        Resources.EditorFont,
                        gridValue.ToString(), "bt1", Values.EditorUiObjectEditor,
                        ui => { ((UiButton)ui).Marked = gridSize == gridValueLocal; },
                        ui => { ChangeGridSteps(gridValueLocal); }));

                    gridValue *= 2;
                }

                //_cublicBezier = new CubicBezier(100, Vector2.Zero, Vector2.One);
                //Game1.EditorUi.AddElement(_bezier0 = new UiNumberInput(
                //    new Rectangle(5, posY += buttonHeight, buttonWidth, buttonHeight),
                //    Resources.EditorFont, 0, 0, 100, 1, "tx", Values.EditorUiObjectEditor, null, ui => UpdateBezierCurve()));
                //Game1.EditorUi.AddElement(_bezier1 = new UiNumberInput(
                //    new Rectangle(5, posY += buttonHeight, buttonWidth, buttonHeight),
                //    Resources.EditorFont, 0, 0, 100, 1, "tx", Values.EditorUiObjectEditor, null, ui => UpdateBezierCurve()));
                //Game1.EditorUi.AddElement(_bezier2 = new UiNumberInput(
                //    new Rectangle(5, posY += buttonHeight, buttonWidth, buttonHeight),
                //    Resources.EditorFont, 0, 0, 100, 1, "tx", Values.EditorUiObjectEditor, null, ui => UpdateBezierCurve()));
                //Game1.EditorUi.AddElement(_bezier3 = new UiNumberInput(
                //    new Rectangle(5, posY += buttonHeight, buttonWidth, buttonHeight),
                //    Resources.EditorFont, 0, 0, 100, 1, "tx", Values.EditorUiObjectEditor, null, ui => UpdateBezierCurve()));
            }

            {
                var buttonWidth = _rightToolbarWidth - 10;
                var buttonHeight = 30;
                var buttonHeightBig = 50;
                var lableHeight = 20;

                // right background
                Game1.EditorUi.AddElement(new UiRectangle(Rectangle.Empty, "left", Values.EditorUiObjectEditor,
                    Values.ColorBackgroundLight, Color.White,
                    ui =>
                    {
                        ui.Rectangle = new Rectangle(Game1.WindowWidth - _rightToolbarWidth, Values.ToolBarHeight,
                            _rightToolbarWidth,
                            Game1.WindowHeight - Values.ToolBarHeight);
                    }));

                var leftPosition = Game1.WindowWidth - buttonWidth - 5;
                posY = Values.ToolBarHeight + 5;

                for (var i = 0; i < MaxParameter; i++)
                {
                    var i1 = i;

                    var textInputHeight = i >= 2 ? buttonHeightBig : buttonHeight;

                    _uiParameterHeader[i] = new UiLabel(
                        new Rectangle(leftPosition, posY += textInputHeight + 5, buttonWidth, lableHeight), "",
                        Values.EditorUiObjectEditor)
                    {
                        SizeUpdate = ui =>
                        {
                            ui.Rectangle = new Rectangle(Game1.WindowWidth - buttonWidth - 5, ui.Rectangle.Y,
                                ui.Rectangle.Width, ui.Rectangle.Height);
                            ((UiLabel)ui).UpdateLabelPosition();
                        }
                    };

                    _uiParameterTextInput[i] = new UiTextInput(
                        new Rectangle(leftPosition, posY += lableHeight, buttonWidth, textInputHeight),
                        Resources.EditorFontMonoSpace,
                        100, "objectparameter", Values.EditorUiObjectEditor, null, ui =>
                        {
                            _strParameter[i1] = ((UiTextInput)ui).StrValue;
                            UpdateObjectParameter();
                        })
                    {
                        SizeUpdate = ui =>
                        {
                            ui.Rectangle = new Rectangle(Game1.WindowWidth - buttonWidth - 5, ui.Rectangle.Y,
                                ui.Rectangle.Width, ui.Rectangle.Height);
                        }
                    };
                }

                // add the text fields
                foreach (var element in _uiParameterTextInput)
                    Game1.EditorUi.AddElement(element);

                // add the labels
                foreach (var element in _uiParameterHeader)
                    Game1.EditorUi.AddElement(element);
            }
        }

        //private void UpdateBezierCurve()
        //{
        //    _cublicBezier.FirstPoint = new Vector2(_bezier0.Value / 100.0f, _bezier1.Value / 100.0f);
        //    _cublicBezier.SecondPoint = new Vector2(_bezier2.Value / 100.0f, _bezier3.Value / 100.0f);
        //}

        public void UpdateObjectSelection(GameTime gameTime)
        {
            // update object selection
            _objectSelectionSelectionScreen.Update(gameTime);
        }

        public void Update(GameTime gameTime)
        {
            Game1.EditorUi.CurrentScreen = Values.EditorUiObjectEditor;

            mousePosition = InputHandler.MousePosition();

            mouseMapPosition = GetMapPosition(mousePosition);

            ObjectCursor = GetGriddedPosition(mouseMapPosition);

            if (_currentMode == Mode.Edit)
                UpdateEditMode();
            else
                UpdateDrawMode();

            LastObjectCursor = ObjectCursor;
        }

        public void UpdateDrawMode()
        {
            // draw
            if (!MultiSelect)
                SelectionStart = ObjectCursor;

            SelectionEnd = ObjectCursor;

            // start to draw or to erase
            if (InsideField() && _currentDrawMode == DrawMode.Nothing)
            {
                if (InputHandler.MouseLeftStart())
                    _currentDrawMode = DrawMode.Draw;
                else if (InputHandler.MouseRightStart())
                    _currentDrawMode = DrawMode.Erase;

                LastObjectCursor = null;
            }

            if (_currentDrawMode == DrawMode.Draw && (InputHandler.MouseLeftDown() || InputHandler.MouseLeftReleased()) ||
                _currentDrawMode == DrawMode.Erase && (InputHandler.MouseRightDown() || InputHandler.MouseRightReleased()))
            {
                if (InputHandler.KeyDown(Keys.LeftAlt) &&
                    (_currentDrawMode == DrawMode.Draw && InputHandler.MouseLeftDown() ||
                     _currentDrawMode == DrawMode.Erase && InputHandler.MouseRightDown()))
                {
                    MultiSelect = true;
                }
                else
                {
                    // no longer in multiselect
                    if (MultiSelect)
                    {
                        MultiSelect = false;
                        FillMultiSelection();
                    }
                    else
                    {
                        // only draw when the cursor was moved or drawing was just startet
                        if (LastObjectCursor != ObjectCursor)
                            FillSelection();
                    }
                }
            }
            else
            {
                MultiSelect = false;
                _currentDrawMode = DrawMode.Nothing;
            }
        }

        public void UpdateEditMode()
        {
            // select the current object
            if (InsideField() && InputHandler.MouseLeftStart())
            {
                GetSingleSelectedObject();

                if (_selectedGameObjectItem != null)
                {
                    var mapPosition = GetMapPosition(mousePosition);
                    _startMovePosition = GetGriddedPosition(mapPosition);

                    // what was this for???
                    _moveOffset = GetGriddedPosition(new Point(
                        mapPosition.X - (int)_selectedGameObjectItem.Parameter[1],
                        mapPosition.Y - (int)_selectedGameObjectItem.Parameter[2]));

                    _moveOffset = new Point(
                        _moveOffset.X * (Values.TileSize / gridSize),
                        _moveOffset.Y * (Values.TileSize / gridSize));

                    _currentEditMode = EditMode.Moving;
                }
            }

            if (InputHandler.MouseLeftReleased())
            {
                _currentEditMode = EditMode.Idle;

                // update parameter after the position was changed
                UpdateSelectedObjectParameters();
            }

            // update the location of the object to move
            if (_currentEditMode == EditMode.Moving)
            {
                var scaledOffset = new Point(
                    (int)(_moveOffset.X * _camera.Scale),
                    (int)(_moveOffset.Y * _camera.Scale));

                var griddedPosition = GetGriddedPosition(GetMapPosition(mousePosition - scaledOffset));

                if (_startMovePosition != griddedPosition)
                {
                    _startMovePosition = griddedPosition;
                    _selectedGameObjectItem.Parameter[1] = griddedPosition.X * Values.TileSize / gridSize;
                    _selectedGameObjectItem.Parameter[2] = griddedPosition.Y * Values.TileSize / gridSize;
                }
            }
        }

        public void DrawObjectSelection(SpriteBatch spriteBatch)
        {
            _objectSelectionSelectionScreen.Draw(spriteBatch);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // draw all the objects
            if (DrawObjectLayer)
            {
                foreach (var gameObjItem in Game1.GameManager.MapManager.CurrentMap.Objects.ObjectList)
                {
                    // draw the object
                    if (EditorObjectTemplates.ContainsKey(gameObjItem.Index))
                    {
                        var gameObject = EditorObjectTemplates[gameObjItem.Index];
                        var scaledSize = new Vector2(gameObject.EditorIconSource.Width, gameObject.EditorIconSource.Height) * gameObject.EditorIconScale;
                        var position = new Vector2(
                            (int)gameObjItem.Parameter[1] + (scaledSize.X > 16 ? 0 : 8 - scaledSize.X / 2),
                            (int)gameObjItem.Parameter[2] + (scaledSize.Y > 16 ? 0 : 8 - scaledSize.Y / 2));
                        gameObject.DrawEditor(spriteBatch, position);

                        // overlay red on the selected object
                        if (_selectedGameObjectItem == gameObjItem)
                            spriteBatch.Draw(Resources.SprWhite,
                                new Rectangle((int)position.X, (int)position.Y, (int)scaledSize.X, (int)scaledSize.Y), Color.Red * 0.25f);
                    }
                }
            }


            if (_currentMode == Mode.Draw)
            {
                // draw the selection
                if (MultiSelect)
                {
                    var left = Math.Min(SelectionStart.X, SelectionEnd.X);
                    var right = Math.Max(SelectionStart.X, SelectionEnd.X);
                    var top = Math.Min(SelectionStart.Y, SelectionEnd.Y);
                    var down = Math.Max(SelectionStart.Y, SelectionEnd.Y);

                    spriteBatch.Draw(Resources.SprWhite,
                        new Rectangle(
                            left * Values.TileSize / gridSize,
                            top * Values.TileSize / gridSize,
                            (right - left + 1) * Values.TileSize / gridSize,
                            (down - top + 1) * Values.TileSize / gridSize), Color.White * 0.5f);
                }

                // draw the cursor
                spriteBatch.Draw(Resources.SprWhite,
                    new Rectangle(
                        ObjectCursor.X * Values.TileSize / gridSize,
                        ObjectCursor.Y * Values.TileSize / gridSize,
                        Values.TileSize / gridSize,
                        Values.TileSize / gridSize), Color.Red * 0.75f);
            }
        }

        public void DrawTop(SpriteBatch spriteBatch)
        {
            // draw the selected object above the blured layer
            var rectangle = new Rectangle(
                0, Game1.WindowHeight - _leftToolbarWidth, _leftToolbarWidth, _leftToolbarWidth);

            _objectSelectionSelectionScreen.DrawSelectedObject(spriteBatch, rectangle);

            //var curveSize = 200;
            //var curvePosition = new Vector2(300, 300);
            
            //for (var i = 0; i < _cublicBezier.Data.Length; i++)
            //{
            //    spriteBatch.Draw(Resources.SprWhite, new Vector2(
            //        curvePosition.X + (i / (float)(_cublicBezier.Data.Length - 1)) * curveSize,
            //        curvePosition.Y - _cublicBezier.Data[i] * curveSize) - new Vector2(1, 1), 
            //        new Rectangle(0, 0, 3, 3), Color.Green);
            //}

            //var smallCurveSize = 450;
            //for (var i = 0; i < smallCurveSize; i++)
            //{
            //    var percentage = i / (float)(smallCurveSize - 1);
            //    var curve = _cublicBezier.EvaluateX(percentage);
            //    spriteBatch.Draw(Resources.SprWhite, new Vector2(curvePosition.X + percentage * curveSize, curvePosition.Y - curve * curveSize), Color.Red);
            //}

            //for (var i = 0; i < curveSize; i++)
            //{
            //    var percentage = i / (float)(curveSize - 1);
            //    var curve = _cublicBezier.EvaluatePosition(percentage);
            //    spriteBatch.Draw(Resources.SprWhite, new Vector2(curvePosition.X + curve.X * curveSize, curvePosition.Y - curve.Y * curveSize), Color.White);
            //}
            
            //spriteBatch.Draw(Resources.SprWhite, curvePosition + 
            //    new Vector2(_cublicBezier.FirstPoint.X, -_cublicBezier.FirstPoint.Y) * curveSize - new Vector2(1, 1), new Rectangle(0, 0, 3, 3), Color.Blue);
            //spriteBatch.Draw(Resources.SprWhite, curvePosition + 
            //    new Vector2(_cublicBezier.SecondPoint.X, -_cublicBezier.SecondPoint.Y) * curveSize - new Vector2(1, 1), new Rectangle(0, 0, 3, 3), Color.Red);

        }

        public bool InsideField()
        {
            return InputHandler.MouseIntersect(new Rectangle(
                _leftToolbarWidth, Values.ToolBarHeight,
                Game1.WindowWidth - _leftToolbarWidth - _rightToolbarWidth,
                Game1.WindowHeight - Values.ToolBarHeight));
        }

        private void GetSingleSelectedObject()
        {
            _selectedGameObjectItem = GetSelectedGameObject(mouseMapPosition, _selectedGameObjectItem);

            UpdateSelectedObjectParameters();
        }

        public void RemoveObject(Rectangle deleteRectangle)
        {
            var currentMap = Game1.GameManager.MapManager.CurrentMap;

            // remove one object that collides with the rectangle
            for (var i = 0; i < currentMap.Objects.ObjectList.Count; i++)
            {
                var objectPosition = GetObjectPosition(currentMap.Objects.ObjectList[i], true);
                var objTemplate = EditorObjectTemplates[currentMap.Objects.ObjectList[i].Index];
                var scaledSize = new Vector2(objTemplate.EditorIconSource.Width, objTemplate.EditorIconSource.Height) * objTemplate.EditorIconScale;

                if (objectPosition.X < deleteRectangle.X + deleteRectangle.Width &&
                    deleteRectangle.X < objectPosition.X + scaledSize.X &&
                    objectPosition.Y < deleteRectangle.Y + deleteRectangle.Height &&
                    deleteRectangle.Y < objectPosition.Y + scaledSize.Y)
                {
                    currentMap.Objects.ObjectList.Remove(currentMap.Objects.ObjectList[i]);
                    return;
                }
            }
        }

        public List<GameObjectItem> GetGameObjectsAt(Point position, bool selectMode)
        {
            var objectList = new List<GameObjectItem>();

            // search for the objects at the given position and add the to the list
            foreach (var gameObject in Game1.GameManager.MapManager.CurrentMap.Objects.ObjectList)
            {
                // @HACK
                // maybe the ObjectList wasn't that good of an idea
                if (ObjectContainsPosition(gameObject, position, selectMode))
                    objectList.Add(gameObject);
            }

            return objectList;
        }

        private Vector2 GetObjectPosition(GameObjectItem gameObjectItem, bool selectMode)
        {
            // calculate the position of the object
            if (!selectMode)
                return new Vector2((int)gameObjectItem.Parameter[1], (int)gameObjectItem.Parameter[2]);

            var objTemplate = EditorObjectTemplates[gameObjectItem.Index];
            var scaledSize = new Vector2(objTemplate.EditorIconSource.Width, objTemplate.EditorIconSource.Height) * objTemplate.EditorIconScale;

            return new Vector2(
                (int)gameObjectItem.Parameter[1] + (scaledSize.X > 16 ? 0 : 8 - scaledSize.X / 2),
                (int)gameObjectItem.Parameter[2] + (scaledSize.Y > 16 ? 0 : 8 - scaledSize.Y / 2));
        }

        private bool ObjectContainsPosition(GameObjectItem gameObjectItem, Point position, bool selectMode)
        {
            var objectPosition = GetObjectPosition(gameObjectItem, selectMode);

            var objTemplate = EditorObjectTemplates[gameObjectItem.Index];
            var scaledSize = new Vector2(objTemplate.EditorIconSource.Width, objTemplate.EditorIconSource.Height) * objTemplate.EditorIconScale;

            return objectPosition.X <= position.X && position.X < objectPosition.X + scaledSize.X &&
                   objectPosition.Y <= position.Y && position.Y < objectPosition.Y + scaledSize.Y;
        }

        public string ObjectToString(object stringObject)
        {
            if (stringObject is Rectangle rectangle)
                return rectangle.X + "." + rectangle.Y + "." + rectangle.Width + "." + rectangle.Height;
            if (stringObject is float)
                return ((float)stringObject).ToString(CultureInfo.InvariantCulture);

            return stringObject.ToString();
        }

        private void UpdateSelectedObjectParameters()
        {
            if (_selectedGameObjectItem == null)
            {
                for (var i = 0; i < MaxParameter; i++)
                {
                    _uiParameterHeader[i].IsVisible = false;
                    _uiParameterTextInput[i].IsVisible = false;
                }

                return;
            }

            var objectParameter = GameObjectTemplates.GameObjectParameter[_selectedGameObjectItem.Index];

            for (var i = 1; i <= MaxParameter; i++)
            {
                if (objectParameter.Length > i)
                {
                    _uiParameterHeader[i - 1].Label = objectParameter[i].Name;

                    _strParameter[i - 1] = "";
                    if (_selectedGameObjectItem.Parameter[i] != null)
                        _strParameter[i - 1] = ObjectToString(_selectedGameObjectItem.Parameter[i]);

                    _uiParameterTextInput[i - 1].StrValue = _strParameter[i - 1];
                    _uiParameterTextInput[i - 1].InputType = GameObjectTemplates.GameObjectParameter[_selectedGameObjectItem.Index][i].ParameterType;
                }

                // hide the empty ui elements
                _uiParameterHeader[i - 1].IsVisible = objectParameter.Length > i;
                _uiParameterTextInput[i - 1].IsVisible = objectParameter.Length > i;
            }

            _objectSelectionSelectionScreen.SelectObject(_selectedGameObjectItem.Index);
            _objectSelectionSelectionScreen.SelectedObjectParameter = _selectedGameObjectItem.Parameter;
        }

        private void UpdateObjectParameter()
        {
            // set the parameter of the object
            if (_selectedGameObjectItem == null) return;

            for (var i = 1; i < _selectedGameObjectItem.Parameter.Length; i++)
            {
                var parameterType = GameObjectTemplates.GameObjectParameter[_selectedGameObjectItem.Index][i].ParameterType;
                var parameter = MapData.ConvertToObject(_strParameter[i - 1], parameterType);

                if (parameter != null)
                    _selectedGameObjectItem.Parameter[i] = parameter;
            }
        }

        private string GetObjectIndex()
        {
            return _currentDrawMode == DrawMode.Draw ? _objectSelectionSelectionScreen.SelectedObjectIndex : null;
        }

        private object[] GetObjectParameter()
        {
            return _objectSelectionSelectionScreen.SelectedObjectParameter;
        }

        private void FillMultiSelection()
        {
            var left = Math.Min(SelectionStart.X, SelectionEnd.X);
            var right = Math.Max(SelectionStart.X, SelectionEnd.X);
            var top = Math.Min(SelectionStart.Y, SelectionEnd.Y);
            var down = Math.Max(SelectionStart.Y, SelectionEnd.Y);

            for (var y = top; y <= down; y++)
                for (var x = left; x <= right; x++)
                {
                    SetMapObject(
                        x * Values.TileSize / gridSize,
                        y * Values.TileSize / gridSize, _currentLayer, GetObjectIndex(), GetObjectParameter());
                }
        }

        private void FillSelection()
        {
            // draw or delete
            SetMapObject(
                ObjectCursor.X * Values.TileSize / gridSize,
                ObjectCursor.Y * Values.TileSize / gridSize, _currentLayer, GetObjectIndex(), GetObjectParameter());
        }

        private void SetMapObject(int x, int y, int z, string index, object[] parameter)
        {
            // remove the object
            if (index == null)
            {
                RemoveObject(new Rectangle(x, y, Values.TileSize / gridSize, Values.TileSize / gridSize));
                return;
            }

            // create a clone of the parameter
            var objParameter = MapData.GetParameterArray(index);

            objParameter[1] = x;
            objParameter[2] = y;

            // this does only make a shallow copy, but the editor does not even support the editing of arrays in the parameters
            // so this should be okay as long the editor does not allow this
            for (var i = 3; i < parameter?.Length; i++)
                objParameter[i] = parameter[i];

            AddObjectToMap(index, objParameter);
        }

        public Point GetMapPosition(Point inputPosition)
        {
            return new Point(
                (int)((inputPosition.X - _camera.Location.X) / _camera.Scale),
                (int)((inputPosition.Y - _camera.Location.Y) / _camera.Scale));
        }

        public Point GetGriddedPosition(Point inputPosition)
        {
            var position = new Point(
                inputPosition.X / (Values.TileSize / gridSize),
                inputPosition.Y / (Values.TileSize / gridSize));

            // fix
            if (inputPosition.X < 0)
                position.X--;
            if (inputPosition.Y < 0)
                position.Y--;

            return position;
        }

        private void ChangeGridSteps(int scale)
        {
            gridSize = scale;
        }

        public void AddObjectToMap(string index, object[] parameter)
        {
            if (index == null) return;

            // only add the object if there is currently not the same object on the position
            var goAtPosition = GetGameObjectsAt(new Point((int)parameter[1], (int)parameter[2]), false);
            foreach (var gameObjects in goAtPosition)
                if (gameObjects.Index == index &&
                    (int)gameObjects.Parameter[1] == (int)parameter[1] &&
                    (int)gameObjects.Parameter[2] == (int)parameter[2])
                    return;

            // add the object
            Game1.GameManager.MapManager.CurrentMap.Objects.ObjectList.Add(new GameObjectItem(index, parameter));
        }

        public GameObjectItem GetSelectedGameObject(Point position, GameObjectItem startSelection)
        {
            var objectsAtPosition = GetGameObjectsAt(position, true);

            if (objectsAtPosition.Count <= 0)
                return null;

            // @Hack
            // finds the start position of the "startSelection"-GameObjectItem
            // so it is possible to switch between objects at the same position
            var startPosition = 0;
            if (startSelection != null)
                for (var i = 0; i < objectsAtPosition.Count; i++)
                    if (objectsAtPosition[i] == startSelection)
                    {
                        startPosition = i + 1;
                        break;
                    }

            var index = startPosition % objectsAtPosition.Count;
            return objectsAtPosition[index];
        }

        public static void OffsetObjects(Map map, int offsetX, int offsetY)
        {
            foreach (var gameObject in map.Objects.ObjectList)
            {
                gameObject.Parameter[1] = (int)gameObject.Parameter[1] + offsetX;
                gameObject.Parameter[2] = (int)gameObject.Parameter[2] + offsetY;
            }
        }
    }
}
