using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjLight : GameObject
    {
        private readonly Rectangle _drawRectangle;
        private readonly Color _lightColor;

        public ObjLight() : base("editor light") { }

        public ObjLight(Map.Map map, int posX, int posY, int size, int colorR, int colorG, int colorB, int colorA, int layer) : base(map)
        {
            EntityPosition = new CPosition(posX + 8, posY + 8, 0);
            EntitySize = new Rectangle(-size / 2, -size / 2, size, size);

            _drawRectangle = new Rectangle(posX + 8 - size / 2, posY + 8 - size / 2, size, size);

            _lightColor = new Color(colorR, colorG, colorB) * (colorA / 255f);

            AddComponent(LightDrawComponent.Index, new LightDrawComponent(DrawLight) { Layer = layer });
        }

        public void DrawLight(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Resources.SprLight, _drawRectangle, _lightColor);
        }
    }
}