using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Base.CObjects
{
    public class CSprite
    {
        public Texture2D SprTexture;
        public CPosition Position;
        public SpriteShader SpriteShader;

        public SpriteEffects SpriteEffect;
        public Rectangle SourceRectangle;
        public Vector2 DrawOffset;
        public Vector2 Center;
        public Color Color = Color.White;

        public float Scale = 1;
        public float Rotation;
        public bool IsVisible = true;

        public CSprite(CPosition position)
        {
            Position = position;
        }

        public CSprite(Texture2D sprTexture, Rectangle sourceRectangle)
        {
            SprTexture = sprTexture;
            SourceRectangle = sourceRectangle;
        }

        public CSprite(Texture2D sprTexture, CPosition position, Rectangle sourceRectangle, Vector2 drawOffset)
        {
            SprTexture = sprTexture;
            Position = position;
            SourceRectangle = sourceRectangle;
            DrawOffset = drawOffset;
        }

        public CSprite(DictAtlasEntry sprite, CPosition position)
        {
            SetSprite(sprite);
            Position = position;
        }

        public CSprite(string spriteId, CPosition position) : this(Resources.GetSprite(spriteId), position)
        { }

        // @REMOVE: drawOffset should probably be the center in most cases?
        public CSprite(DictAtlasEntry sprite, CPosition position, Vector2 drawOffset)
        {
            SetSprite(sprite);
            Position = position;
            DrawOffset = drawOffset;
        }

        // @REMOVE: drawOffset should probably be the center in most cases?
        public CSprite(string spriteId, CPosition position, Vector2 drawOffset) :
            this(Resources.GetSprite(spriteId), position, drawOffset)
        { }

        public void SetSprite(DictAtlasEntry sprite)
        {
            SprTexture = sprite.Texture;
            SourceRectangle = sprite.ScaledRectangle;
            Scale = sprite.Scale;
            Center = sprite.ScaledOrigin;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!IsVisible)
                return;

            // this is used to align the sprite to avoid holes
            var normX = (float)Math.Round((Position.X + DrawOffset.X) * MapManager.Camera.Scale) / MapManager.Camera.Scale;
            var normY = (float)Math.Round((Position.Y + DrawOffset.Y - Position.Z) * MapManager.Camera.Scale) / MapManager.Camera.Scale;

            // change the draw effect
            if (SpriteShader != null)
            {
                spriteBatch.End();
                ObjectManager.SpriteBatchBegin(spriteBatch, SpriteShader);
            }

            spriteBatch.Draw(SprTexture, new Vector2(normX, normY), SourceRectangle, Color, Rotation, Center * Scale, new Vector2(Scale), SpriteEffect, 0);

            // change the draw effect
            // this would not be very efficient if a lot of sprite used effects
            if (SpriteShader != null)
            {
                spriteBatch.End();
                ObjectManager.SpriteBatchBegin(spriteBatch, null);
            }
        }

        public void DrawShadow(SpriteBatch spriteBatch, Color color, int offsetY, float height, float rotation)
        {
            if (!IsVisible)
                return;

            var normX = (float)Math.Round((Position.X + DrawOffset.X - Center.X) * MapManager.Camera.Scale) / MapManager.Camera.Scale;
            var normY = (float)Math.Round((Position.Y + DrawOffset.Y - Center.Y - Position.Z * 0.5f + offsetY) * MapManager.Camera.Scale) / MapManager.Camera.Scale;

            // TODO_OPT: this does currently not support FlipVertically
            DrawHelper.DrawShadow(SprTexture, new Vector2(normX, normY),
                SourceRectangle, SourceRectangle.Width, SourceRectangle.Height,
                SpriteEffect == SpriteEffects.FlipHorizontally, height, rotation, color);
        }
    }
}
