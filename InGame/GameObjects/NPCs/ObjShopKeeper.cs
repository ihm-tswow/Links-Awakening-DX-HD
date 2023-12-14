using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.NPCs
{
    internal class ObjShopkeeper : GameObject
    {
        private readonly BodyComponent _body;
        private readonly BodyDrawComponent _bodyDrawComponent;
        private readonly Animator _animator;

        private readonly Rectangle _thunderTop = new Rectangle(444, 118, 14, 16);
        private readonly Rectangle _thunderBottom = new Rectangle(476, 107, 32, 32);

        private float _directionChange;
        private int _lastDirection = -1;
        private bool _isHoldingItem;

        private float _punishCount;
        private bool _punishMode;
        private bool _punishDialog;
        private bool _isPunishing = true;
        private bool _showThunder;
        private bool _soundEffect;

        public ObjShopkeeper() : base("shopkeeper") { }

        public ObjShopkeeper(Map.Map map, int posX, int posY) : base(map)
        {
            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            _animator = AnimatorSaveLoad.LoadAnimator("NPCs/shopkeeper");

            // player stole from the shop the last time?
            _punishMode = Game1.GameManager.SaveManager.GetString("stoleItem") == "1";
            if (_punishMode)
            {
                EntityPosition = new CPosition(posX + 8 - 39, posY + 16 - 32, 0);
                _animator.Play("stand_3");
            }

            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, sprite, new Vector2(-8, -16));

            _body = new BodyComponent(EntityPosition, -7, -10, 14, 10, 8);
            _bodyDrawComponent = new BodyDrawComponent(_body, sprite, 1);
            var interactionBox = new CBox(EntityPosition, -7, -14, 14, 14, 8);

            AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(OnKeyChange));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(CollisionComponent.Index, new BodyCollisionComponent(_body, Values.CollisionTypes.Normal));
            AddComponent(InteractComponent.Index, new InteractComponent(interactionBox, OnInteract));
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerPlayer, EntityPosition));
            AddComponent(DrawShadowComponent.Index, new DrawShadowCSpriteComponent(sprite));
        }

        public override void Init()
        {
            if (_punishMode)
                MapManager.ObjLink.NextMapPositionEnd =
                    new Vector2(MapManager.ObjLink.NextMapPositionEnd.Value.X, MapManager.ObjLink.NextMapPositionEnd.Value.Y - 5);
        }

        private void Update()
        {
            if (!_punishMode)
                UpdateNormal();
            else
                UpdatePunishMode();
        }

        private void UpdateNormal()
        {
            var playerDistance = new Vector2(
                MapManager.ObjLink.EntityPosition.X - (EntityPosition.X),
                MapManager.ObjLink.EntityPosition.Y - (EntityPosition.Y - 4));

            // rotate in the direction of the player
            var dir = AnimationHelper.GetDirection(playerDistance);

            if (_lastDirection != dir)
            {
                _directionChange -= Game1.DeltaTime;

                if (_directionChange <= 0)
                {
                    // 50/50 chance of making the next direction change be fast or slow
                    _directionChange = Game1.RandomNumber.Next(0, 2) == 0 ?
                        Game1.RandomNumber.Next(0, 250) : Game1.RandomNumber.Next(1500, 2500);
                    // look at the player
                    _animator.Play("stand_" + dir);
                    _lastDirection = dir;
                }
            }

            var blockPath = _isHoldingItem && (_lastDirection == 0 || _lastDirection == 3);
            Game1.GameManager.SaveManager.SetString("isWatched", blockPath ? "1" : "0");
        }

        private void UpdatePunishMode()
        {
            if (Game1.GameManager.InGameOverlay.TextboxOverlay.IsOpen)
                return;

            if (_showThunder)
            {
                _punishCount += Game1.DeltaTime;
                Game1.GameManager.UseShockEffect = _punishCount % 200 < 100;

                if (!_soundEffect)
                {
                    _soundEffect = true;
                    Game1.GameManager.PlaySoundEffect("D378-38-26");
                }
            }

            // show the dialog
            if (!_punishDialog && !MapManager.ObjLink.IsTransitioning)
            {
                Game1.GameManager.StartDialogPath("itemShop_revenge");

                _punishDialog = true;
                _showThunder = true;
            }

            if (_punishCount >= 3200 && _isPunishing)
            {
                _isPunishing = false;
                _showThunder = false;

                Game1.GameManager.SaveManager.SetString("stoleItem", "0");

                Game1.GameManager.UseShockEffect = false;

                // make sure the player actually dies
                Game1.GameManager.InflictDamage(Game1.GameManager.MaxHearths * 4 * 2);
                Game1.GameManager.RemoveItem("potion", 1);
            }

            if (MapManager.ObjLink.IsTransitioning || _showThunder)
                MapManager.ObjLink.UpdatePlayer = false;
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            // draw the shopkeeper
            _bodyDrawComponent.Draw(spriteBatch);

            // draw the thunder effect
            if (!_showThunder)
                return;

            var offsetY = -5;
            var animationOffset = _punishCount % 133 < 66;
            if (_punishCount > 0)
                spriteBatch.Draw(Resources.SprNpCs, new Vector2(EntityPosition.X - 8, EntityPosition.Y + offsetY),
                    new Rectangle(_thunderTop.X + (animationOffset ? _thunderTop.Width + 1 : 0), _thunderTop.Y, _thunderTop.Width, _thunderTop.Height), Color.White);
            if (_punishCount > 66)
                spriteBatch.Draw(Resources.SprNpCs, new Vector2(EntityPosition.X - 8, EntityPosition.Y + offsetY + 16),
                    new Rectangle(_thunderTop.X + (animationOffset ? _thunderTop.Width + 1 : 0), _thunderTop.Y, _thunderTop.Width, _thunderTop.Height), Color.White);
            if (_punishCount > 133)
                spriteBatch.Draw(Resources.SprNpCs, new Vector2(EntityPosition.X - 17, EntityPosition.Y + offsetY + 32),
                    new Rectangle(_thunderBottom.X + (animationOffset ? _thunderBottom.Width + 1 : 0), _thunderBottom.Y, _thunderBottom.Width, _thunderBottom.Height), Color.White);
        }

        private void OnKeyChange()
        {
            var value = Game1.GameManager.SaveManager.GetString("holdItem");
            _isHoldingItem = value == "1";
        }

        private bool OnInteract()
        {
            Game1.GameManager.StartDialogPath("shopkeeper");
            return true;
        }
    }
}