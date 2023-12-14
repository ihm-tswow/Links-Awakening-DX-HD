namespace ProjectZ.InGame.GameObjects.Base.Components
{
    class UpdateComponent : Component
    {
        public delegate void UpdateTemplate();
        public UpdateTemplate UpdateFunction;

        public new static int Index = 12;
        public static int Mask = 0x01 << Index;

        protected UpdateComponent() { }

        public UpdateComponent(UpdateTemplate updateFunction)
        {
            UpdateFunction = updateFunction;
        }
    }
}
