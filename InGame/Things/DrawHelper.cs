using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;

namespace ProjectZ.InGame.Things
{
    class DrawHelper
    {
        // more then 1000 will only make sense on a high resolution with a small game scale
        public const int MaxShadowIndices = 1000;
        public static int CurrentShadowIndex;
        public static short[] IndexDataShadow = new short[MaxShadowIndices * 6];
        public static float ShadowHeight;
        public static float ShadowOffset;
        public static Texture2D LastShadowTexture;

        private static Matrix projectionMatrix;
        private static Matrix outMatrix;
        public static VertexPositionPositionColorTexture[] ShadowVertexArray = new VertexPositionPositionColorTexture[MaxShadowIndices * 4];

        public static void StartShadowDrawing()
        {
            //projectionMatrix = Matrix.CreateOrthographicOffCenter(0,
            //    Game1.Graphics.PreferredBackBufferWidth, Game1.Graphics.PreferredBackBufferHeight, 0, 0, -1);
            //outMatrix = MapManager.Camera.TransformMatrix * projectionMatrix;

            //Resources.NextShadowEffect.Parameters["WorldViewProjection"].SetValue(outMatrix);

            //Game1.SpriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, Resources.NextShadowEffect, MapManager.Camera.TransformMatrix);

            LastShadowTexture = null;
            CurrentShadowIndex = 0;
        }

        public static void EndShadowDrawing()
        {
            //Game1.SpriteBatch.End();

            if (CurrentShadowIndex > 0)
                DrawIndexedDataNew();
        }

        // TODO_End: this should be done using normal spritebatch.Draw()
        public static void DrawShadow(Texture2D sprImage, Vector2 drawPosition, Rectangle sourceRectangle,
            float drawWidth, float drawHeight, bool mirror, float height, float rotation, Color color)
        {
            //Game1.SpriteBatch.Draw(sprImage, drawPosition, sourceRectangle, color);

            if (LastShadowTexture != null && (LastShadowTexture != sprImage ||
                CurrentShadowIndex >= MaxShadowIndices || ShadowHeight != height || ShadowOffset != rotation))
            {
                // draw the stored data
                DrawIndexedDataNew();
                CurrentShadowIndex = 0;
            }

            SetVertexPtIndexed(ShadowVertexArray, CurrentShadowIndex * 4, drawPosition, sourceRectangle,
                drawWidth, drawHeight, sprImage.Width, sprImage.Height, mirror, color);

            SetIndexBuffer(IndexDataShadow, CurrentShadowIndex * 6, CurrentShadowIndex * 4);

            CurrentShadowIndex++;

            ShadowHeight = height;
            ShadowOffset = rotation;
            LastShadowTexture = sprImage;
        }

        public struct VertexPositionPositionColorTexture : IVertexType
        {
            public Vector2 Position;
            public Vector2 TextureCoordinate;
            public Vector2 UpperLeftPosition;
            public Vector2 SourceSize;
            public Color Color;

            public VertexPositionPositionColorTexture(
                Vector2 position, Vector2 textureCoordinate, Vector2 upperLeftPosition, Vector2 sourceSize, Color color)
            {
                Position = position;
                TextureCoordinate = textureCoordinate;
                UpperLeftPosition = upperLeftPosition;
                SourceSize = sourceSize;
                Color = color;
            }

            public static readonly VertexDeclaration VertexDeclaration;

            VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

            static VertexPositionPositionColorTexture()
            {
                var elements = new[]
                {
                    new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
                    new VertexElement(8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
                    new VertexElement(16, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 1),
                    new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 2),
                    new VertexElement(32, VertexElementFormat.Color, VertexElementUsage.Color, 0),
                };
                VertexDeclaration = new VertexDeclaration(elements);
            }
        }

        public static void SetVertexPtIndexed(
            VertexPositionPositionColorTexture[] buffer, int index,
            Vector2 position, Rectangle sourceRectangle, float drawWidth, float drawHeight,
            int textureWidth, int textureHeight, bool mirror, Color color)
        {
            var posLeft = position.X;
            var posRight = position.X + drawWidth;// sourceRectangle.Width;
            var posTop = position.Y;
            var posBottom = position.Y + drawHeight;// sourceRectangle.Height;

            var left = (!mirror ? sourceRectangle.X : sourceRectangle.Right) / (float)textureWidth;
            var right = (!mirror ? sourceRectangle.Right : sourceRectangle.X) / (float)textureWidth;
            var top = sourceRectangle.Y / (float)textureHeight;
            var bottom = sourceRectangle.Bottom / (float)textureHeight;

            buffer[index + 0] = new VertexPositionPositionColorTexture(
                new Vector2(posLeft, posTop), new Vector2(left, top), position,
                new Vector2(sourceRectangle.Width, sourceRectangle.Height), color);
            buffer[index + 1] = new VertexPositionPositionColorTexture(
                new Vector2(posRight, posTop), new Vector2(right, top), position,
                new Vector2(sourceRectangle.Width, sourceRectangle.Height), color);
            buffer[index + 2] = new VertexPositionPositionColorTexture(
                new Vector2(posLeft, posBottom), new Vector2(left, bottom), position,
                new Vector2(sourceRectangle.Width, sourceRectangle.Height), color);
            buffer[index + 3] = new VertexPositionPositionColorTexture(
                new Vector2(posRight, posBottom), new Vector2(right, bottom), position,
                new Vector2(sourceRectangle.Width, sourceRectangle.Height), color);
        }

        public static void DrawIndexedDataNew()
        {
            projectionMatrix = Matrix.CreateOrthographicOffCenter(0, 
                Game1.GameManager.CurrentRenderWidth, Game1.GameManager.CurrentRenderHeight, 0, 0, -1);
            outMatrix = MapManager.Camera.TransformMatrix * projectionMatrix;

            Resources.FullShadowEffect.Parameters["xViewProjection"].SetValue(outMatrix);
            Resources.FullShadowEffect.Parameters["height"].SetValue(ShadowHeight);
            Resources.FullShadowEffect.Parameters["offsetX"].SetValue(ShadowOffset);

            foreach (var pass in Resources.FullShadowEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                Game1.Graphics.GraphicsDevice.Textures[0] = LastShadowTexture;
                Game1.Graphics.GraphicsDevice.DrawUserIndexedPrimitives(
                    PrimitiveType.TriangleList, ShadowVertexArray, 0, CurrentShadowIndex * 4, IndexDataShadow, 0, CurrentShadowIndex * 2);
            }
        }

        public static void SetIndexBuffer(short[] buffer, int position, int offset)
        {
            buffer[position + 0] = (short)(offset + 0);
            buffer[position + 1] = (short)(offset + 1);
            buffer[position + 2] = (short)(offset + 2);

            buffer[position + 3] = (short)(offset + 2);
            buffer[position + 4] = (short)(offset + 1);
            buffer[position + 5] = (short)(offset + 3);
        }

        public static void DrawLight(SpriteBatch spriteBatch, Rectangle lightRectangle, Color lightColor)
        {
            spriteBatch.Draw(Resources.SprLight, lightRectangle, lightColor);
        }

        public static void DrawCenter(SpriteBatch spriteBatch, Texture2D sprTexture, Point offset,
            Rectangle centerRectangle, Rectangle sourceRectangle, int scale)
        {
            spriteBatch.Draw(sprTexture, new Rectangle(
                offset.X + (centerRectangle.X + centerRectangle.Width / 2 - sourceRectangle.Width / 2) * scale,
                offset.Y + (centerRectangle.Y + centerRectangle.Height / 2 - sourceRectangle.Height / 2) * scale,
                sourceRectangle.Width * scale,
                sourceRectangle.Height * scale), sourceRectangle, Color.White);
        }

        public static void DrawNormalized(SpriteBatch spriteBatch, Texture2D texture,
            Vector2 position, Rectangle sourceRectangle, Color color, float scale = 1.0f)
        {
            var normalizedPosition = new Vector2(
                (float)Math.Round(position.X * MapManager.Camera.Scale) / MapManager.Camera.Scale,
                (float)Math.Round(position.Y * MapManager.Camera.Scale) / MapManager.Camera.Scale);

            spriteBatch.Draw(texture, normalizedPosition, sourceRectangle, color, 0, Vector2.Zero, new Vector2(scale), SpriteEffects.None, 0);
        }

        public static void DrawNormalized(SpriteBatch spriteBatch, DictAtlasEntry sprite, Vector2 position, Color color)
        {
            var normalizedPosition = new Vector2(
                (float)Math.Round(position.X * MapManager.Camera.Scale) / MapManager.Camera.Scale,
                (float)Math.Round(position.Y * MapManager.Camera.Scale) / MapManager.Camera.Scale);

            spriteBatch.Draw(sprite.Texture, normalizedPosition, sprite.ScaledRectangle, color, 0, sprite.Origin, new Vector2(sprite.Scale), SpriteEffects.None, 0);
        }
    }
}
