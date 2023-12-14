using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.Controls;
using ProjectZ.InGame.Things;
using System;

namespace ProjectZ.InGame.Interface
{
    public class InterfaceElement
    {
        [Flags]
        public enum Gravities
        {
            Center = 0x01 << 0,
            Left = 0x01 << 1,
            Right = 0x01 << 2,
            Top = 0x01 << 3,
            Bottom = 0x01 << 4
        }

        public enum Directions
        {
            Left, Right, Top, Down
        }

        public enum InputEventReturn
        {
            Nothing, Something
        }

        public Gravities Gravity = Gravities.Center;

        public Color Color;
        public Color SelectionColor;

        public Point Position;
        public Point Size;
        public Point Margin;

        public float CornerRadius = 3.0f;

        public bool SizeChanged;
        public bool Selected;
        public bool Selectable;
        public bool ChangeUp;
        public bool Recalculate;

        public bool Visible
        {
            get => _visible;
            set
            {
                _visible = value;
            }
        }

        // this is ignored at the layout stage
        public bool Hidden
        {
            get => _hidden;
            set
            {
                ChangeUp = true;
                _hidden = value;
            }
        }

        public float SelectionState;

        private float _selectionCounter;
        private float _selectAnimationTime = 75;
        private float _deselectionTime = 50;

        private bool _visible = true;
        private bool _hidden;

        public virtual void Select(Directions direction, bool animate)
        {
            Selected = true;
            SelectionState = animate ? 0 : 1;
            _selectionCounter = animate ? _selectAnimationTime : 0;
        }

        public virtual void Deselect(bool animate)
        {
            Selected = false;
            // do not start the animation from the start if the element was selected in the same frame
            if (_selectionCounter != _selectAnimationTime && animate)
            {
                SelectionState = 1;
                _selectionCounter = _deselectionTime;
            }
            else
            {
                SelectionState = 0;
                _selectionCounter = 0;
            }
        }

        public virtual InputEventReturn PressedButton(CButtons pressedButton)
        {
            return InputEventReturn.Nothing;
        }

        public virtual void CalculatePosition() { }

        public virtual void Update()
        {
            if (_selectionCounter > 0)
            {
                _selectionCounter -= Game1.DeltaTime;
                SelectionState = Selected ? 1 - _selectionCounter / _selectAnimationTime : _selectionCounter / _deselectionTime;
            }
            else
                SelectionState = Selected ? 1 : 0;
        }

        public virtual void Draw(SpriteBatch spriteBatch, Vector2 drawPosition, float scale, float transparency)
        {
            var color = Color.Lerp(Color, SelectionColor, SelectionState) * transparency;

            if (color != Color.Transparent)
            {
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Immediate, null, null, null, null, Resources.RoundedCornerEffect, Game1.GetMatrix);

                Resources.RoundedCornerEffect.Parameters["radius"].SetValue(CornerRadius);
                Resources.RoundedCornerEffect.Parameters["width"].SetValue(Size.X);
                Resources.RoundedCornerEffect.Parameters["height"].SetValue(Size.Y);
                Resources.RoundedCornerEffect.Parameters["scale"].SetValue(Game1.UiScale);

                // draw the background
                spriteBatch.Draw(Resources.SprWhite, new Rectangle(
                    (int)drawPosition.X + (int)(-SelectionState * scale),
                    (int)drawPosition.Y + (int)(-SelectionState * scale),
                    (int)(Size.X * scale) + (int)(SelectionState * 2 * scale),
                    (int)(Size.Y * scale) + (int)(SelectionState * 2 * scale)), color);

                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointWrap, null, null, null, Game1.GetMatrix);
            }

            // draw the debug background
            if (Game1.DebugMode)
                spriteBatch.Draw(Resources.SprWhite, new Rectangle(
                    (int)drawPosition.X,
                    (int)drawPosition.Y,
                    (int)(Size.X * scale),
                    (int)(Size.Y * scale)), Color.White * 0.25f * transparency);
        }
    }
}