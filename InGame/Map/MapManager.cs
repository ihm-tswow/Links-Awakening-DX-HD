using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.Map
{
    public class MapManager
    {
        public static Camera Camera;
        public static ObjLink ObjLink;

        public static BlendState LightBlendState = new BlendState();

        public Map CurrentMap;
        public Map NextMap;

        public bool UpdateCameraX;
        public bool UpdateCameraY;

        private Matrix blurMatrix;

        public MapManager()
        {
            CurrentMap = new Map();
            NextMap = new Map();
            Camera = new Camera();

            LightBlendState.ColorBlendFunction = BlendFunction.Add;
            LightBlendState.ColorDestinationBlend = Blend.InverseSourceAlpha;
            LightBlendState.ColorSourceBlend = Blend.One;

            LightBlendState.AlphaBlendFunction = BlendFunction.Max;
            LightBlendState.AlphaDestinationBlend = Blend.One;
            LightBlendState.AlphaSourceBlend = Blend.One;
        }

        public void Load()
        {
            ObjLink = new ObjLink();
        }

        public void Update(bool frozen)
        {
            // update the objects on the map
            CurrentMap.Objects.Update(frozen);

            CurrentMap.UpdateMapUpdateState();

            UpdateCamera();
        }

        public void UpdateAnimation()
        {
            CurrentMap.Objects.UpdateAnimations();
        }

        public Vector2 GetCameraTarget()
        {
            // update the camera
            if (CurrentMap.CameraTarget.HasValue)
                return CurrentMap.CameraTarget.Value * Camera.Scale;

            return GetCameraTargetLink();
        }

        public Vector2 GetCameraTargetLink()
        {
            return new Vector2(ObjLink.PosX, ObjLink.PosY - 4) * Camera.Scale;
        }

        public void UpdateCamera()
        {
            //if (!UpdateCameraX)
            //centerPosition.Y -= ObjLink.EntityPosition.Z;

            // center the map vertical if it is smaller than the screen
            // not so sure about this
            //if (CurrentMap.MapHeight * 16 * Camera.Scale < Game1.WindowHeight)
            //    centerPosition.Y = CurrentMap.MapHeight * 8 * Camera.Scale;

            if (UpdateCameraX || UpdateCameraY)
                Camera.Center(GetCameraTarget(), UpdateCameraX, UpdateCameraY);

            UpdateCameraX = true;
            UpdateCameraY = true;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // draw the objects under the tilemap
            CurrentMap.Objects.DrawBottom(spriteBatch);

            //Game1.StopWatchTracker.Start("draw tile layers");

            // draw the tile map
            CurrentMap.TileMap.Draw(spriteBatch);

            //Game1.StopWatchTracker.Stop();

            // draw the objects draw over the tileset
            CurrentMap.Objects.DrawMiddle(spriteBatch);

            // draw the blured part of the tile map
            DrawBlur(spriteBatch, Game1.GameManager.TempRT2, Game1.GameManager.TempRT0, Game1.GameManager.TempRT1);

            // draw the objects
            CurrentMap.Objects.Draw(spriteBatch);

            spriteBatch.End();
        }

        public void DrawBegin(SpriteBatch spriteBatch, Effect spriteEffect)
        {
            if (!Game1.GameManager.UseShockEffect)
                spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointWrap, null, null, spriteEffect, blurMatrix);
            else
                ObjectManager.SpriteBatchBegin(spriteBatch, null);
        }

        private void DrawBlur(SpriteBatch spriteBatch, RenderTarget2D blurRT0, RenderTarget2D blurRT1, RenderTarget2D blurRT2)
        {
            var matrixPosition = Vector2.Zero;

            if (!Game1.GameManager.UseShockEffect)
            {
                Game1.Graphics.GraphicsDevice.SetRenderTarget(blurRT0);
                Game1.Graphics.GraphicsDevice.Clear(Color.Transparent);

                var cameraPosition = new Vector2(Camera.RoundX / Camera.Scale, Camera.RoundY / Camera.Scale);
                // offset the position a little bit because we render a bigger region to avoid artefacts at the edges
                matrixPosition = new Vector2((int)cameraPosition.X - 1, (int)cameraPosition.Y - 1);

                blurMatrix = Matrix.CreateScale(1) *
                             Matrix.CreateTranslation(new Vector3(-matrixPosition.X, -matrixPosition.Y, 0)) *
                             Matrix.CreateTranslation(new Vector3(
                                (int)(Game1.GameManager.SideBlurRenderTargetWidth * 0.5f),
                                (int)(Game1.GameManager.SideBlurRenderTargetHeight * 0.5f), 0)) *
                             Matrix.CreateScale(new Vector3(
                                (float)blurRT0.Width / Game1.GameManager.SideBlurRenderTargetWidth,
                                (float)blurRT0.Height / Game1.GameManager.SideBlurRenderTargetHeight, 0));
            }

            DrawBegin(spriteBatch, null);

            // draw object blur stuff
            CurrentMap.Objects.DrawBlur(spriteBatch);

            // blur tile maps
            CurrentMap.TileMap.DrawBlurLayer(spriteBatch);

            spriteBatch.End();

            if (Game1.GameManager.UseShockEffect)
                return;

            Resources.BBlurEffectH.Parameters["pixelX"].SetValue(1.0f / blurRT1.Width);
            Resources.BBlurEffectV.Parameters["pixelY"].SetValue(1.0f / blurRT1.Height);

            // v blur
            Game1.Graphics.GraphicsDevice.SetRenderTarget(blurRT1);
            Game1.Graphics.GraphicsDevice.Clear(Color.Transparent);
            // offset the render target so that we always sample at the same position
            var blurX = -(matrixPosition.X % 2) / 2 + (blurRT1.Width % 2 * 0.5f);
            var blurY = -(matrixPosition.Y % 2) / 2 + (blurRT1.Height % 2 * 0.5f);
            spriteBatch.Begin(SpriteSortMode.Immediate, null, SamplerState.AnisotropicClamp, null, null, Resources.BBlurEffectV, null);
            spriteBatch.Draw(blurRT0, new Vector2(blurX, blurY),
                new Rectangle(0, 0, blurRT0.Width, blurRT0.Height), Color.White, 0, Vector2.Zero, 0.5f, SpriteEffects.None, 0);
            spriteBatch.End();

            // h blur
            Game1.Graphics.GraphicsDevice.SetRenderTarget(blurRT2);
            Game1.Graphics.GraphicsDevice.Clear(Color.Transparent);
            spriteBatch.Begin(SpriteSortMode.Immediate, null, SamplerState.AnisotropicClamp, null, null, Resources.BBlurEffectH, null);
            spriteBatch.Draw(blurRT1, Vector2.Zero, Color.White);
            spriteBatch.End();

            Game1.GameManager.SetActiveRenderTarget();

            spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.AnisotropicClamp, null, null, null, Camera.TransformMatrix);

            // calculate offset to make sure that the render target is at the correct position
            var scale = new Vector2((float)Game1.GameManager.BlurRenderTargetWidth / blurRT1.Width * 2, (float)Game1.GameManager.BlurRenderTargetHeight / blurRT1.Height * 2);
            spriteBatch.Draw(blurRT2, new Vector2(
                matrixPosition.X + matrixPosition.X % 2 - (int)(Game1.GameManager.SideBlurRenderTargetWidth * 0.5f) - (int)(Game1.RenderWidth / (Camera.Scale * 2)) % 2,
                matrixPosition.Y + matrixPosition.Y % 2 - (int)(Game1.GameManager.SideBlurRenderTargetHeight * 0.5f) - (int)(Game1.RenderHeight / (Camera.Scale * 2)) % 2),
                new Rectangle(0, 0, blurRT2.Width, blurRT2.Height), Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 0);

            spriteBatch.End();
        }

        public void DrawLight(SpriteBatch spriteBatch)
        {
            Game1.Graphics.GraphicsDevice.Clear(CurrentMap.LightColor);

            spriteBatch.Begin(SpriteSortMode.Deferred, LightBlendState, SamplerState.AnisotropicClamp, null, null, null, Camera.TransformMatrix);

            CurrentMap.Objects.DrawLight(spriteBatch);

            spriteBatch.End();
        }

        public void ReloadMap()
        {
            // @Hack
            var tempTm = CurrentMap;
            CurrentMap = NextMap;
            NextMap = tempTm;

            NextMap.Objects.ReloadObjects();
            NextMap.Objects.SpawnObject(ObjLink);

            // reset the hole map
            SaveLoadMap.CreateEmptyHoleMap(NextMap.HoleMap, NextMap.MapWidth, NextMap.MapHeight);

            ObjLink.MapInit();

            tempTm = CurrentMap;
            CurrentMap = NextMap;
            NextMap = tempTm;

            CurrentMap.Objects.TriggerKeyChange();
        }

        public void FinishLoadingMap(Map map)
        {
            ObjLink.Map.Objects.RemoveObject(ObjLink);

            // set the player to the correct position
            ObjLink.FinishLoadingMap(map);

            // add the player to the map
            map.Objects.SpawnObject(ObjLink);

            // call key change event for the newly added objects
            map.Objects.TriggerKeyChange();
        }
    }
}
