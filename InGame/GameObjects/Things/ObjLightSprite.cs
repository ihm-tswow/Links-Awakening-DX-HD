using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjLightSprite : GameObject
    {
        private readonly DictAtlasEntry _sprite;
        private readonly Color _lightColor;

        private readonly Vector2 _position;

        private readonly float _rotation;

        public ObjLightSprite() : base("editor light") { }

        public ObjLightSprite(Map.Map map, int posX, int posY, string spriteId, int colorR, int colorG, int colorB, int colorA, int layer, int rotation) : base(map)
        {
            EntityPosition = new CPosition(posX, posY, 0);

            if (!string.IsNullOrEmpty(spriteId))
                _sprite = Resources.GetSprite(spriteId);

            if (_sprite == null)
            {
                Console.WriteLine("Could not find spriteId: " + spriteId);
                IsDead = true;
                return;
            }

            EntitySize = new Rectangle(0, 0, _sprite.SourceRectangle.Width, _sprite.SourceRectangle.Height);

            _position = new Vector2(EntityPosition.X + _sprite.Origin.X, EntityPosition.Y + _sprite.Origin.Y);

            _lightColor = new Color(colorR, colorG, colorB) * (colorA / 255f);
            _rotation = rotation * MathF.PI / 2;

            AddComponent(LightDrawComponent.Index, new LightDrawComponent(DrawLight) { Layer = layer });
        }

        public void DrawLight(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(_sprite.Texture, _position, _sprite.ScaledRectangle, 
                _lightColor, _rotation, _sprite.ScaledOrigin, _sprite.Scale, SpriteEffects.None, 0);
        }
    }
}