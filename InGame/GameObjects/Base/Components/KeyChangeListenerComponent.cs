
namespace ProjectZ.InGame.GameObjects.Base.Components
{
    public class KeyChangeListenerComponent : Component
    {
        public new static int Index = 14;
        public static int Mask = 0x01 << Index;

        public delegate void KeyChangeTemplate();
        public KeyChangeTemplate KeyChangeFunction;

        protected KeyChangeListenerComponent() { }

        public KeyChangeListenerComponent(KeyChangeTemplate keyChangeFunction)
        {
            KeyChangeFunction = keyChangeFunction;
        }
    }
}
