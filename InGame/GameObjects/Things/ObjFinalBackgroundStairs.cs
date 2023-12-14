using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    public class ObjFinalBackgroundStairs : GameObject
    {
        private readonly string _despawnKey;

        private const int DespawnTime = 500;
        private double _despawnCounter;
        private bool _despawning;

        public ObjFinalBackgroundStairs() : base("editor_final_platform") { }

        public ObjFinalBackgroundStairs(Map.Map map, int posX, int posY, string despawnKey) : base(map)
        {
            _despawnKey = despawnKey;

            var sprite = new CSprite("final_background_stairs", new CPosition(posX, posY + 1, 0), new Vector2(0, -1)) { SpriteShader = Resources.ThanosSpriteShader0 };
            Resources.ThanosSpriteShader0.FloatParameter["Percentage"] = 0;

            if (!string.IsNullOrEmpty(_despawnKey))
                AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(OnKeyChange));
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(sprite, Values.LayerBackground));
        }

        private void OnKeyChange()
        {
            if (!_despawning && Game1.GameManager.SaveManager.GetString(_despawnKey) == "1")
                _despawning = true;
        }

        private void Update()
        {
            if (!_despawning)
                return;

            _despawnCounter += Game1.DeltaTime;
            if (_despawnCounter > DespawnTime)
            {
                Map.Objects.DeleteObjects.Add(this);
                return;
            }

            var percentage = (float)_despawnCounter / DespawnTime;
            Resources.ThanosSpriteShader0.FloatParameter["Percentage"] = percentage;
        }
    }
}
