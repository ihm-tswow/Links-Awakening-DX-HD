using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ProjectZ.InGame.Screens
{
    public class Screen
    {
        public string Id;

        public Screen(string screenId)
        {
            Id = screenId.ToUpper();
        }

        public virtual void Load(ContentManager content) { }
        
        public virtual void Update(GameTime gameTime) { }

        public virtual void Draw(SpriteBatch spriteBatch) { }

        public virtual void DrawTop(SpriteBatch spriteBatch) { }

        public virtual void DrawRenderTarget(SpriteBatch spriteBatch) { }

        public virtual void OnResize(int newWidth, int newHeight) { }

        public virtual void OnResizeEnd(int newWidth, int newHeight) { }

        public virtual void OnLoad() { }
    }
}
