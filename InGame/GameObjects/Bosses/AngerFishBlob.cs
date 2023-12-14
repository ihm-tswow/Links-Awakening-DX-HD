using System;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Bosses
{
    internal class AngerFishBlob : GameObject
    {
        private readonly BodyComponent _body;
        private float _counter;
        private bool _init;

        public AngerFishBlob(Map.Map map, int posX, int posY) : base(map)
        {
            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(-2, -2, 5, 5);

            _body = new BodyComponent(EntityPosition, -2, 1, 5, 2, 8)
            {
                CollisionTypes = Values.CollisionTypes.None,
                DeepWaterOffset = -6,
                IgnoresZ = true,
                SplashEffect = false
            };
            _body.VelocityTarget.Y = -0.65f;

            var sprite = new CSprite(Resources.SprNightmares, EntityPosition, new Rectangle(37, 101, 5, 5), new Vector2(-2, -2));

            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(sprite, Values.LayerPlayer));
        }

        private void Update()
        {
            _counter += Game1.DeltaTime;
            _body.VelocityTarget.X = MathF.Sin(_counter / 75f) * 0.15f;

            // despawn the blob
            if (_init && !_body.CurrentFieldState.HasFlag(MapStates.FieldStates.DeepWater))
                Map.Objects.DeleteObjects.Add(this);

            _init = true;
        }
    }
}