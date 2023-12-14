using System;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Systems;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    class ObjChainPlatform : GameObject
    {
        private readonly DictAtlasEntry _spritePlatform;
        private readonly DictAtlasEntry _spriteChain;

        private readonly BoxCollisionComponent _cBoxCollision;

        private readonly CBox _moveBox;

        private readonly CBox _collisionBox;

        private Vector2 _chainPosition;

        private Vector2 _startPosition;
        private Vector2 _bottomPosition;
        private Vector2 _currentVelocity;

        private string _strPlatformKey;
        private string _strPlatformMovedKey;
        private string _strPlatformRestKey;

        private const float MaxSpeed = 1.0f;
        private float _resetVelocity;

        private bool _wasMoved;
        private bool _resettingPlatforms;

        public ObjChainPlatform(Map.Map map, int posX, int posY, string strPlatformKey, int bottom, int chainTop) : base(map)
        {
            _spritePlatform = Resources.GetSprite("small_platform");
            _spriteChain = Resources.GetSprite("platformchain");

            _strPlatformKey = strPlatformKey;
            _strPlatformMovedKey = strPlatformKey + "moved";
            _strPlatformRestKey = strPlatformKey + "reset";

            SprEditorImage = _spritePlatform.Texture;
            EditorIconSource = _spritePlatform.SourceRectangle;

            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, chainTop, 16, -chainTop + bottom + 16);

            _chainPosition = new Vector2(posX + 8 - _spriteChain.SourceRectangle.Width / 2, posY + chainTop);

            _startPosition = new Vector2(posX, posY);
            _bottomPosition = new Vector2(posX, posY + bottom);

            _moveBox = new CBox(EntityPosition, 0, -1, 0, 16, 16, 16);
            _collisionBox = new CBox(EntityPosition, 0, 0, 16, 16, 16);

            AddComponent(CollisionComponent.Index, _cBoxCollision = new BoxCollisionComponent(_collisionBox, Values.CollisionTypes.Normal) { DirectionFlag = 8 });
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerBottom, EntityPosition));

            if (_strPlatformKey != null)
            {
                Game1.GameManager.SaveManager.SetString(_strPlatformKey, "0");
                AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(OnKeyChange));
            }
        }

        private void OnKeyChange()
        {
            if (!_wasMoved && Game1.GameManager.SaveManager.GetString(_strPlatformMovedKey) == "true")
            {
                var strPlatformOffset = Game1.GameManager.SaveManager.GetString(_strPlatformKey);
                if (!string.IsNullOrEmpty(strPlatformOffset))
                {
                    var newOffset = float.Parse(strPlatformOffset, CultureInfo.InvariantCulture);
                    EntityPosition.Set(_startPosition + new Vector2(0, newOffset));
                    Game1.GameManager.SaveManager.SetString(_strPlatformMovedKey, "false");
                    _resettingPlatforms = false;
                }
            }

            // reset the platforms?
            var strReset = Game1.GameManager.SaveManager.GetString(_strPlatformRestKey);
            if (strReset != null && strReset == "true")
            {
                _resettingPlatforms = true;
                Game1.GameManager.SaveManager.SetString(_strPlatformRestKey, "false");
            }
        }

        private void Update()
        {
            _wasMoved = false;

            // is the player standing on the platform?
            var standingOnTop = MapManager.ObjLink._body.BodyBox.Box.Intersects(_moveBox.Box) && MapManager.ObjLink._body.IsGrounded;

            if (standingOnTop && PlayerIsMovable())
            {
                // accelerate the platform
                _currentVelocity += new Vector2(0, 0.025f * Game1.TimeMultiplier);
                _currentVelocity.Y = MathHelper.Clamp(_currentVelocity.Y, 0, MaxSpeed);

                _resettingPlatforms = false;
            }
            else
            {
                // slow down the platform
                _currentVelocity *= (float)Math.Pow(0.85f, Game1.TimeMultiplier);
            }

            if (_currentVelocity.Length() > 0.1f)
                MovePlatform(_currentVelocity);

            // move back to the start position
            if (_resettingPlatforms)
            {
                var offset = _startPosition.Y - EntityPosition.Y;
                
                _resetVelocity += 0.035f * Game1.TimeMultiplier;
                _resetVelocity = MathHelper.Clamp(_resetVelocity, 0, 0.4f);

                if (Math.Abs(offset) > _resetVelocity)
                {
                    offset = Math.Sign(offset) * MathHelper.Clamp(Math.Abs(offset), 0, _resetVelocity * Game1.TimeMultiplier);
                    SetPosition(EntityPosition.Y + offset);
                }
                else
                {
                    _resettingPlatforms = false;
                    SetPosition(_startPosition.Y);
                }
            }
            else
            {
                _resetVelocity = 0;
            }
        }

        private void SetPosition(float newPositionY)
        {
            _wasMoved = true;
            EntityPosition.Set(new Vector2(EntityPosition.X, newPositionY));

            var platformOffset = _startPosition.Y - EntityPosition.Position.Y;
            Game1.GameManager.SaveManager.SetString(_strPlatformKey, platformOffset.ToString(CultureInfo.InvariantCulture));
            Game1.GameManager.SaveManager.SetString(_strPlatformMovedKey, "true");
        }

        private void MovePlatform(Vector2 offset)
        {
            // do not allow the platform to move lower than the max
            var maxOffset = _bottomPosition.Y - EntityPosition.Y;
            if (maxOffset <= 0)
                return;
            if (offset.Y > maxOffset)
                offset.Y = maxOffset;

            // make sure to not move the platform more than the player was able to move
            EntityPosition.Move(offset);

            // move the player down
            SystemBody.MoveBody(MapManager.ObjLink._body, offset * Game1.TimeMultiplier,
                    Values.CollisionTypes.Normal, false, false, false);

            _wasMoved = true;
            if (_strPlatformKey != null)
            {
                // make sure other linked platforms will also be moved
                var platformOffset = _startPosition.Y - EntityPosition.Position.Y;
                Game1.GameManager.SaveManager.SetString(_strPlatformKey, platformOffset.ToString(CultureInfo.InvariantCulture));
                Game1.GameManager.SaveManager.SetString(_strPlatformMovedKey, "true");
            }
        }

        // this checks if the player can be moved down with the platform or if the player
        // is also standing on a ledge and is not really pushing down on the platform
        private bool PlayerIsMovable()
        {
            var lastPosition = MapManager.ObjLink.EntityPosition.Position;

            _cBoxCollision.IsActive = false;
            SystemBody.MoveBody(MapManager.ObjLink._body, new Vector2(0, 1), Values.CollisionTypes.Normal, false, false, false);
            _cBoxCollision.IsActive = true;

            // check if the player was moved
            var playerCanMove = MapManager.ObjLink.EntityPosition.Y != lastPosition.Y;

            // move the player back to the original position
            MapManager.ObjLink.EntityPosition.Set(lastPosition);

            return playerCanMove;
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            // draw the chain
            var chainLinkCount = (int)Math.Ceiling((EntityPosition.Y - _chainPosition.Y) / 16);
            for (var i = 0; i < chainLinkCount; i++)
                spriteBatch.Draw(_spriteChain.Texture, new Vector2(_chainPosition.X, _chainPosition.Y + i * 16), _spriteChain.ScaledRectangle, Color.White);

            // draw the platform
            spriteBatch.Draw(_spritePlatform.Texture, EntityPosition.Position, _spritePlatform.ScaledRectangle, Color.White);
        }
    }
}
