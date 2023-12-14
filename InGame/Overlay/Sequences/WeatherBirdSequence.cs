using Microsoft.Xna.Framework;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.Overlay.Sequences
{
    class WeatherBirdSequence : GameSequence
    {
        private SeqAnimation _objUlrich;

        public WeatherBirdSequence()
        {
            _sequenceWidth = 160;
            _sequenceHeight = 144;
        }

        public override void OnStart()
        {
            Sprites.Clear();
            SpriteDict.Clear();

            var position = Vector2.Zero;

            // background
            Sprites.Add(new SeqSprite("seqWeatherBirdBackground", position, 0));

            Sprites.Add(new SeqAnimation("Sequences/weather bird objects", "top", new Vector2(position.X, position.Y + 112), 1));
            for (int i = 0; i < 5; i++)
                Sprites.Add(new SeqAnimation("Sequences/weather bird objects", "bottom", new Vector2(position.X + 32 * i, position.Y + 136), 1));
            Sprites.Add(new SeqAnimation("Sequences/weather bird objects", "rotator", new Vector2(position.X + 95, position.Y + 56), 1));
            
            // flowers
            Sprites.Add(new SeqAnimation("Sequences/weather bird objects", "flower_red", new Vector2(position.X + 32, position.Y + 128), 1));
            Sprites.Add(new SeqAnimation("Sequences/weather bird objects", "flower_red", new Vector2(position.X + 64, position.Y + 120), 1));
            Sprites.Add(new SeqAnimation("Sequences/weather bird objects", "flower_white", new Vector2(position.X + 8, position.Y + 128), 1));
            Sprites.Add(new SeqAnimation("Sequences/weather bird objects", "flower_white", new Vector2(position.X + 104, position.Y + 128), 1));
            Sprites.Add(new SeqAnimation("Sequences/weather bird objects", "flower_white", new Vector2(position.X + 144, position.Y + 120), 1));

            // link and marin
            AddDrawable("weatherBirdLink", new SeqAnimation("Sequences/weather bird link", "walk", new Vector2(position.X - 40, position.Y + 112), 3) { Shader = Resources.ColorShader, Color = Game1.GameManager.CloakColor });
            AddDrawable("weatherBirdMarin", new SeqAnimation("Sequences/weather bird marin", "walk", new Vector2(position.X - 40 - 22, position.Y + 112), 3));
            AddDrawable("weatherBirdUlrich", _objUlrich = new SeqAnimation("Sequences/weather bird ulrich", "stopped", new Vector2(position.X + 180, position.Y + 112), 2));

            AddDrawable("weatherBirdPhotoFlash", new SeqColor(new Rectangle((int)position.X, (int)position.Y, 160, 144), Color.Transparent, 5));

            // start the sequence path
            Game1.GameManager.StartDialogPath("seq_weather_bird");

            base.OnStart();
        }

        public override void Update()
        {
            // @HACK: ulrich needs to change the layer
            if (Game1.GameManager.SaveManager.GetString("weatherBirdUlrichFront", "0") == "1")
                _objUlrich.Layer = 4;

            base.Update();
        }
    }
}
