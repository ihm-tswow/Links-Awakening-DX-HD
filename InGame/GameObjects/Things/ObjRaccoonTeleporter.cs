using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameSystems;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjRaccoonTeleporter : GameObject
    {
        private readonly int _offsetX;
        private readonly int _offsetY;

        private float _teleportTime;
        private float _teleportCount;

        private float _fadeTime;

        private int _direction;
        private int _mode;
        private bool _isTeleporting;

        public ObjRaccoonTeleporter() : base("editor teleporter")
        {
            EditorColor = Color.Green * 0.5f;
        }

        // mode 0: racoon
        // mode 1: dungeon 6
        public ObjRaccoonTeleporter(Map.Map map, int posX, int posY, int offsetX, int offsetY, int width, int height, int mode) : base(map)
        {
            // TODO_End: the object lights up the scene so we cant set the EntitySize
            //EntityPosition = new CPosition(posX, posY, 0);
            //EntitySize = new Rectangle(0, 0, width, height);

            _offsetX = offsetX;
            _offsetY = offsetY;
            _mode = mode;

            _teleportTime = _mode == 0 ? 300 : 300;
            _fadeTime = mode == 0 ? 200 : 250;

            AddComponent(ObjectCollisionComponent.Index, new ObjectCollisionComponent(new Rectangle(posX, posY, width, height), OnCollision));
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
        }

        private void Update()
        {
            if (!_isTeleporting)
                return;

            if (_mode == 0 || _mode == 1)
                MapManager.ObjLink.FreezePlayer();

            _teleportCount += Game1.DeltaTime * _direction;
            if (_teleportCount >= _teleportTime)
            {
                _teleportCount = _teleportTime;
                _direction = -1;

                // teleport the colliding player to the new position
                MapManager.ObjLink.SetPosition(new Vector2(
                    MapManager.ObjLink.PosX + _offsetX * Values.TileSize,
                    MapManager.ObjLink.PosY + _offsetY * Values.TileSize));

                var goalPosition = Game1.GameManager.MapManager.GetCameraTarget();
                MapManager.Camera.SoftUpdate(goalPosition);
            }

            if (_direction < 0 && _teleportCount <= 0)
            {
                _isTeleporting = false;
            }

            var transitionSystem = (MapTransitionSystem)Game1.GameManager.GameSystems[typeof(MapTransitionSystem)];
            transitionSystem.SetColorMode(_mode == 0 ? Color.White : Color.Black, MathHelper.Clamp(_teleportCount / _fadeTime, 0, 1), false);
        }

        private void OnCollision(GameObject gameObject)
        {
            if (_isTeleporting)
                return;

            _direction = 1;
            _isTeleporting = true;

            if (_mode == 0)
            {
                Game1.GameManager.PlaySoundEffect("D360-30-1E");
                Game1.GameManager.SaveManager.SetString("raccoon_warning", "0");
            }
        }
    }
}