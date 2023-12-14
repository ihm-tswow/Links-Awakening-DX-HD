using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjBush : GameObject
    {
        private readonly BodyComponent _body;
        private readonly BoxCollisionComponent _collisionComponent;
        private readonly CBox _hittableBox;
        private readonly CBox _hittableBoxSmall;

        private readonly CSprite _sprite;

        private readonly string _spawnItem;
        private readonly string _spriteId;
        private readonly bool _hasCollider;
        private readonly bool _drawShadow;
        private readonly bool _setGrassField;
        private readonly int _drawLayer;
        private readonly string _pickupKey;

        private readonly object[] _spawnObjectParameter;
        private readonly string _spawnObjectId;

        private readonly int _fieldPosX;
        private readonly int _fieldPosY;

        public bool RespawnGras = true;

        public ObjBush(Map.Map map, int posX, int posY, string spawnItem, string spriteId,
            bool hasCollider, bool drawShadow, bool setGrassField, int drawLayer, string pickupKey) : base(map, spriteId)
        {
            var sprite = Resources.GetSprite(spriteId);

            EntityPosition = new CPosition(posX + 8, posY + 8, 0);
            EntitySize = new Rectangle(-8, -8, 16, 16);

            _spawnItem = spawnItem;
            _spriteId = spriteId;
            _hasCollider = hasCollider;
            _drawShadow = drawShadow;
            _setGrassField = setGrassField;
            _drawLayer = drawLayer;
            _pickupKey = pickupKey;

            _fieldPosX = posX / 16;
            _fieldPosY = posY / 16;

            // {objName}:{parameter.parameter1...}
            if (!string.IsNullOrEmpty(spawnItem))
            {
                var split = spawnItem?.Split(':');
                if (split?.Length >= 1)
                {
                    _spawnObjectId = split[0];
                    string[] parameter = null;

                    if (split.Length >= 2)
                        parameter = split[1].Split('.');

                    _spawnObjectParameter = MapData.GetParameter(_spawnObjectId, parameter);
                    if (_spawnObjectParameter == null)
                        return;

                    _spawnObjectParameter[1] = posX;
                    _spawnObjectParameter[2] = posY;
                }
            }

            _hittableBox = new CBox(EntityPosition, -7, -7, 0, 14, 13, 8, true);
            _hittableBoxSmall = new CBox(EntityPosition, -4, -4, 0, 8, 8, 8, true);

            if (hasCollider)
            {
                _body = new BodyComponent(EntityPosition, -8, -7, 16, 15, 8)
                {
                    MoveCollision = Collision,
                    DragAir = 1.0f,
                    Gravity = -0.125f
                };
                AddComponent(BodyComponent.Index, _body);

                var collisionBox = new CBox(EntityPosition, -8, -7, 0, 16, 14, 16, true);
                AddComponent(CollisionComponent.Index, _collisionComponent =
                    new BoxCollisionComponent(collisionBox, Values.CollisionTypes.Normal | Values.CollisionTypes.ThrowWeaponIgnore));
                AddComponent(CarriableComponent.Index, new CarriableComponent(
                    new CRectangle(EntityPosition, new Rectangle(-8, -7, 16, 15)), CarryInit, CarryUpdate, CarryThrow));
            }

            if (setGrassField)
                Map.SetFieldState(_fieldPosX, _fieldPosY, MapStates.FieldStates.Grass);

            AddComponent(HittableComponent.Index, new HittableComponent(_hittableBox, OnHit));

            _sprite = new CSprite(spriteId, EntityPosition, Vector2.Zero);
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(_sprite, drawLayer));

            if (drawShadow)
            {
                // not sure where this is used
                if (_body == null)
                    AddComponent(DrawShadowComponent.Index, new DrawShadowSpriteComponent(
                        Resources.SprObjects, EntityPosition, sprite.ScaledRectangle,
                        new Vector2(sprite.Origin.X + 1.0f, sprite.Origin.Y - 1.0f), 1.0f, 0.0f));
                else
                    AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(_body, _sprite));
            }
        }

        private Vector3 CarryInit()
        {
            // the stone was picked up
            _collisionComponent.IsActive = false;
            _body.IsActive = false;

            SpawnItem(Vector2.Zero);

            return new Vector3(EntityPosition.X, EntityPosition.Y + 6, EntityPosition.Z);
        }

        private bool CarryUpdate(Vector3 newPosition)
        {
            EntityPosition.X = newPosition.X;
            EntityPosition.Y = newPosition.Y - 6;
            EntityPosition.Z = newPosition.Z;

            EntityPosition.NotifyListeners();
            return true;
        }

        private void CarryThrow(Vector2 velocity)
        {
            _body.IsGrounded = false;
            _body.IsActive = true;
            _body.Velocity = new Vector3(velocity.X, velocity.Y, 0) * 1.0f;
        }

        private void Collision(Values.BodyCollision direction)
        {
            DestroyBush(new Vector2(_body.Velocity.X, _body.Velocity.Y));
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (IsDead ||
                (damageType & HitType.SwordHold) != 0 ||
                damageType == HitType.Bow ||
                damageType == HitType.Hookshot ||
                damageType == HitType.SwordShot ||
                damageType == HitType.PegasusBootsPush ||
                damageType == HitType.MagicRod && !_hasCollider ||
                damageType == HitType.Boomerang && !_hasCollider ||
                damageType == HitType.ThrownObject && !_hasCollider)
                return Values.HitCollision.None;

            // this is really stupid
            // for the sword attacks a smaller hitbox is used
            if (_hasCollider &&
                (damageType & HitType.Sword) != 0 &&
                gameObject is ObjLink player && !player.IsPoking)
            {
                var collidingRec = player.SwordDamageBox.Rectangle().GetIntersection(_hittableBoxSmall.Box.Rectangle());
                var collidingArea = collidingRec.Width * collidingRec.Height;

                if (collidingArea < 16)
                    return Values.HitCollision.None;
            }

            SpawnItem(direction);

            DestroyBush(direction);

            return Values.HitCollision.NoneBlocking;
        }

        private void SpawnItem(Vector2 direction)
        {
            // set the pickup key
            if (!string.IsNullOrEmpty(_pickupKey))
                Game1.GameManager.SaveManager.SetString(_pickupKey, "1");

            // spawn the object if it exists
            bool spawnedObject = false;

            // try to spawn the object
            if (!string.IsNullOrEmpty(_spawnObjectId))
            {
                var objSpawnedObject = ObjectManager.GetGameObject(Map, _spawnObjectId, _spawnObjectParameter);
                spawnedObject = Map.Objects.SpawnObject(objSpawnedObject);
                if (spawnedObject && objSpawnedObject is ObjItem spawnedItem)
                    spawnedItem.SetVelocity(new Vector3(direction.X * 0.5f, direction.Y * 0.5f, 0.75f));
            }

            if (!spawnedObject)
            {
                // TODO_End reevaluate
                // need to find a source for this data
                // rube1 = 6/100, hearth = 3/100
                string strObject = null;
                var random = Game1.RandomNumber.Next(0, 100);
                if (random < 6)
                    strObject = "ruby";
                else if (random < 9)
                    strObject = "heart";

                // spawn a heart or a ruby
                if (strObject != null)
                {
                    var objItem = new ObjItem(Map, (int)EntityPosition.X - 8, (int)EntityPosition.Y - 8, "j", null, strObject, null, true);
                    objItem.SetVelocity(new Vector3(direction.X * 0.5f, direction.Y * 0.5f, 0.75f));
                    Map.Objects.SpawnObject(objItem);
                }
            }
        }

        public void DestroyBush(Vector2 direction)
        {
            if (IsDead)
                return;
            IsDead = true;

            // sound effect
            Game1.GameManager.PlaySoundEffect("D378-05-05");

            if (RespawnGras)
                Map.Objects.SpawnObject(new ObjBushRespawner(Map, (int)EntityPosition.X - 8, (int)EntityPosition.Y - 8,
                    _spawnItem, _spriteId, _hasCollider, _drawShadow, _setGrassField, _drawLayer, _pickupKey));

            // delete this object
            Map.Objects.DeleteObjects.Add(this);

            // reset FieldStates
            Map.RemoveFieldState(_fieldPosX, _fieldPosY, MapStates.FieldStates.Grass);

            // spawn the leafs
            var offsets = new[] { new Point(-7, -1), new Point(1, -1), new Point(-7, 7), new Point(1, 7) };
            for (var i = 0; i < offsets.Length; i++)
            {
                var posZ = EntityPosition.Z + 5 - Game1.RandomNumber.Next(0, 40) / 10f;
                var newLeaf = new ObjLeaf(Map, (int)EntityPosition.X + offsets[i].X, (int)EntityPosition.Y + offsets[i].Y, posZ, direction);
                Map.Objects.SpawnObject(newLeaf);
            }
        }
    }
}