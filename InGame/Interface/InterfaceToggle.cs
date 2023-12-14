using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.Controls;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.Interface
{
    public class InterfaceToggle : InterfaceElement
    {
        public delegate void BFunction(bool toggleState);
        public BFunction ClickFunction;

        private readonly Color _colorToggledBackground;
        private readonly Color _colorNotToggledBackground = new Color(188, 188, 188);
        private readonly Color _colorToggled;
        private readonly Color _colorNotToggled = new Color(79, 79, 79);

        private readonly Rectangle _toggleBackgroundRectangle;
        private readonly Rectangle _toggleRectangle;

        private float _toggleAnimationState;
        private float _toggleAnimationCounter;

        private const int ToggleAnimationTime = 100;

        private bool _toggleState;

        public bool ToggleState => _toggleState;

        public InterfaceToggle()
        {
            Color = Values.MenuButtonColor;
            SelectionColor = Values.MenuButtonColorSelected;

            _colorToggledBackground = Values.MenuButtonColorSelected;
            _colorToggled = Values.MenuButtonColorSlider;
        }

        public InterfaceToggle(Point size, Point margin, bool startState, BFunction clickFunction) : this()
        {
            Size = size;
            Margin = margin;

            _toggleState = startState;

            ClickFunction = clickFunction;

            _toggleBackgroundRectangle = new Rectangle(0, 0, size.X, size.Y);
            _toggleRectangle = new Rectangle(2, 2, size.Y - 4, size.Y - 4);

            _toggleAnimationState = _toggleState ? 1 : 0;
        }

        public static InterfaceListLayout GetToggleButton(Point size, Point margin, string textKey, bool startState, BFunction clickFunction)
        {
            var toggleLayout = new InterfaceListLayout() { Size = size, Margin = margin, HorizontalMode = true, Selectable = true };

            var toggleSize = new Point((int)(size.Y * 1.75f), size.Y - 2);
            var buttonSize = new Point(size.X - toggleSize.X - 4, size.Y);

            var toggle = new InterfaceToggle(toggleSize, new Point(2, 0), startState, clickFunction);
            var button = new InterfaceButton(buttonSize, new Point(2, 0), textKey, buttonElement => toggle.Toggle());

            toggleLayout.AddElement(button);
            toggleLayout.AddElement(toggle);

            return toggleLayout;
        }

        public override InputEventReturn PressedButton(CButtons pressedButton)
        {
            if (!ControlHandler.ButtonPressed(CButtons.A))
                return InputEventReturn.Nothing;

            Toggle();

            return ClickFunction != null ? InputEventReturn.Something : InputEventReturn.Nothing;
        }

        public void SetToggle(bool state)
        {
            _toggleState = state;
            // no animation
            _toggleAnimationCounter = 0;
        }

        public void Toggle()
        {
            _toggleState = !_toggleState;
            _toggleAnimationCounter = ToggleAnimationTime;

            ClickFunction?.Invoke(_toggleState);
        }

        public override void Update()
        {
            base.Update();

            // update the toggle animation
            _toggleAnimationCounter -= Game1.DeltaTime;
            if (_toggleAnimationCounter <= 0)
                _toggleAnimationCounter = 0;
            var percentage = (float)Math.Sin((1 - _toggleAnimationCounter / ToggleAnimationTime) * Math.PI / 2);
            _toggleAnimationState = _toggleState ? percentage : 1 - percentage;
        }

        public override void Draw(SpriteBatch spriteBatch, Vector2 drawPosition, float scale, float transparency)
        {
            Resources.RoundedCornerEffect.Parameters["scale"].SetValue(Game1.UiScale);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, null, null, null, null, Resources.RoundedCornerEffect, Game1.GetMatrix);

            Resources.RoundedCornerEffect.Parameters["radius"].SetValue(4.0f);
            Resources.RoundedCornerEffect.Parameters["width"].SetValue(_toggleBackgroundRectangle.Width);
            Resources.RoundedCornerEffect.Parameters["height"].SetValue(_toggleBackgroundRectangle.Height);

            // draw the toggle background
            spriteBatch.Draw(Resources.SprWhite, new Rectangle(
                (int)(drawPosition.X + _toggleBackgroundRectangle.X * scale),
                (int)(drawPosition.Y + _toggleBackgroundRectangle.Y * scale),
                (int)(_toggleBackgroundRectangle.Width * scale),
                (int)(_toggleBackgroundRectangle.Height * scale)),
                (_toggleState ? _colorToggledBackground : _colorNotToggledBackground) * transparency);

            Resources.RoundedCornerEffect.Parameters["radius"].SetValue(4.0f);
            Resources.RoundedCornerEffect.Parameters["width"].SetValue(_toggleRectangle.Width);
            Resources.RoundedCornerEffect.Parameters["height"].SetValue(_toggleRectangle.Height);

            // draw the toggle
            spriteBatch.Draw(Resources.SprWhite, new Rectangle(
                (int)(drawPosition.X + (_toggleRectangle.X + _toggleAnimationState * (_toggleBackgroundRectangle.Width - _toggleRectangle.Width - 4)) * scale),
                (int)(drawPosition.Y + _toggleRectangle.Y * scale),
                (int)(_toggleRectangle.Width * scale),
                (int)(_toggleRectangle.Height * scale)),
                (_toggleState ? _colorToggled : _colorNotToggled) * transparency);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointWrap, null, null, null, Game1.GetMatrix);
        }
    }
}