using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.Base;
using ProjectZ.InGame.Controls;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.NPCs;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    class ObjCandyGrabber : GameObject
    {
        private readonly Rectangle _recTop;
        // why is this split into left and right?
        private readonly Rectangle _recGrabberLeft;
        private readonly Rectangle _recGrabberRight;
        private readonly Rectangle _recGrabberLeftClosed;
        private readonly Rectangle _recGrabberRightClosed;
        private readonly Rectangle _recLine;

        //private readonly CPosition EntityPosition;

        private readonly List<GameObject> _collidingObjects = new List<GameObject>();

        private Box _grabberRectangle;
        private Vector2 _vecStart;

        private const float MoveSpeed = 0.25f;
        private const float MoveSpeedGrab = 0.25f;
        private const float MoveSpeedBack = 0.5f;

        private float _blinkCount;
        private float _grabState;
        private float _grab2Count = 5000;
        private float _waitCounter;

        private bool _marinGame;

        private BodyComponent _grabbedBody;

        enum State
        {
            Idle, MoveX, IdleY, WaitY, MoveY, Grab0, Grab1, Grab2, Grab3, BackY, BackX, BackWait
        }

        private State _currentState = State.Idle;

        public ObjCandyGrabber() : base("candy_grabber") { }

        public ObjCandyGrabber(Map.Map map, int posX, int posY) : base(map)
        {
            EntityPosition = new CPosition(posX, posY + 38, 0);
            //EntityPosition = new CPosition(EntityPosition.X, EntityPosition.Y, EntityPosition.Z);

            _recTop = Resources.SourceRectangle("candy_grabber_top");
            _recGrabberLeft = Resources.SourceRectangle("candy_grabber_left");
            _recGrabberRight = Resources.SourceRectangle("candy_grabber_right");
            _recGrabberLeftClosed = Resources.SourceRectangle("candy_grabber_left_closed");
            _recGrabberRightClosed = Resources.SourceRectangle("candy_grabber_right_closed");
            _recLine = Resources.SourceRectangle("candy_grabber_line");

            _vecStart = new Vector2(posX, posY + 38);

            var shadowSourceRectangle = new Rectangle(0, 0, 65, 66);
            var shadowComponent = new DrawShadowSpriteComponent(Resources.SprShadow, EntityPosition, shadowSourceRectangle, new Vector2(0, -5), 1.0f, 0.0f);
            shadowComponent.Width = 16;
            shadowComponent.Height = 8;

            AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(OnKeyChange));
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerPlayer, EntityPosition));
            AddComponent(DrawShadowComponent.Index, shadowComponent);
        }

        private void Update()
        {
            _blinkCount += Game1.DeltaTime;
            if (_blinkCount > 500)
                _blinkCount -= 500;

            _grabberRectangle = new Box(EntityPosition.X + 6, EntityPosition.Y - 3, 0, 4, 4, 2);

            switch (_currentState)
            {
                case State.Idle:
                    if (ControlHandler.ButtonDown(CButtons.B))
                        StartGrabbing();

                    break;
                case State.MoveX:
                    if ((_marinGame || ControlHandler.ButtonDown(CButtons.B)) &&
                        EntityPosition.X < _vecStart.X + 112)
                    {
                        Game1.GameManager.PlaySoundEffect("D378-32-20", false);

                        EntityPosition.Move(new Vector2(MoveSpeed, 0));

                        if (EntityPosition.X > _vecStart.X + 112)
                            EntityPosition.Set(new Vector2(_vecStart.X + 112, EntityPosition.Y));
                    }
                    else
                    {
                        Game1.GameManager.StopSoundEffect("D378-32-20");
                        Game1.GameManager.SaveManager.SetString("trendy_button_1", "0");
                        Game1.GameManager.SaveManager.SetString("trendy_button_2", "1");
                        _currentState = _marinGame ? State.WaitY : State.IdleY;
                    }

                    break;
                case State.IdleY:
                    if (ControlHandler.ButtonDown(CButtons.A))
                        _currentState = State.MoveY;

                    break;
                case State.WaitY:
                    _waitCounter += Game1.DeltaTime;
                    if (_waitCounter > 1000)
                        _currentState = State.MoveY;

                    break;
                case State.MoveY:
                    if ((_marinGame || ControlHandler.ButtonDown(CButtons.A)) &&
                        EntityPosition.Y < _vecStart.Y + 64)
                    {
                        Game1.GameManager.PlaySoundEffect("D378-32-20", false);

                        EntityPosition.Move(new Vector2(0, MoveSpeed));

                        if (EntityPosition.Y > _vecStart.Y + 64)
                            EntityPosition.Set(new Vector2(EntityPosition.X, _vecStart.Y + 64));
                    }
                    else
                    {
                        Game1.GameManager.StopSoundEffect("D378-32-20");
                        Game1.GameManager.SaveManager.SetString("trendy_button_2", "0");

                        _waitCounter = 0;
                        _currentState = State.Grab0;
                    }

                    break;
                case State.Grab0:
                    _waitCounter += Game1.DeltaTime;

                    if (_waitCounter > 800)
                    {
                        _waitCounter = 0;
                        // open the grabber
                        _grab2Count = 0;
                        _currentState = State.Grab1;
                    }

                    break;
                case State.Grab1:
                    _waitCounter += Game1.DeltaTime;

                    if (_waitCounter > 800)
                    {
                        _currentState = State.Grab2;
                    }

                    break;
                case State.Grab2:
                    _grabState += MoveSpeedGrab * Game1.TimeMultiplier;

                    if (_grabState > 15)
                    {
                        _grabState = 15;
                        _currentState = State.Grab3;
                    }

                    break;
                case State.Grab3:
                    _grab2Count += Game1.DeltaTime;

                    if (_grab2Count > 500)
                        _grabState = 16;

                    if (_grab2Count > 2000)
                        Grab();

                    if (_grab2Count > 3000)
                        _currentState = State.BackY;

                    break;
                case State.BackY:
                    _grabState -= MoveSpeedGrab * Game1.TimeMultiplier;
                    if (_grabState < 0)
                    {
                        _grabState = 0;
                        _currentState = State.BackX;
                    }
                    break;
                case State.BackX:
                    Game1.GameManager.PlaySoundEffect("D378-32-20", false);

                    var vecBack = _vecStart - EntityPosition.Position;
                    vecBack.Normalize();

                    EntityPosition.Move(vecBack * MoveSpeedBack);

                    if (EntityPosition.X <= _vecStart.X && EntityPosition.Y <= _vecStart.Y)
                    {
                        Game1.GameManager.StopSoundEffect("D378-32-20");

                        _waitCounter = 0;
                        _currentState = State.BackWait;

                        EntityPosition.Set(_vecStart);

                    }
                    break;
                case State.BackWait:
                    _waitCounter += Game1.DeltaTime;

                    if (_waitCounter > 1500)
                    {
                        _grab2Count = 0;
                        _currentState = State.Idle;
                        // release the item
                        EndGrabbing();
                    }

                    break;
            }

            if (_currentState != State.Idle)
                MapManager.ObjLink.FreezePlayer();

            // update the position of the grabbed body
            UpdateItemPos();
        }

        private void OnKeyChange()
        {
            var strMarinGame = "trendy_marin_game";
            if (!_marinGame && Game1.GameManager.SaveManager.GetString(strMarinGame, "0") == "1")
            {
                Game1.GameManager.SaveManager.RemoveString(strMarinGame);

                _marinGame = true;
                _currentState = State.MoveX;
            }
        }

        private void StartGrabbing()
        {
            // allowed to play and standing on the right spot?
            if (!Game1.GameManager.SaveManager.GetBool("trendy_ready", false))
                return;

            MapManager.ObjLink.Direction = 1;
            MapManager.ObjLink.CurrentState = ObjLink.State.Idle;
            MapManager.ObjLink.FreezePlayer();

            Game1.GameManager.SaveManager.SetString("can_play", "0");
            _currentState = State.MoveX;
        }

        private void Grab()
        {
            // already grabbed an item
            if (_grabbedBody != null)
                return;

            _collidingObjects.Clear();
            Map.Objects.GetComponentList(_collidingObjects,
                (int)_grabberRectangle.Left, (int)_grabberRectangle.Back, (int)_grabberRectangle.Width, (int)_grabberRectangle.Height, BodyComponent.Mask);

            // grab the first body the grabber is colliding with
            foreach (var gameObject in _collidingObjects)
            {
                var objBody = ((BodyComponent)gameObject.Components[BodyComponent.Index]);
                if ((gameObject is ObjItem || _marinGame) && objBody.BodyBox.Box.Intersects(_grabberRectangle))
                {
                    StartGrabbing(objBody);
                    return;
                }
            }
        }

        private void StartGrabbing(BodyComponent body)
        {
            _grabbedBody = body;
            _grabbedBody.AdditionalMovementVT = Vector2.Zero;
            _grabbedBody.IgnoresZ = true;

            Game1.GameManager.PlaySoundEffect("D360-25-19", false);
        }

        private void UpdateItemPos()
        {
            // update the position of the grabbed object
            _grabbedBody?.Position.Set(new Vector3(
                EntityPosition.X + 8, EntityPosition.Y + 0.001f, 13 - _grabState));
        }

        private void EndGrabbing()
        {
            if (_grabbedBody == null)
                return;

            //_grabbedBody.Height = 0;
            _grabbedBody.AdditionalMovementVT = new Vector2(0, 1 / 6.0f);
            _grabbedBody.IgnoresZ = false;
            _grabbedBody = null;
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            // draw the top
            spriteBatch.Draw(Resources.SprObjects, new Vector2(EntityPosition.Position.X, EntityPosition.Position.Y - 38),
                new Rectangle(_recTop.X, _recTop.Y + (_blinkCount >= 250 ? _recTop.Height : 0), _recTop.Width, _recTop.Height), Color.White);

            // line
            spriteBatch.Draw(Resources.SprObjects, new Vector2(
                EntityPosition.X + 6, EntityPosition.Y - 38 + _recTop.Height),
                new Rectangle(_recLine.X, _recLine.Y, _recLine.Width, (int)Math.Ceiling(_grabState)), Color.White);

            // left claw
            spriteBatch.Draw(Resources.SprObjects, new Vector2(
                    EntityPosition.X, EntityPosition.Y - 38 + _recTop.Height + _grabState),
                _grab2Count > 1000 ? _recGrabberLeftClosed : _recGrabberLeft, Color.White);

            // right claw
            spriteBatch.Draw(Resources.SprObjects, new Vector2(
                    EntityPosition.X + _recGrabberLeft.Width,
                    EntityPosition.Y - 38 + _recTop.Height + _grabState),
                _grab2Count > 2000 ? _recGrabberRightClosed : _recGrabberRight, Color.White);

            // draw the collision rectangle
            if (Game1.DebugMode)
            {
                spriteBatch.Draw(Resources.SprWhite, new Rectangle(
                    (int)(_grabberRectangle.X), (int)(_grabberRectangle.Y),
                    (int)(_grabberRectangle.Width), (int)(_grabberRectangle.Height)), Color.SaddleBrown * 0.5f);
            }
        }
    }
}
