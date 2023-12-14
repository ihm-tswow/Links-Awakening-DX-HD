using System;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.GameObjects.Enemies;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjChest : GameObject
    {
        private readonly AiComponent _aiComponent;
        private readonly ObjSprite _spriteFront;
        private readonly CSprite _spriteBack;
        private readonly GameObject _spawnObject;

        private ObjSprite _itemSprite;
        private GameItem _item;

        private readonly string _itemName;
        private readonly string _locationBound;
        public readonly string ItemKey;
        private readonly string _dialogPath;

        // @INFO: this time is also used in the "seashell" script and should be changed there also
        private const int FadeTime = 175;
        private const int MoveTime = 250;

        private bool _isActive = true;
        public override bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                _spriteFront.IsActive = value;

                // this is needed when the last shell in a chest is found because the chest will get deactivated
                if (_itemSprite != null)
                    _itemSprite.Sprite.Color = Color.Transparent;

                CheckOpened();
            }
        }

        private bool _opened;

        public ObjChest() : base("chest") { }

        public ObjChest(Map.Map map, int posX, int posY, string itemName, string itemBounding, string itemKey, int spriteType, bool hitMode) : base(map)
        {
            EntityPosition = new CPosition(posX, posY + 13, 0);
            EntitySize = new Rectangle(0, -13, 16, 16);

            _itemName = itemName;
            _locationBound = itemBounding;
            ItemKey = itemKey;

            var openingTrigger = new AiTriggerCountdown(MoveTime, OpeningTick, OpeningEnd);
            var fadingTrigger = new AiTriggerCountdown(FadeTime, FadeTick, FadeEnd);

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("closed", new AiState());
            _aiComponent.States.Add("opening", new AiState { Init = InitOpen, Trigger = { openingTrigger } });
            _aiComponent.States.Add("opened", new AiState { Init = InitOpen });
            _aiComponent.States.Add("textbox", new AiState(UpdateTextBox));
            _aiComponent.States.Add("fading", new AiState { Trigger = { fadingTrigger } });
            _aiComponent.ChangeState("closed");

            AddComponent(AiComponent.Index, _aiComponent);

            if (!hitMode)
                AddComponent(InteractComponent.Index, new InteractComponent(new CBox(posX + 4, posY + 3, 0, 8, 13, 16), Interact));
            else
            {
                AddComponent(HittableComponent.Index, new HittableComponent(new CBox(posX + 4, posY + 3, 0, 8, 13, 16), OnHit));
            }

            AddComponent(CollisionComponent.Index, new BoxCollisionComponent(
                new CBox(posX, posY + 3, 0, 16, 11, 12), Values.CollisionTypes.Normal | Values.CollisionTypes.Hookshot));

            _spriteBack = new CSprite("chest_back", new CPosition(posX, posY + 12.9f, 0), new Vector2(0, -12.9f));
            _spriteBack.SourceRectangle.X += spriteType * 32;
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(_spriteBack, Values.LayerPlayer));

            // sprite front
            _spriteFront = new ObjSprite(map, posX, posY + 13, "chest_front", Vector2.Zero, Values.LayerPlayer, null);
            _spriteFront.Sprite.SourceRectangle.X += spriteType * 32;
            Map.Objects.SpawnObject(_spriteFront);

            // check if the chest was already opened
            if (!CheckOpened())
            {
                if (_itemName == "greenZol")
                {
                    // @TODO: sound effect
                    var greenZol = new EnemyGreenZol(Map, posX, posY, 8, false);
                    greenZol.SpawnDelay();
                    _spawnObject = greenZol;
                }
                else if (_itemName != null && _itemName.StartsWith("dialog:"))
                {
                    _dialogPath = _itemName.Remove(0, 7);
                }
                else if (!CreateItem())
                    IsDead = true;
            }
        }

        private Values.HitCollision OnHit(GameObject originObject, Vector2 direction, HitType type, int damage, bool pieceOfPower)
        {
            if ((type & HitType.ThrownObject) != 0)
            {
                OpenChest();
                return Values.HitCollision.Blocking;
            }

            return Values.HitCollision.None;
        }

        private bool CheckOpened()
        {
            if (!string.IsNullOrEmpty(ItemKey) && Game1.GameManager.SaveManager.GetString(ItemKey) == "1")
            {
                _aiComponent.ChangeState("opened");
                return true;
            }

            return false;
        }

        private bool CreateItem()
        {
            if (_itemName == null)
                return false;

            _item = Game1.GameManager.ItemManager[_itemName];

            if (_item == null)
                return false;

            Rectangle itemSource;

            if (_item.SourceRectangle.HasValue)
                itemSource = _item.SourceRectangle.Value;
            else
            {
                var baseItem = Game1.GameManager.ItemManager[_item.Name];
                itemSource = baseItem.SourceRectangle.Value;
            }

            // the offset is needed so the item would not be behind the chest
            _itemSprite = new ObjSprite(Map, 0, 0, Resources.SprItem, itemSource, new Vector2(0, -itemSource.Height + 1), Values.LayerPlayer);
            _itemSprite.EntityPosition.Set(new Vector2(EntityPosition.X + 8 - itemSource.Width / 2f, EntityPosition.Y - 0.05f));
            _itemSprite.Sprite.Color = Color.Transparent;

            Map.Objects.SpawnObject(_itemSprite);

            return true;
        }

        private void InitOpen()
        {
            if (_opened)
                return;

            _opened = true;
            _spriteBack.SourceRectangle.X += 16;
            _spriteFront.Sprite.SourceRectangle.X += 16;
        }

        private void OpeningTick(double tick)
        {
            MapManager.ObjLink.FreezePlayer();
            _itemSprite.EntityPosition.Z = (float)Math.Sin((float)(MoveTime - tick) / MoveTime * Math.PI / 1.55f) * 10;
        }

        private void OpeningEnd()
        {
            OpeningTick(0);
            PickUpItem();
        }

        private void PickUpItem()
        {
            _aiComponent.ChangeState("textbox");

            var collectedItem = new GameItemCollected(_itemName)
            { Count = _item.Count, LocationBounding = _locationBound };
            MapManager.ObjLink.PickUpItem(collectedItem, true);

            SetKey();
        }

        private void UpdateTextBox()
        {
            // don't fade away if the item is shown
            if (_item.ShowAnimation != 0)
                FadeEnd();
            else
                _aiComponent.ChangeState("fading");
        }

        private void FadeTick(double time)
        {
            _itemSprite.Sprite.Color = Color.White * (float)(time / FadeTime);
        }

        private void FadeEnd()
        {
            _itemSprite.Sprite.Color = Color.Transparent;
            _aiComponent.ChangeState("opened");
        }

        private void SpawnObject()
        {
            // spawn the object
            Map.Objects.SpawnObject(_spawnObject);
        }

        private void SetKey()
        {
            if (!string.IsNullOrEmpty(ItemKey))
                Game1.GameManager.SaveManager.SetString(ItemKey, "1");
        }

        private bool OpenChest()
        {
            if (_aiComponent.CurrentStateId != "closed")
                return false;

            Game1.GameManager.PlaySoundEffect("D378-04-04");

            // spawn object
            if (_spawnObject != null)
            {
                Game1.GameManager.PlaySoundEffect("D360-29-1D");

                _aiComponent.ChangeState("opened");
                SpawnObject();
                SetKey();
            }
            // show dialog
            else if (_dialogPath != null)
            {
                _aiComponent.ChangeState("opened");
                Game1.GameManager.StartDialogPath(_dialogPath);
                SetKey();
            }
            // spawn item
            else
            {
                _aiComponent.ChangeState("opening");
                _itemSprite.Sprite.Color = Color.White;
            }

            return true;
        }

        private bool Interact()
        {
            // only open if the player is facing up and the chest is closed
            if (MapManager.ObjLink.Direction != 1)
                return false;

            return OpenChest();
        }
    }
}