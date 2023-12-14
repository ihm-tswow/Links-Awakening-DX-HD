using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.NPCs;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    class ObjBridge : GameObject
    {
        private readonly Rectangle _sourceRectangle;

        private readonly ObjMonkeyWorker[] _workers = new ObjMonkeyWorker[7];

        private float _counter;
        private const float Segment1Time = 5000;
        private const float Segment2Time = Segment3Time + 1000;
        private const float Segment3Time = Segment1Time + 1000;
        private const float FinishedTime = Segment3Time + 1500;

        private bool _isRunning;
        private bool _spawnedMonkeys;

        public ObjBridge() : base("bridge") { }

        public ObjBridge(Map.Map map, int posX, int posY) : base(map)
        {
            _sourceRectangle = Resources.SourceRectangle("bridge");

            EntityPosition = new CPosition(posX, posY + 48, 0);
            EntitySize = new Rectangle(0, -48, 16, 48);

            var value = Game1.GameManager.SaveManager.GetString("monkeyBusiness");
            var finished = value == "3";

            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerBottom, EntityPosition));

            if (finished)
            {
                // make sure that the stick is there if it was not already collected
                SpawnStick();
                _counter = FinishedTime;
                return;
            }

            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(KeyChanged));

            // create the workers
            for (var i = 0; i < _workers.Length; i++)
            {
                var randomDir = (Game1.RandomNumber.Next(0, 50) / 50.0f) * MathF.PI * 2;
                var startPosition = new Vector2(EntityPosition.X + 8, EntityPosition.Y - 24) +
                                    new Vector2(MathF.Sin(randomDir), MathF.Cos(randomDir)) * 150;
                var workPosition = new Vector2(EntityPosition.X + 8, EntityPosition.Y - 36 * (i / 6.0f));
                _workers[i] = new ObjMonkeyWorker(map, startPosition, workPosition, startPosition);
            }
        }

        private void KeyChanged()
        {
            var value = Game1.GameManager.SaveManager.GetString("monkeyBusiness");

            if (!_spawnedMonkeys && value == "2")
            {
                _isRunning = true;
                _spawnedMonkeys = true;

                for (var i = 0; i < _workers.Length; i++)
                    Map.Objects.SpawnObject(_workers[i]);
            }
        }

        private void SpawnStick()
        {
            // spawn the stick
            var objStick = new ObjItem(Map, (int)EntityPosition.X, (int)EntityPosition.Y - 32, "", "ow_trade4", "trade4", null);
            if (!objStick.IsDead)
                Map.Objects.SpawnObject(objStick);
        }

        private void Update()
        {
            if (!_isRunning)
                return;

            _counter += Game1.DeltaTime;

            if (_counter > FinishedTime)
            {
                _isRunning = false;
                Game1.GameManager.StartDialogPath("castle_monkey_business");

                SpawnStick();

                for (var i = 0; i < _workers.Length; i++)
                {
                    _workers[i].ToLeave();
                }
            }
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            // draw the segments of the bridge
            if (_counter > Segment1Time)
                spriteBatch.Draw(Resources.SprObjects, new Vector2(EntityPosition.X, EntityPosition.Y - 48), _sourceRectangle, Color.White);
            if (_counter > Segment2Time)
                spriteBatch.Draw(Resources.SprObjects, new Vector2(EntityPosition.X, EntityPosition.Y - 32), _sourceRectangle, Color.White);
            if (_counter > Segment3Time)
                spriteBatch.Draw(Resources.SprObjects, new Vector2(EntityPosition.X, EntityPosition.Y - 16), _sourceRectangle, Color.White);
        }
    }
}
