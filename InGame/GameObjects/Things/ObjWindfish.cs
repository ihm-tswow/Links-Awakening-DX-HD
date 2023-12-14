using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Things;
using System;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjWindfish : GameObject
    {
        private readonly CSprite _sprite;
        private readonly Vector2 _spawnPosition;

        private CPosition _drawPosition;
        private string _spawnKey;
        private float _hoverCounter;
        private double _wobbleCounter;
        private double _wobbleTime;
        private bool _isVisible;

        public ObjWindfish() : base("editor_windfish") { }

        public ObjWindfish(Map.Map map, int posX, int posY, string spawnKey) : base(map)
        {
            _spawnKey = spawnKey;
            _spawnPosition = new Vector2(posX, posY);

            _sprite = new CSprite("final_wale", _drawPosition = new CPosition(posX, posY, 0)) { Color = Color.Transparent, SpriteShader = Resources.WindFishShader };

            if (!string.IsNullOrEmpty(_spawnKey))
                AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(OnKeyChange));
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(_sprite, Values.LayerBackground));
        }

        private void OnKeyChange()
        {
            if (!_isVisible && Game1.GameManager.SaveManager.GetString(_spawnKey) == "1")
            {
                Game1.GameManager.PlaySoundEffect("D370-31-1F");
                _isVisible = true;
            }
            if (_isVisible && Game1.GameManager.SaveManager.GetString(_spawnKey) == "0")
            {
                Game1.GameManager.PlaySoundEffect("D370-31-1F");
                _isVisible = false;
            }
        }

        private void Update()
        {
            _hoverCounter += Game1.DeltaTime;
            _drawPosition.Y = _spawnPosition.Y + MathF.Sin(_hoverCounter / 2000 * MathF.PI * 2) * 2;

            if (_isVisible && _wobbleCounter < 3500)
                UpdateFadeAnimation(1);
            if (!_isVisible && _wobbleCounter > 0)
                UpdateFadeAnimation(-1);
        }

        private void UpdateFadeAnimation(int dir)
        {
            _wobbleTime += Game1.DeltaTime / 125;
            _wobbleCounter += Game1.DeltaTime * dir;
            if (_wobbleCounter < 0)
                _wobbleCounter = 0;
            if (_wobbleCounter > 3500)
                _wobbleCounter = 3500;

            var offset = 0.05f - 0.05f * MathHelper.Clamp((float)(_wobbleCounter - (3500 - 650)) / 650, 0, 1);
            var period = 25f - 5f * MathHelper.Clamp((float)(_wobbleCounter - 1500) / 2000, 0, 1);

            Resources.WindFishShader.FloatParameter["Offset"] = offset;
            Resources.WindFishShader.FloatParameter["Period"] = period;
            Resources.WindFishShader.FloatParameter["Time"] = (float)_wobbleTime;

            var fadePercentage = (float)_wobbleCounter / 3000;
            _sprite.Color = Color.White * fadePercentage;
        }
    }
}