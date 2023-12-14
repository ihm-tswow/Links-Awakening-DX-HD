using System;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base.CObjects;

namespace ProjectZ.InGame.GameObjects.Base.Components
{
    public class DrawComponent : Component, IComparable<DrawComponent>
    {
        public delegate void DrawTemplate(SpriteBatch spriteBatch);
        public DrawTemplate Draw;

        // The position is only used for sorting. Should it be removed?
        public CPosition Position;

        public int Layer;

        public bool IsActive = true;

        public new static int Index = 5;
        public static int Mask = 0x01 << Index;

        protected DrawComponent() { }

        public DrawComponent(int layer, CPosition position)
        {
            Layer = layer;
            Position = position;
        }

        public DrawComponent(DrawTemplate draw, int layer, CPosition position)
        {
            Draw = draw;
            Layer = layer;
            Position = position;
        }
        
        public int CompareTo(DrawComponent other)
        {
            var compare = Layer.CompareTo(other.Layer);

            if (compare != 0)
                return compare;

            return Position.Y.CompareTo(other.Position.Y);
        }
    }
}
