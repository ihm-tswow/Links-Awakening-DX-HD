using System.Collections.Generic;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Pools;
using ProjectZ.InGame.Map;

namespace ProjectZ.InGame.GameObjects.Base.Systems
{
    class SystemAnimation
    {
        public ComponentPool Pool;

        private readonly List<GameObject> _objectList = new List<GameObject>();

        public void Update(bool dialogOpen)
        {
            _objectList.Clear();
            Pool.GetComponentList(_objectList,
                (int)((MapManager.Camera.X - Game1.RenderWidth / 2) / MapManager.Camera.Scale),
                (int)((MapManager.Camera.Y - Game1.RenderHeight / 2) / MapManager.Camera.Scale),
                (int)(Game1.RenderWidth / MapManager.Camera.Scale),
                (int)(Game1.RenderHeight / MapManager.Camera.Scale), BaseAnimationComponent.Mask);

            foreach (var gameObject in _objectList)
            {
                if (!gameObject.IsActive)
                    continue;

                var animationComponent = (gameObject.Components[BaseAnimationComponent.Index]) as BaseAnimationComponent;

                // update the animation
                if (!dialogOpen || animationComponent.UpdateWithOpenDialog)
                    animationComponent.UpdateAnimation();
            }
        }
    }
}
