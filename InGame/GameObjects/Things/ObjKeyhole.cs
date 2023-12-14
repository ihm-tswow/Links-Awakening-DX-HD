using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjKeyhole : GameObject
    {
        private readonly string _itemName;
        private readonly string _outputKey;
        private readonly string _strDialog;

        private readonly int _shakeTime = 2250;
        private readonly int _openTime = 2800;

        private float _counter;
        private bool _isOpening;
        private bool _isPushed;
        private bool _wasPushed;
        private bool _rumbling;

        public ObjKeyhole() : base("keyhole_block") { }

        public ObjKeyhole(Map.Map map, int posX, int posY, string itemName, string outputKey, string strDialog) : base(map)
        {
            _itemName = itemName;
            _outputKey = outputKey;
            _strDialog = strDialog;

            // check if the lock was already activated
            if (_itemName == null || string.IsNullOrEmpty(_outputKey) ||
                Game1.GameManager.SaveManager.GetString(_outputKey) == "1")
                IsDead = true;

            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(PushableComponent.Index, new PushableComponent(new CBox(posX + 3, posY + 8, 0, 10, 8, 8), OnPush) { InertiaTime = 75 });
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType pushType)
        {
            // check if the player has the key to open the door
            if (pushType == PushableComponent.PushType.Impact || _isOpening || direction.Y >= 0)
                return false;

            _isPushed = true;

            if (Game1.GameManager.GetItem(_itemName) == null)
            {
                // make sure to only show the dialog the first time while being pushed and after stopping to push
                if (!_wasPushed)
                    Game1.GameManager.StartDialogPath(_strDialog);

                return false;
            }

            Open();

            return true;
        }

        private void Open()
        {
            _isOpening = true;

            Game1.GbsPlayer.Pause();

            // key sound
            Game1.GameManager.PlaySoundEffect("D378-04-04");
        }

        private void Update()
        {
            _wasPushed = _isPushed;
            _isPushed = false;

            if (!_isOpening)
                return;

            MapManager.ObjLink.FreezePlayer();

            _counter += Game1.DeltaTime;

            if (!_rumbling && _counter > 500)
            {
                _rumbling = true;

                // dungeon one sound
                Game1.GameManager.PlaySoundEffect("D378-42-2A");

                // rumble sound; maybe used for other dungeons?
                //Game1.GameManager.PlaySoundEffect("D378-29-1D");
                //Game1.GameManager.PlaySoundEffect("D378-46-2E");

                // shake the screen
                Game1.GameManager.ShakeScreen(_shakeTime, 2, 1, 5.0f, 2.25f);
            }

            if (_counter <= _openTime)
                return;

            // set the key and open the gate
            Game1.GameManager.SaveManager.SetString(_outputKey, "1");

            Map.Objects.DeleteObjects.Add(this);

            Game1.GbsPlayer.Resume();
        }
    }
}