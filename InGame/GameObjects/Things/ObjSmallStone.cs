using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjSmallStone : GameObject
    {
        private readonly BodyComponent _bodyComponent;
        private readonly BodyDrawShadowComponent _shadowComponent;
        private readonly CSprite _sprite;

        private float _despawnCounter;
        private readonly int _fadeOutTime = 75;
        private readonly int _despawnTime = 150;

        private readonly bool _blueStone;

        public ObjSmallStone(Map.Map map, int posX, int posY, int posZ, Vector3 velocity, bool flipSprite = false, int despawnTime = 0) : base(map)
        {
            EntityPosition = new CPosition(posX, posY + 8, posZ);
            EntitySize = new Rectangle(-4, -24, 8, 24);

            if (despawnTime > 0)
                _despawnTime = despawnTime;
            else
                _despawnTime = Game1.RandomNumber.Next(225, 275);

            _bodyComponent = new BodyComponent(EntityPosition, -4, -8, 8, 8, 8)
            {
                CollisionTypes = Values.CollisionTypes.None,
                Bounciness = 0.75f,
                Gravity = -0.15f,
                Gravity2D = 0.1f,
                Drag = 0.5f,
                DragAir = 1.0f,
                MaxJumpHeight = 8,
                IsGrounded = false,     // this is needed for the MaxJumpHeight to work
                Velocity = velocity,
                MoveCollision = OnCollision
            };

            _blueStone = posZ > 32;

            var sourceRectangle = Resources.SourceRectangle(_blueStone ? "stone_particle_1" : "stone_particle");

            _sprite = new CSprite(Resources.SprObjects, EntityPosition, sourceRectangle, new Vector2(-4, -8));
            _sprite.Color = Color.White;
            _sprite.SpriteEffect = flipSprite ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            AddComponent(BodyComponent.Index, _bodyComponent);
            if (_blueStone)
                AddComponent(UpdateComponent.Index, new UpdateComponent(UpdateBounceDespawn));
            else
                AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(_sprite, _blueStone ? Values.LayerPlayer : Values.LayerTop));
            AddComponent(DrawShadowComponent.Index, _shadowComponent = new BodyDrawShadowComponent(_bodyComponent, _sprite));
        }

        private void OnCollision(Values.BodyCollision collision)
        {
            // jump in a random direction
            if (_blueStone && (collision & Values.BodyCollision.Floor) != 0)
            {
                _bodyComponent.Velocity.X = Game1.RandomNumber.Next(0, 11) / 5f - 1.0f;
                _bodyComponent.Velocity.Y = Game1.RandomNumber.Next(0, 11) / 5f - 1.0f;
                if (_bodyComponent.Velocity.Z > 2.0f)
                    _bodyComponent.Velocity.Z = 2.0f;
            }

            // reflect of a wall
            if ((collision & Values.BodyCollision.Horizontal) != 0)
                _bodyComponent.Velocity.X = -_bodyComponent.Velocity.X * 0.25f;
            if ((collision & Values.BodyCollision.Vertical) != 0)
                _bodyComponent.Velocity.Y = -_bodyComponent.Velocity.Y * 0.25f;
        }

        private void UpdateBounceDespawn()
        {
            if (_bodyComponent.Velocity.Z == 0 && _bodyComponent.IsGrounded)
            {
                Map.Objects.DeleteObjects.Add(this);
            }
        }

        private void Update()
        {
            _despawnCounter += Game1.DeltaTime;

            var transparency = (1 - (_despawnCounter - _despawnTime) / _fadeOutTime);
            _sprite.Color = Color.White * transparency;
            _shadowComponent.Transparency = transparency;

            // delete the object
            if (_despawnCounter >= _despawnTime + _fadeOutTime)
                Map.Objects.DeleteObjects.Add(this);
        }
    }
}