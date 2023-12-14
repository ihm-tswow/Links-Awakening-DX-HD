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
    public class ObjDoorEgg : GameObject
    {
        private readonly DictAtlasEntry _doorSprite;
        private readonly Vector2[] _relicOffsets = new Vector2[8];
        private readonly string _saveKey;

        private bool[] _showInstrument = new bool[8];
        private float _instrumentCounter = -500;
        private int _shownInstrument;
        private int _playerInstrumentCount;
        private int _songEnd;
        private bool _shakeScreen;
        private bool _isRunning;
        private bool _linkOcarinaAnimation;
        private bool _drawInstruments = true;

        public ObjDoorEgg() : base("egg_entry") { }

        public ObjDoorEgg(Map.Map map, int posX, int posY, string saveId) : base(map)
        {
            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            _saveKey = saveId;
            if (!string.IsNullOrEmpty(_saveKey) &&
                Game1.GameManager.SaveManager.GetString(_saveKey) == "1")
            {
                IsDead = true;
                return;
            }

            _doorSprite = Resources.GetSprite("egg_entry");

            // -48 -16 16 48
            // -48 -16 16 48
            _relicOffsets[7] = new Vector2(-16, -40);
            _relicOffsets[0] = new Vector2(16, -40);
            _relicOffsets[6] = new Vector2(-40, -15);
            _relicOffsets[1] = new Vector2(40, -15);
            _relicOffsets[5] = new Vector2(-40, 16);
            _relicOffsets[2] = new Vector2(40, 16);
            _relicOffsets[4] = new Vector2(-16, 48);
            _relicOffsets[3] = new Vector2(16, 48);

            AddComponent(CollisionComponent.Index, new BoxCollisionComponent(new CBox(EntityPosition, -8, -16, 16, 16, 8), Values.CollisionTypes.Normal));
            AddComponent(OcarinaListenerComponent.Index, new OcarinaListenerComponent(OnSongPlayed));
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerBottom, EntityPosition));
        }

        private void OnSongPlayed(int songIndex)
        {
            if (songIndex != 0 || _isRunning)
                return;

            _isRunning = true;
            _linkOcarinaAnimation = false;
            _instrumentCounter = 0;
            _playerInstrumentCount = 0;
            for (var i = 0; i < 8; i++)
            {
                var itemName = "instrument" + i;
                var item = Game1.GameManager.GetItem(itemName);
                if (item != null)
                    _playerInstrumentCount++;
            }

            _songEnd = _playerInstrumentCount <= 2 ? 36000 : 41000;

            // freeze the animation until the song gets played
            MapManager.ObjLink.FreezeAnimationState();

            Game1.GameManager.StopMusic();
        }

        private void Update()
        {
            if (!_isRunning)
                return;

            MapManager.ObjLink.FreezePlayer();

            _instrumentCounter += Game1.DeltaTime;

            // instrument apearing sounds
            if (_instrumentCounter > _shownInstrument * 500 && _shownInstrument < 8)
            {
                var itemName = "instrument" + _shownInstrument;
                var item = Game1.GameManager.GetItem(itemName);
                if (item != null)
                    Game1.GameManager.PlaySoundEffect("D378-43-2B");
                _showInstrument[_shownInstrument] = item != null;
                _shownInstrument++;
            }

            if (!_linkOcarinaAnimation && _instrumentCounter > 8 * 500)
            {
                _linkOcarinaAnimation = true;
                MapManager.ObjLink.StartOcarinaDuo();
                // @TODO: can we only access this state with at least 2 instruments?
                Game1.GameManager.SetMusic(62 + _playerInstrumentCount, 2);
            }

            // shake the screen
            if (!_shakeScreen && _instrumentCounter > _songEnd)
            {
                _drawInstruments = false;

                if (_playerInstrumentCount < 8)
                {
                    _isRunning = false;
                    MapManager.ObjLink.StopOcarinaDuo();
                    Game1.GameManager.SetMusic(-1, 2);
                    return;
                }

                _shakeScreen = true;

                Game1.GameManager.ShakeScreen(2500, 1, 0, 5.5f, 0);
                MapManager.ObjLink.FreezeAnimationState();
            }

            // spawn the stone particles and delete the object
            if (_instrumentCounter > _songEnd + 2500)
            {
                _linkOcarinaAnimation = false;
                MapManager.ObjLink.StopOcarinaDuo();

                Map.Objects.SpawnObject(new ObjSmallStone(Map, (int)EntityPosition.X - 4, (int)EntityPosition.Y - 20, (int)EntityPosition.Z, new Vector3(-1.05f, 0.25f, 3), true, 650));
                Map.Objects.SpawnObject(new ObjSmallStone(Map, (int)EntityPosition.X - 4, (int)EntityPosition.Y - 16, (int)EntityPosition.Z, new Vector3(-1.25f, 0.75f, 3), true, 650));
                Map.Objects.SpawnObject(new ObjSmallStone(Map, (int)EntityPosition.X - 4, (int)EntityPosition.Y - 12, (int)EntityPosition.Z, new Vector3(-0.85f, 1.25f, 3), true, 650));

                Map.Objects.SpawnObject(new ObjSmallStone(Map, (int)EntityPosition.X - 0, (int)EntityPosition.Y - 22, (int)EntityPosition.Z, new Vector3(-0.3f, 0.05f, 3), true, 650));
                Map.Objects.SpawnObject(new ObjSmallStone(Map, (int)EntityPosition.X - 0, (int)EntityPosition.Y - 12, (int)EntityPosition.Z, new Vector3(0.35f, 1.45f, 3), true, 650));

                Map.Objects.SpawnObject(new ObjSmallStone(Map, (int)EntityPosition.X + 4, (int)EntityPosition.Y - 20, (int)EntityPosition.Z, new Vector3(1.0f, 0.25f, 3), true, 650));
                Map.Objects.SpawnObject(new ObjSmallStone(Map, (int)EntityPosition.X + 4, (int)EntityPosition.Y - 16, (int)EntityPosition.Z, new Vector3(1.25f, 0.75f, 3), true, 650));
                Map.Objects.SpawnObject(new ObjSmallStone(Map, (int)EntityPosition.X + 4, (int)EntityPosition.Y - 12, (int)EntityPosition.Z, new Vector3(0.9f, 1.25f, 3), true, 650));

                if (!string.IsNullOrEmpty(_saveKey))
                    Game1.GameManager.SaveManager.SetString(_saveKey, "1");

                Game1.GameManager.SaveManager.SetString("owl", "9_0");

                Game1.GameManager.PlaySoundEffect("D360-35-23");
                Game1.GameManager.PlaySoundEffect("D378-12-0C");

                Map.Objects.DeleteObjects.Add(this);
            }
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            // draw the door
            DrawHelper.DrawNormalized(spriteBatch, _doorSprite, EntityPosition.Position, Color.White);

            // draw the instruments
            if (_drawInstruments)
                for (int i = 0; i < 8; i++)
                {
                    if (!_showInstrument[i])
                        continue;

                    var length = 8 * 250;
                    var counterMod = _instrumentCounter % length;

                    // blink
                    if (_instrumentCounter < i * 500 ||
                        (i * 250 < counterMod && counterMod < i * 250 + 200) ||
                        (((length / 2.95f) + i * 250) % length < counterMod && counterMod < ((length / 2.95f) + i * 250 + 200) % length) ||
                        (((length / 2.95f) * 2 + i * 250) % length < counterMod && counterMod < ((length / 2.95f) * 2 + i * 250 + 200) % length))
                        continue;

                    var itemName = "instrument" + i;
                    var itemInstrument = Game1.GameManager.ItemManager[itemName];
                    var position = new Vector2(EntityPosition.X - 8, EntityPosition.Y - 8) + _relicOffsets[i];

                    ItemDrawHelper.DrawItem(spriteBatch, itemInstrument, position, Color.White, 1, true);
                }
        }
    }
}