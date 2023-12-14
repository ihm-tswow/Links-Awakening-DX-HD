using Microsoft.Xna.Framework;
using ProjectZ.Base;

namespace ProjectZ.InGame.GameObjects.Base.CObjects
{
    public class CRectangle
    {
        public RectangleF Rectangle;
        public Rectangle OffsetSize;

        private bool MoveZ;

        public CRectangle(Rectangle rectangle)
        {
            Rectangle = rectangle;
        }

        public CRectangle(RectangleF rectangle)
        {
            Rectangle = rectangle;
        }

        public CRectangle(CPosition position, Rectangle offsetSize, bool moveZ = false)
        {
            OffsetSize = offsetSize;
            position.AddPositionListener(typeof(CRectangle), UpdateRectangle);
            UpdateRectangle(position);
            MoveZ = moveZ;
        }

        public void UpdateRectangle(CPosition position)
        {
            Rectangle = new RectangleF(
                position.X + OffsetSize.X,
                position.Y + OffsetSize.Y + (MoveZ ? -position.Z : 0),
                OffsetSize.Width, OffsetSize.Height);
        }
    }
}
