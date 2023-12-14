using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.Controls;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.Interface
{
    public class InterfaceSlider : InterfaceElement
    {
        public delegate void BFunction(int number);
        public BFunction NumberChanged;

        public delegate string StringFunction(int number);
        public StringFunction SetString;

        public SpriteFont Font;
        public Color TextColor = Color.White;

        public string Text { get; set; }
        private string TextPostfix;

        public int CurrentStep;
        public int Start;
        public int End;
        public int StepSize;

        private static float _scrollCounter;
        private static int _scrollStartTime = 350;
        private static int _scrollTime = 75;

        private Color _colorSlider;
        private Color _colorSliderBackground = new Color(79, 79, 79);

        private Rectangle _sliderBackgroundRectangle;
        private Rectangle _sliderRectangle;

        private Vector2 _drawOffset;
        private Vector2 _textSize;

        private Point _sliderSize = new Point(2, 2);
        private string _textKey;

        private float _stepWidth;
        private float _animationCounter;
        private int _animationTime = 66;
        private float _animationStepPosition;
        private float _animationStepStart;

        private int _sliderHeight = 4;
        private int _lastStep = -1;
        private int _steps;
        private bool _updateText;

        public InterfaceSlider()
        {
            Selectable = true;

            SelectionColor = Values.MenuButtonColorSelected;

            _colorSlider = Values.MenuButtonColorSlider;
        }

        public InterfaceSlider(SpriteFont font, string key, int width, Point margin, int start, int end, int stepSize, int current, BFunction numberChanged) : this()
        {
            Font = font;
            Size = new Point(width, 11 + 2 + _sliderSize.Y * 4);
            Margin = margin;

            Start = start;
            End = end;
            StepSize = stepSize;
            CurrentStep = current;

            NumberChanged = numberChanged;

            _sliderBackgroundRectangle = new Rectangle(_sliderSize.X * 2, 2, width - _sliderSize.X * 4, _sliderHeight);

            _sliderRectangle = new Rectangle(
                _sliderSize.X, _sliderBackgroundRectangle.Y - _sliderSize.Y,
                _sliderHeight + _sliderSize.X * 2, _sliderHeight + _sliderSize.Y * 2);

            _steps = End - Start + 1;
            _stepWidth = (_sliderBackgroundRectangle.Width - 4) / ((float)_steps - 1);

            _animationStepPosition = _stepWidth * CurrentStep;

            if (key != null)
            {
                _textKey = key;
                UpdateLanguageText();
            }
        }

        public void UpdateStepSize(int start, int end, int steps)
        {
            if (start == Start && end == End)
                return;

            _updateText = true;

            CurrentStep = MathHelper.Clamp(CurrentStep + Start, start, end) - start;

            Start = start;
            End = end;

            _steps = steps;
            _stepWidth = (_sliderBackgroundRectangle.Width - 4) / ((float)_steps - 1);
        }

        public override InputEventReturn PressedButton(CButtons pressedButton)
        {
            _lastStep = CurrentStep;

            if (_scrollCounter < 0)
                _scrollCounter += _scrollTime;

            if (ControlHandler.ButtonDown(CButtons.Left) || ControlHandler.ButtonDown(CButtons.Right))
                _scrollCounter -= Game1.DeltaTime;
            else
                _scrollCounter = _scrollStartTime;

            if (ControlHandler.ButtonPressed(CButtons.Left) ||
                ControlHandler.ButtonDown(CButtons.Left) && _scrollCounter < 0)
            {
                CurrentStep = MathHelper.Clamp(CurrentStep - StepSize, 0, _steps - 1);

                // start the animation
                _animationCounter = _animationTime;
                _animationStepStart = _stepWidth * _lastStep;

                NumberChanged?.Invoke(Start + CurrentStep);

                return InputEventReturn.Something;
            }

            if (ControlHandler.ButtonPressed(CButtons.Right) ||
                ControlHandler.ButtonDown(CButtons.Right) && _scrollCounter < 0)
            {
                CurrentStep = MathHelper.Clamp(CurrentStep + StepSize, 0, _steps - 1);

                // start the animation
                _animationCounter = _animationTime;
                _animationStepStart = _stepWidth * _lastStep;

                NumberChanged?.Invoke(Start + CurrentStep);
                
                return InputEventReturn.Something;
            }
            
            return InputEventReturn.Nothing;
        }

        public void SetText(string strText)
        {
            Text = strText;

            if (SetString != null)
                TextPostfix = SetString(Start + CurrentStep);

            _textSize = Font.MeasureString(Text + TextPostfix);

            if (Size != Point.Zero)
                _drawOffset = new Vector2(Size.X / 2 - _textSize.X / 2, _textSize.Y);
        }

        public void UpdateLanguageText()
        {
            SetText(Game1.LanguageManager.GetString(_textKey, "error"));
        }

        public override void Update()
        {
            base.Update();

            // update the animation
            _animationCounter -= Game1.DeltaTime;
            if (_animationCounter < 0)
                _animationCounter = 0;

            _animationStepPosition = MathHelper.Lerp(_animationStepStart, _stepWidth * CurrentStep,
                (float)Math.Sin((1 - _animationCounter / _animationTime) * Math.PI / 2));
        }

        public override void Draw(SpriteBatch spriteBatch, Vector2 drawPosition, float scale, float transparency)
        {
            base.Draw(spriteBatch, drawPosition, scale, transparency);

            Resources.RoundedCornerEffect.Parameters["scale"].SetValue(Game1.UiScale);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, null, null, null, null, Resources.RoundedCornerEffect, Game1.GetMatrix);

            Resources.RoundedCornerEffect.Parameters["radius"].SetValue(2.0f);
            Resources.RoundedCornerEffect.Parameters["width"].SetValue(_sliderBackgroundRectangle.Width);
            Resources.RoundedCornerEffect.Parameters["height"].SetValue(_sliderBackgroundRectangle.Height);

            // draw the toggle background line
            spriteBatch.Draw(Resources.SprWhite, new Rectangle(
                (int)(drawPosition.X + _sliderBackgroundRectangle.X * scale),
                (int)(drawPosition.Y + _sliderBackgroundRectangle.Y * scale + _drawOffset.Y * scale),
                (int)(_sliderBackgroundRectangle.Width * scale),
                (int)(_sliderBackgroundRectangle.Height * scale)), _colorSliderBackground * transparency);

            Resources.RoundedCornerEffect.Parameters["radius"].SetValue(2.0f);
            Resources.RoundedCornerEffect.Parameters["width"].SetValue(_sliderRectangle.Width);
            Resources.RoundedCornerEffect.Parameters["height"].SetValue(_sliderRectangle.Height);

            // draw the slider 
            var sliderPosition = (_sliderRectangle.X + _animationStepPosition);
            spriteBatch.Draw(Resources.SprWhite, new Rectangle(
                (int)(drawPosition.X + sliderPosition * scale),
                (int)(drawPosition.Y + _sliderRectangle.Y * scale + _drawOffset.Y * scale),
                (int)(_sliderRectangle.Width * scale),
                (int)(_sliderRectangle.Height * scale)), _colorSlider * transparency);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointWrap, null, null, null, Game1.GetMatrix);

            if (_textKey != null && Game1.LanguageManager.GetString(_textKey, "error") != Text || CurrentStep != _lastStep || _updateText)
                UpdateLanguageText();
            _updateText = false;

            if (Text == null)
                return;

            // draw the text
            spriteBatch.DrawString(Font, Text + TextPostfix,
                new Vector2((int)(drawPosition.X + _drawOffset.X * scale), (int)(drawPosition.Y + scale)),
                TextColor * transparency, 0, Vector2.Zero, new Vector2(scale), SpriteEffects.None, 0);
        }
    }
}