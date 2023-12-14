using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.Map
{
    public class Camera
    {
        public Matrix TransformMatrix => Matrix.CreateScale(Scale) *
                                            Matrix.CreateTranslation(new Vector3(-RoundX, -RoundY, 0)) *
                                            Matrix.CreateTranslation(new Vector3((int)(_viewportWidth * 0.5f), (int)(_viewportHeight * 0.5f), 0)) *
                                            Game1.GameManager.GetMatrix;
        public Vector2 Location;
        public Vector2 MoveLocation;

        // this is needed so there is no texture bleeding while rendering the game
        public float RoundX => (int)Math.Round(Location.X + ShakeOffsetX * Scale, MidpointRounding.AwayFromZero);
        public float RoundY => (int)Math.Round(Location.Y + ShakeOffsetY * Scale, MidpointRounding.AwayFromZero);

        public float Scale = 4;
        public float ShakeOffsetX;
        public float ShakeOffsetY;
        public float CameraFollowMultiplier = 1;

        public int X => (int)Math.Round(Location.X + ShakeOffsetX * Scale);
        public int Y => (int)Math.Round(Location.Y + ShakeOffsetY * Scale);

        private Vector2 _cameraDistance;

        private int _viewportWidth;
        private int _viewportHeight;

        public void SetBounds(int viewportWidth, int viewportHeight)
        {
            _viewportWidth = viewportWidth;
            _viewportHeight = viewportHeight;
        }

        public Rectangle GetCameraRectangle()
        {
            var rectangle = new Rectangle(
                (int)RoundX - _viewportWidth / 2,
                (int)RoundY - _viewportHeight / 2,
                _viewportWidth, _viewportHeight);

            return rectangle;
        }

        public Rectangle GetGameView()
        {
            var rectangle = new Rectangle(
                (int)(RoundX / Scale) - (int)(_viewportWidth / 2 / Scale),
                (int)(RoundY / Scale) - (int)(_viewportHeight / 2 / Scale),
                (int)(_viewportWidth / Scale), (int)(_viewportHeight / Scale));

            return rectangle;
        }

        public Rectangle GetGameViewBig()
        {
            var rectangle = new Rectangle(
                (int)(RoundX / Scale) - Values.MinWidth,
                (int)(RoundY / Scale) - Values.MinHeight,
                Values.MinWidth * 2, Values.MinHeight * 2);

            return rectangle;
        }

        public void Center(Vector2 position, bool moveX, bool moveY)
        {
            if (!GameSettings.SmoothCamera)
            {
                Location = position;
                return;
            }

            var direction = position - MoveLocation;

            if (direction != Vector2.Zero)
            {
                var distance = direction.Length() / Scale * CameraFollowMultiplier;
                var speedMult = CameraFunction(distance / 12.5f);

                direction.Normalize();
                var cameraSpeed = direction * speedMult * Scale * Game1.TimeMultiplier;

                if (moveX)
                    MoveLocation.X += cameraSpeed.X;
                if (moveY)
                    MoveLocation.Y += cameraSpeed.Y;

                if (distance <= 0.1f * Game1.TimeMultiplier)
                    MoveLocation = position;
            }

            // this is needed so the player does not wiggle around while the camera is following him
            if (moveX)
                _cameraDistance.X = position.X - MoveLocation.X;
            if (moveY)
                _cameraDistance.Y = position.Y - MoveLocation.Y;

            Location = new Vector2((int)Math.Round(position.X), (int)Math.Round(position.Y)) - _cameraDistance;
        }

        private float CameraFunction(float x)
        {
            var y = MathF.Atan(x);

            if (x > 2)
                y += (x - 2) / 2;

            return y + 0.1f;
        }

        public void ForceUpdate(Vector2 lockPosition)
        {
            MoveLocation = lockPosition;
            Location = lockPosition;
        }

        public void SoftUpdate(Vector2 position)
        {
            MoveLocation = position - _cameraDistance;
            Location = position;
        }

        public void OffsetCameraDistance(Vector2 offset)
        {
            _cameraDistance += offset;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!Game1.DebugMode)
                return;

            var size = 10;
            spriteBatch.Draw(Resources.SprWhite, new Rectangle(
                Game1.WindowWidthEnd / 2 - (int)(size * Scale),
                Game1.WindowHeightEnd / 2 - (int)(size * Scale),
                (int)(size * Scale * 2),
                (int)(size * Scale * 2)), Color.Pink * 0.25f);
        }
    }
}
