using System.Collections.Generic;
using ProjectZ.InGame.GameObjects.Base.Components.AI;

namespace ProjectZ.InGame.GameObjects.Base.Components
{
    class AiComponent : Component
    {
        public new static int Index = 0;
        public static int Mask = 0x01 << Index;

        public Dictionary<string, AiState> States = new Dictionary<string, AiState>();
        public List<AiTrigger> Trigger = new List<AiTrigger>();

        public AiState CurrentState;

        public string CurrentStateId;
        public string LastStateId;

        public void ChangeState(string newStateId, bool silentMode = false)
        {
            LastStateId = CurrentStateId;
            CurrentStateId = newStateId;
            CurrentState = States[newStateId];

            if (!silentMode)
            {
                CurrentState.Init?.Invoke();

                foreach (var trigger in CurrentState.Trigger)
                    trigger.OnInit();
            }
        }
    }
}
