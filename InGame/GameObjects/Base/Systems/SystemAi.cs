using System.Collections.Generic;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Pools;
using ProjectZ.InGame.Map;

namespace ProjectZ.InGame.GameObjects.Base.Systems
{
    class SystemAi
    {
        public ComponentPool Pool;

        private readonly List<GameObject> _objectList = new List<GameObject>();

        public void Update()
        {
            _objectList.Clear();
            Pool.GetComponentList(_objectList,
                (int)((MapManager.Camera.X - Game1.RenderWidth / 2) / MapManager.Camera.Scale),
                (int)((MapManager.Camera.Y - Game1.RenderHeight / 2) / MapManager.Camera.Scale),
                (int)(Game1.RenderWidth / MapManager.Camera.Scale),
                (int)(Game1.RenderHeight / MapManager.Camera.Scale), AiComponent.Mask);

            foreach (var gameObject in _objectList)
            {
                if (!gameObject.IsActive)
                    continue;

                var aiComponent = (gameObject.Components[AiComponent.Index]) as AiComponent;

                aiComponent?.CurrentState.Update?.Invoke();

                foreach (var trigger in aiComponent.CurrentState.Trigger)
                    trigger.Update();

                foreach (var trigger in aiComponent.Trigger)
                    trigger.Update();
            }
        }
    }
}
