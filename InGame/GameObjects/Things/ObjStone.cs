using Microsoft.Xna.Framework;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Dungeon;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjStone : GameObject
    {
        private readonly BodyComponent _body;
        private readonly BoxCollisionComponent _collisionComponent;
        private readonly CarriableComponent _carriableComponent;

        private readonly CBox _upperBox;
        private readonly CBox _lowerBox;
        private readonly CBox _damageBox;

        private readonly Point _spawnPosition;

        private readonly string _spawnItem;
        private readonly string _pickupKey;
        private readonly string _dialogPath;

        private readonly bool _potMessage;

        private int _offsetY = 3;

        private bool _thrown;
        private bool _isAlive = true;
        private bool _damagePlayer;
        private bool _isHeavy;

        public ObjStone(Map.Map map, int posX, int posY, string spriteId, string spawnItem, string pickupKey, string dialogPath, bool isHeavy, bool potMessage) : base(map, spriteId)
        {
            var sprite = Resources.GetSprite(spriteId);

            EntityPosition = new CPosition(posX + 8, posY + 16 - _offsetY, 0);
            EntitySize = new Rectangle(
                -sprite.SourceRectangle.Width / 2, _offsetY - sprite.SourceRectangle.Height * 2, sprite.SourceRectangle.Width, sprite.SourceRectangle.Height * 2 + 4);

            _spawnPosition = new Point(posX, posY + 2);

            _spawnItem = spawnItem;
            _pickupKey = pickupKey;
            _dialogPath = dialogPath;

            _isHeavy = isHeavy;
            _potMessage = potMessage;

            _upperBox = new CBox(EntityPosition, -4, -8 + _offsetY, 0, 8, 8, 4, true);
            _lowerBox = new CBox(EntityPosition, -4, -8 + _offsetY, 0, 8, 8, 4);

            _damageBox = new CBox(EntityPosition, -7, -14 + _offsetY, 0, 14, 14, 12, true);

            var height = map.Is2dMap ? 15 : 13;
            var heightOffset = map.Is2dMap ? 0 : 2;
            var collisionBox = new CBox(EntityPosition, -sprite.SourceRectangle.Width / 2, -height + _offsetY, 0, sprite.SourceRectangle.Width, height - heightOffset, 12, true);
            _body = new BodyComponent(EntityPosition, -4, -8 + _offsetY, 8, 8, 12)
            {
                CollisionTypes = Values.CollisionTypes.Normal,
                MoveCollision = OnCollision,
                HoleAbsorb = OnHoleAbsorb,
                DragAir = 1.0f,
                Gravity = -0.125f,
                IgnoreHeight = true
            };

            var cSprite = new CSprite(spriteId, EntityPosition, new Vector2(-sprite.SourceRectangle.Width / 2, -sprite.SourceRectangle.Height + _offsetY));

            if (!string.IsNullOrEmpty(_dialogPath))
                AddComponent(PushableComponent.Index, new PushableComponent(collisionBox, OnPush) { InertiaTime = 50 });
            AddComponent(BodyComponent.Index, _body);
            AddComponent(CarriableComponent.Index, _carriableComponent = new CarriableComponent(
                new CRectangle(EntityPosition, new Rectangle(
                    -sprite.SourceRectangle.Width / 2, -13 + _offsetY, sprite.SourceRectangle.Width, 13)), CarryInit, CarryUpdate, CarryThrow)
            {
                IsHeavy = _isHeavy
            });
            AddComponent(CollisionComponent.Index, _collisionComponent = new BoxCollisionComponent(collisionBox, Values.CollisionTypes.Normal | Values.CollisionTypes.Hookshot));
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(cSprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(_body, cSprite));
        }

        public bool MakeFlyingStone()
        {
            // was already picked up?
            if (!_isAlive || !_collisionComponent.IsActive)
                return false;

            _body.IgnoresZ = true;
            _collisionComponent.IsActive = false;
            _carriableComponent.IsActive = false;

            var damageBox = new CBox(EntityPosition, -6, -13, 0, 12, 20, 8, true);
            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageBox, HitType.Enemy, 3) { OnDamage = DamagePlayer });

            // deal damage to the player
            _damagePlayer = true;

            return true;
        }

        private bool DamagePlayer()
        {
            if (MapManager.ObjLink.HitPlayer(_damageBox.Box, HitType.Enemy, 2))
            {
                OnCollision();
                return true;
            }

            return false;
        }

        public void ThrowStone(Vector2 direction)
        {
            _thrown = true;
            _body.VelocityTarget = direction;
            _body.CollisionTypes = Values.CollisionTypes.None;
        }

        public void LetGo()
        {
            _body.IgnoresZ = false;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType pushType)
        {
            if (pushType == PushableComponent.PushType.Impact)
                return false;

            Game1.GameManager.StartDialogPath(_dialogPath);

            return false;
        }

        private void Update()
        {
            if (!_thrown)
                return;

            // this is used because the normal collision detection looks strang when throwing directly towards a lower wall
            var outBox = Box.Empty;
            if (!Map.Is2dMap &&
                Map.Objects.Collision(_upperBox.Box, Box.Empty, Values.CollisionTypes.Normal, 0, _body.Level, ref outBox) &&
                Map.Objects.Collision(_lowerBox.Box, Box.Empty, Values.CollisionTypes.Normal, 0, _body.Level, ref outBox))
                OnCollision();

            if (_damagePlayer)
                return;

            // TODO: find the right hittype with the correct amount of damage or create a extra one?
            var hitCollision = Map.Objects.Hit(this, _damageBox.Box.Center, _damageBox.Box, HitType.ThrownObject, 2, false);

            // hit something?
            if (hitCollision != Values.HitCollision.None &&
                hitCollision != Values.HitCollision.NoneBlocking)
            {
                OnCollision();
            }
        }

        private Vector3 CarryInit()
        {
            if (_spawnItem != null)
            {
                // spawn item
                var objItem = new ObjItem(Map, _spawnPosition.X, _spawnPosition.Y, "j", _pickupKey, _spawnItem, "");
                if (!objItem.IsDead)
                    Map.Objects.SpawnObject(objItem);
                else if (_spawnItem == "fairy")
                {
                    // spawn fairy
                    var objFairy = new ObjDungeonFairy(Map, (int)EntityPosition.X, (int)EntityPosition.Y, 0);
                    Map.Objects.SpawnObject(objFairy);
                }
            }
            // @HACK: if we spawn an item we use the pickupKey as the item save key
            // set the pickup key
            else if (!string.IsNullOrEmpty(_pickupKey))
                Game1.GameManager.SaveManager.SetString(_pickupKey, "1");

            // the stone was picked up
            _collisionComponent.IsActive = false;
            _body.IsActive = false;
            // we ignore move collisions and use the update methode to get nicer looking collisions
            if (!Map.Is2dMap)
                _body.CollisionTypes = Values.CollisionTypes.None;

            return new Vector3(EntityPosition.X, EntityPosition.Y - _offsetY, EntityPosition.Z);
        }

        private bool CarryUpdate(Vector3 newPosition)
        {
            EntityPosition.X = newPosition.X;

            if (!Map.Is2dMap)
            {
                EntityPosition.Y = newPosition.Y - _offsetY;
                EntityPosition.Z = newPosition.Z;
            }
            else
            {
                EntityPosition.Y = newPosition.Y - _offsetY - newPosition.Z;
                EntityPosition.Z = 0;
            }

            EntityPosition.NotifyListeners();
            return true;
        }

        private void CarryThrow(Vector2 velocity)
        {
            _thrown = true;
            _body.IsGrounded = false;
            _body.IsActive = true;
            _body.Velocity = new Vector3(velocity.X, velocity.Y, 0) * 1.0f;
            _body.Level = MapStates.GetLevel(MapManager.ObjLink._body.CurrentFieldState);
        }

        private void OnCollision(Values.BodyCollision direction)
        {
            // not sure why we check for the floor collision
            if ((direction & Values.BodyCollision.Floor) != 0 || (_thrown && Map.Is2dMap))
                OnCollision();
        }

        private void OnCollision()
        {
            if (!_isAlive)
                return;

            if (_body.CurrentFieldState.HasFlag(MapStates.FieldStates.DeepWater))
            {
                // spawn splash effect
                var fallAnimation = new ObjAnimator(Map,
                    (int)(_body.Position.X + _body.OffsetX + _body.Width / 2.0f),
                    (int)(_body.Position.Y + _body.OffsetY + _body.Height / 2.0f),
                    Values.LayerPlayer, "Particles/fishingSplash", "idle", true);
                Map.Objects.SpawnObject(fallAnimation);
            }
            else
            {
                if (_potMessage)
                    Game1.GameManager.StartDialogPath("break_pot");

                Game1.GameManager.PlaySoundEffect(_isHeavy ? "D378-41-29" : "D378-09-09");

                // spawn small particle stones
                SpawnParticles(EntityPosition.ToVector3());
                if (_isHeavy)
                    SpawnParticles(new Vector3(EntityPosition.X, EntityPosition.Y, EntityPosition.Z + 12));
            }

            // remove the stone object from the map
            Map.Objects.DeleteObjects.Add(this);

            _isAlive = false;
        }

        // would be nicer if it would not directly absorb the stone
        private void OnHoleAbsorb()
        {
            if (!_isAlive)
                return;

            // remove the stone object from the map
            Map.Objects.DeleteObjects.Add(this);

            // play sound effect
            Game1.GameManager.PlaySoundEffect("D360-24-18");

            var fallAnimation = new ObjAnimator(Map, 0, 0, Values.LayerBottom, "Particles/fall", "idle", true);
            fallAnimation.EntityPosition.Set(new Vector2(
                _body.Position.X + _body.OffsetX + _body.Width / 2.0f - 5,
                _body.Position.Y + _body.OffsetY + _body.Height / 2.0f - 5));
            Map.Objects.SpawnObject(fallAnimation);

            _isAlive = false;
        }

        private void SpawnParticles(Vector3 position)
        {
            var diff = 200f;

            var mult = 0.125f;
            var bodyVelocity = new Vector3(
                _body.Velocity.X * mult, _body.Velocity.Y * mult, 1.25f);

            if (Map.Is2dMap)
            {
                if ((_body.VelocityCollision & Values.BodyCollision.Horizontal) != 0)
                    bodyVelocity.X = -bodyVelocity.X * 0.5f;
                if ((_body.VelocityCollision & Values.BodyCollision.Vertical) != 0)
                    bodyVelocity.Y = -bodyVelocity.Y * 0.5f;
            }

            var rndMin = 50;
            var rndMax = 75;

            Vector3 vector0;
            Vector3 vector1;
            Vector3 vector2;
            Vector3 vector3;

            if (Map.Is2dMap)
            {
                bodyVelocity.Y = 0;
                bodyVelocity.Z = 0;
                rndMin = 55;
                vector0 = new Vector3(-0.25f, -3, 0) * Game1.RandomNumber.Next(rndMin, rndMax) / diff;
                vector1 = new Vector3(-0.75f, -2.75f, 0) * Game1.RandomNumber.Next(rndMin, rndMax) / diff;
                vector2 = new Vector3(0.25f, -3, 0) * Game1.RandomNumber.Next(rndMin, rndMax) / diff;
                vector3 = new Vector3(0.75f, -2.75f, 0) * Game1.RandomNumber.Next(rndMin, rndMax) / diff;
            }
            else
            {
                vector0 = new Vector3(-1, -1, 0) * Game1.RandomNumber.Next(rndMin, rndMax) / diff;
                vector1 = new Vector3(-1, 0, 0) * Game1.RandomNumber.Next(rndMin, rndMax) / diff;
                vector2 = new Vector3(1, -1, 0) * Game1.RandomNumber.Next(rndMin, rndMax) / diff;
                vector3 = new Vector3(1, 0, 0) * Game1.RandomNumber.Next(rndMin, rndMax) / diff;
            }

            vector0 += bodyVelocity;
            vector1 += bodyVelocity;
            vector2 += bodyVelocity;
            vector3 += bodyVelocity;

            var stone0 = new ObjSmallStone(Map, (int)position.X - 2, (int)position.Y - 13 + _offsetY, (int)position.Z, vector0, true);
            var stone1 = new ObjSmallStone(Map, (int)position.X - 1, (int)position.Y - 8 + _offsetY, (int)position.Z, vector1, true);
            var stone2 = new ObjSmallStone(Map, (int)position.X + 3, (int)position.Y - 13 + _offsetY, (int)position.Z, vector2, false);
            var stone3 = new ObjSmallStone(Map, (int)position.X + 2, (int)position.Y - 8 + _offsetY, (int)position.Z, vector3, false);

            Map.Objects.SpawnObject(stone0);
            Map.Objects.SpawnObject(stone1);
            Map.Objects.SpawnObject(stone2);
            Map.Objects.SpawnObject(stone3);
        }
    }
}