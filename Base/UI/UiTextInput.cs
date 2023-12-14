using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ProjectZ.InGame.Things;

namespace ProjectZ.Base.UI
{
    public class UiTextInput : UiElement
    {
        public Type InputType;
        public float MaxLength;

        public string StrValue
        {
            get => _strValue;
            set
            {
                _strValue = value;
                _cursorIndex = StrValue.Length;
                _selectionStart = -1;
            }
        }

        private readonly UiFunction _onTextUpdate;
        private string _strValue = "";

        private int _cursorIndex;

        private int _selectionStart = -1;
        private int _selectionEnd = -1;
        private bool _mouseSelecting;

        private float _buttonDownCounter;
        private int _buttonResetTime = 35;
        private int _buttonResetInitTime = 250;

        private int _maxCharacterCount;
        private float _cursorCounter;
        private Vector2 _fontSize;

        private double _lastMouseClick;

        public UiTextInput(Rectangle rectangle, SpriteFont font, float maxLength,
            string elementId, string screen, UiFunction update, UiFunction onTextUpdate) : base(elementId, screen)
        {
            Rectangle = rectangle;
            UpdateFunction = update;
            _onTextUpdate = onTextUpdate;
            MaxLength = maxLength;
            Font = font;

            _fontSize = font.MeasureString("A");
            _maxCharacterCount = (int)((rectangle.Width - 10) / _fontSize.X);
        }

        public override void Update()
        {
            base.Update();

            _cursorCounter += Game1.DeltaTime;

            if (InputHandler.KeyDown(Keys.Left) || InputHandler.KeyDown(Keys.Right) ||
                InputHandler.KeyDown(Keys.Up) || InputHandler.KeyDown(Keys.Down) ||
                InputHandler.KeyDown(Keys.Back) || InputHandler.KeyDown(Keys.Delete))
            {
                _buttonDownCounter -= Game1.DeltaTime;
            }
            else
            {
                _buttonDownCounter = _buttonResetInitTime;
            }

            if (!Selected) return;

            var mousePosition = InputHandler.MousePosition().ToVector2();
            var newCursorIndex = SetCursor(mousePosition);
            var mouseReleased = InputHandler.MouseLeftReleased();

            if (InputHandler.MouseLeftPressed())
            {
                // set the cursor to the position the user clicked on
                if (_lastMouseClick < Game1.TotalGameTime - 550 || newCursorIndex != _cursorIndex)
                {
                    _mouseSelecting = false;
                    _lastMouseClick = Game1.TotalGameTime;
                    _cursorIndex = newCursorIndex;
                    _selectionStart = -1;
                }
                // select the word the user clicked on
                else
                {
                    _selectionStart = CursorPositionSkip(-1);
                    _selectionEnd = CursorPositionSkip(1);
                    _cursorIndex = _selectionEnd;
                }
            }
            else if (InputHandler.MouseLeftDown() &&
                     (_mouseSelecting || (_selectionStart == -1 && newCursorIndex != _cursorIndex)))
            {
                _mouseSelecting = true;
                _selectionStart = Math.Min(_cursorIndex, newCursorIndex);
                _selectionEnd = Math.Max(_cursorIndex, newCursorIndex);
            }

            if (_mouseSelecting && InputHandler.MouseLeftReleased())
            {
                _mouseSelecting = false;
                _cursorIndex = newCursorIndex;
            }

            var inputString = InputHandler.ReturnCharacter();
            if (_strValue.Length < MaxLength && inputString != "")
            {
                // delete the selection first
                if (_selectionStart != -1)
                    DeleteSelection();

                _strValue = _strValue.Substring(0, _cursorIndex) + inputString +
                            _strValue.Substring(_cursorIndex, _strValue.Length - _cursorIndex);

                _cursorIndex++;
            }

            //delete last position
            if ((InputHandler.KeyPressed(Keys.Back) || InputHandler.KeyDown(Keys.Back) &&
                 _buttonDownCounter <= 0) && _strValue.Length > 0 && _cursorIndex > 0)
            {
                // delete selection
                if (_selectionStart != -1)
                {
                    DeleteSelection();
                }
                else
                {
                    _strValue = _strValue.Remove(_cursorIndex - 1, 1);
                    _cursorIndex--;
                }

                _buttonDownCounter += _buttonResetTime;
                _cursorCounter = 0;
            }

            if ((InputHandler.KeyPressed(Keys.Delete) || InputHandler.KeyDown(Keys.Delete) &&
                 _buttonDownCounter <= 0) && _strValue.Length > 0)
            {
                // delete selection
                if (_selectionStart != -1)
                    DeleteSelection();
                else if (_cursorIndex < _strValue.Length)
                    _strValue = _strValue.Remove(_cursorIndex, 1);

                _buttonDownCounter += _buttonResetTime;
                _cursorCounter = 0;
            }

            if (InputHandler.KeyPressed(Keys.Up))
            {
                _cursorIndex = 0;
                _cursorCounter = 0;
            }

            if (InputHandler.KeyDown(Keys.Down))
            {
                _cursorIndex = _strValue.Length;
                _cursorCounter = 0;
            }

            // move the cursor to the end of the word
            if (InputHandler.KeyPressed(Keys.Left) || InputHandler.KeyDown(Keys.Left) && _buttonDownCounter <= 0)
            {
                if (InputHandler.KeyDown(Keys.LeftControl))
                    _cursorIndex = CursorPositionSkip(-1);
                else
                    MoveCursor(-1);

                if (!InputHandler.KeyDown(Keys.LeftShift))
                    _selectionStart = -1;

                ResetCursorTimer();
            }

            if (InputHandler.KeyPressed(Keys.Right) || InputHandler.KeyDown(Keys.Right) && _buttonDownCounter <= 0)
            {
                if (InputHandler.KeyDown(Keys.LeftControl))
                    _cursorIndex = CursorPositionSkip(1);
                else
                    MoveCursor(1);

                if (!InputHandler.KeyDown(Keys.LeftShift))
                    _selectionStart = -1;

                ResetCursorTimer();
            }

            if (InputType == typeof(int))
            {
                if (InputHandler.MouseWheelDown())
                    AddValue(-1);
                if (InputHandler.MouseWheelUp())
                    AddValue(1);
            }
            if (InputType == typeof(bool))
            {
                if (InputHandler.MouseWheelDown() || InputHandler.MouseWheelUp())
                    ToggleBool();
            }

            _cursorIndex = MathHelper.Clamp(_cursorIndex, 0, _strValue.Length);

            InputHandler.ResetInputState();

            _onTextUpdate?.Invoke(this);
        }

        private void DeleteSelection()
        {
            _strValue = _strValue.Remove(_selectionStart, _selectionEnd - _selectionStart);

            if (_cursorIndex > _selectionStart)
                _cursorIndex -= _selectionEnd - _selectionStart;

            _selectionStart = -1;
        }

        private int SetCursor(Vector2 position)
        {
            var textPosition = GetTextPosition();

            // cursor will be set in front or behind the character depending on where you click
            var posX = (int)((position.X - textPosition.X + _fontSize.X / 2) / _fontSize.X);
            var posY = (int)(position.Y - textPosition.Y) / (int)_fontSize.Y;

            var xIndex = MathHelper.Clamp(posX, 0, _maxCharacterCount);
            var yIndex = MathHelper.Clamp(posY, 0, _maxCharacterCount);

            var positionIndex = xIndex + yIndex * _maxCharacterCount;

            _cursorCounter = 0;
            return MathHelper.Clamp(positionIndex, 0, _strValue.Length);
        }

        private void MoveCursor(int offset)
        {
            _cursorIndex += offset;
        }

        private void ResetCursorTimer()
        {
            _buttonDownCounter += _buttonResetTime;
            _cursorCounter = 0;
        }

        /// <summary>
        /// Move the cursor in the given direction.
        /// This will try to skip over words.
        /// </summary>
        /// <param name="direction"></param>
        private int CursorPositionSkip(int direction)
        {
            if (_strValue.Length == 0)
                return 0;

            var offset = direction < 0 ? -1 : 0;
            var characterIndex = _cursorIndex + offset;
            var lastCharacter = -1;

            while (0 <= characterIndex && characterIndex < _strValue.Length)
            {
                var characterType = GetCharacterType(_strValue[characterIndex]);
                // we break if there is a new character type at the cursor position
                if (lastCharacter != -1 && characterType != 0 && lastCharacter != characterType)
                {
                    characterIndex -= offset;
                    return characterIndex;
                }
                lastCharacter = characterType;

                characterIndex += direction;
            }

            return direction < 0 ? 0 : _strValue.Length;
        }

        private int GetCharacterType(char character)
        {
            if (character == ' ')
                return 0;

            if (('a' <= character && character <= 'z') ||
                ('A' <= character && character <= 'Z') ||
                ('0' <= character && character <= '9') ||
                character == '_')
            {
                return 1;
            }

            return 2;
        }

        public void AddValue(int diff)
        {
            var converted = ConvertToType();

            if (converted == null) return;

            var intValue = (int)converted + diff;
            _strValue = intValue.ToString();
        }

        public void ToggleBool()
        {
            var converted = ConvertToType();

            if (converted == null) return;

            var boolValue = !(bool)converted;
            _strValue = boolValue.ToString();
        }

        public object ConvertToType()
        {
            object output = null;

            if (InputType == typeof(int))
            {
                int.TryParse(_strValue, out var intResult);
                output = intResult;
            }
            else if (InputType == typeof(bool))
            {
                bool.TryParse(_strValue, out var boolResult);
                output = boolResult;
            }

            return output;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Resources.SprWhite, Rectangle, BackgroundColor);

            Label = StrValue;

            // add line breaks if needed
            if (_maxCharacterCount >= 1)
            {
                var breakCount = 1;
                var length = Label.Length;
                while (length > _maxCharacterCount)
                {
                    length -= _maxCharacterCount;
                    Label = Label.Insert(breakCount * _maxCharacterCount + breakCount - 1, "\n");
                    breakCount++;
                }
            }

            // draw the text
            var textPosition = GetTextPosition();

            // draw the selection
            if (Selected && _selectionStart != -1)
            {
                var drawIndex = _selectionStart;

                // draw the selection line by line
                while (drawIndex < _selectionEnd)
                {
                    var drawIndexEnd = MathHelper.Min(
                        drawIndex + _maxCharacterCount - (drawIndex % _maxCharacterCount), _selectionEnd);
                    var drawPosition = new Vector2(
                        (drawIndex % _maxCharacterCount) * _fontSize.X,
                        (drawIndex / _maxCharacterCount) * _fontSize.Y);
                    spriteBatch.Draw(Resources.SprWhite,
                        new Rectangle(
                            (int)(textPosition.X + drawPosition.X), (int)(textPosition.Y + drawPosition.Y),
                            (drawIndexEnd - drawIndex) * (int)_fontSize.X, (int)_fontSize.Y), Color.Blue);
                    drawIndex = drawIndexEnd;
                }
            }

            spriteBatch.DrawString(Font, Label, textPosition, FontColor);

            // draw the cursor
            if (Selected && _cursorCounter % 700 < 350 && _maxCharacterCount > 0)
            {
                var offset = _cursorIndex != 0 && _cursorIndex % _maxCharacterCount == 0 ? 1 : 0;
                var cursorPosition = new Vector2(
                    textPosition.X + (_cursorIndex % _maxCharacterCount + offset * _maxCharacterCount) * _fontSize.X - 3,
                    textPosition.Y + (_cursorIndex / _maxCharacterCount - offset) * _fontSize.Y);

                spriteBatch.DrawString(Font, "|", cursorPosition, FontColor);
            }
        }

        private Vector2 GetTextPosition()
        {
            var label = Label.Length > 0 ? Label : "0";
            return new Vector2(Rectangle.X + 5, (int)(Rectangle.Y + Rectangle.Height / 2 - Font.MeasureString(label).Y / 2));
        }
    }
}