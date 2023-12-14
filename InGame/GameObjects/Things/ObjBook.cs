using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    class ObjBook : GameObject
    {
        private readonly BodyComponent _body;
        private readonly CSprite _sprite;
        private readonly DictAtlasEntry _spriteBook;
        private readonly DictAtlasEntry _spriteBookOpen;
        private readonly string _strKey;
        private readonly string _dialogKey;
        // when the dialogKey + "_open" key is set the book sprite will be shown as opened
        private readonly string _openBookKey;
        private bool _hasFallen;

        public ObjBook() : base("book_0") { }

        public ObjBook(Map.Map map, int posX, int posY, string strKey, string dialogKey, int spriteIndex) : base(map)
        {
            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -16, 16, 18);

            _strKey = strKey;
            _dialogKey = dialogKey;
            _openBookKey = dialogKey + "_open";

            spriteIndex = MathHelper.Clamp(spriteIndex, 0, 3);

            // has the book already fallen down?
            if (!string.IsNullOrEmpty(_strKey) && Game1.GameManager.SaveManager.GetString(_strKey) != "1")
            {
                EntityPosition.Z = 30;
                EntitySize = new Rectangle(-Values.FieldWidth / 2, -32, Values.FieldWidth, Values.FieldHeight);
            }
            else
                _hasFallen = true;

            _spriteBook = Resources.GetSprite("book_" + spriteIndex);
            _spriteBookOpen = Resources.GetSprite("book_open");
            _sprite = new CSprite(_spriteBook, EntityPosition, new Vector2(-4, -11));

            var tileRectangle = map.GetField(posX, posY);
            _body = new BodyComponent(EntityPosition, -4, -11, 8, 11, 8)
            {
                IsActive = false,
                Gravity = -0.125f,
                Bounciness = 0.45f,
            };

            var interactionBox = new CBox(EntityPosition, -6, -11, 0, 12, 13, 8);
            if (!_hasFallen)
            {
                var hitBox = new CBox(EntityPosition.X + EntitySize.X, EntityPosition.Y + EntitySize.Y, 0, EntitySize.Width, EntitySize.Height, 16);
                AddComponent(HittableComponent.Index, new HittableComponent(hitBox, OnHit));
            }
            AddComponent(BodyComponent.Index, _body);
            AddComponent(InteractComponent.Index, new InteractComponent(interactionBox, OnInteract));
            AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(OnKeyChange));
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(_sprite, Values.LayerBottom));
        }

        private void OnKeyChange()
        {
            var keyState = Game1.GameManager.SaveManager.GetString(_openBookKey);
            var openBook = keyState == "1";
            _sprite.SetSprite(openBook ? _spriteBookOpen : _spriteBook);
            _sprite.DrawOffset.X = -_sprite.SourceRectangle.Width / 2 * _sprite.Scale;
            _sprite.DrawOffset.Y = -11;
        }

        private bool OnInteract()
        {
            Game1.GameManager.StartDialogPath(_dialogKey);

            return true;
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (_hasFallen)
                return Values.HitCollision.None;

            if (damageType == HitType.PegasusBootsPush)
                StartFalling();

            return Values.HitCollision.None;
        }

        private void StartFalling()
        {
            _hasFallen = true;
            _body.IsActive = true;

            if (!string.IsNullOrEmpty(_strKey))
                Game1.GameManager.SaveManager.SetString(_strKey, "1");
        }
    }
}
