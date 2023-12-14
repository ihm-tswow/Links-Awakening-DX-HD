using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    public class ObjSprite : GameObject
    {
        public CSprite Sprite;

        // sprite + shadow
        public ObjSprite(Map.Map map, int posX, int posY,
            string spriteId, Vector2 positionOffset, int layer,
            string shadowSpriteId) : base(map, spriteId)
        {
            EntityPosition = new CPosition(posX + positionOffset.X, posY + positionOffset.Y, 0);

            var sprite = Resources.GetSprite(spriteId);
            Sprite = new CSprite(sprite, EntityPosition);
            EntitySize = new Rectangle(-(int)sprite.Origin.X, -(int)sprite.Origin.Y, Sprite.SourceRectangle.Width, Sprite.SourceRectangle.Height);

            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(Sprite, layer));

            if (!string.IsNullOrEmpty(shadowSpriteId))
            {
                var shadowSprite = Resources.GetSprite(shadowSpriteId);
                AddComponent(DrawShadowComponent.Index,
                    new DrawShadowSpriteComponent(shadowSprite.Texture, EntityPosition, shadowSprite.ScaledRectangle, -shadowSprite.Origin));
            }
        }

        // sprite + shadow + collision
        public ObjSprite(Map.Map map, int posX, int posY,
            string spriteId, Vector2 positionOffset, int layer,
            string shadowSpriteId,
            Rectangle collisionRectangle, Values.CollisionTypes collisionType) : this(map, posX, posY, spriteId, positionOffset, layer, shadowSpriteId)
        {
            AddComponent(CollisionComponent.Index, new BoxCollisionComponent(new CBox(EntityPosition,
                collisionRectangle.X, collisionRectangle.Y, 0, collisionRectangle.Width, collisionRectangle.Height, 16), collisionType));
        }

        // used for the chest
        public ObjSprite(Map.Map map, int posX, int posY, Texture2D sprTexture, Rectangle sourceRectangle, Vector2 drawOffset, int layer) : base(map)
        {
            SprEditorImage = sprTexture;
            EditorIconSource = sourceRectangle;

            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle((int)drawOffset.X, (int)drawOffset.Y, sourceRectangle.Width, sourceRectangle.Height);

            Sprite = new CSprite(sprTexture, EntityPosition, sourceRectangle, drawOffset);

            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(Sprite, layer));
        }
    }
}