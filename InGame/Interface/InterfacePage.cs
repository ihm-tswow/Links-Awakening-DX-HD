using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.Controls;

namespace ProjectZ.InGame.Interface
{
    public class InterfacePage
    {
        public InterfaceElement PageLayout;

        public virtual void OnLoad(Dictionary<string, object> intent) { }

        public virtual void OnPop(Dictionary<string, object> intent) { }

        public virtual void OnReturn(Dictionary<string, object> intent) { }

        public virtual void Update(CButtons pressedButtons, GameTime gameTime)
        {
            PageLayout?.PressedButton(pressedButtons);
            PageLayout?.Update();
        }

        public virtual void Draw(SpriteBatch spriteBatch, Vector2 position, int scale, float transparency)
        {
            PageLayout?.Draw(spriteBatch, position, scale, transparency);
        }
    }
}
