using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.Things;

namespace ProjectZ.Base.UI
{
    public class UiLabel : UiElement
    {
        private string _label;
        public sealed override string Label
        {
            get => _label;
            set { _label = value; UpdateLabelPosition(); }
        }

        private Vector2 _textPosition;

        public UiLabel(Rectangle rectangle, SpriteFont font, string text, string elementId, string screen, UiFunction update, Color backgroundColor)
            : base(elementId, screen)
        {
            Rectangle = rectangle;
            UpdateFunction = update;

            BackgroundColor = backgroundColor;
            Font = font;

            Label = text;
        }

        public UiLabel(Rectangle rectangle, SpriteFont font, string text, string elementId, string screen, UiFunction update) :
            this(rectangle, font, text, elementId, screen, update, Color.Transparent)
        { }

        public UiLabel(Rectangle rectangle, string text, string screen) : base("", screen)
        {
            BackgroundColor = Color.Transparent;
            Rectangle = rectangle;
            Label = text;
        }

        public void UpdateLabelPosition()
        {
            _textPosition = new Vector2(
                (int)(Rectangle.X + Rectangle.Width / 2 - Font.MeasureString(_label).X / 2),
                (int)(Rectangle.Y + Rectangle.Height / 2 - Font.MeasureString(_label).Y / 2));
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            // draw the background
            spriteBatch.Draw(Resources.SprWhite, Rectangle, BackgroundColor);
            // draw the label
            spriteBatch.DrawString(Font, _label, _textPosition, FontColor);
        }
    }
}