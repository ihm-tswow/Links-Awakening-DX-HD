using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.Things;

namespace ProjectZ.Base.UI
{
    public class UiElement
    {
        public delegate void UiFunction(UiElement uiElement);

        public UiFunction ClickFunction;
        public UiFunction UpdateFunction;
        public UiFunction SizeUpdate;

        public SpriteFont Font;
        public Rectangle Rectangle;
        public Color BackgroundColor = Values.ColorUiEditor;
        public Color FontColor = new Color(255, 255, 255);

        public string[] Screens;
        public string ElementId;
        public virtual string Label { get; set; }
        public bool IsVisible = true;
        public bool Selected;
        public bool Remove;

        public UiElement(string elementId, string screen)
        {
            ElementId = elementId;
            Screens = screen.ToUpper().Split(':');
            Font = Resources.EditorFont;
        }

        public virtual void Update()
        {
            // select the element if the mouse if cursor is hovering over it
            Selected = InputHandler.MouseIntersect(Rectangle);
            // call the update function of the element
            UpdateFunction?.Invoke(this);
        }

        public virtual void Draw(SpriteBatch spriteBatch) { }

        public virtual void DrawBlur(SpriteBatch spriteBatch) { }
    }
}