using Microsoft.Xna.Framework.Graphics;

namespace ProjectZ.InGame.GameSystems
{
    public class GameSystem
    {
        public virtual void OnLoad() { }

        public virtual void Update() { }

        public virtual void Draw(SpriteBatch spriteBatch) { }
    }
}
