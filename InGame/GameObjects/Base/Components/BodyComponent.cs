using Microsoft.Xna.Framework;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Base.Components
{
    public class BodyComponent : Component
    {
        public delegate void MoveCollisionFunction(Values.BodyCollision collision);
        public delegate void HoleAbsorbFunction();
        public delegate void HoleOnPullFunction(Vector2 direction, float percentage);
        public delegate void DeepWaterFunction();

        public MoveCollisionFunction MoveCollision;
        public HoleAbsorbFunction HoleAbsorb;
        public HoleOnPullFunction HoleOnPull;
        public DeepWaterFunction OnDeepWaterFunction;

        public CBox BodyBox;
        public CPosition Position;

        public Vector3 Velocity;
        public Vector2 VelocityTarget;
        public Vector2 LastVelocityTarget;
        public Vector2 SlideOffset;
        public Vector2 HoleAbsorption;

        // used for the rolling bands
        // could probably be done in a better way (interface?)
        public Vector2 AdditionalMovementVT;
        public Vector2 LastAdditionalMovementVT;

        public RectangleF FieldRectangle = RectangleF.Empty;

        public MapStates.FieldStates CurrentFieldState = MapStates.FieldStates.Init;
        public Values.CollisionTypes CollisionTypes = Values.CollisionTypes.Normal;
        public Values.CollisionTypes CollisionTypesIgnore;
        public Values.CollisionTypes AvoidTypes;

        public Values.BodyCollision VelocityCollision;
        public Values.BodyCollision LastVelocityCollision;

        public float JumpStartHeight;
        public float MaxJumpHeight = 4;
        public float Drag = 0.8f;
        public float DragAir = 0.9f;
        public float DragWater = 0.95f;
        public float Gravity = -0.25f;
        public float Gravity2D = 0.1f;
        public float Gravity2DWater = 0.025f;
        public float Bounciness = 0;
        public float Bounciness2D = 0;
        public float SpeedMultiply = 1;
        public float AbsorbPercentage = 1.0f;
        // not sure why this was changed from beeing zero
        public float AbsorbStop = 0.15f;
        public float MaxSlideDistance = 6.0f;

        public float Width
        {
            get => BodyBox.Box.Width;
            set => BodyBox.Box.Width = value;
        }
        public float Height
        {
            get => BodyBox.Box.Height;
            set => BodyBox.Box.Height = value;
        }
        public float Depth
        {
            get => BodyBox.Box.Depth;
            set => BodyBox.Box.Depth = value;
        }
        public float OffsetX
        {
            get => BodyBox.OffsetX;
            set => BodyBox.OffsetX = value;
        }
        public float OffsetY
        {
            get => BodyBox.OffsetY;
            set => BodyBox.OffsetY = value;
        }

        public int DeepWaterOffset = -3;

        public int Level = 0;

        // used to make the xy movement happen in one step
        // if there is a collision do not move any of them independently
        public bool SimpleMovement = false;
        // if the body is already inside a collider ignore the collider or not
        public bool IgnoreInsideCollision = true;

        public bool IsActive = true;
        public bool IsGrounded = true;
        public bool WasGrounded = true;
        public bool IgnoresZ;
        public bool IgnoreHoles;
        public bool IsPusher;
        public bool IgnoreHeight;
        public bool IsSlider;
        public bool IsAbsorbed;
        public bool WasHolePulled;
        public bool DisableVelocityTargetMultiplier;    // this is used for the vacuum
        public bool RestAdditionalMovement = true;
        public bool SplashEffect = true;
        public bool UpdateFieldState = true;

        public new static int Index = 2;
        public static int Mask = 0x01 << Index;

        public BodyComponent(CPosition position, int offsetX, int offsetY, int width, int height, int depth)
        {
            Position = position;
            BodyBox = new CBox(position, offsetX, offsetY, width, height, depth);
        }
    }
}
