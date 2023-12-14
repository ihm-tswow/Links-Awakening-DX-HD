using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    class ObjBackgroundColor : GameObject
    {
        public Color BackgroundColor = Color.White;
        public float Percentage;

        public ObjBackgroundColor() : base((Map.Map) null)
        {
            SprEditorImage = Resources.SprObjects;
            EditorIconSource = new Rectangle(240, 16, 16, 16);

            // should be on top of every other object,
            // except the player object but only while transitioning
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerTop, new CPosition(0, 0, 0)));
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.End();

            // draw the background
            spriteBatch.Begin();
            spriteBatch.Draw(Resources.SprWhite, new Rectangle(0, 0, Game1.RenderWidth, Game1.RenderHeight), BackgroundColor * Percentage);
            spriteBatch.End();

            spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointWrap, null, null, null, MapManager.Camera.TransformMatrix);
        }
    }
}