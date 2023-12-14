using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjLeaf : GameObject
    {
        private readonly AiComponent _aiComponent;
        private readonly CSprite _sprite;
        private readonly DrawShadowSpriteComponent _shadowSprite;

        private Vector2 _velocity;

        private double _objTimer;

        private readonly float _direction;
        private readonly float _fallSpeed = 0.075f;

        private readonly int _despawnTime = 250;

        public ObjLeaf(Map.Map map, int posX, int posY, float posZ, Vector2 velocity) : base(map)
        {
            _velocity = velocity;

            _objTimer = (int)(Game1.RandomNumber.Next(0, 250) * velocity.X);
            _direction = velocity.X / 2f;

            EntityPosition = new CPosition(posX, posY, posZ);
            EntitySize = new Rectangle(-2, -9, 12, 10);

            _aiComponent = new AiComponent();

            var stateFalling = new AiState(StateFalling);

            var stateLieing = new AiState(StateLie);
            stateLieing.Trigger.Add(new AiTriggerRandomTime(() => _aiComponent.ChangeState("fading"), 250, 750));

            var stateDespawning = new AiState(StateFading);
            stateDespawning.Trigger.Add(new AiTriggerCountdown(_despawnTime, FadeTick, FadeEnd));

            _aiComponent.States.Add("falling", stateFalling);
            _aiComponent.States.Add("lie", stateLieing);
            _aiComponent.States.Add("fading", stateDespawning);

            _aiComponent.ChangeState("falling");


            _shadowSprite = new DrawShadowSpriteComponent(Resources.SprShadow,
                EntityPosition, new Rectangle(0, 0, 65, 66), new Vector2(-1, -3), 1.0f, 0.0f);
            _shadowSprite.Width = 10;
            _shadowSprite.Height = 4;

            var sourceRectangle = Resources.SourceRectangle("leaf");
            _sprite = new CSprite(Resources.SprObjects, EntityPosition, sourceRectangle, new Vector2(0, -6));
            _sprite.Color = Color.White * 0.8f;

            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(_sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, _shadowSprite);
        }

        public void StateFalling()
        {
            _objTimer += Game1.DeltaTime;

            // fall down
            EntityPosition.Z -= Game1.TimeMultiplier * _fallSpeed;

            // fall state
            _sprite.DrawOffset.X = (float)Math.Sin(_objTimer / 150f * _direction) * 3;
            _shadowSprite.DrawOffset.X = _sprite.DrawOffset.X - 1;
            EntityPosition.Move(_velocity);

            // flip the leaf depending on the direction it is moving
            if (Math.Cos(_objTimer / 150f) + _velocity.X < 0)
                _sprite.SpriteEffect = SpriteEffects.None;
            else
                _sprite.SpriteEffect = SpriteEffects.FlipHorizontally;

            _velocity *= (float)Math.Pow(0.85, Game1.TimeMultiplier);

            if (EntityPosition.Z <= 0)
            {
                EntityPosition.Z = 0;
                _aiComponent.ChangeState("lie");
            }
        }

        public void StateLie() { }

        public void StateFading() { }

        // fade away
        public void FadeTick(double currentState)
        {
            _sprite.Color = Color.White * (float)(currentState / _despawnTime) * 0.9f;
            _shadowSprite.Color = _sprite.Color;
        }

        // remove the leaf
        public void FadeEnd()
        {
            Map.Objects.DeleteObjects.Add(this);
        }
    }
}