using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjPowder : GameObject
    {
        private Rectangle _sourceRectangle = new Rectangle(1, 145, 4, 4);

        private Vector3[] _points = new Vector3[3];
        private Vector3[] _velocity = new Vector3[3];
        private float[] _live = new float[3];

        private float _gravity = -0.05f;
        private bool _damage;

        public ObjPowder(Map.Map map, float posX, float posY, float posZ, bool playerPowder) : base(map)
        {
            EntityPosition = new CPosition(posX, posY, posZ);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            _points[0] = new Vector3(posX, posY, posZ + 7);
            _points[1] = new Vector3(posX - 1, posY, posZ + 6);
            _points[2] = new Vector3(posX + 1, posY, posZ + 6);

            _velocity[0] = new Vector3(0, 0, 0);
            _velocity[1] = new Vector3(-Game1.RandomNumber.Next(50, 150) / 1000f, 0, 0);
            _velocity[2] = new Vector3(Game1.RandomNumber.Next(50, 150) / 1000f, 0, 0);

            _live[0] = 1;
            _live[1] = 1;
            _live[2] = 1;

            // play sound effect
            if (playerPowder)
                Game1.GameManager.PlaySoundEffect("D360-05-05", true);
            else
                _damage = true;

            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerPlayer, EntityPosition));
            AddComponent(LightDrawComponent.Index, new LightDrawComponent(DrawLight));
        }

        public void Update()
        {
            var finishedFalling = true;

            for (var i = 0; i < _points.Length; i++)
            {
                if (_points[i].Z <= 3 && !_damage)
                {
                    // deals damage
                    _damage = true;
                    Map.Objects.Hit(this, new Vector2(EntityPosition.X, EntityPosition.Y),
                        new Box(EntityPosition.X - 3, EntityPosition.Y - 8, 0, 6, 10, 8),
                        HitType.MagicPowder, 2, false, false);
                }

                // finished falling
                if (_points[i].Z <= 0)
                {
                    _points[i].Z = 0;
                    _live[i] -= 0.1f * Game1.TimeMultiplier;
                }
                else
                {
                    _points[i] += _velocity[i] * Game1.TimeMultiplier;
                    _velocity[i].Z += _gravity * Game1.TimeMultiplier;
                }

                if (_live[i] > 0)
                    finishedFalling = false;
                else
                    _live[i] = 0;
            }

            // remove object from the map
            if (finishedFalling)
            {
                Map.Objects.DeleteObjects.Add(this);
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            for (var i = 0; i < _points.Length; i++)
            {
                spriteBatch.Draw(Resources.SprItem, new Vector2(
                    _points[i].X - _sourceRectangle.Width / 2,
                    _points[i].Y - _sourceRectangle.Height - _points[i].Z), _sourceRectangle, Color.White * _live[i]);
            }
        }

        public void DrawLight(SpriteBatch spriteBatch)
        {
            // draw shadow
            for (var i = 0; i < _points.Length; i++)
            {
                DrawHelper.DrawLight(spriteBatch, new Rectangle(
                        (int)_points[i].X - 12, (int)(_points[i].Y - _points[i].Z) - 2 - 12, 24, 24), new Color(255, 220, 220) * 0.125f * _live[i]);
            }
        }
    }
}