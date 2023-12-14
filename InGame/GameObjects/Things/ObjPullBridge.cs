using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    class ObjPullBridge : GameObject
    {
        private readonly DictAtlasEntry _spriteStart;
        private readonly DictAtlasEntry _spriteMiddle;
        private readonly DictAtlasEntry _spriteEnd;
        private readonly DictAtlasEntry _spriteHook;
        private readonly DictAtlasEntry _spriteRope;
        private readonly DictAtlasEntry _spritePullBridge;

        private Vector2 _startPosition;

        private readonly string _strKey;
        private float _state;

        private readonly int _min = 6;
        private readonly int _max = 72;

        private readonly bool _up;
        private bool _startedPulling;
        private bool _finishedPulling;

        public ObjPullBridge(Map.Map map, int posX, int posY, string strKey, bool up) : base(map, "pull_bridge")
        {
            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, up ? 0 : -64, 16, 80);

            _strKey = strKey;
            _up = up;

            _spritePullBridge = Resources.GetSprite("pull_bridge");
            _spriteStart = Resources.GetSprite("pull_bridge_start");
            _spriteMiddle = Resources.GetSprite("pull_bridge_middle");
            _spriteEnd = Resources.GetSprite("pull_bridge_end");
            _spriteHook = Resources.GetSprite("pull_bridge_hook");
            _spriteRope = Resources.GetSprite("pull_bridge_rope");

            var box = new CBox(posX + 4, posY + 2, 0, 8, 12, 8);

            // was the bridge already pulled?
            if (!string.IsNullOrEmpty(_strKey) && Game1.GameManager.SaveManager.GetString(_strKey) == "1")
                FinishedPull();
            else
            {
                AddComponent(HittableComponent.Index, new HittableComponent(box, OnHit));
                AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            }

            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerBottom, EntityPosition));
        }

        private Values.HitCollision OnHit(GameObject originObject, Vector2 direction, HitType type, int damage, bool pieceOfPower)
        {
            var dir = AnimationHelper.GetDirection(direction);

            if (!_startedPulling && type == HitType.Hookshot)
            {
                // attacking from the wrong direction?
                if(!(_up && dir == 1 || !_up && dir == 3))
                    return Values.HitCollision.RepellingParticle;

                MapManager.ObjLink.Hookshot.HookshotPosition.AddPositionListener(typeof(ObjPullBridge), OnPositionChange);

                _startPosition = MapManager.ObjLink.Hookshot.HookshotPosition.Position;
                _startedPulling = true;

                return Values.HitCollision.Blocking;
            }

            return Values.HitCollision.None;
        }

        private void OnPositionChange(CPosition newPosition)
        {
            var distance = _startPosition - newPosition.Position;
            _state = Math.Clamp((distance.Length() + _min) / _max, 0, 1);

            if (_state >= 1)
                FinishedPull();
        }

        private void Update()
        {
            if (_startedPulling && !_finishedPulling && !MapManager.ObjLink.Hookshot.IsMoving)
                FinishedPull();

            //_state = ((float)Math.Clamp(Math.Sin(Game1.TotalGameTime / 1000f) * 0.6f + 0.5f, 0, 1) * (_max - _min) + _min) / _max;
        }

        private void FinishedPull()
        {
            _state = 1;
            _finishedPulling = true;
            MapManager.ObjLink.Hookshot.HookshotPosition.PositionChangedDict.Remove(typeof(ObjPullBridge));
            RemoveHoles();

            if (!string.IsNullOrEmpty(_strKey))
                Game1.GameManager.SaveManager.SetString(_strKey, "1");
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            var state = (int)Math.Floor((_state * _max) / 8);

            var startPosition = new Vector2(EntityPosition.Position.X + 8, EntityPosition.Position.Y + (_up ? 0 : 16));
            var position = startPosition;
            var dir = _up ? 1 : -1;

            if (_state * _max <= _min)
            {
                spriteBatch.Draw(_spritePullBridge.Texture, new Vector2(position.X, position.Y + dir * 16), _spritePullBridge.ScaledRectangle, Color.White,
                    _up ? MathF.PI : 0, new Vector2(_spritePullBridge.ScaledRectangle.Width / 2f, 0), Vector2.One, SpriteEffects.None, 0);
                return;
            }

            if (state >= 9)
            {
                position.Y = startPosition.Y + (6 + _state * _max + 2) * dir;
                spriteBatch.Draw(_spriteEnd.Texture, position, _spriteEnd.ScaledRectangle, Color.White,
                    _up ? MathF.PI : 0, new Vector2(_spriteEnd.ScaledRectangle.Width / 2f, 0), Vector2.One, SpriteEffects.None, 0);
            }
            else
            {
                position.Y = startPosition.Y + (6 + _state * _max - 4) * dir;
                spriteBatch.Draw(_spriteRope.Texture, position, _spriteRope.ScaledRectangle, Color.White,
                    _up ? MathF.PI : 0, new Vector2(_spriteRope.ScaledRectangle.Width / 2f, 0), Vector2.One, SpriteEffects.None, 0);
            }

            position = new Vector2(startPosition.X, startPosition.Y + (state * 8 + 6) * dir);
            for (var y = 0; y < state; y++)
            {
                spriteBatch.Draw(_spriteMiddle.Texture, position, _spriteMiddle.ScaledRectangle, Color.White,
                    _up ? MathF.PI : 0, new Vector2(_spriteMiddle.ScaledRectangle.Width / 2f, 0), Vector2.One, SpriteEffects.None, 0);
                position.Y -= 8 * dir;
            }

            spriteBatch.Draw(_spriteStart.Texture, position, _spriteStart.ScaledRectangle, Color.White,
                _up ? MathF.PI : 0, new Vector2(_spriteStart.ScaledRectangle.Width / 2f, 0), Vector2.One, SpriteEffects.None, 0);

            // draw the hook
            if (state < 9)
            {
                position = startPosition;
                position.Y = startPosition.Y + (6 + _state * _max + 5) * dir;
                spriteBatch.Draw(_spriteHook.Texture, position, _spriteHook.ScaledRectangle, Color.White,
                    _up ? MathF.PI : 0, new Vector2(_spriteHook.ScaledRectangle.Width / 2f, 0), Vector2.One, SpriteEffects.None, 0);
            }
        }

        private void RemoveHoles()
        {
            var holeList = new List<GameObject>();
            Map.Objects.GetGameObjectsWithTag(holeList, Values.GameObjectTag.Hole,
                (int)EntityPosition.X + EntitySize.X, (int)EntityPosition.Y + EntitySize.Y, EntitySize.Width - 1, EntitySize.Height - 1);

            Map.Objects.DeleteObjects.AddRange(holeList);
        }
    }
}
