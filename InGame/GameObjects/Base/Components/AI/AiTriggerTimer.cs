
namespace ProjectZ.InGame.GameObjects.Base.Components.AI
{
    class AiTriggerTimer : AiTrigger
    {
        public int StartTime;
        public double CurrentTime;
        public bool State;

        public AiTriggerTimer(int startTime)
        {
            StartTime = startTime;
        }

        public override void OnInit()
        {
            State = false;
            CurrentTime = StartTime;
        }
        
        public override void Update()
        {
            if (CurrentTime > 0)
                CurrentTime -= Game1.DeltaTime;

            if (CurrentTime <= 0)
                State = true;
        }

        public void Reset()
        {
            CurrentTime = StartTime;
            State = false;
        }
    }
}
