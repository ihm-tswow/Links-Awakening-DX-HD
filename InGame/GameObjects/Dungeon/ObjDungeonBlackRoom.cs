using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Dungeon
{
    internal class ObjDungeonBlackRoom : GameObject
    {
        private readonly Rectangle _roomRectangle;
        private readonly Rectangle _roomRectangleSmall;
        private readonly string _saveId;

        private RectangleF _collisionRectangle;

        private float _changeCount;
        private float _percentage;
        
        private int _changeTime = 66;
        
        private bool _isTransitioning;

        public ObjDungeonBlackRoom(Map.Map map, int posX, int posY, string saveId, int width, int height) : base(map)
        {
            SprEditorImage = Resources.SprWhite;
            EditorIconSource = new Rectangle(0, 0, 16, 16);
            EditorColor = Color.Red * 0.75f;

            _saveId = saveId;

            if (!string.IsNullOrEmpty(_saveId) && Game1.GameManager.SaveManager.GetString(saveId) == "1")
            {
                IsDead = true;
                return;
            }

            _roomRectangle = new Rectangle(posX, posY, width, height);
            _roomRectangleSmall = new Rectangle(posX + 14, posY + 14, width - 28, height - 28);

            _collisionRectangle = new RectangleF(posX - 16, posY - 16, width + 32, height + 32);
            _changeCount = _changeTime;

            // room can also get uncovered by setting a key
            if (!string.IsNullOrEmpty(_saveId))
                AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(OnKeyChange));

            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(LightDrawComponent.Index, new LightDrawComponent(DrawLight) { Layer = Values.LightLayer2 });
            AddComponent(BlurDrawComponent.Index, new BlurDrawComponent(DrawBlur));
        }

        private void OnKeyChange()
        {
            var keyState = Game1.GameManager.SaveManager.GetString(_saveId);
            if (keyState == "1")
                _isTransitioning = true;
        }

        private void Update()
        {
            if (!_isTransitioning && _collisionRectangle.Intersects(MapManager.ObjLink.BodyRectangle))
                _isTransitioning = true;

            if (_isTransitioning)
            {
                _changeCount -= Game1.DeltaTime;
                if (_changeCount < 0)
                {
                    Map.Objects.DeleteObjects.Add(this);

                    if (!string.IsNullOrEmpty(_saveId))
                        Game1.GameManager.SaveManager.SetString(_saveId, "1");
                }
            }

            _percentage = _changeCount / _changeTime;
        }

        private void DrawLight(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Resources.SprWhite, _roomRectangleSmall, Color.Black * _percentage);
        }

        private void DrawBlur(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Resources.SprWhite, _roomRectangle, Color.Black * _percentage);
        }
    }
}