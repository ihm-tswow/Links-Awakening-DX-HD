using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Bosses
{
    class BossSlimeEelSpawn : GameObject
    {
        private enum SpawnState
        {
            Init,
            Shake,
            FloorBreak,
            FloorGone,
            Wall0,
            ShakeEnd,
            Wall1,
            Wall2,
            Wall3,
        }

        private SpawnState _spawnState;
        private float _spawnCounter;

        private readonly BossSlimeEel _slimeEel;

        private readonly BossSlimeEelTail[] _tailParts = new BossSlimeEelTail[5];

        private readonly AiComponent _aiComponent;

        private readonly CSprite[] _sprite = new CSprite[20];

        private RectangleF _fieldRectangle;
        private Vector2 _centerPosition;

        private float _rotation;
        private readonly float _rotationSpeed;
        private int _rotationDirection = 1;

        private float _shakeSoundCounter;

        private float _tailState;
        private int _tailIndex;

        private bool _tailIsMoving = true;
        private bool _tailComeOut = true;
        private bool _isVisible = false;

        public BossSlimeEelSpawn() : base("slime eel") { }

        public BossSlimeEelSpawn(Map.Map map, int posX, int posY, string saveKey) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            _centerPosition = new Vector2(posX + 8, posY + 8);

            if (!string.IsNullOrWhiteSpace(saveKey) && Game1.GameManager.SaveManager.GetString(saveKey) == "1")
            {
                // respawn the heart if the player died after he killed the boss without collecting the heart
                SpawnHeart();

                SpawnHoles();

                IsDead = true;
                return;
            }
            
            _sprite[0] = new CSprite("eel_floor_broken", new CPosition(posX - 8, posY - 8, 0), Vector2.Zero);
            _sprite[1] = new CSprite("eel_floor_broken", new CPosition(posX + 8, posY - 8, 0), Vector2.Zero);
            _sprite[2] = new CSprite("eel_floor_broken", new CPosition(posX - 8, posY + 8, 0), Vector2.Zero);
            _sprite[3] = new CSprite("eel_floor_broken", new CPosition(posX + 8, posY + 8, 0), Vector2.Zero);

            _sprite[4] = new CSprite("eel_floor", new CPosition(posX - 8, posY - 8, 0), Vector2.Zero);
            _sprite[5] = new CSprite("eel_floor", new CPosition(posX + 8, posY - 8, 0), Vector2.Zero);
            _sprite[6] = new CSprite("eel_floor", new CPosition(posX - 8, posY + 8, 0), Vector2.Zero);
            _sprite[7] = new CSprite("eel_floor", new CPosition(posX + 8, posY + 8, 0), Vector2.Zero);

            _sprite[8] = new CSprite("eel_wall_open", new CPosition(posX - 40, posY - 56, 0), Vector2.Zero);
            _sprite[9] = new CSprite("eel_wall_open", new CPosition(posX + 24, posY - 56, 0), Vector2.Zero);
            _sprite[10] = new CSprite("eel_wall_open", new CPosition(posX - 40, posY + 56, 0), Vector2.Zero) { SpriteEffect = SpriteEffects.FlipVertically };
            _sprite[11] = new CSprite("eel_wall_open", new CPosition(posX + 24, posY + 56, 0), Vector2.Zero) { SpriteEffect = SpriteEffects.FlipVertically };

            _sprite[12] = new CSprite("eel_wall", new CPosition(posX - 40, posY - 72, 0), Vector2.Zero) { SpriteEffect = SpriteEffects.FlipVertically };
            _sprite[13] = new CSprite("eel_wall", new CPosition(posX + 24, posY - 72, 0), Vector2.Zero) { SpriteEffect = SpriteEffects.FlipVertically };
            _sprite[14] = new CSprite("eel_wall", new CPosition(posX - 40, posY + 72, 0), Vector2.Zero);
            _sprite[15] = new CSprite("eel_wall", new CPosition(posX + 24, posY + 72, 0), Vector2.Zero);

            _sprite[16] = new CSprite("eel_wall", new CPosition(posX - 40, posY - 56, 0), Vector2.Zero);
            _sprite[17] = new CSprite("eel_wall", new CPosition(posX + 24, posY - 56, 0), Vector2.Zero);
            _sprite[18] = new CSprite("eel_wall", new CPosition(posX - 40, posY + 56, 0), Vector2.Zero) { SpriteEffect = SpriteEffects.FlipVertically };
            _sprite[19] = new CSprite("eel_wall", new CPosition(posX + 24, posY + 56, 0), Vector2.Zero) { SpriteEffect = SpriteEffects.FlipVertically };

            // moved down so that we draw over the door
            EntityPosition = new CPosition(posX + 8, posY + 64, 0);
            EntitySize = new Rectangle(-80, -120, 160, 128);

            _fieldRectangle = Map.GetField(posX, posY, 16);

            // ~4 sec for turn
            _rotationSpeed = (MathF.PI * 2) / 60 / 4;
            
            for (var i = 0; i < _tailParts.Length; i++)
            {
                var spriteIndex = i < 4 ? 1 : 2;
                if (i == 0)
                    spriteIndex = 0;

                _tailParts[i] = new BossSlimeEelTail(map, _centerPosition, spriteIndex, null);
                map.Objects.SpawnObject(_tailParts[i]);
            }

            _slimeEel = new BossSlimeEel(map, _centerPosition, this, saveKey);
            map.Objects.SpawnObject(_slimeEel);

            var stateHidden = new AiState(UpdateHidden);
            var stateSpawn = new AiState(UpdateSpawn);
            var stateIdle = new AiState(UpdateIdle);
            var stateDespawn = new AiState(UpdateDespawn);

            _aiComponent = new AiComponent();

            _aiComponent.States.Add("hidden", stateHidden);
            _aiComponent.States.Add("spawn", stateSpawn);
            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("despawn", stateDespawn);

            _aiComponent.ChangeState("hidden");

            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerBottom, EntityPosition));
        }

        public void SetTailState(float tailState)
        {
            _tailComeOut = false;
            _tailState = tailState;
        }

        public void ToDespawn()
        {
            _aiComponent.ChangeState("despawn");
        }

        public void ChangeRotation()
        {
            _rotationDirection = -_rotationDirection;
        }

        public void SetTailIsMoving(bool isMoving)
        {
            _tailComeOut = true;
            _tailIsMoving = isMoving;
        }

        private void UpdateHidden()
        {
            // player entered the room?
            if (_fieldRectangle.Contains(MapManager.ObjLink.BodyRectangle))
            {
                Game1.GameManager.StartDialogPath("slime_eel");
                _aiComponent.ChangeState("spawn");
            }
        }

        private void UpdateSpawn()
        {
            _spawnCounter += Game1.DeltaTime;
            var spawnTime = 900;
            var endTime = 5500 + spawnTime * 4;

            if(_spawnState == SpawnState.Shake || _spawnState == SpawnState.FloorBreak)
            {
                // @TODO: sound effect; gets louder over time
                _shakeSoundCounter -= Game1.DeltaTime;
                if (_shakeSoundCounter < 0)
                {
                    _shakeSoundCounter += 150;
                    Game1.GameManager.PlaySoundEffect("D378-61-3E", true);
                }
            }

            if (_spawnState == SpawnState.Init && _spawnCounter > 1000)
            {
                _spawnState = SpawnState.Shake;
                Game1.GameManager.ShakeScreen(endTime, 1, 2, 6.5f, 4.5f);
            }
            else if (_spawnState == SpawnState.Shake && _spawnCounter > 2000)
            {
                _spawnState = SpawnState.FloorBreak;

                for (var i = 0; i < 4; i++)
                    _sprite[4 + i].IsVisible = false;
            }
            else if (_spawnState == SpawnState.FloorBreak && _spawnCounter > 3000)
            {
                _spawnState = SpawnState.FloorGone;

                for (var i = 0; i < 4; i++)
                    _sprite[i].IsVisible = false;
                _isVisible = true;

                SpawnStonesCenter();
                SpawnHoles();
            }
            else if (_spawnState == SpawnState.FloorGone && _spawnCounter > 5500)
            {
                _spawnState = SpawnState.Wall0;
                _sprite[16].IsVisible = false;
                _slimeEel.SpawnAttack(0);

                SpawnStonesWall((int)_centerPosition.X - 32, (int)_centerPosition.Y - 56, 1);
            }
            else if (_spawnState == SpawnState.Wall0 && _spawnCounter > 6000)
            {
                _spawnState = SpawnState.ShakeEnd;

            }
            else if (_spawnState == SpawnState.ShakeEnd && _spawnCounter > 5500 + spawnTime)
            {
                _spawnState = SpawnState.Wall1;
                _sprite[17].IsVisible = false;
                _slimeEel.SpawnAttack(1);

                SpawnStonesWall((int)_centerPosition.X + 32, (int)_centerPosition.Y - 56, 1);
            }
            else if (_spawnState == SpawnState.Wall1 && _spawnCounter > 5500 + spawnTime * 2)
            {
                _spawnState = SpawnState.Wall2;
                _sprite[18].IsVisible = false;
                _slimeEel.SpawnAttack(2);

                SpawnStonesWall((int)_centerPosition.X - 32, (int)_centerPosition.Y + 48, -1);
            }
            else if (_spawnState == SpawnState.Wall2 && _spawnCounter > 5500 + spawnTime * 3)
            {
                _spawnState = SpawnState.Wall3;
                _sprite[19].IsVisible = false;
                _slimeEel.SpawnAttack(3);

                SpawnStonesWall((int)_centerPosition.X + 32, (int)_centerPosition.Y + 48, -1);
            }
            else if (_spawnState == SpawnState.Wall3 && _spawnCounter > endTime)
            {
                _aiComponent.ChangeState("idle");
                _slimeEel.ToSpawned();
            }

            if (_isVisible)
            {
                // move the tail out of the hole
                _tailState += Game1.TimeMultiplier * 0.025f;
                if (_tailState > 1)
                    _tailState = 1;

                // move the tail left/right
                _rotation = -MathF.Sin((_spawnCounter - 3000) / (endTime - 3000) * MathF.PI * 7) * 0.35f;
            }

            UpdateTail();
        }

        private void SpawnStonesCenter()
        {
            SpawnStoneLine((int)_centerPosition.X - 10, (int)_centerPosition.Y - 12, -1);
            SpawnStoneLine((int)_centerPosition.X - 4, (int)_centerPosition.Y - 12, -0.25f);
            SpawnStoneLine((int)_centerPosition.X + 4, (int)_centerPosition.Y - 12, 0.25f);
            SpawnStoneLine((int)_centerPosition.X + 10, (int)_centerPosition.Y - 12, 1f);
        }

        private void SpawnStoneLine(int posX, int posY, float dir)
        {
            var randomOffset0 = Game1.RandomNumber.Next(80, 120) / 100f;
            var randomOffset1 = Game1.RandomNumber.Next(80, 120) / 100f;
            var randomOffset2 = Game1.RandomNumber.Next(80, 120) / 100f;

            var stone0 = new ObjSmallStone(Map, posX, posY, 0, new Vector3(dir * 0.35f, -0.25f, 1.25f) * randomOffset0, true);
            var stone1 = new ObjSmallStone(Map, posX, posY + 6, 0, new Vector3(dir * 0.35f, 0.0f, 1.25f) * randomOffset1, true);
            var stone2 = new ObjSmallStone(Map, posX, posY + 12, 0, new Vector3(dir * 0.35f, 0.25f, 1.25f) * randomOffset2, true);

            Map.Objects.SpawnObject(stone0);
            Map.Objects.SpawnObject(stone1);
            Map.Objects.SpawnObject(stone2);
        }

        private void SpawnStonesWall(int posX, int posY, int dir)
        {
            Game1.GameManager.PlaySoundEffect("D378-12-0C");

            var randomOffset0 = Game1.RandomNumber.Next(90, 110) / 100f;
            var randomOffset1 = Game1.RandomNumber.Next(90, 110) / 100f;
            var randomOffset2 = Game1.RandomNumber.Next(90, 110) / 100f;
            var randomOffset3 = Game1.RandomNumber.Next(90, 110) / 100f;

            var stone0 = new ObjSmallStone(Map, posX - 5, posY + 2 * dir, 0, new Vector3(-0.15f, 0.95f * dir, 0.85f) * randomOffset0, true);
            var stone1 = new ObjSmallStone(Map, posX - 7, posY, 0, new Vector3(-0.45f, 0.75f * dir, 0.85f) * randomOffset1, true);
            var stone2 = new ObjSmallStone(Map, posX + 5, posY + 2 * dir, 0, new Vector3(0.15f, 0.95f * dir, 0.85f) * randomOffset2, true);
            var stone3 = new ObjSmallStone(Map, posX + 7, posY, 0, new Vector3(0.45f, 0.75f * dir, 0.85f) * randomOffset3, true);

            Map.Objects.SpawnObject(stone0);
            Map.Objects.SpawnObject(stone1);
            Map.Objects.SpawnObject(stone2);
            Map.Objects.SpawnObject(stone3);
        }

        private void SpawnHoles()
        {
            for (var i = 0; i < 4; i++)
            {
                var hole = new ObjHole(Map,
                    (int)_centerPosition.X - 16 + (i % 2) * 16,
                    (int)_centerPosition.Y - 16 + (i / 2) * 16, 16, 16, Rectangle.Empty, 0, 0, 0);
                Map.Objects.SpawnObject(hole);
            }
        }

        private void UpdateDespawn()
        {
            _tailState -= Game1.TimeMultiplier * 0.025f;
            if (_tailState <= 0)
            {
                _tailState = 0;
                _isVisible = false;
            }

            UpdateTail();
        }

        private void UpdateIdle()
        {
            if (_tailIsMoving)
                _rotation += Game1.TimeMultiplier * _rotationSpeed * _rotationDirection;

            if (_tailComeOut)
            {
                _tailState += Game1.TimeMultiplier * 0.025f;
                if (_tailState > 1)
                    _tailState = 1;
            }

            UpdateTail();
        }

        private void UpdateTail()
        {
            var tailLength = _tailState * 47;

            var parts = (int)(tailLength / 10) + 1;
            if (!_isVisible)
                parts = 0;

            _tailIndex = 5 - parts;

            for (var i = 0; i < 5 - parts; i++)
                _tailParts[i].SetActive(false);

            var lastPosition = Vector2.Zero;
            for (var i = 0; i < parts; i++)
            {
                var index = 5 - parts + i;
                _tailParts[index].SetActive(true);

                // rotate the tip farther in the rotation direction
                var dist = ((tailLength - (parts - 1 - i) * 10) / 47f);
                var mult = dist * dist;
                var rotation = _rotation - MathF.Sin(MathF.PI + _rotation * 2) * 0.75f * mult;
                var newPosition = new Vector2(MathF.Sin(rotation), -MathF.Cos(rotation)) * (tailLength - (parts - 1 - i) * 10);

                if (i > 0)
                {
                    // make sure that the distance between the parts is 10
                    var direction = newPosition - lastPosition;
                    direction.Normalize();
                    newPosition = lastPosition + direction * 10;
                }

                lastPosition = newPosition;
                _tailParts[index].EntityPosition.Set(_centerPosition + newPosition);
            }
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            for (var i = 0; i < _sprite.Length; i++)
                _sprite[i].Draw(spriteBatch);

            // we cant let the object have its own draw method because we could not set the draw order
            if (_isVisible)
                for (var i = _tailIndex; i < _tailParts.Length; i++)
                    _tailParts[i].Sprite.Draw(spriteBatch);
        }

        private void SpawnHeart()
        {
            // spawn big heart
            Map.Objects.SpawnObject(new ObjItem(Map,
                (int)_centerPosition.X - 8, (int)_centerPosition.Y - 32, "j", "d5_nHeart", "heartMeterFull", null));
        }
    }
}