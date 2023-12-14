using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ProjectZ.InGame.Things;

namespace ProjectZ.Base.UI
{
    public class UiNumberInput : UiElement
    {
        public float MinValue;
        public float MaxValue;
        public float Value;
        public float OldValue;

        private readonly UiFunction _onNumberUpdate;
        private string _strValue;
        private string _strNewValue;
        private readonly float _stepSize;
        private int _mouseWheel;

        public UiNumberInput(Rectangle rectangle, SpriteFont font, float value, float minValue, float maxValue,
            float stepSize, string elementId, string screen, UiFunction update, UiFunction onNumberUpdate)
            : base(elementId, screen)
        {
            Rectangle = rectangle;
            UpdateFunction = update;
            _onNumberUpdate = onNumberUpdate;

            Value = value;
            _strValue = value.ToString(CultureInfo.InvariantCulture);

            MinValue = minValue;
            MaxValue = maxValue;

            _stepSize = stepSize;

            Font = font;
        }

        public override void Update()
        {
            base.Update();

            OldValue = Value;
            _strValue = Value.ToString(CultureInfo.InvariantCulture);

            if (Selected)
            {
                _strNewValue = _strValue == "0" ? "" : _strValue;

                var returnNumber = InputHandler.ReturnNumber();
                if (returnNumber >= 0)
                    _strNewValue += returnNumber.ToString();

                // delete last position
                if (InputHandler.KeyPressed(Keys.Back) || InputHandler.MouseRightPressed(Rectangle))
                    _strNewValue = _strValue.Substring(0, _strValue.Length - 1);

                // if everything was delete
                if (_strNewValue == "")
                    _strNewValue = "0";

                // add .
                if (_stepSize % 1 > 0 && (InputHandler.KeyPressed(Keys.OemPeriod) || InputHandler.KeyPressed(Keys.OemComma)) && !_strValue.Contains("."))
                    _strNewValue += ".";

                // change value when scrolling
                if (InputHandler.MouseState.ScrollWheelValue > _mouseWheel && float.Parse(_strNewValue, CultureInfo.InvariantCulture) + _stepSize <= MaxValue)
                    _strNewValue = (float.Parse(_strNewValue, CultureInfo.InvariantCulture) + _stepSize).ToString(CultureInfo.InvariantCulture);
                if (InputHandler.MouseState.ScrollWheelValue < _mouseWheel && float.Parse(_strNewValue, CultureInfo.InvariantCulture) - _stepSize >= MinValue)
                    _strNewValue = (float.Parse(_strNewValue, CultureInfo.InvariantCulture) - _stepSize).ToString(CultureInfo.InvariantCulture);

                InputHandler.ResetInputState();

                float.TryParse(_strNewValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var newValue);
                if (newValue <= MaxValue &&
                    (!_strNewValue.Contains(".") || _strNewValue.Split('.')[1].Length <= (_stepSize % 1).ToString(CultureInfo.InvariantCulture).Length - 2))
                    _strValue = _strNewValue;

                float.TryParse(_strValue, NumberStyles.Float, CultureInfo.InvariantCulture, out Value);

                if (OldValue != Value && Value >= MinValue)
                    _onNumberUpdate?.Invoke(this);
            }
            else
            {
                if (Value != (int)(Value / _stepSize) * _stepSize)
                {
                    Value = (int)(Value / _stepSize) * _stepSize;
                    _strValue = Value.ToString(CultureInfo.InvariantCulture);
                    _onNumberUpdate(this);
                }
            }

            _mouseWheel = InputHandler.MouseState.ScrollWheelValue;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            // draw the background
            spriteBatch.Draw(Resources.SprWhite, Rectangle, BackgroundColor);

            Label = _strValue + (Selected ? "|" : "");

            // draw the value
            var textPosition = new Vector2(Rectangle.X + 5, (int)(Rectangle.Y + Rectangle.Height / 2 - Font.MeasureString(Label).Y / 2));
            spriteBatch.DrawString(Font, Label, textPosition, FontColor);
        }
    }
}