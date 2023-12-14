using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjFinalStairs : GameObject
    {
        private DrawSpriteComponent _drawComponent;
        private CBox _collisionBox;

        private const string spriteId = "final_stairs";
        private string _spawnKey;
        private bool _collided;
        private bool _spawned;

        private float _spawnTime = 8 / 60f * 1000;
        private float _spawnCounter;
        private int _spawnIndex;

        public ObjFinalStairs() : base(spriteId) { }

        public ObjFinalStairs(Map.Map map, int posX, int posY, string spawnKey) : base(map)
        {
            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 16, 16);

            _spawnKey = spawnKey;
            _collisionBox = new CBox(posX + 7, posY + 5, 0, 2, 2, 2);

            if (!string.IsNullOrEmpty(_spawnKey))
                AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(OnKeyChange));
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, _drawComponent = new DrawSpriteComponent(spriteId, EntityPosition, Values.LayerBottom) { IsActive = false });
        }

        private void OnKeyChange()
        {
            if (!_spawned && Game1.GameManager.SaveManager.GetString(_spawnKey) == "1")
            {
                _spawned = true;
                _drawComponent.IsActive = true;
                Game1.GameManager.PlaySoundEffect("D360-47-2F");
            }
        }

        private void Update()
        {
            if (!_spawned)
                return;

            if (_collided && _spawnIndex < 2)
            {
                _spawnCounter += Game1.DeltaTime;
                if(_spawnCounter > _spawnTime)
                {
                    _spawnCounter -= _spawnTime;
                    _spawnIndex++;

                    var objSprite = new ObjSprite(Map, (int)EntityPosition.X, (int)EntityPosition.Y - _spawnIndex * 16, spriteId, Vector2.Zero, Values.LayerBottom, null);
                    Map.Objects.SpawnObject(objSprite);
                }
            }

            // collision with the player?
            if (!_collided && MapManager.ObjLink._body.BodyBox.Box.Contains(_collisionBox.Box))
            {
                _collided = true;
                Game1.GameManager.StartDialogPath("final_stairs");
                MapManager.ObjLink.SetPosition(new Vector2(EntityPosition.X + 8, EntityPosition.Y + 12));
            }
        }
    }
}