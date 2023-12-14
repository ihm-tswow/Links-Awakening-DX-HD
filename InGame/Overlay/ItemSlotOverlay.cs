using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.Base.UI;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.Overlay
{
    class ItemSlotOverlay
    {
        public static Rectangle RecItemselection = new Rectangle(6, 29, 30, 20);

        public static int DistX = 2;
        public static int DistY = 2;

        public Point ItemSlotPosition;

        //  3
        // 2 1
        //  0
        private static Rectangle[] _itemSlots = {
            new Rectangle(RecItemselection.Width + DistX / 2 - RecItemselection.Width / 2,
                RecItemselection.Height * 2 + DistY * 2,
                RecItemselection.Width, RecItemselection.Height),
            new Rectangle(RecItemselection.Width + DistX, RecItemselection.Height + DistY,
                RecItemselection.Width, RecItemselection.Height),
            new Rectangle(0, RecItemselection.Height + DistY,
                RecItemselection.Width, RecItemselection.Height),
            new Rectangle(RecItemselection.Width + DistX / 2 - RecItemselection.Width / 2, 0,
                RecItemselection.Width, RecItemselection.Height)
        };

        private static readonly UiRectangle[] _uiBackgroundBoxes = new UiRectangle[4];

        public ItemSlotOverlay()
        {
            for (var i = 0; i < _itemSlots.Length; i++)
            {
                _uiBackgroundBoxes[i] =
                    new UiRectangle(_itemSlots[i], "itemBox" + i, Values.ScreenNameGame, Values.OverlayBackgroundColor, Values.OverlayBackgroundBlurColor, null) { Radius = Values.UiBackgroundRadius };
                Game1.EditorUi.AddElement(_uiBackgroundBoxes[i]);
            }
        }

        public void SetTransparency(float transparency)
        {
            for (var i = 0; i < _itemSlots.Length; i++)
            {
                _uiBackgroundBoxes[i].BackgroundColor = Values.OverlayBackgroundColor * transparency;
                _uiBackgroundBoxes[i].BlurColor = Values.OverlayBackgroundBlurColor * transparency;
            }
        }

        public static void Draw(SpriteBatch spriteBatch, Point position, int scale, float transparency)
        {
            // draw the item slots
            for (var i = 0; i < _itemSlots.Length; i++)
            {
                var slotRectangle = new Rectangle(_itemSlots[i].X, _itemSlots[i].Y, RecItemselection.Width, RecItemselection.Height);
                ItemDrawHelper.DrawItemWithInfo(spriteBatch, Game1.GameManager.Equipment[i], position, slotRectangle, scale, Color.White * transparency);
            }
        }

        public void UpdatePositions(Rectangle uiWindow, Point offset, int scale)
        {
            // bottom left corner
            ItemSlotPosition = new Point(uiWindow.X + 16 * scale,
                uiWindow.Y + uiWindow.Height - (RecItemselection.Height * 3 + DistY * 2 + 16) * scale);

            // update the background rectangles
            for (var i = 0; i < _itemSlots.Length; i++)
            {
                _uiBackgroundBoxes[i].Rectangle = new Rectangle(
                    ItemSlotPosition.X + _itemSlots[i].X * scale + offset.X,
                    ItemSlotPosition.Y + _itemSlots[i].Y * scale + offset.Y,
                    _itemSlots[i].Width * scale, _itemSlots[i].Height * scale);
            }
        }
    }
}
