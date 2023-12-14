using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;
using System;

namespace ProjectZ.InGame.GameObjects.Dungeon
{
    public class ObjTowerBackground : GameObject
    {
        private readonly DictAtlasEntry _clouds;
        private readonly Color _colorSky;
        private readonly Color _colorCloud;
        private readonly Vector2 _spawnPosition;

        private float _cloudOffset;

        public ObjTowerBackground() : base("final_cloud") { }

        public ObjTowerBackground(Map.Map map, int posX, int posY) : base(map)
        {
            _spawnPosition = new Vector2(posX, posY);

            _clouds = Resources.GetSprite("tower_clouds");
            _colorSky = new Color(106, 98, 253);
            _colorCloud = new Color(254, 254, 254);

            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerBackground, new CPosition(posX, posY, 0)));
        }

        private void Update()
        {
            // move the clouds
            _cloudOffset += (0.75f + MathF.Sin(_cloudOffset) * 0.25f) * 0.0125f * Game1.TimeMultiplier;
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            var cameraRectangle = MapManager.Camera.GetCameraRectangle();

            // draw the cloud background
            spriteBatch.Draw(Resources.SprWhite, new Rectangle(
                (int)(cameraRectangle.X / MapManager.Camera.Scale - MapManager.Camera.Scale),
                (int)(_spawnPosition.Y + 80),
                (int)(cameraRectangle.Width / MapManager.Camera.Scale + MapManager.Camera.Scale * 2),
                (int)(Map.MapHeight * Values.TileSize - (int)(_spawnPosition.Y + 80))), _colorCloud);

            // draw the sky background
            spriteBatch.Draw(Resources.SprWhite, new Rectangle(
                (int)(cameraRectangle.X / MapManager.Camera.Scale - MapManager.Camera.Scale),
                (int)(cameraRectangle.Y / MapManager.Camera.Scale - MapManager.Camera.Scale),
                (int)(cameraRectangle.Width / MapManager.Camera.Scale + MapManager.Camera.Scale * 2),
                (int)(_spawnPosition.Y + 48) - (int)(cameraRectangle.Y / MapManager.Camera.Scale - MapManager.Camera.Scale)), _colorSky);

            // draw the clouds
            var leftCloud = (int)Math.Floor((cameraRectangle.X / MapManager.Camera.Scale) / _clouds.SourceRectangle.Width) - 1;
            var rightCloud = (int)Math.Ceiling((cameraRectangle.Right / MapManager.Camera.Scale) / _clouds.SourceRectangle.Width);
            for (var i = leftCloud; i < rightCloud; i++)
                //if (i != 0)
                    DrawHelper.DrawNormalized(spriteBatch, _clouds, new Vector2(_spawnPosition.X + _clouds.SourceRectangle.Width * i + _cloudOffset % _clouds.SourceRectangle.Width, _spawnPosition.Y + 48), Color.White);

        }
    }
}
