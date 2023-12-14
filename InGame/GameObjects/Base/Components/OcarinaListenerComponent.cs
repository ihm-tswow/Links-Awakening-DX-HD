
namespace ProjectZ.InGame.GameObjects.Base.Components
{
    public class OcarinaListenerComponent : Component
    {
        public new static int Index = 15;
        public static int Mask = 0x01 << Index;

        public delegate void OcarinaPlayedTemplate(int ocarinaSong);
        public OcarinaPlayedTemplate OcarinaPlayedFunction;

        protected OcarinaListenerComponent() { }

        public OcarinaListenerComponent(OcarinaPlayedTemplate ocarinaPlayedFunction)
        {
            OcarinaPlayedFunction = ocarinaPlayedFunction;
        }
    }
}
