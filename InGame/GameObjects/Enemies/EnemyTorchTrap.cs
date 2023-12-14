using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    class EnemyTorchTrap : GameObject
    {
        private readonly Rectangle _fieldRectangle;
        private readonly string _key;

        private float _fireballCounter;
        private const int FireballTime = 3000;
        private bool _isActive;

        public EnemyTorchTrap() : base("torch trap") { }

        public EnemyTorchTrap(Map.Map map, int posX, int posY, string key) : base(map)
        {
            EntityPosition = new CPosition(posX + 8, posY + 8, 0);
            EntitySize = new Rectangle(-8, -8, 16, 16);

            _fieldRectangle = map.GetField(posX, posY, 16);

            _key = key;

            if (!string.IsNullOrEmpty(_key))
                AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(OnKeyChanged));
            else
                _isActive = true;

            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
        }

        private void OnKeyChanged()
        {
            _isActive = Game1.GameManager.SaveManager.GetString(_key) != "1";
        }

        private void Update()
        {
            if (!_isActive || !_fieldRectangle.Contains(MapManager.ObjLink.EntityPosition.Position))
                return;

            _fireballCounter += Game1.DeltaTime;
            if (_fireballCounter < FireballTime)
                return;
            
            _fireballCounter -= FireballTime;

            // spawn a fireball?
            var distance = EntityPosition.Position - MapManager.ObjLink.EntityPosition.Position;
            if (distance.Length() < 106)
                Map.Objects.SpawnObject(new EnemyFireball(Map, (int)EntityPosition.X, (int)EntityPosition.Y, 1.25f));
        }
    }
}
