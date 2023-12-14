using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.Interface
{
    public class InterfaceLabel : InterfaceElement
    {
        public SpriteFont Font;

        public Gravities TextAlignment
        {
            get { return _textAlignment; }
            set
            {
                _textAlignment = value;
                if (Text != null)
                    SetText(Text);
            }
        }

        public Color TextColor = Color.White;

        public string Text { get; set; }
        public bool Translate = true;

        private Vector2 _drawOffset;
        private Vector2 _textSize;

        private Gravities _textAlignment = Gravities.Center;

        private readonly string _textKey;

        public InterfaceLabel(SpriteFont font, string key, Point size, Point margin)
        {
            Font = font;
            Size = size;
            Margin = margin;

            if (string.IsNullOrEmpty(key))
                return;

            _textKey = key;
            UpdateLanguageText();
        }

        public InterfaceLabel(string key, Point size, Point margin) : this(Resources.GameFont, key, size, margin)
        { }

        public InterfaceLabel(string key) : this(key, Point.Zero, Point.Zero)
        {
            Size = new Point((int)_textSize.X, (int)_textSize.Y);
        }

        public void SetText(string strText)
        {
            Text = strText;

            _textSize = Font.MeasureString(Text);

            if (Size != Point.Zero)
            {
                _drawOffset = new Vector2(Size.X / 2 - _textSize.X / 2, Size.Y / 2 - _textSize.Y / 2);

                // left/right
                if ((TextAlignment & Gravities.Left) != 0)
                    _drawOffset.X = 0;
                else if ((TextAlignment & Gravities.Right) != 0)
                    _drawOffset.X = Size.X - _textSize.X;

                // top/bottom
                if ((TextAlignment & Gravities.Top) != 0)
                    _drawOffset.Y = 0;
                else if ((TextAlignment & Gravities.Bottom) != 0)
                    _drawOffset.Y = Size.Y - _textSize.Y;
            }
        }

        public void UpdateLanguageText()
        {
            SetText(Game1.LanguageManager.GetString(_textKey, "error"));
        }

        public override void Draw(SpriteBatch spriteBatch, Vector2 drawPosition, float scale, float transparency)
        {
            base.Draw(spriteBatch, drawPosition, scale, transparency);

            if (Translate && _textKey != null && Game1.LanguageManager.GetString(_textKey, "error") != Text)
                UpdateLanguageText();

            if (Text == null)
                return;

            // draw the text
            spriteBatch.DrawString(Font, Text,
                new Vector2(
                    (int)(drawPosition.X + _drawOffset.X * scale),
                    (int)(drawPosition.Y + (_drawOffset.Y + 1) * scale)),
                TextColor * transparency, 0, Vector2.Zero, new Vector2(scale), SpriteEffects.None, 0);
        }
    }
}