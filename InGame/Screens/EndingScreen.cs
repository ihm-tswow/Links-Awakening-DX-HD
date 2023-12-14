using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.Screens
{
    internal class EndingScreen : Screen
    {
        private float _counter;

        public EndingScreen(string screenId) : base(screenId)
        {

        }

        public override void OnLoad()
        {
            _counter = 2000;

            Game1.GameManager.ResetMusic();
            Game1.GameManager.SetMusic(67, 0);

            Game1.GbsPlayer.SetVolumeMultiplier(1.0f);
            Game1.GbsPlayer.Play();
        }

        public override void Update(GameTime gameTime)
        {
            _counter -= Game1.DeltaTime;
            if (_counter < 0)
                Game1.ScreenManager.ChangeScreen(Values.ScreenNameMenu);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Begin();
            spriteBatch.DrawString(Resources.GameFont, "Ending", new Vector2(100, 100), Color.Red);
            spriteBatch.End();
        }
    }
}
