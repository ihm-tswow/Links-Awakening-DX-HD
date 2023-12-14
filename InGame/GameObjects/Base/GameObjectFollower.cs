using Microsoft.Xna.Framework;

namespace ProjectZ.InGame.GameObjects.Base
{
    public class GameObjectFollower : GameObject
    {
        public GameObjectFollower(string spriteId) : base(spriteId) { }

        public GameObjectFollower(Map.Map map) : base(map) { }

        public virtual void SetPosition(Vector2 position) { }
    }
}
