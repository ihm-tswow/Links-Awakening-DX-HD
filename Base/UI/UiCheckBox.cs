using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.Things;

namespace ProjectZ.Base.UI
{
    public class UiCheckBox : UiElement
    {
        public bool CurrentState;

        private readonly Rectangle _checkBox;
        private readonly Rectangle _checkBoxFill;
        private readonly Vector2 _textBoxPosition;
        private int _border = 4;

        public UiCheckBox(Rectangle rectangle, SpriteFont font, string text, string elementId, string screen, bool currentState, UiFunction update, UiFunction click)
            : base(elementId, screen)
        {
            Rectangle = new Rectangle(rectangle.X + rectangle.Height + 5, rectangle.Y, rectangle.Width - rectangle.Height - 5, rectangle.Height);
            _checkBox = new Rectangle(rectangle.X, rectangle.Y, rectangle.Height, rectangle.Height);

            _checkBoxFill = new Rectangle(
                _checkBox.X + _border, _checkBox.Y + _border,
                _checkBox.Width - _border * 2, _checkBox.Height - _border * 2);

            Label = text;
            CurrentState = currentState;
            UpdateFunction = update;
            ClickFunction = click;

            Font = font;

            var labelSize = Font.MeasureString(Label);
            _textBoxPosition = new Vector2(
                (int)(Rectangle.X + Rectangle.Width / 2 - labelSize.X / 2),
                (int)(Rectangle.Y + Rectangle.Height / 2 - labelSize.Y / 2));
        }

        public override void Update()
        {
            base.Update();

            if (ClickFunction != null && InputHandler.MouseLeftPressed() && Selected)
            {
                CurrentState = !CurrentState;
                ClickFunction(this);
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            // draw the background
            spriteBatch.Draw(Resources.SprWhite, Rectangle, Selected ? FontColor : BackgroundColor);

            spriteBatch.Draw(Resources.SprWhite, _checkBox, BackgroundColor);

            // draw the selection
            if (CurrentState)
                spriteBatch.Draw(Resources.SprWhite, _checkBoxFill, FontColor);

            // draw the label
            spriteBatch.DrawString(Font, Label, _textBoxPosition, Selected ? BackgroundColor : FontColor);
        }
    }
}