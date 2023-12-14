using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    public class ObjHookshot : GameObject
    {
        public CPosition HookshotPosition => _hookshotPosition;

        public bool IsMoving;

        private readonly List<GameObject> _itemList = new List<GameObject>();
        private readonly CPosition _hookshotPosition;
        private ObjItem _item;

        private readonly DictAtlasEntry _spriteChain;
        private readonly DictAtlasEntry _spriteHook;

        private readonly BodyComponent _body;
        private readonly CBox _damageBox;

        private Vector2 _direction;
        private Vector2 _startPositionOffset;

        private const float Speed = 3;

        private bool _comingBack;
        private bool _pullingPlayer;
        private bool _pokeParticleSpawned;

        private float _soundCounter;

        public ObjHookshot()
        {
            _hookshotPosition = new CPosition(0, 0, 0);
            _hookshotPosition.AddPositionListener(typeof(ObjHookshot), UpdateItemPosition);

            EntitySize = new Rectangle(-10, -10, 20, 20);

            _damageBox = new CBox(_hookshotPosition, -5, -5, 0, 10, 10, 8, true);

            _body = new BodyComponent(_hookshotPosition, -1, -1, 2, 2, 8)
            {
                IgnoresZ = true,
                IgnoreHoles = true,
                IgnoreInsideCollision = false,
                MoveCollision = OnCollision,
            };

            _spriteChain = Resources.GetSprite("hookshot_chain");
            _spriteHook = Resources.GetSprite("hookshot_hook");

            AddComponent(BodyComponent.Index, _body);
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerPlayer, _hookshotPosition));
        }

        public void Reset()
        {
            IsMoving = false;
        }

        public void Start(Map.Map map, Vector3 position, Vector2 direction)
        {
            Map = map;

            _hookshotPosition.Set(position);
            _startPositionOffset = new Vector2(position.X, position.Y) - MapManager.ObjLink.EntityPosition.Position;

            _body.VelocityTarget = direction * Speed;
            _body.CollisionTypes = Values.CollisionTypes.Normal | Values.CollisionTypes.Hookshot;
            // if the player is on an upper level he can shoot down through walls that are blocking if the is not on the upper level
            _body.Level = MapStates.GetLevel(MapManager.ObjLink._body.CurrentFieldState);

            _direction = direction;

            _comingBack = false;
            _pullingPlayer = false;
            _item = null;

            IsMoving = true;
            _pokeParticleSpawned = false;
        }

        private void Update()
        {
            _soundCounter += Game1.DeltaTime;
            if (_soundCounter > 65)
            {
                _soundCounter -= 65;
                Game1.GameManager.PlaySoundEffect("D378-11-0B", true);
            }

            if (_pullingPlayer)
            {
                if (!MapManager.ObjLink.UpdateHookshotPull())
                    Despawn();

                return;
            }

            var direction = MapManager.ObjLink.EntityPosition.Position + _startPositionOffset - _hookshotPosition.Position;
            var distance = direction.Length();

            _hookshotPosition.Z = MapManager.ObjLink.EntityPosition.Z;

            if (!_comingBack)
            {
                if (distance > 120)
                    ComeBack();
            }
            else
            {
                if (direction != Vector2.Zero)
                    direction.Normalize();

                _body.VelocityTarget = direction * Speed;

                if (distance < 2)
                    Despawn();
            }

            CollectItem();

            // do not hit stuff while coming back
            if (!_comingBack)
            {
                // damage: 2
                var collision = Map.Objects.Hit(this, _hookshotPosition.Position, _damageBox.Box, HitType.Hookshot, 2, false, false);
                if ((collision & (
                    Values.HitCollision.Enemy | Values.HitCollision.Blocking |
                    Values.HitCollision.Repelling | Values.HitCollision.RepellingParticle)) != 0)
                {
                    if ((collision & Values.HitCollision.RepellingParticle) != 0 && !_pokeParticleSpawned)
                        Repell();

                    ComeBack();
                }
            }
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            // draw the chain
            var handPosition = MapManager.ObjLink.EntityPosition.ToVector3();
            handPosition.X += _startPositionOffset.X;
            handPosition.Y += _startPositionOffset.Y;
            var direction = _hookshotPosition.ToVector3() - handPosition;
            for (var i = 0; i < 3; i++)
                spriteBatch.Draw(_spriteChain.Texture, new Vector2(handPosition.X - 2, handPosition.Y - 2 - MapManager.ObjLink.EntityPosition.Z) +
                                                       new Vector2(direction.X, direction.Y) * ((i + 0.75f) / 4f) +
                                                       new Vector2(0, -direction.Z * ((i + 0.75f) / 4f)), _spriteChain.SourceRectangle, Color.White);

            // draw the hook
            spriteBatch.Draw(_spriteHook.Texture, new Vector2(_hookshotPosition.X - 7,
                _hookshotPosition.Y - 7 - _hookshotPosition.Z), _spriteHook.SourceRectangle, Color.White);
        }

        private void Despawn()
        {
            IsMoving = false;
            Map.Objects.DeleteObjects.Add(this);
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
                    if (newItem.IsActive && !newItem.Collected)
                    {
                        _item = newItem;
                        _item.InitCollection();
                        ComeBack();
                    }
                }
            }
        }

        private void UpdateItemPosition(CPosition position)
        {
            _item?.EntityPosition.Set(new Vector3(position.X, position.Y + 4, position.Z));
        }

        private void ComeBack()
        {
            _comingBack = true;
            _body.CollisionTypes = Values.CollisionTypes.None;
        }

        private void OnCollision(Values.BodyCollision collision)
        {
            var collidingBox = Box.Empty;
            var box = _body.BodyBox.Box;
            box.X += _direction.X;
            box.Y += _direction.Y;

            if (Map.Objects.Collision(box, Box.Empty, Values.CollisionTypes.Hookshot, 0, _body.Level, ref collidingBox))
            {
                // gets pulled towards the colliding box
                _body.VelocityTarget = Vector2.Zero;
                _pullingPlayer = true;
                MapManager.ObjLink.StartHookshotPull();
            }
            else
            {
                Repell();

                ComeBack();
            }
        }

        private void Repell()
        {
            _pokeParticleSpawned = true;

            // hookshot repels from the colliding box
            Game1.GameManager.PlaySoundEffect("D360-07-07");

            var animation = new ObjAnimator(Map, 0, 0, Values.LayerTop, "Particles/swordPoke", "run", true);
            animation.EntityPosition.Set(_hookshotPosition.Position);
            Map.Objects.SpawnObject(animation);
        }
    }
}