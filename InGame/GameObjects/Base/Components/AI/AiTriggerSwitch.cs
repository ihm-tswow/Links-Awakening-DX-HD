
namespace ProjectZ.InGame.GameObjects.Base.Components.AI
{
    class AiTriggerSwitch : AiTrigger
    {
        public int StartTime;
        public double CurrentTime;
        public bool State;

        public AiTriggerSwitch(int startTime)
        {
            StartTime = startTime;
        }

        public override void OnInit()
        {
            State = true;
            CurrentTime = 0;
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
