using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ProjectZ.InGame.SaveLoad
{
    public class DictAtlasEntry
    {
        public readonly Texture2D Texture;

        public readonly Rectangle SourceRectangle;
        public readonly Rectangle ScaledRectangle;
        public readonly Vector2 Origin;
        public readonly Vector2 ScaledOrigin;

        public readonly int TextureScale;
        public readonly float Scale;

        public DictAtlasEntry(Texture2D texture, Rectangle sourceRectangle, Vector2 origin, int textureScale)
        {
            Texture = texture;

            SourceRectangle = sourceRectangle;
            ScaledRectangle = new Rectangle(
                sourceRectangle.X * textureScale, sourceRectangle.Y * textureScale,
                sourceRectangle.Width * textureScale, sourceRectangle.Height * textureScale);

            Origin = origin;
            ScaledOrigin = new Vector2(origin.X * textureScale, origin.Y * textureScale);

            TextureScale = textureScale;
            Scale = 1.0f / textureScale;
        }
    }
}
