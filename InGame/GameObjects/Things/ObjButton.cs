using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjButton : GameObject
    {
        private readonly BoxCollisionComponent _collisionComponent;
        private readonly DrawSpriteComponent _sprite;

        private readonly string _strKey;

        private float _counter;
        private readonly int _pushTime = 200;

        private bool _isColliding;
        private bool _isActivated;

        public ObjButton() : base("button") { }

        public ObjButton(Map.Map map, int posX, int posY, string strKey) : base(map)
        {
            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 16, 16);

            _strKey = strKey;

            _collisionComponent = new BoxCollisionComponent(new CBox(EntityPosition, 3, 3, 10, 10, 2), Values.CollisionTypes.Normal);
            _sprite = new DrawSpriteComponent("button", EntityPosition, Vector2.Zero, Values.LayerBottom);

            AddComponent(ObjectCollisionComponent.Index, new ObjectCollisionComponent(new Rectangle(posX + 3, posY + 3, 10, 10), OnCollision));
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(CollisionComponent.Index, _collisionComponent);
            AddComponent(DrawComponent.Index, _sprite);
            AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(OnKeyChange));
        }

        private void OnKeyChange()
        {
            // was the button already pressed before?
            if (!string.IsNullOrEmpty(_strKey) && Game1.GameManager.SaveManager.GetString(_strKey) == "1")
                Activate();
        }

        private void Update()
        {
            if (_isActivated)
                return;

            if (_isColliding)
            {
                _counter -= Game1.DeltaTime;

                // activate the button if the player is standing on it long enough
                if (_counter <= 0)
                {
                    Game1.GameManager.PlaySoundEffect("D370-14-0E");

                    if (!string.IsNullOrEmpty(_strKey))
                        Game1.GameManager.SaveManager.SetString(_strKey, "1");

                    Activate();
                }
            }
            else
                _counter = _pushTime;

            _isColliding = false;
        }

        private void OnCollision(GameObject gameObject)
        {
            // is the player standing on the button?
            if (MapManager.ObjLink._body.IsGrounded)
                _isColliding = true;
        }

        private void Activate()
        {
            if (_isActivated)
                return;

            _isActivated = true;
            _collisionComponent.CollisionBox.Box.Depth = 0;
            _sprite.Sprite.SourceRectangle.X += 16;
        }
    }
}