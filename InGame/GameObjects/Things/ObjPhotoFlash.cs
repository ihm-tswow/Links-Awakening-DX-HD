using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    class ObjPhotoFlash : GameObject
    {
        private Rectangle _rectangle;

        private float _flashTime = 125;
        private float _percentage = 1;
        private bool _fullScreen;

        public ObjPhotoFlash(Map.Map map) : base(map)
        {
            _rectangle = new Rectangle(0, 0, Map.MapWidth * Values.TileSize, Map.MapHeight * Values.TileSize);

            // on the overworld we use a fullscreen flash
            if (map.MapWidth >= 4 || map.MapHeight >= 4)
                _fullScreen = true;

            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerTop, new CPosition(0, 0, 0)));
            AddComponent(LightDrawComponent.Index, new LightDrawComponent(DrawLight));
        }

        public override void Init()
        {
            Game1.GameManager.PlaySoundEffect("D378-63-40");
        }

        private void Update()
        {
            _flashTime -= Game1.DeltaTime;
            if (_flashTime > 0)
                return;

            _percentage -= Game1.TimeMultiplier * 0.075f;
            if (_percentage < 0)
                Map.Objects.DeleteObjects.Add(this);
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            if (_fullScreen)
                spriteBatch.Draw(Resources.SprWhite, MapManager.Camera.GetGameView(), Color.White * _percentage);
            else
                spriteBatch.Draw(Resources.SprWhite, _rectangle, Color.White * _percentage);
        }

        private void DrawLight(SpriteBatch spriteBatch)
        {
            if (_fullScreen)
                spriteBatch.Draw(Resources.SprWhite, MapManager.Camera.GetGameView(), Color.White * _percentage);
            else
                spriteBatch.Draw(Resources.SprWhite, _rectangle, Color.White * _percentage);
        }
    }
}