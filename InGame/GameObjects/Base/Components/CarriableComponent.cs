using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base.CObjects;

namespace ProjectZ.InGame.GameObjects.Base.Components
{
    public class CarriableComponent : Component
    {
        public delegate void StartFunction();
        public StartFunction StartGrabbing;

        public delegate Vector3 InitFunction();
        public InitFunction Init;

        public delegate bool UpdatePositionFunction(Vector3 position);
        public UpdatePositionFunction UpdatePosition;

        public delegate void ThrowFunction(Vector2 direction);
        public ThrowFunction Throw;
        
        public delegate bool PullFunction(Vector2 direction);
        public PullFunction Pull;

        public CRectangle Rectangle;

        public int CarryHeight = 13;

        public bool IsHeavy;
        public bool IsPickedUp;
        public bool IsActive = true;

        public new static int Index = 3;
        public static int Mask = 0x01 << Index;

        public CarriableComponent(CRectangle rectangle, InitFunction init, UpdatePositionFunction updatePosition, ThrowFunction @throw)
        {
            Rectangle = rectangle;
            Init = init;
            UpdatePosition = updatePosition;
            Throw = @throw;
        }
    }
}
