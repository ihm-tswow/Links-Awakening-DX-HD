using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.SaveLoad;

namespace ProjectZ.InGame.Interface
{
    public class InterfaceImage : InterfaceElement
    {
        public Rectangle SourceRectangle;
        public Vector2 Offset;
        public Color ImageColor = Color.White;
        public Vector2 Origin;
        public SpriteEffects Effects;

        private readonly Texture2D _sprImage;
        private readonly Vector2 _drawOffset;

        public InterfaceImage(DictAtlasEntry sprite, Point margin)
        {
            _sprImage = sprite.Texture;
            SourceRectangle = sprite.ScaledRectangle;

            Size = new Point(sprite.SourceRectangle.Width, sprite.SourceRectangle.Height);
            Margin = margin;
        }

        public InterfaceImage(Texture2D sprImage, Rectangle rectangle, Point size, Point margin)
        {
            _sprImage = sprImage;
            SourceRectangle = rectangle;

            if (size == Point.Zero)
                Size = new Point(SourceRectangle.Width, SourceRectangle.Height);
            else
                Size = size;

            Margin = margin;

            if (size != Point.Zero)
                _drawOffset = new Vector2(Size.X / 2 - SourceRectangle.Width / 2, Size.Y / 2 - SourceRectangle.Height / 2);
        }

        public override void Draw(SpriteBatch spriteBatch, Vector2 drawPosition, float scale, float transparency)
        {
            base.Draw(spriteBatch, drawPosition, scale, transparency);

            if (!Visible || Hidden)
                return;

            spriteBatch.Draw(_sprImage, new Rectangle(
                (int)(drawPosition.X + _drawOffset.X * scale + Offset.X * scale),
                (int)(drawPosition.Y + _drawOffset.Y * scale + Offset.Y * scale),
                (int)(SourceRectangle.Width * scale),
                (int)(SourceRectangle.Height * scale)), SourceRectangle, ImageColor * transparency, 0, Origin, Effects, 0);
        }
    }
}