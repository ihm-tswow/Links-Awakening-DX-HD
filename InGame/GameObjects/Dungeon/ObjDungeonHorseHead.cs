using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Dungeon
{
    internal class ObjDungeonHorseHead : GameObject
    {
        private readonly BodyComponent _body;
        private readonly CSprite _cSprite;

        // field that the horse head can not leave
        private readonly Rectangle _fieldRectangle;

        private readonly DictAtlasEntry _spriteHeadUp;
        private readonly DictAtlasEntry _spriteHeadDown;

        private readonly string _strKey;

        private int _throwDirection;
        private int _bounceCount;

        private int _direction;
        private bool _wasThrown;
        private bool _isUp;
        private bool _wasUp;
        private bool _chessBounces;

        public ObjDungeonHorseHead() : base("horse_head_up") { }

        public ObjDungeonHorseHead(Map.Map map, int posX, int posY, string strKey, int direction) : base(map)
        {
            EntityPosition = new CPosition(posX + 8, posY + 13, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            _strKey = strKey;
            _direction = direction;

            _fieldRectangle = map.GetField(posX, posY, 15);

            // this is the same size as the player so that it can not get thrown into the wall
            _body = new BodyComponent(EntityPosition, -5, -10, 10, 10, 14)
            {
                CollisionTypes = Values.CollisionTypes.Normal | Values.CollisionTypes.NPCWall,
                MoveCollision = Collision,
                DragAir = 0.95f,
                Gravity = -0.125f,
                FieldRectangle = map.GetField(posX, posY, 16)
            };

            _spriteHeadUp = Resources.GetSprite("horse_head_up");
            _spriteHeadDown = Resources.GetSprite("horse_head_down");

            _cSprite = new CSprite(_spriteHeadDown, EntityPosition, new Vector2(-8, -13));

            AddComponent(BodyComponent.Index, _body);
            AddComponent(CarriableComponent.Index, new CarriableComponent(
                new CRectangle(EntityPosition, new Rectangle(-7, -14, 14, 14)), CarryInit, CarryUpdate, CarryThrow));
            AddComponent(PushableComponent.Index, new PushableComponent(_body.BodyBox, OnPush));
            AddComponent(HittableComponent.Index, new HittableComponent(_body.BodyBox, OnHit));
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(_cSprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(_body, _cSprite));

            DecrementUpState();
            UpdateSprite();
        }

        private void IncrementUpState()
        {
            if (string.IsNullOrEmpty(_strKey))
                return;

            // _strKey will get set to 1 after two horse heads stand up
            var currentState = Game1.GameManager.SaveManager.GetString(_strKey);
            if (currentState != "x")
                Game1.GameManager.SaveManager.SetString(_strKey, "x");
            else
                Game1.GameManager.SaveManager.SetString(_strKey, "1");
        }

        private void DecrementUpState()
        {
            if (string.IsNullOrEmpty(_strKey))
                return;

            var currentState = Game1.GameManager.SaveManager.GetString(_strKey);
            if (currentState == "x")
                Game1.GameManager.SaveManager.SetString(_strKey, "0");
        }

        private void UpdateSprite()
        {
            var newSprite = _wasThrown || _isUp ? _spriteHeadUp : _spriteHeadDown;
            _cSprite.SetSprite(newSprite);
            _cSprite.DrawOffset.X = -newSprite.SourceRectangle.Width / 2;

            _cSprite.SpriteEffect = SpriteEffects.None;

            if (_direction == 1 || _direction == 2)
                _cSprite.SpriteEffect = SpriteEffects.FlipVertically;
            if (_direction >= 2)
                _cSprite.SpriteEffect |= SpriteEffects.FlipHorizontally;
        }

        private void Update()
        {
            UpdateSprite();
        }

        private Vector3 CarryInit()
        {
            // the ball was picked up
            _body.IsActive = false;

            return new Vector3(EntityPosition.X, EntityPosition.Y, EntityPosition.Z);
        }

        private bool CarryUpdate(Vector3 newPosition)
        {
            // if the player tries to move the head out of the field it will fall down
            if (!_fieldRectangle.Contains(new Vector2(newPosition.X, newPosition.Y)))
                return false;

            EntityPosition.Set(new Vector3(newPosition.X, newPosition.Y, newPosition.Z + 3));
            return true;
        }

        private void CarryThrow(Vector2 velocity)
        {
            _throwDirection = AnimationHelper.GetDirection(velocity);

            if (velocity.Length() > 0)
            {
                _wasThrown = true;
                _chessBounces = true;
                _direction = Game1.RandomNumber.Next(1, 3);
                _isUp = false;
            }

            _body.Velocity = new Vector3(velocity.X, velocity.Y, 0) * 1.0f;

            Release();
        }

        private void Release()
        {
            _bounceCount = 0;
            _body.JumpStartHeight = 0;
            _body.IsGrounded = false;
            _body.IsActive = true;

            // we change the bounciness to make sure that we only bounce 2 times
            _body.Bounciness = _chessBounces ? 0.725f : 0.0f;
        }

        private void Collision(Values.BodyCollision direction)
        {
            if ((direction & Values.BodyCollision.Floor) != 0 && _chessBounces)
            {
                _wasThrown = false;
                _bounceCount++;

                _direction = AnimationHelper.OffsetDirection(_direction, _throwDirection > 2 ? 1 : -1);

                // jump to the left or right after the second bounce
                var velocityDirection = _throwDirection;
                if (_bounceCount == 3)
                {
                    _chessBounces = false;
                    _body.Bounciness = 0;
                    velocityDirection =
                        AnimationHelper.OffsetDirection(velocityDirection, Game1.RandomNumber.Next(0, 2) * 2 - 1);

                    // 50% chance that the horse head will stand up after the throw
                    if (Game1.RandomNumber.Next(0, 4) <= 1 || _wasUp)
                    {
                        _body.Velocity = Vector3.Zero;

                        // not in the og game; maybe find a better sound
                        Game1.GameManager.PlaySoundEffect("D370-14-0E");

                        if (!_wasUp)
                            IncrementUpState();

                        _wasUp = true;
                        _isUp = true;
                        // make sure that the head is standing up
                        if (_direction == 1)
                            _direction = 0;
                        if (_direction == 2)
                            _direction = 3;
                    }
                    else
                    {
                        Game1.GameManager.PlaySoundEffect("D360-29-1D");
                    }
                }

                if (_bounceCount <= 3)
                {
                    var velocity = AnimationHelper.DirectionOffset[velocityDirection];
                    _body.Velocity.X = velocity.X * 1.25f;
                    _body.Velocity.Y = velocity.Y * 1.25f;
                }
            }

            // make sure that the throw direction gets changed so the next bounce will not go towards the wall
            if ((direction & (Values.BodyCollision.Horizontal | Values.BodyCollision.Vertical)) != 0)
                _throwDirection = AnimationHelper.OffsetDirection(_throwDirection, 2);

            if ((direction & Values.BodyCollision.Horizontal) != 0)
                _body.Velocity.X = -_body.Velocity.X * 0.65f;
            if ((direction & Values.BodyCollision.Vertical) != 0)
                _body.Velocity.Y = -_body.Velocity.Y * 0.65f;
        }

        private Values.HitCollision OnHit(GameObject originObject, Vector2 direction, HitType type, int damage, bool pieceOfPower)
        {
            if (_body.Velocity.Length() < 0.1f)
            {
                _body.Velocity.X = direction.X * 0.25f;
                _body.Velocity.Y = direction.Y * 0.25f;
            }

            return Values.HitCollision.RepellingParticle;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType pushType)
        {
            if (pushType == PushableComponent.PushType.Impact)
            {
                _body.Velocity.X = direction.X * 0.25f;
                _body.Velocity.Y = direction.Y * 0.25f;
                return true;
            }

            return false;
        }
    }
}