using Microsoft.Xna.Framework.Input;

namespace ProjectZ.InGame.Controls
{
    public class ButtonMapper
    {
        public Keys[] Keys;
        public Buttons[] Buttons;

        public ButtonMapper(Keys[] keys, Buttons[] buttons)
        {
            Keys = keys;
            Buttons = buttons;
        }
    }
}