using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjFog : GameObject
    {
        private Vector2 _offset0;
        private Vector2 _offset1;

        private readonly Vector2 _position;

        private readonly float _scale;
        private readonly float _transparency;

        private readonly int _timeOffset;

        public ObjFog(Map.Map map, int posX, int posY, float scale, float transparency) : base(map)
        {
            SprEditorImage = Resources.SprItem;
            EditorIconSource = new Rectangle(64, 168, 16, 16);

            var height = (int)(Resources.SprFog.Height * scale);
            EntityPosition = new CPosition(posX, posY + height, 0);
            EntitySize = new Rectangle(0, -height, (int)(Resources.SprFog.Width * scale), height);

            _position = new Vector2(posX, posY);

            _scale = scale;
            _transparency = transparency;

            _timeOffset = Game1.RandomNumber.Next(0, 5000);

            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerTop, EntityPosition));
        }

        private void Update()
        {
            _offset0 = new Vector2(MathF.Sin((float)((Game1.TotalGameTime + _timeOffset) / 2000)) * 16, MathF.Sin((float)((Game1.TotalGameTime + _timeOffset) / 6000)) * 2);
            _offset1 = new Vector2(MathF.Sin((float)((Game1.TotalGameTime + _timeOffset) / 3250)) * 16, MathF.Sin((float)((Game1.TotalGameTime + _timeOffset) / 7500)) * 2);
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            // the fog would break the shock effect
            if (Game1.GameManager.UseShockEffect)
                return;

            var sourceRectangle = new Rectangle(0, 0, Resources.SprFog.Width, Resources.SprFog.Height);
            spriteBatch.Draw(Resources.SprFog, _position + _offset0, sourceRectangle, Color.White * _transparency, 0, Vector2.Zero, _scale, SpriteEffects.None, 0);
            spriteBatch.Draw(Resources.SprFog, _position + _offset1, sourceRectangle, Color.White * _transparency, 0, Vector2.Zero, _scale, SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically, 0);
        }
    }
}