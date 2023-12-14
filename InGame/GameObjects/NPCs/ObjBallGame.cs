using System;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.NPCs
{
    internal class ObjBallGame : GameObject
    {
        private readonly Rectangle _shadowSourceRectangle = new Rectangle(0, 0, 65, 66);
        private readonly DrawShadowSpriteComponent _shadowComponent;
        private readonly AiComponent _aiComponent;

        private readonly ObjPersonNew _firstPerson;
        private readonly ObjPersonNew _secondPerson;

        private readonly Vector2 _ballStart;
        private readonly Vector2 _ballEnd;

        private float _throwCount;
        private int _throwTime = 650;
        private int _throwHeight = 12;
        private int _throwDirection = 1;

        public ObjBallGame() : base("green_child") { }

        public ObjBallGame(Map.Map map, int posX, int posY, string spawnCondition) : base(map)
        {
            // check if the entity should get spawned
            if (!string.IsNullOrEmpty(spawnCondition))
            {
                var condition = SaveLoad.SaveCondition.GetConditionNode(spawnCondition);
                if (!condition.Check())
                {
                    IsDead = true;
                    return;
                }
            }

            EntityPosition = new CPosition(posX, posY + 16, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            _firstPerson = new ObjPersonNew(Map, posX, posY, null, "npc_boy_left", "npc_boy_ball_left", null, new Rectangle(0, 0, 14, 10));
            Map.Objects.SpawnObject(_firstPerson);
            _secondPerson = new ObjPersonNew(Map, posX + 48, posY, null, "npc_boy_right", "npc_boy_ball_right", null, new Rectangle(0, 0, 14, 10));
            Map.Objects.SpawnObject(_secondPerson);

            var sourceRectangle = new Rectangle(338, 10, 6, 6);

            _ballStart = new Vector2(posX + 13, posY + 15);
            _ballEnd = new Vector2(posX + 64 - 13, posY + 15);

            var statePreThrow = new AiState();
            statePreThrow.Trigger.Add(new AiTriggerRandomTime(() => _aiComponent.ChangeState("jumping"), 100, 200));
            var stateJump = new AiState(UpdateJump);
            stateJump.Trigger.Add(new AiTriggerRandomTime(ToThrowJump, 500, 750));
            var stateJumpThrow = new AiState(UpdateJumpingThrowing);
            stateJumpThrow.Trigger.Add(new AiTriggerRandomTime(ToThrow, 50, 200));
            var stateThrow = new AiState(UpdateThrow);

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("preThrow", statePreThrow);
            _aiComponent.States.Add("jumping", stateJump);
            _aiComponent.States.Add("throwJump", stateJumpThrow);
            _aiComponent.States.Add("throw", stateThrow);
            _aiComponent.ChangeState("throw");

            AddComponent(AiComponent.Index, _aiComponent);
            var sprite = new CSprite(Resources.SprNpCs, EntityPosition,
                sourceRectangle, new Vector2(-sourceRectangle.Width / 2, -sourceRectangle.Height));
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(sprite, Values.LayerPlayer));

            _shadowComponent = new DrawShadowSpriteComponent(
                Resources.SprShadow, EntityPosition, _shadowSourceRectangle,
                new Vector2(-4, -2), 1.0f, 0.0f);
            _shadowComponent.Width = 8;
            _shadowComponent.Height = 4;
            AddComponent(DrawShadowComponent.Index, _shadowComponent);
        }

        private void UpdateJump()
        {
            if (_throwDirection < 0 && _firstPerson.Body.IsGrounded)
                _firstPerson.Body.Velocity.Z = 1f;

            if (_throwDirection > 0 && _secondPerson.Body.IsGrounded)
                _secondPerson.Body.Velocity.Z = 1f;
        }

        private void UpdateJumpingThrowing()
        {
            UpdateJump();
            UpdateThrow();
        }

        private void UpdateThrow()
        {
            _throwCount += Game1.DeltaTime * _throwDirection;

            if (_throwCount > _throwTime)
            {
                _throwDirection = -_throwDirection;
                _throwCount = _throwTime;
                _aiComponent.ChangeState("preThrow");
            }
            else if (_throwCount < 0)
            {
                _throwDirection = -_throwDirection;
                _throwCount = 0;
                _aiComponent.ChangeState("preThrow");
            }

            var throwState = _throwCount / (float)_throwTime;
            var newPosition = Vector2.Lerp(_ballStart, _ballEnd, throwState);
            EntityPosition.Set(newPosition);
            EntityPosition.Z = 3 + (float)Math.Sin(throwState * Math.PI) * _throwHeight;
            _shadowComponent.Color = Color.White * (1 - (float)Math.Sin(throwState * Math.PI) * 0.5f);
        }

        private void ToThrowJump()
        {
            if (_throwDirection < 0)
            {
                _secondPerson.Animator.Play("throw");
                StartThrow();
            }
            else
            {
                _firstPerson.Animator.Play("throw");
                StartThrow();
            }

            _aiComponent.ChangeState("throwJump");
        }

        private void ToThrow()
        {
            _aiComponent.ChangeState("throw");
        }

        private void StartThrow()
        {
            var random = Game1.RandomNumber.Next(100, 200) / 100f;
            _throwTime = (int)(random * 300);
            _throwHeight = (int)(random * 6);
            _throwCount = _throwCount > 0 ? _throwTime : 0;
        }
    }
}