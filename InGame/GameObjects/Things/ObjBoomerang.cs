using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjBoomerang : GameObject
    {
        private readonly List<GameObject> _itemList = new List<GameObject>();

        private readonly BodyComponent _body;
        private readonly CBox _damageBox;

        private ObjItem _item;

        private Vector2 _startPosition;
        private Vector2 _direction;

        private bool _comingBack;

        private bool _isReady = true;
        public bool IsReady => _isReady;

        public ObjBoomerang()
        {
            EntityPosition = new CPosition(0, 0, 4);
            EntityPosition.AddPositionListener(typeof(ObjBoomerang), UpdateItemPosition);
            EntitySize = new Rectangle(-8, -12, 16, 16);

            _damageBox = new CBox(EntityPosition, -5, -5, 0, 10, 10, 4, true);

            var animation = AnimatorSaveLoad.LoadAnimator("Objects/boomerang");
            animation.Play("run");

            _body = new BodyComponent(EntityPosition, -1, -1, 2, 2, 8)
            {
                IgnoresZ = true,
                MoveCollision = OnCollision,
                CollisionTypesIgnore = Values.CollisionTypes.ThrowWeaponIgnore,
                IgnoreInsideCollision = false,
            };

            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(animation, sprite, new Vector2(-6, -6));

            AddComponent(BodyComponent.Index, _body);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(_body, sprite));
        }

        public void Reset()
        {
            _isReady = true;
        }

        public void Start(Map.Map map, Vector3 position, Vector2 direction)
        {
            Map = map;

            EntityPosition.Set(new Vector3(position.X, position.Y, position.Z + 4));

            _startPosition = new Vector2(position.X, position.Y);
            _direction = direction;
            _body.VelocityTarget = Vector2.Zero;
            _body.CollisionTypes = Values.CollisionTypes.Normal;
            // if the player is on an upper level he can shoot down through walls that are blocking if the is not on the upper level
            _body.Level = MapStates.GetLevel(MapManager.ObjLink._body.CurrentFieldState);

            _comingBack = false;
            _isReady = false;
            _item = null;
        }

        private void Update()
        {
            // play sound effect
            Game1.GameManager.PlaySoundEffect("D378-45-2D", false);

            if (!_comingBack)
            {
                EntityPosition.Z = AnimationHelper.MoveToTarget(EntityPosition.Z, 4, 0.35f * Game1.TimeMultiplier);

                var distance = (_startPosition - EntityPosition.Position).Length();
                var speed = 3f - (float)Math.Sin(MathHelper.Clamp(distance / 80, 0, 1) * (Math.PI / 2));
                _body.VelocityTarget = _direction * speed;

                if (distance >= 80)
                    ComeBack();
            }
            else
            {
                EntityPosition.Z = AnimationHelper.MoveToTarget(EntityPosition.Z, 4, 1.25f * Game1.TimeMultiplier);

                var direction = new Vector2(
                    MapManager.ObjLink.EntityPosition.Position.X,
                    MapManager.ObjLink.EntityPosition.Position.Y - 3) - EntityPosition.Position;
                var distance = direction.Length();
                var speed = 3f - (float)Math.Sin(MathHelper.Clamp(distance / 80, 0, 1) * (Math.PI / 2));
                speed = Math.Min(speed, distance);

                if (direction != Vector2.Zero)
                    direction.Normalize();
                _body.VelocityTarget = direction * speed;

                // MapManager.ObjLink.IsGrounded()
                if ((Map.Is2dMap || Math.Abs(MapManager.ObjLink.EntityPosition.Z - EntityPosition.Z) <= 6) && distance < 2)
                {
                    _isReady = true;
                    Map.Objects.DeleteObjects.Add(this);
                }
            }

            CollectItem();

            var collision = Map.Objects.Hit(this, EntityPosition.Position, _damageBox.Box, HitType.Boomerang, 32, false);
            if (!_comingBack &&
                (collision & (Values.HitCollision.Blocking | Values.HitCollision.Repelling | Values.HitCollision.Enemy)) != 0)
            {
                var particle = (collision & Values.HitCollision.Repelling) != 0;
                ComeBack(particle);
            }
        }

        private void CollectItem()
        {
            if (_item != null && !_item.Collected)
                return;

            _item = null;

            _itemList.Clear();
            Map.Objects.GetComponentList(_itemList, (int)_damageBox.Box.X, (int)_damageBox.Box.Y,
                (int)_damageBox.Box.Width, (int)_damageBox.Box.Height, CollisionComponent.Mask);

            // check if an item was found
            foreach (var gameObject in _itemList)
            {
                var collidingBox = Box.Empty;
                var collisionObject = gameObject.Components[CollisionComponent.Index] as CollisionComponent;
                if ((collisionObject.CollisionType & Values.CollisionTypes.Item) != 0 &&
                     collisionObject.Collision(_damageBox.Box, 0, 0, ref collidingBox))
                {
                    var newItem = (ObjItem)collisionObject.Owner;
                    if (!newItem.Collected)
                    {
                        _item = newItem;
                        _item.InitCollection();
                    }
                }
            }
        }

        private void UpdateItemPosition(CPosition position)
        {
            _item?.EntityPosition.Set(new Vector3(position.X, position.Y + 4, position.Z));
        }

        private void OnCollision(Values.BodyCollision collision)
        {
            ComeBack(true);
        }

        private void ComeBack(bool particle = false)
        {
            if (particle)
            {
                var animation = new ObjAnimator(Map, 0, 0, Values.LayerTop, "Particles/swordPoke", "run", true);
                animation.EntityPosition.Set(new Vector3(EntityPosition.X, EntityPosition.Y, EntityPosition.Z));
                Map.Objects.SpawnObject(animation);

                Game1.GameManager.PlaySoundEffect("D360-07-07");
            }

            _comingBack = true;
            _body.CollisionTypes = Values.CollisionTypes.None;
        }
    }
}