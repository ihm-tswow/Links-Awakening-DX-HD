using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Dungeon
{
    internal class ObjDungeonEntrance : GameObject
    {
        private readonly BoxCollisionComponent _collisionComponent;
        private readonly CSprite _sprite;

        private readonly string _strKey;
        private float _counter;
        private int _openSpeed = 40;
        private bool _opening;
        private bool _isOpen;

        public ObjDungeonEntrance(Map.Map map, int posX, int posY, string spriteName, string strKey) : base(map)
        {
            var sprite = Resources.GetSprite(spriteName);
            if (sprite == null)
            {
                IsDead = true;
                return;
            }

            SprEditorImage = sprite.Texture;
            EditorIconSource = sprite.ScaledRectangle;

            _strKey = strKey;
            EntityPosition = new CPosition(posX, posY, 0);

            // do not spawn the entrance if it is already open
            if (!string.IsNullOrEmpty(_strKey) && Game1.GameManager.SaveManager.GetString(_strKey) == "1")
            {
                IsDead = true;
                return;
            }

            AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(OnKeyChange));
            AddComponent(CollisionComponent.Index, _collisionComponent =
                new BoxCollisionComponent(new CBox(posX, posY, 0, 16, 16, 16), Values.CollisionTypes.Normal));
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            _sprite = new CSprite(sprite.Texture, EntityPosition, sprite.ScaledRectangle, Vector2.Zero);
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(_sprite, Values.LayerBottom));
        }

        private void Update()
        {
            if (!_opening) 
                return;

            _counter -= Game1.DeltaTime;

            if (_counter > 0) 
                return;

            _counter += _openSpeed;

            if (_sprite.SourceRectangle.Height > 0)
            {
                _sprite.SourceRectangle.Y++;
                _sprite.SourceRectangle.Height--;

                if(_sprite.SourceRectangle.Height == 8)
                    Game1.GameManager.PlaySoundEffect("D360-35-23", false, 1, 0, true);
            }
            else
            {
                _opening = false;
                _isOpen = true;
                _collisionComponent.IsActive = false;
            }
        }

        private void Open()
        {
            if (_isOpen)
                return;

            _opening = true;
        }

        private void OnKeyChange()
        {
            if (!string.IsNullOrEmpty(_strKey) && Game1.GameManager.SaveManager.GetString(_strKey) == "1")
                Open();
        }
    }
}