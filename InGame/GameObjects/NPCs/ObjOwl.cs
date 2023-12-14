using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;
using System;

namespace ProjectZ.InGame.GameObjects.NPCs
{
    internal class ObjOwl : GameObject
    {
        private readonly CSprite _sprite;
        private readonly BodyDrawComponent _drawComponent;
        private readonly BodyDrawShadowComponent _drawShadowComponent;
        private readonly AiComponent _aiComponent;

        private Animator _animator;
        private float _landingSpeed = 0.125f;

        private CPosition _owlPosition;

        // https://cubic-bezier.com/#.17,.67,.83,.67
        private readonly CubicBezier _landingCurve = new CubicBezier(100, new Vector2(0.35f, 1f), new Vector2(0.8f, 1));
        private readonly CubicBezier _leavingCurve = new CubicBezier(100, new Vector2(0.25f, 0.04f), new Vector2(0.35f, 0.11f));

        private Vector3 _startPosition;
        private Vector3 _landPosition;
        private Vector3 _leavePosition;

        private readonly string _strKey;
        private readonly string _keyCondition;

        private float _airCount;
        private float _flySoundCount;
        private int _enterTime = 2000;

        private int originX;
        private int originY;

        private bool _isAlive;
        private bool _wasTriggered;
        private bool _triggerCollided;

        private bool _hoverMode;
        private double _hoverCounter;

        private bool _sitMode;
        private int _mode; // 0: leave after talking; 1: stay after talking; 2: spawn from the top

        public ObjOwl() : base("owl") { }

        public ObjOwl(Map.Map map, int posX, int posY, string keyCondition, Rectangle triggerRectangle, bool hoveMode, string strKey, int mode) : base(map)
        {
            _strKey = strKey;
            _keyCondition = keyCondition;
            _hoverMode = hoveMode;
            _mode = mode;

            // always gets updated
            //EntityPosition = new CPosition(posX, posY, 0);
            //EntitySize = new Rectangle(-16, -32, 48, 64);

            originX = posX + 8;
            originY = posY + 16;

            _owlPosition = new CPosition(_startPosition.X, _startPosition.Y, _startPosition.Z);

            var body = new BodyComponent(_owlPosition, -6, -8, 12, 8, 8)
            {
                IgnoresZ = true
            };

            _animator = AnimatorSaveLoad.LoadAnimator("NPCs/owl");
            _sprite = new CSprite(_owlPosition);

            var animationComponent = new AnimationComponent(_animator, _sprite, Vector2.Zero);

            //var stateDebug = new AiState();
            var stateWait = new AiState(UpdateWait);
            var stateEnter = new AiState(UpdateEnter) { Init = InitEnter };
            var stateTalk = new AiState(UpdateTalk) { Init = InitTalking };
            var stateTalked = new AiState() { Init = InitTalked };
            var stateLeave = new AiState(UpdateLeave) { Init = InitLeave };
            var stateSit = new AiState(UpdateSit) { };

            _aiComponent = new AiComponent();
            //_aiComponent.States.Add("debug", stateDebug);
            _aiComponent.States.Add("wait", stateWait);
            _aiComponent.States.Add("enter", stateEnter);
            _aiComponent.States.Add("talk", stateTalk);
            _aiComponent.States.Add("talked", stateTalked);
            _aiComponent.States.Add("leave", stateLeave);
            _aiComponent.States.Add("sit", stateSit);
            _aiComponent.ChangeState("wait");

            AddComponent(BodyComponent.Index, body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, _drawComponent = new BodyDrawComponent(body, _sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, _drawShadowComponent = new BodyDrawShadowComponent(body, _sprite) { OffsetY = 0 });
            var enterRectangle = new Rectangle(posX + 8 + triggerRectangle.X, posY + 8 + triggerRectangle.Y, triggerRectangle.Width, triggerRectangle.Height);
            AddComponent(ObjectCollisionComponent.Index, new ObjectCollisionComponent(enterRectangle, OnCollision));

            _sprite.Color = Color.Transparent;
            _drawComponent.IsActive = false;
            _drawShadowComponent.IsActive = false;

            // sitting infront of the egg
            if (mode == 2)
            {
                if (Game1.GameManager.SaveManager.GetString(_keyCondition, "0") == "1")
                {
                    IsDead = true;
                    return;
                }

                _isAlive = true;
                _sitMode = true;
            }
            else
            {
                AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(KeyChanged));

                // add key change listener to activate/deactivate the owl
                if (!string.IsNullOrEmpty(_keyCondition))
                    KeyChanged();
            }
        }

        private void KeyChanged()
        {
            var value = Game1.GameManager.SaveManager.GetString(_strKey, "0");
            _isAlive = value == _keyCondition;
        }

        private void OnCollision(GameObject gameObject)
        {
            if (!_wasTriggered && _isAlive)
                _triggerCollided = true;
        }

        private void UpdateSit()
        {
            var playerDirection = MapManager.ObjLink.EntityPosition.Position - _owlPosition.Position;
            if (playerDirection.Length() < 64 && MapManager.ObjLink.IsGrounded())
            {
                // start playing owl music
                Game1.GameManager.SetMusic(33, 2);

                _leavePosition = new Vector3(originX, originY - 90, 90);
                _aiComponent.ChangeState("talk");
            }
        }

        private void UpdateWait()
        {
            if (_triggerCollided && !MapManager.ObjLink.IsRailJumping() && !MapManager.ObjLink.IsTransitioning)
                _aiComponent.ChangeState("enter");
        }

        private void InitEnter()
        {
            if (_wasTriggered)
                return;

            if (_hoverMode)
            {
                _animator.Play("hover");

                _startPosition = new Vector3(originX, originY - 64, 64);
                _landPosition = new Vector3(originX, originY, 0);
                _leavePosition = new Vector3(originX, originY - 64, 64);
            }
            else if (_mode == 2)
            {
                _animator.Play("fly");

                _startPosition = new Vector3(originX, originY - 64, 64);
                _landPosition = new Vector3(originX, originY, 0);
                _leavePosition = new Vector3(originX, originY - 64, 64);
            }
            else
            {
                _animator.Play("fly");

                _startPosition = new Vector3(originX - 64, originY - 64, 64);
                _landPosition = new Vector3(originX, originY, 0);
                _leavePosition = new Vector3(originX + 64, originY - 64, 64);
            }

            _airCount = 0;
            _flySoundCount = 0;

            // start playing owl music; not in the final scene
            if (Game1.GameManager.GetCurrentMusic() != 88)
                Game1.GameManager.SetMusic(33, 2);

            MapManager.ObjLink.FreezePlayer();
            _wasTriggered = true;

            _drawComponent.IsActive = true;

            if (!_hoverMode)
                _drawShadowComponent.IsActive = true;
        }

        private void UpdateEnter()
        {
            MapManager.ObjLink.FreezePlayer();

            _airCount += Game1.DeltaTime;

            _flySoundCount -= Game1.DeltaTime;
            if (_flySoundCount < 0)
            {
                _flySoundCount = 500;
                Game1.GameManager.PlaySoundEffect("D378-45-2D", false);
            }

            if (_airCount > _enterTime)
                _airCount = _enterTime;

            var time = _airCount / _enterTime;
            if (_airCount < _enterTime)
            {
                var currentPosition = Vector3.Lerp(_startPosition, _landPosition, _landingCurve.EvaluateX(time));
                _owlPosition.Set(currentPosition);

                if (!_hoverMode && time > 0.9)
                    _animator.Play("idle");
            }
            else
            {
                if (!_hoverMode)
                    _animator.Play("idle");

                _owlPosition.Set(_landPosition);

                _aiComponent.ChangeState("talk");
            }

            // player looks at the owl
            if (_airCount > _enterTime - 1000)
            {
                var playerDir = _owlPosition.Position - MapManager.ObjLink.EntityPosition.Position;
                MapManager.ObjLink.Direction = AnimationHelper.GetDirection(playerDir);
            }

            // fade in
            if (time <= 0.1f)
                _sprite.Color = Color.White * (time / 0.1f);
            else
                _sprite.Color = Color.White;
        }

        private void InitTalking()
        {
            _hoverCounter = 0;

            if (_sitMode)
                Game1.GameManager.StartDialogPath(_keyCondition);
            else
                Game1.GameManager.StartDialogPath(_strKey);

            Game1.GameManager.InGameOverlay.TextboxOverlay.OwlMode = true;
        }

        private void UpdateTalk()
        {
            MapManager.ObjLink.FreezePlayer();

            // hover up/down
            if (_hoverMode)
            {
                _hoverCounter += Game1.DeltaTime;

                var hoverPosition = _landPosition;
                hoverPosition.Y -= MathF.Sin((GetHoverState((float)_hoverCounter / 1000f, 0.0f) - 0.0f) * MathF.PI * 2 - MathF.PI / 2) * 3 + 3;
                _owlPosition.Set(hoverPosition);
            }

            // leave of just sit there
            if (!Game1.GameManager.InGameOverlay.TextboxOverlay.IsOpen)
                _aiComponent.ChangeState((_mode == 0 || _mode == 2) ? "leave" : "talked");
        }

        private float GetHoverState(float _hoverCounter, float startPercentage)
        {
            // gradient up to 0.5f
            var gradient = 0.9f;
            // time needed to reach 0.5f
            var timeX = 0.5f * gradient;

            _hoverCounter += startPercentage * gradient;
            _hoverCounter %= 1;

            if (_hoverCounter < timeX)
                return _hoverCounter / gradient;
            else
                return timeX / gradient + (1 - timeX / gradient) * (_hoverCounter - timeX) / (1 - timeX);
        }

        private void InitLeave()
        {
            _airCount = 0;
            _landPosition.X = _owlPosition.X;
            _landPosition.Y = _owlPosition.Y;

            if (!_hoverMode)
            {
                _animator.Play("fly");
                _animator.SpeedMultiplier = 1.5f;
            }

            // stop playing music
            Game1.GameManager.SetMusic(-1, 2);
        }

        private void UpdateLeave()
        {
            _flySoundCount -= Game1.DeltaTime;
            if (_flySoundCount < 0)
            {
                _flySoundCount = 175;
                Game1.GameManager.PlaySoundEffect("D378-05-05");
            }

            _airCount += Game1.DeltaTime;

            if (_airCount > _enterTime)
                _airCount = _enterTime;

            var time = _airCount / (float)_enterTime;
            if (_airCount < _enterTime)
            {
                var currentPosition = Vector3.Lerp(_landPosition, _leavePosition, _leavingCurve.EvaluateX(time));
                _owlPosition.Set(currentPosition);
            }
            else
            {
                Map.Objects.DeleteObjects.Add(this);
            }

            // fade out
            if (time >= 0.9f)
                _sprite.Color = Color.White * ((1 - time) / 0.1f);
            else
                _sprite.Color = Color.White;
        }

        private void InitTalked()
        {
            // stop playing music
            Game1.GameManager.SetMusic(-1, 2);
        }
    }
}