using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.Things;
using Microsoft.Xna.Framework.Graphics;
using System;
using ProjectZ.InGame.GameObjects.Base.Components.AI;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjCockParticle : GameObject
    {
        private readonly CSprite[] _sprites = new CSprite[4];
        private readonly CPosition[] _positions = new CPosition[4];

        private readonly Color _color0 = new Color(57, 0, 189);
        private readonly Color _color1 = new Color(255, 181, 49);

        // center where the particle is circling around
        private readonly Vector2[] _circleCenters = new Vector2[7];
        // the amount we circle a specifiy center
        private readonly float[] _circleRadians = new float[7];

        private readonly Vector2 _endPosition;

        // 800ms to circle the circle with a radius of 10px = ~1px per frame
        private const float CircleSpeed = MathF.PI / 800;
        private const int Radius = 10;
        private const int OffsetEnd = 12;
        // 0/1 clockwise/reversed clockwise
        private const int Direction = 1;

        private float _moveCounter;
        private float _startRadiants;

        private bool _isRunning = true;

        public ObjCockParticle(Map.Map map, Vector2 endPosition) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(endPosition.X, endPosition.Y, 0);
            EntitySize = new Rectangle(-8, -8, 16, 16);

            _circleRadians[0] = 2.1f * (MathF.PI / 2);
            _circleRadians[1] = 2.1f * (MathF.PI / 2);
            _circleRadians[2] = 3.4f * (MathF.PI / 2);
            _circleRadians[3] = 2.2f * (MathF.PI / 2);
            _circleRadians[4] = 1.8f * (MathF.PI / 2);
            _circleRadians[5] = 2.9f * (MathF.PI / 2);
            _circleRadians[6] = 1 * (MathF.PI / 2);

            var startOffset = new Vector2(-Radius, -OffsetEnd);
            _circleCenters[_circleCenters.Length - 1] = endPosition + startOffset;
            // calculate the center positions
            var radiantSum = MathF.PI;
            for (int i = _circleCenters.Length - 2; i >= 0; i--)
            {
                var radiant = radiantSum - _circleRadians[i + 1];
                if (i % 2 == Direction)
                    radiant = radiantSum + _circleRadians[i + 1];

                if (i % 2 == Direction)
                    radiantSum += _circleRadians[i + 1] + MathF.PI;
                else
                    radiantSum -= _circleRadians[i + 1] + MathF.PI;

                var offset = new Vector2(-MathF.Cos(radiant), MathF.Sin(radiant)) * Radius * 2;
                _circleCenters[i] = _circleCenters[i + 1] + offset;
            }

            _startRadiants = MathF.PI;
            for (int i = 0; i < _circleRadians.Length; i++)
            {
                if (i % 2 == Direction)
                    _startRadiants -= _circleRadians[i];
                else
                    _startRadiants += _circleRadians[i];
            }
            if (_circleRadians.Length % 2 == 0)
                _startRadiants += MathF.PI;

            _sprites[0] = new CSprite("cock_particle_0", _positions[0] = new CPosition(0, 0, 0), new Vector2(-4, -4));
            _sprites[1] = new CSprite("cock_particle_1", _positions[1] = new CPosition(0, 0, 0), new Vector2(-3, -3));
            _sprites[2] = new CSprite("cock_particle_2", _positions[2] = new CPosition(0, 0, 0), new Vector2(-2, -2));
            _sprites[3] = new CSprite("cock_particle_2", _positions[3] = new CPosition(0, 0, 0), new Vector2(-2, -2));

            _endPosition = endPosition;

            var damageCollider = new CBox(EntityPosition, -3, -3, 0, 6, 6, 8);
            var hittableBox = new CBox(EntityPosition, -4, -4, 0, 8, 8, 8);

            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerTop, EntityPosition));
            AddComponent(LightDrawComponent.Index, new LightDrawComponent(DrawLight));
        }

        private void Update()
        {
            _moveCounter += Game1.DeltaTime;

            var position0 = GetPosition(_moveCounter);
            _positions[0].Set(position0);
            var position1 = GetPosition(_moveCounter - 100);
            _positions[1].Set(position1);
            var position2 = GetPosition(_moveCounter - 200);
            _positions[2].Set(position2);
            var position3 = GetPosition(_moveCounter - 300);
            _positions[3].Set(position3);

            if (position0 == Vector2.Zero)
                _isRunning = false;
        }

        private Vector2 GetPosition(float time)
        {
            var circleTime = time * CircleSpeed;
            var radiantSum = _startRadiants;
            for (int i = 0; i < _circleRadians.Length; i++)
            {
                if (circleTime < _circleRadians[i])
                {
                    var radiant = radiantSum + circleTime;
                    // move arount the circle in clockwise or reversed clockwise
                    if (i % 2 != Direction)
                        radiant = radiantSum - circleTime;

                    return _circleCenters[i] + new Vector2(-MathF.Cos(radiant), MathF.Sin(radiant)) * Radius;
                }
                else
                {
                    circleTime -= _circleRadians[i];

                    if (i % 2 == Direction)
                        radiantSum += _circleRadians[i] + MathF.PI;
                    else
                        radiantSum -= _circleRadians[i] + MathF.PI;
                }
            }

            var startPosition = _circleCenters[_circleCenters.Length - 1] - new Vector2(-MathF.Cos(radiantSum), MathF.Sin(radiantSum)) * Radius;
            var targetPosition = _endPosition;
            var percentage = (circleTime / CircleSpeed) / 1000 * (60 / 12);
            var newPosition = Vector2.Lerp(startPosition, targetPosition, percentage);

            if (percentage < 1)
                return newPosition;
            else
                return Vector2.Zero;
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            // debug points
            //for (int i = 0; i < _circleCenters.Length; i++)
            //    spriteBatch.Draw(Resources.SprWhite, _circleCenters[i] - new Vector2(1, 1), new Rectangle(0, 0, 2, 2), Color.Red);

            // blink
            var color = (Game1.TotalGameTime % (AiDamageState.BlinkTime * 2) < AiDamageState.BlinkTime) ? _color0 : _color1;

            for (int i = 0; i < _sprites.Length; i++)
            {
                // zero vector => invisible
                if (_positions[i].Position != Vector2.Zero)
                {
                    _sprites[i].Color = color;
                    _sprites[i].Draw(spriteBatch);
                }
            }
        }

        private void DrawLight(SpriteBatch spriteBatch)
        {
            for (int i = 0; i < _sprites.Length; i++)
            {
                // zero vector => invisible
                if (_positions[i].Position != Vector2.Zero)
                {
                    var sizeMult = i == 0 ? 1 : 0.75f;// (4 - i) / 4f;
                    DrawHelper.DrawLight(spriteBatch,
                        new Rectangle((int)(_positions[i].X - 16 * sizeMult), (int)(_positions[i].Y - 16 * sizeMult),
                        (int)(32 * sizeMult), (int)(32 * sizeMult)), Color.White);
                }
            }
        }

        public bool IsRunning()
        {
            return _isRunning;
        }
    }
}