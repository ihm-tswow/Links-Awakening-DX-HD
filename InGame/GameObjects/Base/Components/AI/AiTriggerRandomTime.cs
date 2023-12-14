
namespace ProjectZ.InGame.GameObjects.Base.Components.AI
{
    class AiTriggerRandomTime : AiTrigger
    {
        public delegate void TriggerFunction();

        public TriggerFunction Triggered;

        public double CurrentTime;

        public int MinTime;
        public int MaxTime;

        public bool IsRunning = true;
        public bool ResetAfterEnd = true;

        public AiTriggerRandomTime(TriggerFunction triggered, int minTime, int maxTime)
        {
            Triggered = triggered;

            MinTime = minTime;
            MaxTime = maxTime;
        }

        public override void OnInit()
        {
            IsRunning = true;
            CurrentTime = Game1.RandomNumber.Next(MinTime, MaxTime);
        }

        public override void Update()
        {
            if (!IsRunning)
                return;

            CurrentTime -= Game1.DeltaTime;

            if (CurrentTime > 0)
                return;

            Triggered();

            if (ResetAfterEnd)
                OnInit();
            else
                IsRunning = false;
        }
    }
}
