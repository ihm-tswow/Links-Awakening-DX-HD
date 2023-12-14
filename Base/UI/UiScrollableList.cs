using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.Things;

namespace ProjectZ.Base.UI
{
    public class UiScrollableList : UiElement
    {
        public string[] ItemList = new string[0];
        public int SelectionListItemHeight = 20;
        public int? Selection;
        public int? MouseOverSelection;

        public int MaxLength;
        public int ListLength;
        public int ListPositionY;

        public int SelectionListState
        {
            get => MathHelper.Clamp(_selectionListState, 0, ItemList.Length - (Rectangle.Height) / SelectionListItemHeight);
            set => _selectionListState = value;
        }

        private int _selectionListState;

        public UiScrollableList(Rectangle rectangle, string elementId, string screen, UiFunction update)
            : base(elementId, screen)
        {
            Rectangle = rectangle;
            UpdateFunction = update;
        }

        public override void Update()
        {
            base.Update();

            Selection = null;
            MouseOverSelection = null;

            var scrollDirection = MathHelper.Clamp(InputHandler.LastMousState.ScrollWheelValue - InputHandler.MouseState.ScrollWheelValue, -1, 1);
            if (InputHandler.MouseIntersect(Rectangle))
                SelectionListState += scrollDirection;

            MaxLength = Rectangle.Height / SelectionListItemHeight;
            ListLength = MathHelper.Min(ItemList.Length, MaxLength);
            ListPositionY = Rectangle.Height / 2 - (MaxLength * SelectionListItemHeight) / 2;

            if (InputHandler.MouseIntersect(new Rectangle(Rectangle.X, Rectangle.Y + ListPositionY, Rectangle.Width, ListLength * SelectionListItemHeight)))
            {
                var selection = (InputHandler.MousePosition().Y - Rectangle.Y - ListPositionY) / SelectionListItemHeight;

                if (!(selection == 0 && selection + SelectionListState > 0) && !(selection == ListLength - 1 && selection + SelectionListState != ItemList.Length - 1))
                    MouseOverSelection = selection + SelectionListState;
                if (InputHandler.MouseLeftPressed())
                {
                    MouseOverSelection = null;

                    if (selection == 0 && selection + SelectionListState > 0)
                        SelectionListState = 0;
                    else if (selection == ListLength - 1 && selection + SelectionListState != ItemList.Length - 1)
                        SelectionListState = ItemList.Length - Rectangle.Height / SelectionListItemHeight;
                    else
                        Selection = selection + SelectionListState;
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            for (var i = 0; i < ListLength; i++)
            {
                string strText;
                if (i == 0 && i + SelectionListState > 0)
                    strText = "▲";
                else if (i == ListLength - 1 && i + SelectionListState != ItemList.Length - 1)
                    strText = "▼";
                else
                    strText = ItemList[i + SelectionListState];

                var drawRectangle = new Rectangle(Rectangle.X, Rectangle.Y +
                                                               i * SelectionListItemHeight + ListPositionY, Rectangle.Width, SelectionListItemHeight);
                //mark if the mouse is over
                if (InputHandler.MouseIntersect(drawRectangle))
                    spriteBatch.Draw(Resources.SprWhite, drawRectangle, Color.Black * 0.25f);

                Basics.DrawStringCenter(Font, strText,
                    new Rectangle(Rectangle.X, Rectangle.Y + i * SelectionListItemHeight + ListPositionY, Rectangle.Width, SelectionListItemHeight), Color.White);
            }
        }
    }
}