using System;

namespace ProjectZ.InGame.GameObjects.Base.Components.AI
{
    class AiStunnedState
    {
        private readonly AiComponent _aiComponent;
        private readonly AnimationComponent _animationComponent;

        private readonly int _shakeTime;

        private string _oldState;
        private float _spriteOffsetX;

        public string ReturnState;
        public float ShakeOffset = 2;
        public bool SilentStateChange = true;

        public AiStunnedState(AiComponent aiComponent, AnimationComponent animationComponent, int stunTime, int shakeTime)
        {
            _aiComponent = aiComponent;
            _animationComponent = animationComponent;
            _shakeTime = shakeTime;

            var stateStunned = new AiState();
            stateStunned.Trigger.Add(new AiTriggerCountdown(stunTime, null, () => _aiComponent.ChangeState("shake")));
            var stateShake = new AiState { Init = InitShake };
            stateShake.Trigger.Add(new AiTriggerCountdown(_shakeTime, ShakeTick, ShakeEnd));

            aiComponent.States.Add("stunned", stateStunned);
            aiComponent.States.Add("shake", stateShake);
        }

        public void StartStun()
        {
            // make sure to not save the stunned state to not create an endless stunned loop
            if (_aiComponent.CurrentStateId != "stunned" &&
                _aiComponent.CurrentStateId != "shake")
                _oldState = _aiComponent.CurrentStateId;

            Game1.GameManager.PlaySoundEffect("D360-03-03");

            _aiComponent.ChangeState("stunned");
        }

        public bool IsStunned()
        {
            return _aiComponent.CurrentStateId == "stunned" || _aiComponent.CurrentStateId == "shake";
        }

        private void InitShake()
        {
            _spriteOffsetX = _animationComponent.SpriteOffset.X;
        }

        private void ShakeTick(double counter)
        {
            // 4 frames to go left/right
            _animationComponent.SpriteOffset.X = _spriteOffsetX + ShakeOffset * MathF.Sin(MathF.PI * ((_shakeTime - (float)counter) / 1000 * (60 / 4f)));
            _animationComponent.UpdateSprite();
        }

        private void ShakeEnd()
        {
            _animationComponent.SpriteOffset.X = _spriteOffsetX;

            // change back to the state before the entity got stunned
            _aiComponent.ChangeState(ReturnState != null ? ReturnState : _oldState, SilentStateChange);
        }
    }
}
