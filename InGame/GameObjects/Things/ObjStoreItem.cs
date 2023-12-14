using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjStoreItem : GameObject
    {
        private readonly GameItem _item;

        private readonly Rectangle _sourceRectangle;
        private readonly Vector2 _itemPosition;

        private readonly string _itemName;
        private readonly int _itemPrice;
        private readonly int _itemCount;

        private bool _holding;

        public ObjStoreItem() : base("item") { }

        public ObjStoreItem(Map.Map map, int posX, int posY, string itemName, int itemPrice, int count) : base(map)
        {
            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 32, 40);

            _itemPrice = itemPrice;
            _itemCount = count;

            _itemName = itemName;
            _item = Game1.GameManager.ItemManager[itemName];

            if (_item == null)
            {
                IsDead = true;
                return;
            }

            if (_item.SourceRectangle.HasValue)
                _sourceRectangle = _item.SourceRectangle.Value;
            else
            {
                var baseItem = Game1.GameManager.ItemManager[_item.Name];
                _sourceRectangle = baseItem.SourceRectangle.Value;
            }

            var countLength = _itemCount.ToString().Length;
            var textWidth = _itemCount > 1 ? ItemDrawHelper.LetterWidth * countLength + countLength + 6 : 0;

            _itemPosition = new Vector2(
                EntityPosition.X + 16 - (int)(_sourceRectangle.Width + textWidth) / 2,
                EntityPosition.Y + 22 - _sourceRectangle.Height / 2);

            var interactRectangle = new CBox(posX, posY, 0, 32, 40, 16);
            AddComponent(InteractComponent.Index, new InteractComponent(interactRectangle, Interact));
            AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(KeyChanged));
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerPlayer, EntityPosition));
            AddComponent(DrawShadowComponent.Index, new DrawShadowComponent(DrawShadow));
        }

        private bool Interact()
        {
            if (!_holding)
            {
                if (MapManager.ObjLink.StoreItem != null)
                    return false;

                Game1.GameManager.SaveManager.SetString("itemShopItem", _itemName);
                Game1.GameManager.SaveManager.SetString("itemShopPrice", _itemPrice.ToString());
                Game1.GameManager.SaveManager.SetString("itemShopCount", _itemCount.ToString());

                MapManager.ObjLink.StartHoldingItem(_item);
            }
            else
            {

                MapManager.ObjLink.StopHoldingItem();
            }

            Game1.GameManager.PlaySoundEffect("D360-19-13");

            _holding = !_holding;

            return true;
        }

        private void KeyChanged()
        {
            if (!_holding)
                return;

            var value = Game1.GameManager.SaveManager.GetString("holdItem");
            var result = Game1.GameManager.SaveManager.GetString("result");

            if (value == "0")
            {
                _holding = false;
                MapManager.ObjLink.StopHoldingItem();

                // the item was bought?
                if (result != null && result == "0")
                    Map.Objects.DeleteObjects.Add(this);
            }
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            if (_holding)
                return;

            // draw the price of the item
            var priceLength = _itemPrice.ToString().Length;
            var textWidth = ItemDrawHelper.LetterWidth * priceLength + priceLength - 1;
            ItemDrawHelper.DrawNumber(spriteBatch,
                    (int)(EntityPosition.X + 16 - textWidth / 2f),
                    (int)(EntityPosition.Y + 12 - ItemDrawHelper.LetterHeight), _itemPrice, priceLength, 1, Color.Black);

            if (_itemCount > 1)
            {
                spriteBatch.DrawString(Resources.GameFont, "x",
                    new Vector2(
                        (int)(_itemPosition.X + _sourceRectangle.Width),
                        (int)(EntityPosition.Y + 24 - ItemDrawHelper.LetterHeight)), Color.Black);

                var countLength = _itemCount.ToString().Length;
                ItemDrawHelper.DrawNumber(spriteBatch,
                        (int)(_itemPosition.X + _sourceRectangle.Width + 7),
                        (int)(EntityPosition.Y + 25 - ItemDrawHelper.LetterHeight), _itemCount, countLength, 1, Color.Black);
            }

            // draw the item
            ItemDrawHelper.DrawItem(spriteBatch, _item, _itemPosition, Color.White, 1, true);
        }

        private void DrawShadow(SpriteBatch spriteBatch)
        {
            if (_holding)
                return;

            var baseItem = _item.SourceRectangle.HasValue ? _item : Game1.GameManager.ItemManager[_item.Name];
            var sourceRectangle = baseItem.SourceRectangle.Value;

            DrawHelper.DrawShadow(Resources.SprItem, _itemPosition,
                sourceRectangle, sourceRectangle.Width, sourceRectangle.Height,
                false, Map.ShadowHeight, Map.ShadowRotation, Color.White);
        }
    }
}