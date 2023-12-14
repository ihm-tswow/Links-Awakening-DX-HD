using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Dungeon
{
    internal class ObjDungeonFairy : GameObject
    {
        private readonly GameItem _carriedItem;
        private readonly Rectangle _carriedItemSourceRectangle;

        private readonly CSprite _sprite;
        private readonly CBox _collectionBox;
        private Vector2 _direction;

        private float _currentRotation;
        private float _directionChange;

        private float _currentSpeed;
        private float _lastSpeed;
        private float _speedGoal;

        private float _flyCounter;

        private int _flyTime;

        private const int MinSpeed = 10;
        private const int MaxSpeed = 75;

        // the fairy is not collectable directly
        private float _collectionCooldown = 500;

        private const float FadeOutTime = 100;
        private float _collectionCounter = FadeOutTime;

        private float _targetHeight = 16;
        // the butterfly will stay around this distance from the start point
        private float _positionZ;
        private int _startDistance;
        private bool _collected;
        private bool _itemMode;

        public ObjDungeonFairy() : base("fairy") { }

        public ObjDungeonFairy(Map.Map map, int posX, int posY, int posZ, string carriedItem = null) : base(map)
        {
            EntityPosition = new CPosition(posX, posY, posZ);
            EntitySize = new Rectangle(-4, -30, 8, 30);

            _positionZ = posZ;

            var body = new BodyComponent(EntityPosition, -4, -8, 8, 8, 8)
            {
                IgnoresZ = true
            };

            if (!string.IsNullOrEmpty(carriedItem))
            {
                _carriedItem = Game1.GameManager.ItemManager[carriedItem];

                if (_carriedItem.SourceRectangle.HasValue)
                    _carriedItemSourceRectangle = _carriedItem.SourceRectangle.Value;
                else
                {
                    var baseItem = Game1.GameManager.ItemManager[_carriedItem.Name];
                    _carriedItemSourceRectangle = baseItem.SourceRectangle.Value;
                }

                _targetHeight += 12;
                _itemMode = true;
                _collectionCounter = 750;
            }

            // start by flying away from the player
            _startDistance = Game1.RandomNumber.Next(25, 50);
            var playerDirection = EntityPosition.Position - MapManager.ObjLink.EntityPosition.Position;
            _currentRotation = MathF.Atan2(playerDirection.Y, playerDirection.X);
            _flyTime = 500;
            _flyCounter = _flyTime;

            _currentSpeed = Game1.RandomNumber.Next(MaxSpeed / 2, MaxSpeed) / 100f;
            _lastSpeed = _currentSpeed;
            _speedGoal = Game1.RandomNumber.Next(MaxSpeed / 2, MaxSpeed) / 100f;

            _sprite = new CSprite("fairy", EntityPosition, new Vector2(-4, -13));

            _collectionBox = new CBox(EntityPosition, -4, -10, _itemMode ? -16 : 0, 8, 10, 8, !_itemMode);

            AddComponent(BodyComponent.Index, body);
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerPlayer, EntityPosition));
            AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(body, _sprite));
        }

        private void Update()
        {
            if (!_collected)
                UpdateFlying();
            else
                UpdateCollected();
        }

        private void UpdateFlying()
        {
            if (_collectionCooldown > 0)
                _collectionCooldown -= Game1.DeltaTime;

            _flyCounter -= Game1.DeltaTime;

            // ascent
            if (_positionZ < _targetHeight)
                _positionZ += Game1.TimeMultiplier * 0.25f;
            else
                _positionZ = _targetHeight;

            if (_flyCounter < 0)
            {
                _flyTime = Game1.RandomNumber.Next(500, 1000);
                _flyCounter += _flyTime;

                // set a new speed goal
                _lastSpeed = _speedGoal;
                _speedGoal = Game1.RandomNumber.Next(MinSpeed, MaxSpeed) / 100f;

                var randomDirection = ((Game1.RandomNumber.Next(0, 20) - 10) / 6f) * ((float)Math.PI / (60 * (_flyCounter / 1000f)));

                // direction back to the base
                var startDifference = EntityPosition.Position - MapManager.ObjLink.EntityPosition.Position;
                var targetRotation = Math.Atan2(startDifference.Y, startDifference.X);
                var rotationDifference = (float)targetRotation - _currentRotation;
                while (rotationDifference < 0)
                    rotationDifference += (float)Math.PI * 2;
                rotationDifference = rotationDifference % (float)(Math.PI * 2);
                rotationDifference -= (float)Math.PI;
                var newRotation = rotationDifference / (60 * (_flyCounter / 1000f));

                // calculate the new rotation direction of the fairy
                // the farther away it is from the start position the more likely it is to rotate to face the start position
                _directionChange = MathHelper.Lerp(randomDirection, newRotation, MathHelper.Clamp(startDifference.Length() / _startDistance, 0, 1));
            }

            // update the speed
            _currentSpeed = MathHelper.Lerp(_speedGoal, _lastSpeed, _flyCounter / _flyTime);

            // update direction
            _currentRotation += _directionChange * Game1.TimeMultiplier;
            _currentRotation = _currentRotation % (float)(Math.PI * 2);
            _direction = new Vector2((float)Math.Cos(_currentRotation), (float)Math.Sin(_currentRotation)) * _currentSpeed;
            EntityPosition.Move(_direction);

            EntityPosition.Z = _positionZ + (float)Math.Sin(Game1.TotalGameTime / 200) * 1.5f;
            _sprite.SpriteEffect = _direction.X < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            // collision with the player
            if (_collectionCooldown < 0 && MapManager.ObjLink.PlayerRectangle.Intersects(_collectionBox.Box.Rectangle()))
                CollectFairy();
        }

        private void CollectFairy()
        {
            if (!_itemMode)
            {
                // heal the player
                Game1.GameManager.HealPlayer(4 * 6);
                ItemDrawHelper.EnableHeartAnimationSound();
            }
            else
            {
                // collect the item the fairy was carrying
                var cItem = new GameItemCollected(_carriedItem.Name)
                {
                    Count = _carriedItem.Count
                };

                MapManager.ObjLink.PickUpItem(cItem, true);
            }

            Game1.GameManager.PlaySoundEffect("D370-01-01");
            _collected = true;
        }

        private void UpdateCollected()
        {
            _collectionCounter -= Game1.DeltaTime;

            if (_collectionCounter < 0)
            {
                IsActive = false;
                Map.Objects.DeleteObjects.Add(this);
            }
            else
            {
                _sprite.Color = Color.White * MathHelper.Clamp(_collectionCounter / FadeOutTime, 0, 1);
                EntityPosition.Move(_direction);
                if (_itemMode)
                    EntityPosition.Z += Game1.TimeMultiplier * 0.25f;
            }
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            _sprite.Draw(spriteBatch);

            // draw the item if the fairy is carrying one
            if (_carriedItem != null && !_collected)
            {
                ItemDrawHelper.DrawItem(spriteBatch, _carriedItem, new Vector2(
                    EntityPosition.X - _carriedItemSourceRectangle.Width / 2, EntityPosition.Y - EntityPosition.Z - 1), Color.White, 1, true);
            }
        }
    }
}