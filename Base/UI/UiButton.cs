using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.Things;

namespace ProjectZ.Base.UI
{
    public class UiButton : UiElement
    {
        public Texture2D ButtonIcon
        {
            get => _buttonIcon;
            set
            {
                _buttonIcon = value;
                UpdateIconRectangle();
            }
        }
        public bool Marked;

        private Texture2D _buttonIcon;
        private Rectangle _iconRectangle;

        public UiButton(Rectangle rectangle, SpriteFont font, string text, string elementId, string screen, UiFunction update, UiFunction click)
            : base(elementId, screen)
        {
            Rectangle = rectangle;
            Label = text;
            UpdateFunction = update;
            ClickFunction = click;

            Font = font;
        }

        public override void Update()
        {
            base.Update();

            if (!Selected || ClickFunction == null || !InputHandler.MouseLeftPressed()) return;

            ClickFunction(this);
            InputHandler.ResetInputState();
        }

        private void UpdateIconRectangle()
        {
            var widthScale = (float)Rectangle.Width / ButtonIcon.Width;
            var heightScale = (float)Rectangle.Height / ButtonIcon.Height;

            var imageScale = MathHelper.Min(widthScale, heightScale);
            _iconRectangle = new Rectangle(Rectangle.X, Rectangle.Y,
                (int)(ButtonIcon.Width * imageScale), (int)(ButtonIcon.Height * imageScale));
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            // draw the button background
            spriteBatch.Draw(Resources.SprWhite, Rectangle, (Selected || Marked) ? FontColor : BackgroundColor);

            var labelSize = Font.MeasureString(Label);
            var textLeft = ButtonIcon != null ? _iconRectangle.Right : Rectangle.X;
            var textPosition = new Vector2(
                (int)(textLeft + (Rectangle.Width - _iconRectangle.Width) / 2 - labelSize.X / 2),
                (int)(Rectangle.Y + Rectangle.Height / 2 - labelSize.Y / 2));

            spriteBatch.DrawString(Font, Label, textPosition, (Selected || Marked) ? BackgroundColor : FontColor);

            if (ButtonIcon != null)
                spriteBatch.Draw(ButtonIcon, _iconRectangle, (Selected || Marked) ? BackgroundColor : FontColor);
        }
    }
}