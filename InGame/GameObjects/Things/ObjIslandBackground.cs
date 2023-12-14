using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjIslandBackground : GameObject
    {
        class Cloud
        {
            public int Index;
            public float LiveCounter;
            public float Transparency = 1;
            public Vector2 Position;

            public Cloud(int index, Vector2 position)
            {
                Index = index;
                Position = position;
                LiveCounter = Game1.RandomNumber.Next(5000, 50000);
            }
        }

        private Rectangle[] cloudSourceRectangles = new Rectangle[3];

        private Cloud[] _clouds;

        private const int GradientHeight = 120;

        private readonly Rectangle _waveSource;
        private readonly Rectangle _topWaveSource;

        private readonly Color _colorSky = new Color(65, 90, 255);
        private readonly Color _colorOceanBright = new Color(66, 89, 255);

        private readonly DictAtlasEntry _oceanGradient;

        private const int LeftCloudPosition = -1300;

        private int _topWaveFrame;
        private int _topWaveSpeed = 250;

        public ObjIslandBackground() : base("water_3") { }

        public ObjIslandBackground(Map.Map map, int posX, int posY) : base(map)
        {
            _waveSource = Resources.SourceRectangle("water_3");
            _waveSource.Width = 16;
            _topWaveSource = Resources.SourceRectangle("water_12");
            _topWaveSource.Width = 16;

            cloudSourceRectangles[0] = Resources.SourceRectangle("cloud_0");
            cloudSourceRectangles[1] = Resources.SourceRectangle("cloud_1");
            cloudSourceRectangles[2] = Resources.SourceRectangle("cloud_2");

            _clouds = new Cloud[50];

            _oceanGradient = Resources.GetSprite("overworld_gradient");

            // spawn the clouds
            var positionX = LeftCloudPosition;
            for (var i = 0; i < _clouds.Length; i++)
            {
                var index = Game1.RandomNumber.Next(0, cloudSourceRectangles.Length);
                _clouds[i] = new Cloud(index, new Vector2(positionX, 32 - cloudSourceRectangles[index].Height));
                positionX += cloudSourceRectangles[index].Width + Game1.RandomNumber.Next(1, 7) * 16;
            }

            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerBackground, new CPosition(posX, posY, 0)));
        }

        public void Update()
        {
            // all the animations are in sync
            _topWaveFrame = ((int)(Game1.TotalGameTime) % (4 * _topWaveSpeed)) / _topWaveSpeed;

            // move the clouds
            foreach (var cloud in _clouds)
            {
                var cloudSpeed = (Math.Sin(cloud.Position.X / 100) * 0.125 + 1) * 0.015 * Game1.TimeMultiplier;
                cloud.Position.X -= (float)cloudSpeed;

                // set the cloud to the right position
                if (cloud.Position.X < LeftCloudPosition)
                {
                    float rightPosition = 0;
                    foreach (var cloud1 in _clouds)
                    {
                        if (cloud1.Position.X > rightPosition)
                            rightPosition = cloud1.Position.X + cloudSourceRectangles[cloud1.Index].Width;
                    }
                    cloud.Position.X = rightPosition + Game1.RandomNumber.Next(1, 7) * 16;
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (MapManager.Camera.Scale <= 0)
                return;

            var cameraRectangle = MapManager.Camera.GetCameraRectangle();

            var left = (int)(cameraRectangle.X / _waveSource.Width / MapManager.Camera.Scale) - 1;
            var right = (int)(cameraRectangle.Right / _waveSource.Width / MapManager.Camera.Scale) + 1;
            var top = (int)(cameraRectangle.Y / _waveSource.Height / MapManager.Camera.Scale);
            var bottom = (int)(cameraRectangle.Bottom / _waveSource.Height / MapManager.Camera.Scale) + 1;

            if (cameraRectangle.X < 0)
            {
                left--;
                right++;
            }
            if (cameraRectangle.Y < 0)
            {
                top--;
                bottom++;
            }

            // draw the top waves
            if (top <= 2 && bottom > 2)
                for (var x = left; x < right + 1; x++)
                {
                    spriteBatch.Draw(Resources.SprObjects, new Rectangle(
                            x * _topWaveSource.Width, 2 * _topWaveSource.Height, _topWaveSource.Width, _topWaveSource.Height),
                        new Rectangle(
                            _topWaveSource.X + _topWaveSource.Width * _topWaveFrame, _topWaveSource.Y,
                            _topWaveSource.Width, _topWaveSource.Height), Color.White);
                }

            // change context to have smooth gradient transition
            spriteBatch.End();
            ObjectManager.SpriteBatchBeginAnisotropic(spriteBatch, null);

            if (top <= 10 && bottom >= 4)
                DrawGradient(spriteBatch, left, right, 3, 10);
            if (left < 1)
                DrawGradient(spriteBatch, left, 2, Math.Max(3, top), Math.Min(bottom, GradientHeight + 3));
            if (right > 16 * 10)
                DrawGradient(spriteBatch, 16 * 10, right, Math.Max(3, top), Math.Min(bottom, GradientHeight + 3));

            spriteBatch.End();
            ObjectManager.SpriteBatchBegin(spriteBatch, null);

            var oceanBottomTop = Math.Max(3 + GradientHeight, top);
            var oceanBottomBottom = Math.Max(3 + GradientHeight, bottom);

            spriteBatch.Draw(Resources.SprWhite, new Rectangle(
                    left * Values.TileSize, oceanBottomTop * Values.TileSize, (right - left + 1) * Values.TileSize, (oceanBottomBottom - oceanBottomTop + 1) * Values.TileSize), _colorOceanBright);

            // draw the sky
            if (top < 2)
            {
                spriteBatch.Draw(Resources.SprWhite, new Rectangle(
                    left * 16, (top - 1) * 16,
                    (right - left + 1) * 16, (-top + 3) * 16), _colorSky);
            }

            // draw the clouds
            foreach (var cloud in _clouds)
            {
                DrawHelper.DrawNormalized(spriteBatch, Resources.SprObjects, cloud.Position, cloudSourceRectangles[cloud.Index], Color.White * cloud.Transparency);
            }
        }

        private void DrawGradient(SpriteBatch spriteBatch, int left, int right, int top, int bottom)
        {
            spriteBatch.Draw(_oceanGradient.Texture, new Rectangle(
                left * Values.TileSize, top * Values.TileSize, (right - left) * Values.TileSize, (bottom - top) * Values.TileSize),
                new Rectangle(
                    _oceanGradient.ScaledRectangle.X,
                    _oceanGradient.ScaledRectangle.Y + (int)(_oceanGradient.ScaledRectangle.Height * ((top + 0) / (float)GradientHeight)),
                    _oceanGradient.ScaledRectangle.Width,
                    (int)(_oceanGradient.ScaledRectangle.Height * ((bottom - (top + 3)) / (float)GradientHeight))), Color.White);
        }
    }
}