
namespace ProjectZ.InGame.GameObjects.Base.Components.AI
{
    class AiTriggerUpdate : AiTrigger
    {
        public delegate void UpdateFunction();
        public UpdateFunction UpdateFun;

        public AiTriggerUpdate(UpdateFunction update)
        {
            UpdateFun = update;
        }

        public override void Update()
        {
            UpdateFun?.Invoke();
        }
    }
}
