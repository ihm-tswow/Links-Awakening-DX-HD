using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Dungeon
{
    class ObjDungeonPillar : GameObject
    {
        private readonly BoxCollisionComponent _collisionComponent;
        private readonly AiComponent _aiComponent;

        private readonly CSprite[] _sprites = new CSprite[5];
        private readonly string[] _spriteIds = { "pillar_bottom", "pillar_middle", "pillar_middle", "pillar_middle", "pillar_top" };

        private string _saveKey;

        private int _pillarIndex = 5;
        private float _fallCount;
        private float _particleCounter;
        private float _stoneCounter;

        private Vector2 _basePosition;

        public ObjDungeonPillar() : base("pillar_bottom") { }

        public ObjDungeonPillar(Map.Map map, int posX, int posY, string saveKey) : base(map)
        {
            _saveKey = saveKey;
            if (!string.IsNullOrEmpty(_saveKey))
            {
                var strKeyState = Game1.GameManager.SaveManager.GetString(_saveKey);
                if (strKeyState != null && strKeyState == "1")
                {
                    IsDead = true;
                    return;
                }
            }

            for (var i = 0; i < 5; i++)
                _sprites[i] = new CSprite(_spriteIds[i], new CPosition(posX, posY - i * 16, 0), Vector2.Zero);

            _basePosition = new Vector2(posX, posY);

            EntityPosition = new CPosition(posX + 8, posY + 14, 0);
            EntitySize = new Rectangle(-8, -78, 16, 80);

            var stateIdle = new AiState();
            var stateShaking = new AiState { Init = InitShake };
            stateShaking.Trigger.Add(new AiTriggerCountdown(1300, null, () => _aiComponent.ChangeState("falling")));
            var stateFalling = new AiState(UpdateFalling);

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("shaking", stateShaking);
            _aiComponent.States.Add("falling", stateFalling);
            _aiComponent.ChangeState("idle");

            var collisionBox = new CBox(posX + 1, posY + 4, 0, 14, 12, 16);
            var hitBox = new CBox(posX, posY - 8, 0, 16, 24, 16);

            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(HittableComponent.Index, new HittableComponent(hitBox, OnHit));
            AddComponent(CollisionComponent.Index, _collisionComponent = new BoxCollisionComponent(collisionBox, Values.CollisionTypes.Normal));
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerPlayer, EntityPosition));
        }

        private void InitShake()
        {
            Game1.GameManager.ShakeScreen(1300 + 5 * 500, 3, 2, 5, 2.5f);

            Game1.GameManager.PlaySoundEffect("D360-11-0B");
            Game1.GameManager.PlaySoundEffect("D378-37-25");
        }

        private void UpdateFalling()
        {
            _fallCount += Game1.DeltaTime;
            _particleCounter += Game1.DeltaTime;
            _stoneCounter -= Game1.DeltaTime;

            if (_fallCount > 500)
            {
                _fallCount -= 500;
                _pillarIndex--;

                // remove the pillar?
                if (_pillarIndex <= 0)
                {
                    // we set the key here so that the collapse sequence will be shown after the pillar is gone
                    // this can lead to situations where the player changes the map while the pillar is collapsing and the state will not get saved
                    Game1.GameManager.SaveManager.SetString(_saveKey, "1");
                    Map.Objects.DeleteObjects.Add(this);
                }
            }

            if (_stoneCounter < 0)
            {
                _stoneCounter = 850;

                var stonePosX = (int)_basePosition.X + Game1.RandomNumber.Next(0, 48) - 24;
                var stonePosY = (int)_basePosition.Y + Game1.RandomNumber.Next(0, 48) - 24;
                Map.Objects.SpawnObject(new ObjSmallStone(Map, stonePosX, stonePosY, 64, new Vector3(0, 0, 0), true));
            }

            // spawn particles
            if (_particleCounter > 100)
            {
                _particleCounter -= 100;

                var positionX = (int)_basePosition.X + Game1.RandomNumber.Next(0, 28) - 14;
                var positionY = (int)_basePosition.Y + Game1.RandomNumber.Next(0, 8) - 2;
                Map.Objects.SpawnObject(new ObjAnimator(Map, positionX, positionY, Values.LayerTop, "Particles/spawn", "run", true));
            }
        }

        private Values.HitCollision OnHit(GameObject originObject, Vector2 direction, HitType type, int damage, bool pieceOfPower)
        {
            if (originObject.GetType() == typeof(ObjBall) && _aiComponent.CurrentStateId == "idle")
            {
                _aiComponent.ChangeState("shaking");
                return Values.HitCollision.Blocking;
            }

            return Values.HitCollision.None;
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            for (var i = 0; i < _pillarIndex; i++)
            {
                _sprites[5 - _pillarIndex + i].Position.Set(new Vector2(_basePosition.X, _basePosition.Y - i * 16));
                _sprites[5 - _pillarIndex + i].Draw(spriteBatch);
            }
        }
    }
}
