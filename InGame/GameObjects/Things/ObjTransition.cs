using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    class ObjTransition : GameObject
    {
        public Color TransitionColor;
        public float Percentage;

        public float Brightness;
        public float WobblePercentage;
        public bool WobbleTransition;

        private float _circleSize;

        public ObjTransition(Map.Map map) : base(map)
        {
            SprEditorImage = Resources.SprObjects;
            EditorIconSource = new Rectangle(240, 16, 16, 16);

            // should be on top of every other object,
            // except the player object but only while transitioning
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerTop, new CPosition(0, 0, 0)));
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            var gameWidth = Game1.RenderWidth;
            var gameHeight = Game1.RenderHeight;

            if (!WobbleTransition)
            {
                _circleSize = (1 - Percentage) * (float)Math.Sqrt(gameWidth * gameWidth + gameHeight * gameHeight) / 2;

                var playerPosition = new Vector2(
                    MapManager.ObjLink.PosX * MapManager.Camera.Scale,
                    (MapManager.ObjLink.PosY - 8 - MapManager.ObjLink.PosZ) * MapManager.Camera.Scale);
                var centerX = gameWidth / 2.0f - (MapManager.Camera.Location.X - playerPosition.X);
                var centerY = gameHeight / 2.0f - (MapManager.Camera.Location.Y - playerPosition.Y);

                Resources.CircleShader.Parameters["softRad"].SetValue(15f);
                Resources.CircleShader.Parameters["size"].SetValue(_circleSize);
                Resources.CircleShader.Parameters["centerX"].SetValue(centerX);
                Resources.CircleShader.Parameters["centerY"].SetValue(centerY);
                Resources.CircleShader.Parameters["width"].SetValue(gameWidth);
                Resources.CircleShader.Parameters["height"].SetValue(gameHeight);

                // draw the circle
                spriteBatch.Begin(SpriteSortMode.Immediate, null, SamplerState.PointWrap, null, null, Resources.CircleShader, Game1.GetMatrix);
                spriteBatch.Draw(Resources.SprWhite, new Rectangle(0, 0, gameWidth, gameHeight), TransitionColor);
                spriteBatch.End();
            }
            else
            {
                // draw the wobble transition effect
                Game1.GameManager.ChangeRenderTarget();

                Resources.WobbleEffect.Parameters["width"].SetValue(gameWidth);
                Resources.WobbleEffect.Parameters["height"].SetValue(gameHeight);
                Resources.WobbleEffect.Parameters["scale"].SetValue(MapManager.Camera.Scale);
                Resources.WobbleEffect.Parameters["brightness"].SetValue(Brightness);

                Resources.WobbleEffect.Parameters["offset"].SetValue(WobblePercentage * 30);
                Resources.WobbleEffect.Parameters["offsetWidth"].SetValue((0.5f - MathF.Cos(WobblePercentage * 4) / 2) * 3);
                Resources.WobbleEffect.Parameters["offsetHeight"].SetValue(16);

                spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointWrap, null, null, Resources.WobbleEffect);
                spriteBatch.Draw(Game1.GameManager.GetLastRenderTarget(), Vector2.Zero, Color.White);
                spriteBatch.End();
            }
        }
    }
}