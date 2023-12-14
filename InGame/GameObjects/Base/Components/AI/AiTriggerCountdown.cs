namespace ProjectZ.InGame.GameObjects.Base.Components.AI
{
    class AiTriggerCountdown : AiTrigger
    {
        public delegate void TriggerFunction(double counter);
        public delegate void TriggerEndFunction();

        public TriggerFunction TickFunction;
        public TriggerEndFunction CountdownEnd;

        public double CurrentTime;
        public int StartTime;
        public bool ResetAfterEnd;

        private bool _initRunningState;
        private bool _isRunning;

        public AiTriggerCountdown(int startTime, TriggerFunction tickFunction, TriggerEndFunction countdownEnd, bool initRunningState = true)
        {
            StartTime = startTime;

            TickFunction = tickFunction;
            CountdownEnd = countdownEnd;

            _initRunningState = initRunningState;
        }

        public override void OnInit()
        {
            CurrentTime = StartTime;
            _isRunning = _initRunningState;
        }

        public override void Update()
        {
            if (!_isRunning)
                return;

            CurrentTime -= Game1.DeltaTime;

            if (CurrentTime <= 0)
            {
                _isRunning = false;
                CountdownEnd?.Invoke();
                if (ResetAfterEnd)
                    OnInit();
            }
            else
                TickFunction?.Invoke(CurrentTime);
        }

        public bool IsRunning()
        {
            return _isRunning;
        }

        public void Restart()
        {
            CurrentTime = StartTime;
            _isRunning = true;
        }

        public void Start()
        {
            _isRunning = true;
        }

        public void Stop()
        {
            _isRunning = false;
        }
    }
}
