using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.Screens
{
    internal class GameScreen : Screen
    {
        public GameScreen(string screenId) : base(screenId) { }

        public override void Load(ContentManager content) { }

        public override void OnLoad()
        {
            Game1.GameManager.OnLoad();
        }

        public override void Update(GameTime gameTime)
        {
            Game1.EditorUi.CurrentScreen = Values.ScreenNameGame;

            Game1.GameManager.UpdateGame();
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Game1.GameManager.DrawGame(spriteBatch);
        }

        public override void DrawTop(SpriteBatch spriteBatch)
        {
            Game1.GameManager.DrawTop(spriteBatch);
        }

        public override void DrawRenderTarget(SpriteBatch spriteBatch)
        {
            Game1.GameManager.DrawRenderTarget(spriteBatch);
        }

        public override void OnResize(int newWidth, int newHeight)
        {
            Game1.GameManager.OnResize();
        }

        public override void OnResizeEnd(int newWidth, int newHeight)
        {
            Game1.GameManager.OnResizeEnd();
        }
    }
}
