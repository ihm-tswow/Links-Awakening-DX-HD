using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.Things;
using ProjectZ.InGame.Map;
using System.Collections.Generic;
using ProjectZ.InGame.GameObjects.Things;

namespace ProjectZ.InGame.GameObjects.Dungeon
{
    internal class ObjDungeonSixEntry : GameObject
    {
        private List<GameObject> _objectList;

        private readonly CSprite _sprite;
        private readonly string _strKey;

        private bool _opening;
        private bool _isOpen;
        private bool _init;

        private float _openCounter;

        private bool _spawnParticles;
        private float _particleCounter;

        public ObjDungeonSixEntry() : base("dungeonSixEntry") { }

        public ObjDungeonSixEntry(Map.Map map, int posX, int posY, string strKey) : base(map)
        {
            // do not spawn the entrance if it is already open
            if (!string.IsNullOrEmpty(strKey) && Game1.GameManager.SaveManager.GetString(strKey) == "1")
            {
                _isOpen = true;
            }

            _strKey = strKey;

            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 80, 64);

            _sprite = new CSprite("dungeonSixEntry", EntityPosition, Vector2.Zero) { IsVisible = _isOpen };

            if (!_isOpen)
            {
                AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(OnKeyChange));
                AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            }
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(_sprite, Values.LayerBottom));
        }

        private void Update()
        {
            if (!_init)
            {
                _init = true;
                // deactivate the objects at the position of the entry 
                _objectList = Map.Objects.GetObjects((int)EntityPosition.X, (int)EntityPosition.Y + 16, 80, 48);
                SetObjectState(false);
            }

            if (!_opening)
                return;

            MapManager.ObjLink.FreezePlayer();

            _openCounter += Game1.DeltaTime;

            if (_spawnParticles)
            {
                _particleCounter += Game1.DeltaTime;
                if (_particleCounter > 75)
                {
                    _particleCounter -= 75;
                    var posX = (int)EntityPosition.X + Game1.RandomNumber.Next(0, 64);
                    var posY = (int)EntityPosition.Y + _sprite.SourceRectangle.Height - 12 + Game1.RandomNumber.Next(0, 8);
                    Map.Objects.SpawnObject(new ObjAnimator(Map, posX, posY, Values.LayerPlayer, "Particles/spawn", "run", true));
                }
            }

            if (_openCounter > 2000)
            {
                _openCounter -= 750;
                _spawnParticles = true;

                if (_sprite.SourceRectangle.Height < 64)
                {
                    EntityPosition.Set(new Vector2(EntityPosition.X, EntityPosition.Y - 8));
                    _sprite.SourceRectangle.Height += 8;

                    Game1.GameManager.PlaySoundEffect("D360-47-2F");

                    if (_sprite.SourceRectangle.Height == 64)
                    {
                        _spawnParticles = false;
                        Game1.GameManager.PlaySoundEffect("D360-02-02");
                    }
                }
                else
                {
                    _opening = false;
                    _isOpen = true;
                    Game1.GameManager.SetMusic(-1, 2);
                }
            }
        }

        private void SetObjectState(bool isActive)
        {
            for (var i = 0; i < _objectList.Count; i++)
                _objectList[i].IsActive = isActive;
        }

        private void Open()
        {
            if (_opening || _isOpen)
                return;

            Game1.GbsPlayer.Stop();

            SetObjectState(true);

            _opening = true;

            _sprite.IsVisible = true;
            _sprite.SourceRectangle.Height = 32;

            EntityPosition.Set(new Vector2(EntityPosition.X, EntityPosition.Y + 32));

            Game1.GameManager.ShakeScreen(4250, 2, 1, 5.0f, 2.25f);
        }

        private void OnKeyChange()
        {
            if (!string.IsNullOrEmpty(_strKey) && Game1.GameManager.SaveManager.GetString(_strKey) == "1")
                Open();
        }
    }
}