using Microsoft.Xna.Framework;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.MidBoss
{
    internal class MBossSmasherBall : GameObject
    {
        private readonly DamageFieldComponent _damageField;
        private readonly CarriableComponent _carriableComponent;
        private readonly BodyComponent _body;
        private readonly CBox _damageBox;
        private readonly RectangleF _fieldRectangle;

        private bool _isPickedUp;
        private bool _hitEnemies;

        public MBossSmasherBall(Map.Map map, Vector2 position) : base(map)
        {
            EntityPosition = new CPosition(position.X, position.Y, 0);
            EntitySize = new Rectangle(-8, -32, 16, 32);

            // this is the same size as the player so that it can not get thrown into the wall
            _body = new BodyComponent(EntityPosition, -4, -10, 8, 10, 14)
            {
                CollisionTypes = Values.CollisionTypes.Normal | Values.CollisionTypes.NPCWall,
                MoveCollision = Collision,
                DragAir = 1.0f,
                Gravity = -0.125f,
                FieldRectangle = map.GetField((int)position.X, (int)position.Y, 12)
            };

            _fieldRectangle = _body.FieldRectangle;

            var cSprite = new CSprite("smasher_ball", EntityPosition, new Vector2(-8, -15));

            var bodyBox = new CBox(EntityPosition, -7, -12, 14, 11, 14);
            _damageBox = new CBox(EntityPosition, -7, -14, 0, 14, 14, 14, true);
            _damageBox = new CBox(EntityPosition, -2, -2, 0, 2, 2, 2, true);

            AddComponent(BodyComponent.Index, _body);
            AddComponent(CarriableComponent.Index, _carriableComponent = new CarriableComponent(new CRectangle(EntityPosition, new Rectangle(-7, -14, 14, 14)), CarryInit, CarryUpdate, CarryThrow));
            AddComponent(PushableComponent.Index, new PushableComponent(bodyBox, OnPush));
            AddComponent(HittableComponent.Index, new HittableComponent(bodyBox, OnHit));
            AddComponent(DamageFieldComponent.Index, _damageField = new DamageFieldComponent(_damageBox, HitType.ThrownObject, 4) { IsActive = false });
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(cSprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(_body, cSprite) { ShadowWidth = 12, ShadowHeight = 6 });
        }

        public void Destroy()
        {
            // spawn explosion
            var animation = new ObjAnimator(Map, 0, 0, Values.LayerTop, "Particles/explosion0", "runc", true);
            animation.EntityPosition.Set(new Vector2(EntityPosition.X, EntityPosition.Y - EntityPosition.Z - 8));
            Map.Objects.SpawnObject(animation);

            Map.Objects.DeleteObjects.Add(this);
        }

        private void Update()
        {
            if (_hitEnemies)
            {
                var collision = Map.Objects.Hit(this, EntityPosition.Position, _damageBox.Box, HitType.ThrownObject, 2, false);
                if (collision != Values.HitCollision.None)
                {
                    _body.Velocity.X = -_body.Velocity.X * 0.45f;
                    _body.Velocity.Y = -_body.Velocity.Y * 0.45f;
                }
            }
        }

        /// <summary>
        /// Returns if the ball can be picket up by the boss. This is the case if it is laying on the ground and the player is not holding it.
        /// </summary>
        /// <returns></returns>
        public bool IsAvailable()
        {
            return !_isPickedUp && _body.IsGrounded;
        }

        /// <summary>
        /// Init Pickup by the boss
        /// </summary>
        /// <returns></returns>
        public bool InitPickup()
        {
            if (_isPickedUp)
                return false;

            _carriableComponent.IsActive = false;
            _damageField.IsActive = true;
            _body.IgnoresZ = true;
            return true;
        }

        public void EndPickup()
        {
            _body.IgnoresZ = false;
            _body.Velocity = Vector3.Zero;

            _carriableComponent.IsActive = true;
        }

        public void Throw(Vector3 direction)
        {
            // make sure to not get over walls
            _body.IsGrounded = false;
            _body.JumpStartHeight = 0;
            _body.IgnoresZ = false;
            _body.Velocity = direction;

            _carriableComponent.IsActive = true;
        }

        private Values.HitCollision OnHit(GameObject originObject, Vector2 direction, HitType type, int damage, bool pieceOfPower)
        {
            // do not get hit by itself
            if (originObject == this)
                return Values.HitCollision.None;

            return Values.HitCollision.RepellingParticle;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType pushType)
        {
            if (pushType == PushableComponent.PushType.Impact)
                return true;

            return false;
        }

        private Vector3 CarryInit()
        {
            // the ball was picked up
            _isPickedUp = true;
            _body.IsActive = false;

            return new Vector3(EntityPosition.X, EntityPosition.Y, EntityPosition.Z);
        }

        private bool CarryUpdate(Vector3 newPosition)
        {
            // if the player tries to move the ball out of the field it will just fall down
            if (!_fieldRectangle.Contains(new Vector2(newPosition.X, newPosition.Y)))
                return false;

            EntityPosition.Set(new Vector3(newPosition.X, newPosition.Y, newPosition.Z));
            return true;
        }

        private void CarryThrow(Vector2 velocity)
        {
            Release();
            _body.Velocity = new Vector3(velocity.X, velocity.Y, 0) * 1.0f;
            _hitEnemies = true;
        }

        private void Release()
        {
            _isPickedUp = false;
            // @HACK: we need to make sure that the boss is not walking into walls
            _body.JumpStartHeight = 0;
            _body.IsGrounded = false;
            _body.IsActive = true;
        }

        private void Collision(Values.BodyCollision direction)
        {
            if ((direction & Values.BodyCollision.Floor) != 0)
            {
                Game1.GameManager.PlaySoundEffect("D360-09-09");

                // stop hitting the player/boss when the ball touches the ground
                _damageField.IsActive = false;
                _hitEnemies = false;
            }

            if ((direction & Values.BodyCollision.Horizontal) != 0)
                _body.Velocity.X = -_body.Velocity.X * 0.65f;
            if ((direction & Values.BodyCollision.Vertical) != 0)
                _body.Velocity.Y = -_body.Velocity.Y * 0.65f;
        }
    }
}