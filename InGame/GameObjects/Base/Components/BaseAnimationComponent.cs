
namespace ProjectZ.InGame.GameObjects.Base.Components
{
    class BaseAnimationComponent : Component
    {
        public new static int Index = 1;
        public static int Mask = 0x01 << Index;

        public bool UpdateWithOpenDialog;

        public virtual void UpdateAnimation() { }
    }
}
