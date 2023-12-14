using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Things;
using System;

namespace ProjectZ.InGame.GameObjects.Dungeon
{
    internal class ObjWaterCurrentTester : GameObject
    {
        private BodyComponent _body;

        private double _spawnTime;

        public ObjWaterCurrentTester(Map.Map map, int posX, int posY) : base(map)
        {
            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(-2, -2, 4, 4);

            // this is the same size as the player so that it can not get thrown into the wall
            _body = new BodyComponent(EntityPosition, -2, -2, 4, 4, 14)
            {
                CollisionTypes =
                    Values.CollisionTypes.Normal |
                    Values.CollisionTypes.NPCWall,
                IgnoreHoles = true,
                DragAir = 1.0f,
                Drag = 1.0f,
                Bounciness = 1.0f,
                //MoveCollision = OnMoveCollision
            };

            _spawnTime = Game1.TotalGameTime;

            //var speed = 2.5f;
            //var direction = Game1.RandomNumber.Next(0, 1000) / 10f;
            //_body.VelocityTarget.X = MathF.Sin(direction) * speed;
            //_body.VelocityTarget.Y = MathF.Cos(direction) * speed;

            var cSprite = new CSprite("teleporter_outer", EntityPosition, new Vector2(-2, -2));

            AddComponent(BodyComponent.Index, _body);
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(cSprite, Values.LayerPlayer));
        }

        private void Update()
        {
            if (_spawnTime + 1000 < Game1.TotalGameTime)
            {
                Map.Objects.DeleteObjects.Add(this);
            }
        }

        //private void OnMoveCollision(Values.BodyCollision collision)
        //{
        //    if ((collision & Values.BodyCollision.Horizontal) != 0)
        //        _body.VelocityTarget.X = -_body.VelocityTarget.X;
        //    if ((collision & Values.BodyCollision.Vertical) != 0)
        //        _body.VelocityTarget.Y = -_body.VelocityTarget.Y;
        //}
    }
}