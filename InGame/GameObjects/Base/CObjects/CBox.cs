using ProjectZ.Base;

namespace ProjectZ.InGame.GameObjects.Base.CObjects
{
    public class CBox
    {
        public Box Box;

        public float OffsetX;
        public float OffsetY;
        public float OffsetZ;

        private readonly bool _followZ;

        public CBox(float posX, float posY, float posZ, float width, float height, float depth)
        {
            Box = new Box(posX, posY, posZ, width, height, depth);
        }

        public CBox(CPosition position, float offsetX, float offsetY, float offsetZ, float width, float height, float depth, bool followZ = false)
        {
            OffsetX = offsetX;
            OffsetY = offsetY;
            OffsetZ = offsetZ;

            Box.Width = width;
            Box.Height = height;
            Box.Depth = depth;

            _followZ = followZ;
            Set(position);
        }

        public CBox(CPosition position, float offsetX, float offsetY, float width, float height, float depth) :
            this(position, offsetX, offsetY, 0, width, height, depth)
        { }

        public void Set(CPosition position)
        {
            if (_followZ)
            {
                position.AddPositionListener(typeof(CBox), UpdateBoxZ);
                UpdateBoxZ(position);
            }
            else
            {
                position.AddPositionListener(typeof(CBox), UpdateBox);
                UpdateBox(position);
            }
        }

        private void UpdateBoxZ(CPosition position)
        {
            Box = new Box(
                position.X + OffsetX,
                position.Y + OffsetY - position.Z,
                OffsetZ,
                Box.Width, Box.Height, Box.Depth);
        }

        private void UpdateBox(CPosition position)
        {
            Box = new Box(
                position.X + OffsetX,
                position.Y + OffsetY,
                position.Z + OffsetZ,
                Box.Width, Box.Height, Box.Depth);
        }
    }
}
