using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.GameObjects.Base.Systems;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjItem : GameObject
    {
        public bool IsJumping;
        public bool Collectable;
        public bool Collected;

        public string SaveKey;

        private GameItem _item;
        private string _itemName;
        private string _locationBound;

        private AiComponent _aiComponent;
        private DrawShadowSpriteComponent _shadowComponent;
        private BodyComponent _body;
        private AiTriggerCountdown _delayCountdown;
        private BodyDrawComponent _bodyDrawComponent;
        private CRectangle _collectionRectangle;

        private Rectangle _sourceRectangle;
        private Rectangle _sourceRectangleWing = new Rectangle(2, 250, 8, 15);

        private Color _color = Color.White;
        private Rectangle _shadowSourceRectangle = new Rectangle(0, 0, 65, 66);

        private float _fadeOffset;

        private double _deepWaterCounter;
        private float _despawnCount;
        private int _despawnTime = 350;
        private int _fadeStart = 250;
        private int _moveStopTime = 250;
        private int _lastFieldTime;

        private bool _isFlying;
        private bool _isSwimming;
        private bool _isVisible = true;
        private bool _despawn;

        public ObjItem() : base("item") { }

        public ObjItem(Map.Map map, int posX, int posY, string strType, string saveKey, string itemName, string locationBound, bool despawn = false) : base(map)
        {
            if (!string.IsNullOrEmpty(saveKey))
            {
                SaveKey = saveKey;

                // item has already been collected
                if (Game1.GameManager.SaveManager.GetString(SaveKey) == "1")
                {
                    IsDead = true;
                    return;
                }
            }

            _item = Game1.GameManager.ItemManager[itemName];
            _itemName = itemName;
            _locationBound = locationBound;
            _despawn = despawn;

            if (_item == null)
            {
                IsDead = true;
                return;
            }

            var baseItem = _item.SourceRectangle.HasValue ? _item : Game1.GameManager.ItemManager[_item.Name];
            if (baseItem.MapSprite != null)
                _sourceRectangle = baseItem.MapSprite.SourceRectangle;
            else
                _sourceRectangle = baseItem.SourceRectangle.Value;

            EntityPosition = new CPosition(posX + 8, posY + 8 + 3, 0);
            EntitySize = new Rectangle(-9, -16, 18, 18);

            // add sound for the bounces
            _body = new BodyComponent(EntityPosition, -4, -8, 8, 8, 8)
            {
                RestAdditionalMovement = false,
                Gravity = -0.1f,
                Bounciness = 0.7f,
                IgnoreHeight = true,
                CollisionTypes = Values.CollisionTypes.Normal |
                                 Values.CollisionTypes.LadderTop,
                HoleAbsorb = OnHoleAbsorb,
                MoveCollision = OnMoveCollision
            };

            if (!string.IsNullOrEmpty(strType))
            {
                // jumping item
                if (strType == "j")
                {
                    IsJumping = true;
                    if (!Map.Is2dMap)
                        _body.Velocity.Z = 1f;
                    else
                    {
                        Collectable = true;
                        _body.Velocity.Y = -1f;
                    }

                    // needed because at the first frame the value is still true
                    _body.IsGrounded = false;
                }
                // fall from the sky
                else if (strType == "d")
                {
                    IsJumping = true;
                    EntityPosition.Z = 60;

                    // needed because at the first frame the value is still true
                    _body.IsGrounded = false;
                    _body.RestAdditionalMovement = true;
                }
                // fly
                else if (strType == "w")
                {
                    _body.IsActive = false;
                    EntityPosition.Z = 10;
                    Collectable = true;
                    _isFlying = true;
                }
                // item is in the water
                else if (strType == "s")
                {
                    _isSwimming = true;
                }
            }
            else
            {
                Collectable = true;
            }

            var stateIdle = new AiState(UpdateIdle);
            // despawn after 15sec, but only if it was jumping or fall from the sky
            if (string.IsNullOrEmpty(saveKey) && !_isFlying && !Collectable)
                stateIdle.Trigger.Add(new AiTriggerCountdown(15000, null, ToFading));

            var stateDelay = new AiState();
            var stateHoleFall = new AiState();
            stateHoleFall.Trigger.Add(new AiTriggerCountdown(125, null, HoleDespawn));
            stateDelay.Trigger.Add(_delayCountdown = new AiTriggerCountdown(0, null, () =>
            {
                _aiComponent.ChangeState("idle");
                _isVisible = true;
                if (!Map.Is2dMap)
                    _body.Velocity.Z = 1f;
                else
                    _body.Velocity.Y = -1f;
                EntityPosition.Set(new Vector3(EntityPosition.X, EntityPosition.Y, 0));
            }));

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("boomerang", new AiState());
            _aiComponent.States.Add("fading", new AiState(UpdateFading));
            _aiComponent.States.Add("delay", stateDelay);
            _aiComponent.States.Add("holeFall", stateHoleFall);
            _aiComponent.ChangeState("idle");

            // we make the collision box a little bit bigger; this is used for the genie where the heart can technically spawn inside the lamp
            // with the little extra size the heart will still be collectable
            var height = Math.Min(_sourceRectangle.Width, 8);
            _collectionRectangle = new CRectangle(EntityPosition,
                new Rectangle(
                    -_sourceRectangle.Width / 2 - 1, -height,
                    _sourceRectangle.Width + 2, height));
            var box = new CBox(EntityPosition,
                -_sourceRectangle.Width / 2, -height,
                _sourceRectangle.Width, height, 16);

            AddComponent(BodyComponent.Index, _body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(ObjectCollisionComponent.Index, new ObjectCollisionComponent(_collectionRectangle, OnCollision));
            AddComponent(CollisionComponent.Index, new BoxCollisionComponent(box, Values.CollisionTypes.Item));

            // item can be collected by hitting it
            if (_item.ShowAnimation == 0)
                AddComponent(HittableComponent.Index, new HittableComponent(box, OnHit));

            _shadowComponent = new DrawShadowSpriteComponent(
                Resources.SprShadow, EntityPosition, _shadowSourceRectangle,
                new Vector2(-_sourceRectangle.Width / 2 - 1, -_sourceRectangle.Width / 4 - 2), 1.0f, 0.0f);
            _shadowComponent.Width = _sourceRectangle.Width + 2;
            _shadowComponent.Height = _sourceRectangle.Width / 2 + 2;

            _bodyDrawComponent = new BodyDrawComponent(_body, Draw, Values.LayerPlayer);

            if (!_isSwimming)
            {
                AddComponent(DrawComponent.Index, _bodyDrawComponent);
                AddComponent(DrawShadowComponent.Index, _shadowComponent);
            }

            if (_itemName == "shellPresent")
            {
                _shadowComponent.IsActive = false;
                _bodyDrawComponent.Layer = Values.LayerBottom;
                _collectionRectangle.OffsetSize.X = -4;
                _collectionRectangle.OffsetSize.Y = -8;
                _collectionRectangle.OffsetSize.Width = 8;
                _collectionRectangle.OffsetSize.Height = 8;
                _collectionRectangle.UpdateRectangle(EntityPosition);
            }
            if (_itemName == "shell")
            {
                // dont spawn additional shells if the player already found 20
                var state = Game1.GameManager.SaveManager.GetString("shellsFound", "0");
                if (state == "1")
                    IsDead = true;
            }
            if (_itemName == "sword2")
            {
                _bodyDrawComponent.Layer = Values.LayerBottom;
            }
        }

        public override void Init()
        {
            _lastFieldTime = Map.GetUpdateState(EntityPosition.Position);
        }

        public MapStates.FieldStates GetBodyFieldState()
        {
            return SystemBody.GetFieldState(_body);
        }

        public void SpawnBoatSequence()
        {
            // shrink the collection rectangle
            _collectionRectangle.OffsetSize.X = (int)(_collectionRectangle.OffsetSize.X * 0.25f);
            _collectionRectangle.OffsetSize.Width = (int)(_collectionRectangle.OffsetSize.Width * 0.25f);
            _bodyDrawComponent.Layer = Values.LayerTop;

            _body.Velocity = new Vector3(1, -2.25f, 0);
            _body.DragAir = 1.0f;
        }

        public void SetVelocity(Vector3 velocity)
        {
            _body.Velocity = velocity;
        }

        public void InitCollection()
        {
            _body.IgnoresZ = true;
            Collectable = true;
            _aiComponent.ChangeState("boomerang");
        }

        public void SetSpawnDelay(int delay)
        {
            _isVisible = false;
            _delayCountdown.StartTime = delay;
            _aiComponent.ChangeState("delay");
        }

        private void UpdateIdle()
        {
            if (_body.IsGrounded)
                Collectable = true;

            // field went out of the update range?
            var updateState = Map.GetUpdateState(EntityPosition.Position);
            if (_lastFieldTime < updateState && _despawn)
                ToFading();

            if (!_body.IsActive)
                EntityPosition.Z = 20 - _sourceRectangle.Height / 2 + (float)Math.Sin((Game1.TotalGameTime / 1050) * Math.PI * 2) * 1.5f;

            // fall into the water
            if (!_isSwimming && !Map.Is2dMap)
            {
                if (_body.IsGrounded && _body.CurrentFieldState.HasFlag(MapStates.FieldStates.DeepWater))
                {
                    _deepWaterCounter -= Game1.DeltaTime;

                    if (_deepWaterCounter <= 0)
                    {
                        // spawn splash effect
                        var fallAnimation = new ObjAnimator(Map,
                            (int)(_body.Position.X + _body.OffsetX + _body.Width / 2.0f),
                            (int)(_body.Position.Y + _body.OffsetY + _body.Height / 2.0f),
                            Values.LayerPlayer, "Particles/fishingSplash", "idle", true);
                        Map.Objects.SpawnObject(fallAnimation);

                        Map.Objects.DeleteObjects.Add(this);
                    }
                }
                else
                {
                    _deepWaterCounter = 125;
                }
            }

            if (!Map.Is2dMap)
                _shadowComponent.Color = Color.White * ((128 + EntityPosition.Z) / 128f);
            else
                _shadowComponent.Color = _body.IsGrounded ? Color.White : Color.Transparent;
        }

        private void ToFading()
        {
            _body.IgnoresZ = true;
            _aiComponent.ChangeState("fading");
        }

        private void UpdateFading()
        {
            _despawnCount += Game1.DeltaTime;

            // move item up if it was collected
            if (Collected && _despawnCount < _moveStopTime)
                _fadeOffset = -(float)Math.Sin(_despawnCount / _moveStopTime * Math.PI / 1.5f) * 10;

            // fade the item after fadestart
            if (_fadeStart <= _despawnCount)
                _color = Color.White * (1 - ((_despawnCount - _fadeStart) / (_despawnTime - _fadeStart)));

            _shadowComponent.Color = _color;

            // remove the object
            if (_despawnCount > _despawnTime)
                Map.Objects.DeleteObjects.Add(this);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!_isVisible)
                return;

            ItemDrawHelper.DrawItem(spriteBatch, _item,
                new Vector2(EntityPosition.X - _sourceRectangle.Width / 2.0f, EntityPosition.Y - EntityPosition.Z - _sourceRectangle.Height + _fadeOffset), _color, 1, true);

            if (!_isFlying)
                return;

            var wingFlap = (Game1.TotalGameTime % (16 / 60f * 1000)) < (8 / 60f * 1000) ? SpriteEffects.FlipVertically : SpriteEffects.None;
            // left wing
            spriteBatch.Draw(Resources.SprItem, new Vector2(
                    EntityPosition.X - _sourceRectangleWing.Width - 4f,
                    EntityPosition.Y - EntityPosition.Z - _sourceRectangle.Height / 2 - 10 + _fadeOffset),
                _sourceRectangleWing, _color, 0, Vector2.Zero, Vector2.One, SpriteEffects.None | wingFlap, 0);

            // right wing
            spriteBatch.Draw(Resources.SprItem, new Vector2(
                    EntityPosition.X + 4f,
                    EntityPosition.Y - EntityPosition.Z - _sourceRectangle.Height / 2 - 10 + _fadeOffset),
                _sourceRectangleWing, _color, 0, Vector2.Zero, Vector2.One,
                SpriteEffects.FlipHorizontally | wingFlap, 0);
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            // item can be collected with the sword
            if ((damageType & HitType.Sword) != 0 &&
                (damageType & HitType.SwordHold) == 0)
                Collect();

            return Values.HitCollision.NoneBlocking;
        }

        private void OnMoveCollision(Values.BodyCollision collision)
        {
            // sound should play but only for the trendy game? maybe add a extra item type?
            if ((collision & Values.BodyCollision.Floor) != 0 && _body.Velocity.Z > 0.55f ||
                ((collision & Values.BodyCollision.Bottom) != 0 && _body.Velocity.Y < 0f && Map.Is2dMap))
            {
                // metalic bounce sound
                if (_item.Name == "smallkey" || _item.Name == "sword2")
                    Game1.GameManager.PlaySoundEffect("D378-23-17");
                else
                    Game1.GameManager.PlaySoundEffect("D360-09-09");
            }
        }

        private void OnHoleAbsorb()
        {
            if (Collected)
                return;

            _body.IsActive = false;
            if (_aiComponent.CurrentStateId != "holeFall")
                _aiComponent.ChangeState("holeFall");
        }

        private void HoleDespawn()
        {
            // play sound effect
            Game1.GameManager.PlaySoundEffect("D360-24-18");

            var fallAnimation = new ObjAnimator(Map, (int)EntityPosition.X - 5, (int)EntityPosition.Y - 8, Values.LayerBottom, "Particles/fall", "idle", true);
            Map.Objects.SpawnObject(fallAnimation);

            Map.Objects.DeleteObjects.Add(this);
        }

        private void OnCollision(GameObject gameObject)
        {
            // only collect the item when the player is near it in the z dimension
            // maybe the collision component should have used Boxes instead of Rectangles
            if (Math.Abs(EntityPosition.Z - MapManager.ObjLink.EntityPosition.Z) < 8 &&
                (!_isSwimming || MapManager.ObjLink.IsDiving()))
                Collect();
        }

        private void Collect()
        {
            if (!Collectable || Collected)
                return;

            if (_isFlying && MapManager.ObjLink.EntityPosition.Z < 7)
                return;

            // do not collect the item while the player is not grounded
            if (_item.ShowAnimation != 0 &&
                (!Map.Is2dMap && !MapManager.ObjLink._body.IsGrounded ||
                 Map.Is2dMap && !MapManager.ObjLink._body.IsGrounded && !MapManager.ObjLink.IsInWater2D()))
                return;

            Collected = true;
            _body.IsActive = false;
            _bodyDrawComponent.WaterOutline = false;

            if (Map.Is2dMap)
                _body.Velocity.Y = 0f;
            else
                _body.Velocity.Z = 0f;

            // gets picked up
            var cItem = new GameItemCollected(_itemName)
            {
                Count = _item.Count,
                LocationBounding = _locationBound
            };
            MapManager.ObjLink.PickUpItem(cItem, true);

            // do not fade away the item if it the player shows it
            if (_item.ShowAnimation != 0)
                Map.Objects.DeleteObjects.Add(this);
            else
                ToFading();

            if (SaveKey != null)
                Game1.GameManager.SaveManager.SetString(SaveKey, "1");
        }
    }
}