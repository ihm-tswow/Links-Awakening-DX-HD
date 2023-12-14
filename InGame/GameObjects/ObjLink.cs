using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ProjectZ.Base;
using ProjectZ.InGame.Controls;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Systems;
using ProjectZ.InGame.GameObjects.Dungeon;
using ProjectZ.InGame.GameObjects.NPCs;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.GameSystems;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects
{
    public partial class ObjLink : GameObject
    {
        public enum State
        {
            Idle, Pushing, Grabbing, Pulling, Jumping, Attacking, Charging, Blocking, PreCarrying, Carrying, Throwing, CarryingItem, PickingUp, Falling,
            Ocarina, OcarinaTelport, Rafting, Pushed,
            FallRotateEntry,
            Drowning, Drowned, Swimming,
            Teleporting, MagicRod, Hookshot, Bombing, Powdering, Digging, BootKnockback,
            TeleporterUpWait, TeleporterUp, TeleportFallWait, TeleportFall,
            Dying, InitStunned, Stunned, Knockout,
            SwordShow0, SwordShow1, SwordShowLv2,
            ShowInstrumentPart0, ShowInstrumentPart1, ShowInstrumentPart2, ShowInstrumentPart3,
            ShowToadstool,
            CloakShow0, CloakShow1,
            Intro, BedTransition,
            Sequence, FinalInstruments,
            Frozen
        }

        public State CurrentState;

        private List<GameObject> _bombList = new List<GameObject>();
        private List<GameObject> _ocarinaList = new List<GameObject>();
        private List<GameObject> _destroyableWallList = new List<GameObject>();

        // movement stuff
        public float PosX => EntityPosition.X;
        public float PosY => EntityPosition.Y;
        public float PosZ => EntityPosition.Z;

        private const float WalkSpeed = 1.0f;
        private const float WalkSpeedPoP = 20 / 16f;
        private const float BootsRunningSpeed = 2.0f;
        private const float SwimSpeed = 0.5f;
        private const float SwimSpeedA = 1.0f;

        private float _currentWalkSpeed;
        private float _waterSoundCounter;

        private readonly Vector2[] _walkDirection = { new Vector2(-1, 0), new Vector2(0, -1), new Vector2(1, 0), new Vector2(0, 1) };

        public Vector2 ForwardVector
        {
            get => _walkDirection[Direction];
        }

        private Vector2 _moveVelocity;
        private Vector2 _lastMoveVelocity;
        private Vector2 _lastBaseMoveVelocity;
        public Vector2 LastMoveVector;

        private Point _lastTilePosition;

        public int Direction;
        private bool _isWalking;

        // player animations
        public readonly Animator Animation;
        private int _animationOffsetX = -7;
        private int _animationOffsetY = -16;

        private CSprite _sprite;
        private float _spriteTransparency;

        private bool _isVisible;
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                _isVisible = value;
                _sprite.IsVisible = value;
            }
        }

        // weapon animations
        private Animator AnimatorWeapons;

        private double _fallEntryCounter;

        // hole stuff
        private Vector2 _holeResetPoint;
        private Vector2 _alternativeHoleResetPosition; // map change on hole fall
        public string HoleResetRoom;
        public string HoleResetEntryId;
        public int HoleTeleporterId;
        public bool WasHoleReset;
        private double _holeTeleportCounter;
        // counter to start the level change
        private float _holeFallCounter;
        private bool _isFallingIntoHole;

        // body stuff
        public BodyComponent _body;
        public RectangleF BodyRectangle => _body.BodyBox.Box.Rectangle();
        public RectangleF PlayerRectangle => new RectangleF(PosX - 4, PosY - 12 - PosZ, 8, 12);
        private BodyDrawComponent _drawBody;
        private BodyDrawShadowComponent _shadowComponent;
        private DrawComponent.DrawTemplate _bodyDrawFunction;

        // carried object
        private GameObject _carriedGameObject;
        private DrawComponent _carriedObjDrawComp;
        private CarriableComponent _carriedComponent;
        private Vector3 _carryStartPosition;

        // show item
        public GameItem ShowItem;
        private Vector2 _showItemOffset;
        // used to only collect the item after it was shown
        private GameItemCollected _collectedShowItem;
        private string _pickupDialogOverride;
        private string _additionalPickupDialog;
        private double _itemShowCounter;
        private bool _showItem;

        private bool _savedPreItemPickup;
        public bool SavePreItemPickup
        {
            get { return _savedPreItemPickup; }
        }

        private const int dist0 = 30;
        private const int dist1 = 15;
        private readonly Vector2[] _showInstrumentOffset = {
            new Vector2(-dist1, -dist0), new Vector2(dist1, -dist0), new Vector2(dist0, dist1), new Vector2(dist0, -dist1),
            new Vector2(dist1, dist0),new Vector2(-dist1, dist0),new Vector2(-dist0, -dist1),new Vector2(-dist0, dist1) };

        private readonly int[] _instrumentMusicIndex = { 31, 39, 40, 41, 42, 43, 44, 45 };

        // show sword lv2
        private float _showSwordLv2Counter;
        private float _showSwordL2ParticleCounter;
        private bool _shownSwordLv2Dialog;

        // transition stuff
        public Vector2? MapTransitionStart;
        public Vector2? MapTransitionEnd;
        public Vector2? NextMapPositionStart;
        public Vector2? NextMapPositionEnd;
        public string NextMapPositionId;
        public int DirectionEntry;
        public bool IsTransitioning;
        public bool NextMapFallStart;
        public bool NextMapFallRotateStart;
        public bool TransitionOutWalking;
        public bool TransitionInWalking;
        private bool _wasTransitioning;
        private bool _startBedTransition;

        // rail jump
        private Vector2 _railJumpStartPosition;
        private Vector2 _railJumpTargetPosition;
        private float _railJumpPositionZ;
        private float _railJumpPercentage;
        private float _railJumpHeight;

        // swim stuff
        private Vector2 _swimVelocity;
        private float _swimBoostCount;
        private float _diveCounter;

        // store item picked up by the player
        public GameItem StoreItem;
        private int _storeItemWidth;
        private int _storeItemHeight;
        private Vector2 _storePickupPosition;
        private bool _showStealMessage;

        // follower
        private GameObjectFollower _objFollower;
        private ObjCock _objRooster;
        private ObjMarin _objMaria;

        private const string _spawnGhostKey = "spawn_ghost";
        private ObjGhost _objGhost;
        private bool _spawnGhost;

        // boots
        private bool _bootsHolding;
        private bool _bootsRunning;
        private bool _wasBootsRunning;
        private bool _bootsStop;
        private float _bootsCounter;
        private float _bootsRunTime = 500;
        private float _bootsParticleTime = 120;

        // trapped state
        private int _trapInteractionCount;
        private bool _isTrapped;
        private bool _trappedDisableItems;

        // raft
        private ObjRaft _objRaft;
        private bool _isRafting;

        // stonelifter pull
        private const float PullTime = 100;
        private const float PullMaxTime = 400;
        private const float PullResetTime = -133;
        private float _pullCounter;
        private bool _isPulling;
        private bool _wasPulling;
        // pick up time
        private const float PreCarryTime = 200;
        private float _preCarryCounter;

        // drown stuff
        private Vector2 _drownResetPosition;
        private float _drownCounter;
        private float _drownResetCounter;

        // sword stuff
        public Box SwordDamageBox;
        private float _swordPokeTime = 100;
        private float _swordPokeCounter;

        private Vector2[] _shootSwordOffset;
        private bool _shotSword;

        public CBox DamageCollider;
        private Vector2 _hitVelocity;
        public const int BlinkTime = 66;
        public const int CooldownTime = BlinkTime * 16;

        private double _hitCount;
        private double _hitRepelTime;
        private double _hitParticleTime;

        private const float SwordChargeTime = 500;
        private float _swordChargeCounter;
        private bool _swordPoked;
        private bool _stopCharging;

        private Point[] _pokeAnimationOffset;
        private bool _isHoldingSword;
        private bool _isSwingingSword;
        public bool CarrySword;

        // items
        private ObjBoomerang _boomerang = new ObjBoomerang();
        private Vector2[] _boomerangOffset;
        private Vector2[] _arrowOffset;
        public bool HasFlippers;

        // arrow
        private const float ArrowSpeed = 3;
        private const float ArrowSpeedPoP = 4;

        // shield
        public bool CarryShield;
        private bool _wasBlocking;

        // hookshot
        public ObjHookshot Hookshot = new ObjHookshot();
        private Vector2[] _hookshotOffset;
        private bool _hookshotPull;

        // magic rod
        private Vector2[] _magicRodOffset;
        private const float MagicRodSpeed = 3;
        private const float MagicRodSpeedPoP = 4;

        // shovel
        private Vector2[] _shovelOffset;
        private Point _digPosition;
        private bool _hasDug;
        private bool _canDig;

        private Vector2[] _powderOffset;
        private Vector2[] _bombOffset;

        // ocarina
        private float _ocarinaCounter;
        private int _ocarinaNoteIndex;
        private int _ocarinaSong;

        // jump stuff
        private bool _canJump = true;
        private const float JumpAcceleration = 2.35f;
        private float _railJumpSpeed;
        // should probably have been a different state because we do not want to be able to use certain items while railjumping compared to normally jumping
        private bool _railJump;
        private bool _startedJumping;
        private bool _hasStartedJumping;

        // cloak transition
        private int CloakTransitionTime = 2200;
        private float _cloakTransitionCounter;
        private float _cloakPercentage;
        private int CloakTransitionOutTime = 2500;
        private float _cloakTransitionOutCounter;

        // teleport stuff
        private ObjDungeonTeleporter _teleporter;
        private string _teleportMap;
        private string _teleporterId;
        private float _teleportCounter;
        private float _teleportCounterFull;
        private int _teleportState;

        // instrument stuff
        // @TODO: replace
        private Rectangle[] _noteSourceRectangles = { new Rectangle(145, 97, 10, 12), new Rectangle(156, 97, 6, 12) };
        private bool[] _noteInit = { false, false };
        private int[] _noteSpriteIndex = { 0, 0 };

        private double _instrumentPickupTime;
        private float _instrumentCounter;
        private float _instrumentEndTime;
        private int _instrumentIndex;
        private int _instrumentCycleTime = 1000;
        private bool _drawInstrumentEffect;
        private bool _pickingUpInstrument;
        private bool _pickingUpSword;

        // push stuff
        private Vector2 _pushStart;
        private Vector2 _pushEnd;
        private float _pushCounter;
        private int _pushTime;

        // used by the vaccuum
        private float _rotationCounter;

        // stunned state
        private float _stunnedCounter;
        private bool _stunnedParticles;

        // final sequence
        private int _finalIndex;
        private double _finalSeqCounter;

        // save position
        public string SaveMap;
        public Vector2 SavePosition;
        public int SaveDirection;

        // other stuff
        public Point CollisionBoxSize;
        private MapStates.FieldStates _lastFieldState;

        public bool CanWalk;
        public bool DisableItems;
        public bool UpdatePlayer;
        public bool IsPoking;
        private bool _pokeStart;
        private bool _isLocked;
        private bool _isGrabbed;
        private bool _isFlying;
        private bool _inDungeon;

#if DEBUG
        private bool _attackMode;
#endif

        private DictAtlasEntry _stunnedParticleSprite;

        public ObjLink() : base((Map.Map)null)
        {
            EntityPosition = new CPosition(0, 0, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            // load the player + sword animations
            Animation = AnimatorSaveLoad.LoadAnimator("link0");
            AnimatorWeapons = AnimatorSaveLoad.LoadAnimator("Objects/sword");

            _stunnedParticleSprite = Resources.GetSprite("stunned particle");

            CollisionBoxSize = new Point(8, 8);

            _body = new BodyComponent(EntityPosition, -4, -10, 8, 10, 8)
            {
                IsPusher = true,
                IsSlider = true,
                MaxJumpHeight = 3,
                Drag = 0.9f,
                DragAir = 0.9f,
                Gravity = -0.15f,
                Gravity2D = 0.1f,
                AbsorbPercentage = 1f,
                HoleOnPull = OnHolePull,
                HoleAbsorb = OnHoleAbsorb,
                MoveCollision = OnMoveCollision,
                CollisionTypes = Values.CollisionTypes.Normal |
                                 Values.CollisionTypes.Enemy |
                                 Values.CollisionTypes.PlayerItem |
                                 Values.CollisionTypes.LadderTop,
            };

            DamageCollider = new CBox(EntityPosition, -5, -10, 10, 10, 8);

            _powderOffset = new[]
            {
                new Vector2(-12, 0),
                new Vector2(-2, -CollisionBoxSize.Y -5),
                new Vector2(12, 0),
                new Vector2(2, 10)
            };

            _boomerangOffset = new[]
            {
                new Vector2(-10, -3),
                new Vector2(-2, -CollisionBoxSize.Y -1),
                new Vector2(10, -3),
                new Vector2(2, 6)
            };

            _arrowOffset = new[]
            {
                new Vector2(-10, 0),
                new Vector2(-2, -CollisionBoxSize.Y -1),
                new Vector2(10, 0),
                new Vector2(2, 6)
            };

            _magicRodOffset = new[]
            {
                new Vector2(-10, -4),
                new Vector2(-4, -CollisionBoxSize.Y - 8),
                new Vector2(10, -4),
                new Vector2(3, 4)
            };

            _shootSwordOffset = new[]
            {
                new Vector2(-10, -4),
                new Vector2(-5, -CollisionBoxSize.Y - 8),
                new Vector2(10, -4),
                new Vector2(4, 4)
            };

            _hookshotOffset = new[]
            {
                new Vector2(-5, -4),
                new Vector2(-3, -CollisionBoxSize.Y - 2),
                new Vector2(5, -4),
                new Vector2(3, 0)
            };

            _shovelOffset = new[]
            {
                new Vector2(-9, -1),
                new Vector2(0, -14),
                new Vector2(9, -1),
                new Vector2(0, 1)
            };

            _bombOffset = new[]
            {
                new Vector2(-10, 0),
                new Vector2(0, -CollisionBoxSize.Y - 2),
                new Vector2(10, 0),
                new Vector2(0, 8)
            };

            _pokeAnimationOffset = new[]
            {
                new Point(-16, -4),
                new Point(-4, -CollisionBoxSize.Y - 16),
                new Point(16, -4),
                new Point(5, 12)
            };

            _sprite = new CSprite(EntityPosition);
            // cant just change the offset value without changing the blocking rectangle
            var animatorComponent = new AnimationComponent(Animation, _sprite, new Vector2(_animationOffsetX, _animationOffsetY));

            // custom draw function
            _drawBody = new BodyDrawComponent(_body, DrawLink, Values.LayerPlayer);
            _bodyDrawFunction = _drawBody.Draw;
            _drawBody.Draw = Draw;

            AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(OnKeyChange));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(BaseAnimationComponent.Index, animatorComponent);
            AddComponent(CollisionComponent.Index, new BodyCollisionComponent(_body, Values.CollisionTypes.Player));
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, _drawBody);
            AddComponent(DrawShadowComponent.Index, _shadowComponent = new BodyDrawShadowComponent(_body, _sprite));

            EntityPosition.AddPositionListener(typeof(CarriableComponent), UpdatePositionCarriedObject);
        }

        private void Update()
        {
#if DEBUG
            if (InputHandler.KeyPressed(Keys.Y))
                Game1.GameManager.InitPieceOfPower();
            if (InputHandler.KeyPressed(Keys.X))
                _attackMode = !_attackMode;
            if (_attackMode)
            {
                var damageBox = new Box(EntityPosition.X - 160, EntityPosition.Y - 140, 0, 320, 280, 16);
                var damageOrigin = damageBox.Center;
                //Map.Objects.Hit(this, damageOrigin, damageBox, HitType.Sword1, Game1.GameManager.PieceOfPowerIsActive ? 2 : 1, Game1.GameManager.PieceOfPowerIsActive);
                //Map.Objects.Hit(this, damageOrigin, damageBox, HitType.Bomb, 2, false);
                Map.Objects.Hit(this, damageOrigin, damageBox, HitType.Bow, 2, false);
                //Map.Objects.Hit(this, damageOrigin, damageBox, HitType.Hookshot, 2, false);
                //Map.Objects.Hit(this, damageOrigin, damageBox, HitType.MagicRod, 2, false);
                //Map.Objects.Hit(this, damageOrigin, damageBox, HitType.MagicPowder, 2, false);
                //Map.Objects.Hit(this, damageOrigin, damageBox, HitType.PegasusBootsSword, 2, false);
                //Map.Objects.Hit(this, damageOrigin, damageBox, HitType.PegasusBootsPush, 2, false);
                //Map.Objects.Hit(this, damageOrigin, damageBox, HitType.ThrownObject, 2, false);
                //Map.Objects.Hit(this, damageOrigin, damageBox, HitType.SwordShot, 2, false);
                //Map.Objects.Hit(this, damageOrigin, damageBox, HitType.SwordHold, 2, false);
                //Map.Objects.Hit(this, damageOrigin, damageBox, HitType.SwordSpin, 2, false);
            }
#endif

            if (CurrentState == State.FallRotateEntry)
            {
                _fallEntryCounter += Game1.DeltaTime;
                Direction = (int)(DirectionEntry + (_fallEntryCounter + 96) / 48) % 4;

                if (_body.IsGrounded)
                    CurrentState = State.Idle;

                UpdateAnimation();
            }

            // @HACK
            // this is only needed because the player should not be able to step into the door 1 frame after finishing the transition
            // this would cause the door transition to not start
            if (IsTransitioning || _wasTransitioning)
            {
                _wasTransitioning = IsTransitioning;
                return;
            }

            // first photo sequence
            if (CurrentState == State.Pushed)
            {
                _pushCounter += Game1.DeltaTime;

                // push towards the target position
                if (_pushCounter > _pushTime)
                {
                    EntityPosition.Set(_pushEnd);
                    CurrentState = State.Idle;
                }
                else
                {
                    var percentage = MathF.Sin((_pushCounter / _pushTime) * MathF.PI * 0.5f);
                    var newPosition = Vector2.Lerp(_pushStart, _pushEnd, percentage);
                    EntityPosition.Set(newPosition);
                }
            }

            // need to update the bomb to make sure it does not explode while the player is not getting updated
            if (_carriedComponent != null && _carriedComponent.IsPickedUp)
            {
                // used to updated the position to match the animation
                // gets called twice when moving
                // not sure how this could be done better
                UpdatePositionCarriedObject(EntityPosition);
            }

            if (!UpdatePlayer)
            {
                UpdatePlayer = true;

                // only update the animation
                if (!Is2DMode)
                    UpdateAnimation();
                else
                    Update2DFrozen();

                UpdateOcarinaAnimation();
                UpdateDive();
                UpdateDrawComponents();

                return;
            }

            UpdateHeartWarningSound();

            if (CurrentState == State.FinalInstruments)
            {
                _finalSeqCounter -= Game1.DeltaTime;
                if (_finalIndex == 0)
                {
                    if (_finalSeqCounter <= 0)
                    {
                        _finalIndex = 1;
                        _finalSeqCounter += 2250;
                        Animation.Play("show1");
                        Game1.GameManager.PlaySoundEffect("D360-52-34");
                    }
                }
                else if (_finalIndex == 1)
                {
                    if (_finalSeqCounter <= 0)
                        ((MapShowSystem)Game1.GameManager.GameSystems[typeof(MapShowSystem)]).StartEnding();
                }

                return;
            }
            else if (CurrentState == State.CloakShow0)
            {
                _cloakTransitionCounter += Game1.DeltaTime;
                _cloakPercentage = _cloakTransitionCounter / CloakTransitionTime;

                if (_cloakTransitionCounter > CloakTransitionTime)
                {
                    _cloakPercentage = 1;

                    if (ShowItem.Name == "cloakBlue")
                        Game1.GameManager.StartDialog("cloak_blue");
                    if (ShowItem.Name == "cloakRed")
                        Game1.GameManager.StartDialog("cloak_red");

                    CurrentState = State.CloakShow1;

                    // add the item to the inventory
                    if (_collectedShowItem != null)
                    {
                        Game1.GameManager.CollectItem(_collectedShowItem, 0);
                        _collectedShowItem = null;
                    }

                    ShowItem = null;
                }
            }
            else if (CurrentState == State.CloakShow1)
            {
                _cloakTransitionOutCounter += Game1.DeltaTime;

                var transitionSystem = (MapTransitionSystem)Game1.GameManager.GameSystems[typeof(MapTransitionSystem)];
                transitionSystem.SetColorMode(Color.White, MathHelper.Clamp(_cloakTransitionOutCounter / 1000f, 0, 1));

                if (_cloakTransitionOutCounter > CloakTransitionOutTime)
                {
                    Game1.GameManager.StartDialogPath("color_fairy_4");

                    Direction = 3;
                    MapTransitionStart = EntityPosition.Position;
                    MapTransitionEnd = MapTransitionStart;
                    TransitionOutWalking = false;

                    // append a map change
                    ((MapTransitionSystem)Game1.GameManager.GameSystems[typeof(MapTransitionSystem)]).AppendMapChange(
                        "overworld.map", "cloakOut", false, true, Color.White, true);
                }
            }
            else if (CurrentState == State.ShowToadstool)
            {
                CurrentState = State.Idle;
            }
            else if (CurrentState == State.SwordShowLv2)
            {
                _showSwordL2ParticleCounter += Game1.DeltaTime;
                if (_showSwordL2ParticleCounter > 4800 && !_shownSwordLv2Dialog)
                {
                    _shownSwordLv2Dialog = true;
                    _showSwordL2ParticleCounter = 0;
                    Game1.GameManager.SetMusic(-1, 2);
                    Game1.GameManager.StartDialogPath("sword2Collected");
                }
                // make sure to show the sword while the dialog box is open
                else if (_shownSwordLv2Dialog)
                {
                    ShowItem = null;
                    CurrentState = State.Idle;
                }
            }
            else if (CurrentState == State.PickingUp && !_pickingUpInstrument && !_pickingUpSword)
            {
                Game1.GameManager.FreezeWorldAroundPlayer = true;
            }
            else if (CurrentState == State.TeleporterUpWait)
            {
                _holeTeleportCounter += Game1.DeltaTime;
                if (_holeTeleportCounter > 1000)
                {
                    CurrentState = State.TeleporterUp;

                    _holeTeleportCounter -= 1000;
                    _shadowComponent.Transparency = 0;

                    Game1.GameManager.PlaySoundEffect("D360-37-25");
                }
            }
            else if (CurrentState == State.TeleporterUp)
            {
                _holeTeleportCounter += Game1.DeltaTime;
                var time = 400;

                EntityPosition.Z = (float)(_holeTeleportCounter / time) * 128;
                Direction = (int)(_holeTeleportCounter / 64) % 4;

                // fade in
                var percentage = MathHelper.Clamp(1 - ((float)_holeTeleportCounter - (time - 100)) / 100, 0, 1);
                _spriteTransparency = percentage;
                _shadowComponent.Transparency = percentage;

                if (_holeTeleportCounter > time)
                {
                    _holeTeleportCounter -= time;

                    if (ObjOverworldTeleporter.TeleporterDictionary.TryGetValue(HoleTeleporterId, out var teleporter))
                        teleporter.SetNextTeleporterPosition();
                    else
                        CurrentState = State.Idle;  // should not happen
                }
            }
            else if (CurrentState == State.TeleportFallWait)
            {
                _holeTeleportCounter += Game1.DeltaTime;
                var time = 350;

                if (_holeTeleportCounter > time)
                {
                    _holeTeleportCounter -= time - 50;
                    _body.Velocity = new Vector3(0, 0, 0);
                    CurrentState = State.TeleportFall;
                }
            }
            else if (CurrentState == State.TeleportFall)
            {
                _holeTeleportCounter += Game1.DeltaTime;
                Direction = (int)(_holeTeleportCounter / 64) % 4;

                // fade in
                var percentage = MathHelper.Clamp((float)_holeTeleportCounter / 100, 0, 1);

                if (_body.IsGrounded)
                {
                    percentage = 1;
                    CurrentState = State.Idle;

                    UpdateSaveLocation();

                    // save settings?
                    if (GameSettings.Autosave)
                    {
                        SaveGameSaveLoad.SaveGame(Game1.GameManager);
                        Game1.GameManager.InGameOverlay.InGameHud.ShowSaveIcon();
                    }
                }

                _spriteTransparency = percentage;
                _shadowComponent.Transparency = percentage;
            }

            if (CurrentState == State.Knockout)
                return;

            // stunned player
            if (CurrentState == State.InitStunned && _hitVelocity.Length() < 0.25f)
            {
                Animation.Play("stunned");
                CurrentState = State.Stunned;
            }
            if (CurrentState == State.Stunned && _stunnedCounter > 0)
            {
                _stunnedCounter -= Game1.DeltaTime;

                if (_stunnedCounter <= 0)
                    CurrentState = State.Idle;
            }

            AnimatorWeapons.Update();

            // update all the item stuff
            // this need to be before the update method to correctly start jumping?
            UpdateItem();

            if (Is2DMode)
                Update2D();
            else
                Update3D();

            UpdateOcarina();

            UpdateDamageShader();
            _hitCount -= Game1.DeltaTime;

            if (_savedPreItemPickup && (CurrentState == State.Idle || CurrentState == State.Swimming))
                EndPickup();

            // die?
            if (Game1.GameManager.CurrentHealth <= 0 && !Game1.GameManager.UseShockEffect)
                OnDeath();

            UpdateDrawComponents();

            DisableItems = false;
            HoleResetRoom = null;
            CanWalk = true;
            _canJump = true;
            _isLocked = false;

            _hasStartedJumping = _startedJumping;
            _startedJumping = false;

            _currentWalkSpeed = Game1.GameManager.PieceOfPowerIsActive ? WalkSpeedPoP : WalkSpeed;
        }

        #region Draw

        private void Draw(SpriteBatch spriteBatch)
        {
            Game1.DebugText += "Jump Timer: " + _railJumpPercentage + "\n";
            Game1.DebugText += "Player State: " + CurrentState;

            if (!IsVisible)
                return;

            // draw the player sprite behind the sword
            if (Direction != 1 && !_isTrapped)
                _bodyDrawFunction(spriteBatch);

            // draw the sword/magic rod
            if (CurrentState == State.Attacking ||
                CurrentState == State.Charging ||
                CurrentState == State.SwordShow0 ||
                CurrentState == State.MagicRod ||
                (_bootsRunning && CarrySword))
            {
                var changeColor = _swordChargeCounter <= 0 &&
                            Game1.TotalGameTime % (8 / 0.06) >= 4 / 0.06 &&
                            ObjectManager.CurrentEffect != Resources.DamageSpriteShader0.Effect;

                // change the draw shader
                if (changeColor)
                {
                    spriteBatch.End();
                    ObjectManager.SpriteBatchBegin(spriteBatch, Resources.DamageSpriteShader0);
                }

                AnimatorWeapons.Draw(spriteBatch, new Vector2(EntityPosition.X - 7, EntityPosition.Y - 16 - EntityPosition.Z), Color.White);

                if (changeColor)
                {
                    spriteBatch.End();
                    ObjectManager.SpriteBatchBegin(spriteBatch, null);
                }
            }

            // draw the sword after the first pickup
            if (CurrentState == State.SwordShow1)
            {
                var itemSword = Game1.GameManager.ItemManager["sword1"];
                var position = new Vector2(
                    BodyRectangle.X - itemSword.SourceRectangle.Value.Width / 2f,
                    (EntityPosition.Y - EntityPosition.Z - 15) - itemSword.SourceRectangle.Value.Height);

                ItemDrawHelper.DrawItem(spriteBatch, itemSword, position, Color.White, 1, true);
            }

            // draw the toadstool
            if (CurrentState == State.ShowToadstool)
            {
                var itemToadstool = Game1.GameManager.ItemManager["toadstool"];
                var position = new Vector2(
                    BodyRectangle.X - itemToadstool.SourceRectangle.Value.Width / 2f,
                    (EntityPosition.Y - EntityPosition.Z - 15) - itemToadstool.SourceRectangle.Value.Height);

                ItemDrawHelper.DrawItem(spriteBatch, itemToadstool, position, Color.White, 1);
            }

            // draw the player sprite in front of the sword
            if (Direction == 1 && !_isTrapped)
                _bodyDrawFunction(spriteBatch);

            if (_drawInstrumentEffect)
                DrawInstrumentEffect(spriteBatch);

            // draw the picked up store item
            if (StoreItem != null)
                ItemDrawHelper.DrawItem(spriteBatch, StoreItem, _storePickupPosition, Color.White, 1, true);

            // draw the shown item
            if (ShowItem != null)
            {
                var itemPosition = EntityPosition.Position + _showItemOffset;
                itemPosition.Y -= EntityPosition.Z;

                if (CurrentState == State.CloakShow0)
                {
                    ItemDrawHelper.DrawItem(spriteBatch, ShowItem, itemPosition, Color.White * (1 - _cloakPercentage), 1, true);
                }
                else if (ShowItem.Name == "sword2")
                {
                    var swordImage = Resources.GetSprite("sword2Show");
                    DrawHelper.DrawNormalized(spriteBatch, swordImage.Texture, itemPosition, swordImage.ScaledRectangle, Color.White, swordImage.Scale);
                }
                else
                    ItemDrawHelper.DrawItem(spriteBatch, ShowItem, itemPosition, Color.White, 1, true);
            }

            // draw the object the player is carrying
            if (_carriedObjDrawComp != null)
            {
                _carriedObjDrawComp.IsActive = true;
                _carriedObjDrawComp.Draw(spriteBatch);
                _carriedObjDrawComp.IsActive = false;
            }

            // draw the dots over the head in the stunned state
            if (CurrentState == State.Stunned && _stunnedParticles)
            {
                var rotation = (float)(Game1.TotalGameTime / 1200) * MathF.PI * 2;
                var offset0 = new Vector2(MathF.Cos(rotation) * 8 - 2, MathF.Sin(rotation) * 3 - 2);
                DrawHelper.DrawNormalized(spriteBatch, _stunnedParticleSprite,
                    offset0 + new Vector2(EntityPosition.X, EntityPosition.Y - EntityPosition.Z - 18), Color.White);

                var offset1 = new Vector2(MathF.Cos(rotation + MathF.PI) * 8 - 2, MathF.Sin(rotation + MathF.PI) * 3 - 2);
                DrawHelper.DrawNormalized(spriteBatch, _stunnedParticleSprite,
                    offset1 + new Vector2(EntityPosition.X, EntityPosition.Y - EntityPosition.Z - 18), Color.White);
            }

            if (CurrentState == State.SwordShowLv2)
                DrawSwordL2Particles(spriteBatch);

            // draw the notes while showing an instrument
            {
                var leftNotePosition = new Vector2(EntityPosition.X - 8, EntityPosition.Y - 24);
                DrawNote(spriteBatch, leftNotePosition, new Vector2(-0.4f, -1.0f), 0);

                var rightNotePosition = new Vector2(EntityPosition.X + 8, EntityPosition.Y - 24);
                DrawNote(spriteBatch, rightNotePosition, new Vector2(0.4f, -1.0f), 1);
            }

            if (CurrentState == State.FinalInstruments)
                DrawFinalInstruments(spriteBatch);

            if (Game1.DebugMode)
            {
                // draw the save hole position
                spriteBatch.Draw(Resources.SprWhite,
                    new Vector2(_holeResetPoint.X - 5, _holeResetPoint.Y - 5), new Rectangle(0, 0,
                       10, 10), Color.HotPink * 0.65f);

                // weapon damage rectangle
                var swordRectangle = SwordDamageBox.Rectangle();
                spriteBatch.Draw(Resources.SprWhite,
                    new Vector2(swordRectangle.X, swordRectangle.Y), new Rectangle(0, 0,
                        (int)swordRectangle.Width, (int)swordRectangle.Height), Color.Blue * 0.75f);
            }
        }

        private void DrawSwordL2Particles(SpriteBatch spriteBatch)
        {
            DrawSwordParticle(spriteBatch, new Vector2(EntityPosition.X - 4, EntityPosition.Y - 22), new Vector2(-32, -16), -125, 300, 200, 0);
            DrawSwordParticle(spriteBatch, new Vector2(EntityPosition.X - 4, EntityPosition.Y - 22), new Vector2(-32, -16), -125 - 250, 300, 200, 0);

            DrawSwordParticle(spriteBatch, new Vector2(EntityPosition.X - 4, EntityPosition.Y - 22), new Vector2(-32, -32), 0, 300, 200, 1);
            DrawSwordParticle(spriteBatch, new Vector2(EntityPosition.X - 4, EntityPosition.Y - 22), new Vector2(-32, -32), -250, 300, 200, 1);

            DrawSwordParticle(spriteBatch, new Vector2(EntityPosition.X - 4, EntityPosition.Y - 22), new Vector2(-24, -52), -50, 450, 50, 2);
            DrawSwordParticle(spriteBatch, new Vector2(EntityPosition.X - 4, EntityPosition.Y - 22), new Vector2(-24, -52), -50 - 250, 450, 50, 2);

            DrawSwordParticle(spriteBatch, new Vector2(EntityPosition.X - 4, EntityPosition.Y - 22), new Vector2(0, -64), -75, 450, 50, 3);
            DrawSwordParticle(spriteBatch, new Vector2(EntityPosition.X - 4, EntityPosition.Y - 22), new Vector2(0, -64), -75 - 250, 450, 50, 3);

            DrawSwordParticle(spriteBatch, new Vector2(EntityPosition.X - 4, EntityPosition.Y - 22), new Vector2(24, -52), -50, 450, 50, 4);
            DrawSwordParticle(spriteBatch, new Vector2(EntityPosition.X - 4, EntityPosition.Y - 22), new Vector2(24, -52), -50 - 250, 450, 50, 4);

            DrawSwordParticle(spriteBatch, new Vector2(EntityPosition.X - 4, EntityPosition.Y - 22), new Vector2(32, -32), 0, 300, 200, 5);
            DrawSwordParticle(spriteBatch, new Vector2(EntityPosition.X - 4, EntityPosition.Y - 22), new Vector2(32, -32), -250, 300, 200, 5);

            DrawSwordParticle(spriteBatch, new Vector2(EntityPosition.X - 4, EntityPosition.Y - 22), new Vector2(32, -16), -125, 300, 200, 6);
            DrawSwordParticle(spriteBatch, new Vector2(EntityPosition.X - 4, EntityPosition.Y - 22), new Vector2(32, -16), -125 - 250, 300, 200, 6);
        }

        private void DrawInstrumentEffect(SpriteBatch spriteBatch)
        {
            var fadeTime = 100;
            var speed = 500;
            var center = new Vector2(EntityPosition.X, EntityPosition.Y - 20);

            {
                var time = (float)(Game1.TotalGameTime % speed);
                var state = MathF.Sin((time / speed) * MathF.PI * 0.475f);
                var distance = 32 - 20 * state;
                var transparency = MathHelper.Clamp(time / fadeTime, 0, 1) *
                                   MathHelper.Clamp((speed - time) / fadeTime, 0, 1);
                var sourceRectangle = time < (speed / 1.65f) ? new Rectangle(194, 114, 12, 12) : new Rectangle(194, 98, 12, 12);

                for (var y = 0; y < 2; y++)
                    for (var x = 0; x < 2; x++)
                    {
                        var position = new Vector2(
                            center.X - 6 + (x * 2 - 1) * distance,
                            center.Y - 6 + (y * 2 - 1) * distance);
                        spriteBatch.Draw(Resources.SprItem, position, sourceRectangle,
                            Color.White * transparency, 0, Vector2.Zero, Vector2.One,
                            (x == 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None) |
                            (y == 0 ? SpriteEffects.FlipVertically : SpriteEffects.None), 0);
                    }
            }

            {
                var time = (float)((Game1.TotalGameTime + speed / 2) % speed);
                var state = MathF.Sin((time / speed) * MathF.PI * 0.475f);
                var distance = 40 - 34 * state;
                var transparency = MathHelper.Clamp(time / fadeTime, 0, 1) *
                                   MathHelper.Clamp((speed - time) / fadeTime, 0, 1);
                var sourceRectangle = time < (speed / 1.65f) ? new Rectangle(176, 116, 16, 8) : new Rectangle(176, 100, 16, 8);

                for (var y = 0; y < 2; y++)
                    for (var x = 0; x < 2; x++)
                    {
                        var rotation = (float)((x * 2 + y) * Math.PI / 2);

                        var position = new Vector2(
                            center.X + (y == 0 ? (x * 2 - 1) * distance : 0),
                            center.Y + (y == 0 ? 0 : (x * 2 - 1) * distance));

                        spriteBatch.Draw(Resources.SprItem, position, sourceRectangle,
                            Color.White * transparency, rotation, new Vector2(16, 4), Vector2.One, SpriteEffects.None, 0);
                    }
            }
        }

        private void DrawFinalInstruments(SpriteBatch spriteBatch)
        {
            if (_finalIndex != 1)
                return;

            var percentage = 0.25f + Math.Clamp((float)(2500 - _finalSeqCounter) / 2000, 0, 1) * 0.75f;

            // draw the instruments
            for (var i = 0; i < 8; i++)
            {
                var itemInstrument = Game1.GameManager.ItemManager["instrument" + i];
                var position = new Vector2(EntityPosition.X - 8, EntityPosition.Y - 60) + _showInstrumentOffset[i] * percentage;
                ItemDrawHelper.DrawItem(spriteBatch, itemInstrument, position, Color.White, 1, true);
            }
        }

        private void DrawLink(SpriteBatch spriteBatch)
        {
            _sprite.Draw(spriteBatch);

            // draw the colored cloak
            var texture = _sprite.SprTexture;

            var cloakColor = Game1.GameManager.CloakColor;
            if (CurrentState == State.CloakShow0 && ShowItem != null && ShowItem.Name == "cloakBlue")
                cloakColor = Color.Lerp(cloakColor, ItemDrawHelper.CloakColors[1], _cloakPercentage);
            else if (CurrentState == State.CloakShow0 && ShowItem != null && ShowItem.Name == "cloakRed")
                cloakColor = Color.Lerp(cloakColor, ItemDrawHelper.CloakColors[2], _cloakPercentage);

            _sprite.Color = cloakColor * _spriteTransparency;
            _sprite.SprTexture = Resources.SprLinkCloak;
            _sprite.Draw(spriteBatch);

            _sprite.Color = Color.White * _spriteTransparency;
            _sprite.SprTexture = texture;
        }

        private void DrawNote(SpriteBatch spriteBatch, Vector2 position, Vector2 direction, int noteIndex)
        {
            var timeOffset = noteIndex * _instrumentCycleTime / 2;

            if (_instrumentCounter < timeOffset ||
                (CurrentState != State.ShowInstrumentPart1 || _drawInstrumentEffect) &&
                ((_instrumentCounter - timeOffset) / _instrumentCycleTime + 1) * _instrumentCycleTime + timeOffset > _instrumentEndTime)
                return;

            var time = (_instrumentCounter + timeOffset) % _instrumentCycleTime;

            var transparency = 1.0f;
            // fade out
            if (time > _instrumentCycleTime - 100)
            {
                _noteInit[noteIndex] = false;
                transparency = (_instrumentCycleTime - time) / 100f;
            }
            // fade in
            else if (time < 100)
            {
                if (!_noteInit[noteIndex])
                {
                    _noteInit[noteIndex] = true;
                    _noteSpriteIndex[noteIndex] = Game1.RandomNumber.Next(0, 2);

                }
                transparency = time / 100;
            }

            position += direction * time * 0.02f + new Vector2(-direction.X, direction.Y) * (float)Math.Sin(time * 0.015) * 0.75f;
            position += new Vector2(
                -_noteSourceRectangles[_noteSpriteIndex[noteIndex]].Width / 2f,
                -_noteSourceRectangles[_noteSpriteIndex[noteIndex]].Height);

            spriteBatch.Draw(Resources.SprItem, position,
                _noteSourceRectangles[_noteSpriteIndex[noteIndex]], Color.White * transparency);
        }

        private void DrawSwordParticle(SpriteBatch spriteBatch, Vector2 position, Vector2 direction, int timeOffset, int fullTime, int timeDelay, int index)
        {
            var fadeTime = 50;
            var particleTime = (_showSwordL2ParticleCounter + timeOffset) % (fullTime + timeDelay);
            var percentage = particleTime / fullTime;
            var colorTransparency = Math.Min((fullTime - particleTime) / fadeTime, particleTime / fadeTime);
            var particlePosition = position + percentage * direction;
            var spriteParticle = Resources.GetSprite("sword_particle_" + index);

            if (0 < particleTime && particleTime < fullTime)
                DrawHelper.DrawNormalized(spriteBatch, spriteParticle.Texture,
                    particlePosition - spriteParticle.Origin, spriteParticle.ScaledRectangle, Color.White * colorTransparency, spriteParticle.Scale);
        }

        private void DrawLight(SpriteBatch spriteBatch)
        {
            DrawHelper.DrawLight(spriteBatch, new Rectangle((int)EntityPosition.X - 45, (int)EntityPosition.Y - 45, 90, 90), new Color(255, 255, 255) * 0.125f);
        }

        public void DrawTransition(SpriteBatch spriteBatch)
        {
            if (!IsVisible)
                return;

            _bodyDrawFunction(spriteBatch);

            if (_drawInstrumentEffect)
                DrawInstrumentEffect(spriteBatch);

            // draw the shown item
            if (ShowItem != null)
            {
                var itemPosition = EntityPosition.Position + _showItemOffset;
                ItemDrawHelper.DrawItem(spriteBatch, ShowItem, itemPosition, Color.White, 1, true);
            }
        }

        #endregion

        private void OnKeyChange()
        {
            var strCloak = "cloak_transition";
            var cloakTransition = Game1.GameManager.SaveManager.GetString(strCloak);
            if (cloakTransition == "1")
            {
                _cloakTransitionCounter = 0;
                _cloakPercentage = 0;
                _cloakTransitionOutCounter = 0;

                Game1.GameManager.SaveManager.RemoveString(strCloak);
                Game1.GameManager.SaveManager.SetString(strCloak, "0");

                CurrentState = State.CloakShow0;
            }

            // play animation?
            var strAnimation = "link_direction";
            var newDirection = Game1.GameManager.SaveManager.GetString(strAnimation);
            if (!string.IsNullOrEmpty(newDirection))
            {
                Direction = int.Parse(newDirection);
                UpdateAnimation();
                Game1.GameManager.SaveManager.SetString(strAnimation, null);
            }

            // start moving? [set:link_move,-16,32]
            var moveValue = Game1.GameManager.SaveManager.GetString("link_move");
            if (!string.IsNullOrEmpty(moveValue))
            {
                var split = moveValue.Split(',');
                var directionX = float.Parse(split[0], CultureInfo.InvariantCulture);
                var directionY = float.Parse(split[1], CultureInfo.InvariantCulture);

                var velocity = new Vector2(directionX, directionY);
                _body.VelocityTarget = velocity;
                Direction = AnimationHelper.GetDirection(velocity);
                _isWalking = true;

                Game1.GameManager.SaveManager.SetString("link_move_collision", "0");
                Game1.GameManager.SaveManager.RemoveString("link_move");
            }

            var idleValue = Game1.GameManager.SaveManager.GetString("link_idle");
            if (!string.IsNullOrEmpty(idleValue))
            {
                CurrentState = State.Idle;
                Game1.GameManager.SaveManager.RemoveString("link_idle");
            }

            var hideHudValue = Game1.GameManager.SaveManager.GetString("hide_hud");
            if (!string.IsNullOrEmpty(hideHudValue))
            {
                Game1.GameManager.InGameOverlay.HideHud(true);
                Game1.GameManager.SaveManager.RemoveString("hide_hud");
            }

            // start moving? [set:link_push,-16,0,200]
            var pushValue = Game1.GameManager.SaveManager.GetString("link_push");
            if (!string.IsNullOrEmpty(pushValue))
            {
                var split = pushValue.Split(',');

                // init movement
                if (split.Length == 1)
                {
                    _pushStart = EntityPosition.Position;
                    _pushEnd = new Vector2(80, 94);
                    _pushTime = int.Parse(split[0]);
                }
                else
                {
                    var offsetX = float.Parse(split[0], CultureInfo.InvariantCulture);
                    var offsetY = float.Parse(split[1], CultureInfo.InvariantCulture);
                    _pushStart = EntityPosition.Position;
                    _pushEnd = _pushStart + new Vector2(offsetX, offsetY);
                    _pushTime = int.Parse(split[2]);
                }

                _pushCounter = 0;
                CurrentState = State.Pushed;

                Game1.GameManager.SaveManager.RemoveString("link_push");
            }

            // link animation
            var animationValue = Game1.GameManager.SaveManager.GetString("link_animation");
            if (!string.IsNullOrEmpty(animationValue))
            {
                Animation.Play(animationValue);
                CurrentState = State.Sequence;
                Game1.GameManager.SaveManager.RemoveString("link_animation");
            }

            var linkFinal = Game1.GameManager.SaveManager.GetString("link_final");
            if (!string.IsNullOrEmpty(linkFinal))
            {
                _finalIndex = 0;
                _finalSeqCounter = 1500;
                Animation.Play("final_stand_down");
                CurrentState = State.FinalInstruments;
                Game1.GameManager.SetMusic(62, 2);
                Game1.GameManager.SaveManager.RemoveString("link_final");
            }

            // start diving?
            var diveValue = Game1.GameManager.SaveManager.GetString("link_dive");
            if (!string.IsNullOrEmpty(diveValue))
            {
                _diveCounter = int.Parse(diveValue);
                CurrentState = State.Swimming;
                Game1.GameManager.SaveManager.RemoveString("link_dive");
            }

            // boomerang trading
            // can be exchanged for: shovel, feather
            var boomerangValue = Game1.GameManager.SaveManager.GetString("boomerang_trade");
            if (!string.IsNullOrEmpty(boomerangValue))
            {
                Game1.GameManager.SaveManager.RemoveString("boomerang_trade");

                if (Game1.GameManager.Equipment[1] != null &&
                    (Game1.GameManager.Equipment[1].Name == "shovel" ||
                     Game1.GameManager.Equipment[1].Name == "feather" ||
                     Game1.GameManager.Equipment[1].Name == "magicRod" ||
                     Game1.GameManager.Equipment[1].Name == "hookshot"))
                {
                    Game1.GameManager.SaveManager.SetString("tradded_item", Game1.GameManager.Equipment[1].Name);
                    Game1.GameManager.Equipment[1] = null;

                    Game1.GameManager.StartDialogPath("npc_hidden_boomerang");
                }
                else
                {
                    Game1.GameManager.StartDialogPath("npc_hidden_reject");
                }
            }

            var boomerangReturnValue = Game1.GameManager.SaveManager.GetString("boomerang_trade_return");
            if (!string.IsNullOrEmpty(boomerangReturnValue))
            {
                Game1.GameManager.SaveManager.RemoveString("boomerang_trade_return");

                // remove the boomerang
                Game1.GameManager.RemoveItem("boomerang", 1);

                // return the traded item
                var tradedItem = Game1.GameManager.SaveManager.GetString("tradded_item");
                var item = new GameItemCollected(tradedItem);
                MapManager.ObjLink.PickUpItem(item, true);
                _pickupDialogOverride = "npc_hidden_4";
            }

            var spawnGhostValue = Game1.GameManager.SaveManager.GetString(_spawnGhostKey);
            if (!string.IsNullOrEmpty(spawnGhostValue))
            {
                _spawnGhost = true;
            }
        }

        private void OnMoveCollision(Values.BodyCollision collision)
        {
            // knockback
            if (CurrentState == State.Idle && _wasBootsRunning)
            {
                var knockBack = false;

                if ((collision & Values.BodyCollision.Horizontal) != 0 && Direction % 2 == 0)
                {
                    var dirX = (collision & Values.BodyCollision.Left) != 0 ? -1 : 1;
                    _body.Velocity.X = -dirX;
                    Game1.GameManager.ShakeScreen(750, 2, 1, 5.5f, 2.5f, dirX, 1);
                    knockBack = true;
                }
                if ((collision & Values.BodyCollision.Vertical) != 0 && Direction % 2 != 0)
                {
                    var dirY = (collision & Values.BodyCollision.Top) != 0 ? -1 : 1;
                    _body.Velocity.Y = -dirY;
                    Game1.GameManager.ShakeScreen(750, 1, 2, 2.5f, 5.5f, 1, dirY);
                    knockBack = true;
                }

                if (knockBack)
                {
                    _bootsRunning = false;
                    _bootsCounter = 0;

                    _body.Velocity.Z = 2.0f;
                    CurrentState = State.BootKnockback;

                    var damageOrigin = BodyRectangle.Center;
                    var damageBox = _body.BodyBox.Box;
                    damageBox.X += AnimationHelper.DirectionOffset[Direction].X;
                    damageBox.Y += AnimationHelper.DirectionOffset[Direction].Y;

                    Game1.GameManager.PlaySoundEffect("D360-11-0B");

                    Map.Objects.Hit(this, damageOrigin, damageBox, HitType.PegasusBootsPush, 0, false);
                }
            }

            // what is this?
            if ((collision & Values.BodyCollision.Floor) != 0)
            {
                _moveVelocity = _lastMoveVelocity * 0.5f;
                _lastBaseMoveVelocity = _moveVelocity;
            }

            if (CurrentState == State.BootKnockback &&
                (collision & Values.BodyCollision.Floor) != 0)
            {
                CurrentState = State.Idle;
                _body.Velocity.Z = 0;
            }

            if (Is2DMode)
                OnMoveCollision2D(collision);
            else
            {
                // colliding horizontally or vertically? -> start pushing
                if (CurrentState == State.Idle && _isWalking && (
                        (collision & Values.BodyCollision.Horizontal) != 0 && (Direction == 0 || Direction == 2) ||
                        (collision & Values.BodyCollision.Vertical) != 0 && (Direction == 1 || Direction == 3)))
                {
                    var box = _body.BodyBox.Box;
                    // offset by one in the walk direction
                    box.X += AnimationHelper.DirectionOffset[Direction].X;
                    box.Y += AnimationHelper.DirectionOffset[Direction].Y;
                    var cBox = Box.Empty;
                    var outBox = Box.Empty;
                    // check if the object we are walking into is actually an object where the push animation should be played
                    if (Map.Objects.Collision(box, cBox, _body.CollisionTypes, Values.CollisionTypes.PushIgnore, Direction, _body.Level, ref outBox))
                        CurrentState = State.Pushing;
                }

                if (CurrentState == State.Swimming)
                {
                    if ((collision & Values.BodyCollision.Horizontal) != 0)
                        _moveVelocity.X = 0;
                    if ((collision & Values.BodyCollision.Vertical) != 0)
                        _moveVelocity.Y = 0;
                }

                // used for scripting (final stript stop at the top of the stairs)
                Game1.GameManager.SaveManager.SetString("link_move_collision", "1");

                // stop the hit velocity if the are colliding with a wall
                // this was done because the player pushes into the hitVelocity direction
                if ((collision & Values.BodyCollision.Horizontal) != 0 && _body.VelocityTarget.X == 0)
                    _hitVelocity.X = 0;
                if ((collision & Values.BodyCollision.Vertical) != 0 && _body.VelocityTarget.Y == 0)
                    _hitVelocity.Y = 0;

                if (CurrentState == State.Charging &&
                    ((collision & Values.BodyCollision.Left) != 0 && Direction == 0 ||
                     (collision & Values.BodyCollision.Top) != 0 && Direction == 1 ||
                     (collision & Values.BodyCollision.Right) != 0 && Direction == 2 ||
                     (collision & Values.BodyCollision.Bottom) != 0 && Direction == 3))
                {
                    if (_swordPokeCounter <= 0)
                    {
                        IsPoking = true;
                        _pokeStart = true;

                        Animation.Play("poke_" + Direction);
                        AnimatorWeapons.Play("poke_" + Direction);
                        CurrentState = State.Attacking;
                        _swordChargeCounter = SwordChargeTime;
                    }

                    _swordPokeCounter -= Game1.DeltaTime;
                }
            }
        }

        private void OnHolePull(Vector2 direction, float percentage)
        {
            // disable jumping if the player stands on top of a hole
            // if the hole is 14x14 the player should not be able to stand between the holes and jump out of them
            // player 8x10
            // hole area while standing between 4 14x14 holes: 2x10 + 2x8 = 36
            // 1 - 36/80 = 0.55
            if (percentage >= 0.55f)
                _canJump = false;
        }

        private void Update3D()
        {
            _isWalking = false;
            WasHoleReset = false;

            if (CurrentState == State.Intro)
            {
                var walkVelocity = ControlHandler.GetMoveVector2();

                if (Animation.CurrentAnimation.Id == "intro_sit" &&
                    !Game1.GameManager.InGameOverlay.TextboxOverlay.IsOpen && walkVelocity.Length() > Values.ControllerDeadzone)
                {
                    CurrentState = State.Idle;
                    Direction = 2;
                    StartRailJump(EntityPosition.Position + new Vector2(12, 4), 1, 1);
                    Animation.Play("intro_jump");

                    Game1.GameManager.SaveManager.SetString("played_intro", "1");
                }

                return;
            }

            // finished jumping into the bed?
            if (_startBedTransition && CurrentState == State.Idle)
            {
                CurrentState = State.BedTransition;

                _startBedTransition = false;

                Animation.Play("bed");
            }

            if (CurrentState == State.BedTransition)
                return;

            if (CurrentState == State.SwordShow0)
            {
                if (!Animation.IsPlaying)
                {
                    Animation.Play("show2");
                    _showSwordLv2Counter = 500;
                    CurrentState = State.SwordShow1;

                    Game1.GameManager.PlaySoundEffect("D360-07-07");

                    var animation = new ObjAnimator(Map, 0, 0, Values.LayerTop, "Particles/swordPoke", "run", true);
                    animation.EntityPosition.Set(new Vector2(
                        BodyRectangle.X,
                        EntityPosition.Y - EntityPosition.Z - 30));
                    Map.Objects.SpawnObject(animation);
                }
                else
                    return;
            }
            else if (CurrentState == State.SwordShow1)
            {
                _showSwordLv2Counter -= Game1.DeltaTime;
                if (_showSwordLv2Counter < 0)
                    CurrentState = State.Idle;
            }

            if (_isRafting && (CurrentState == State.Rafting || CurrentState == State.Charging))
            {
                var moveVelocity = ControlHandler.GetMoveVector2();

                var moveVelocityLength = moveVelocity.Length();
                if (moveVelocityLength > 1)
                    moveVelocity.Normalize();

                if (moveVelocityLength > Values.ControllerDeadzone)
                {
                    _isWalking = true;
                    _objRaft.TargetVelocity(moveVelocity * 0.5f);

                    if (CurrentState != State.Charging)
                    {
                        var vectorDirection = ToDirection(moveVelocity);
                        Direction = vectorDirection;
                    }
                }
            }

            if (_isFlying && CurrentState == State.Carrying)
            {
                var moveVelocity = ControlHandler.GetMoveVector2();

                var moveVelocityLength = moveVelocity.Length();
                if (moveVelocityLength > 1)
                    moveVelocity.Normalize();

                if (moveVelocityLength > Values.ControllerDeadzone)
                {
                    _objRooster.TargetVelocity(moveVelocity, 0.5f, Direction);

                    var vectorDirection = ToDirection(moveVelocity);
                    Direction = vectorDirection;
                }
            }

            // we need to prevent overlays from being opened because they do not stop the music and it would run out of sync
            if ((ShowItem != null && ShowItem.Name.StartsWith("instrument")) ||
                CurrentState == State.ShowInstrumentPart0 ||
                CurrentState == State.ShowInstrumentPart1 ||
                CurrentState == State.ShowInstrumentPart2 ||
                CurrentState == State.ShowInstrumentPart3)
                Game1.GameManager.InGameOverlay.DisableInventoryToggle = true;

            if (CurrentState == State.ShowInstrumentPart0)
            {
                // is the sound effect still playing?
                if (_instrumentPickupTime + 7500 < Game1.TotalGameTime)
                {
                    Game1.GameManager.SetMusic(_instrumentMusicIndex[_instrumentIndex], 2);
                    Game1.GbsPlayer.Play();
                    Game1.GbsPlayer.SoundGenerator.SetStopTime(8);
                    CurrentState = State.ShowInstrumentPart1;
                }
            }
            else if (CurrentState == State.ShowInstrumentPart1)
            {
                _instrumentCounter += Game1.DeltaTime;

                if (_instrumentCounter > 3500)
                {
                    _drawInstrumentEffect = true;
                    Game1.GameManager.PlaySoundEffect("D360-43-2B", false);
                }

                if (Game1.GbsPlayer.SoundGenerator.WasStopped && Game1.GbsPlayer.SoundGenerator.FinishedPlaying())
                {
                    Game1.GameManager.SetMusic(-1, 0);
                    Game1.GameManager.SetMusic(-1, 2);
                    Game1.GameManager.PlaySoundEffect("D378-44-2C");

                    _instrumentCounter = 0;
                    CurrentState = State.ShowInstrumentPart2;
                }
            }
            else if (CurrentState == State.ShowInstrumentPart2)
            {
                _instrumentCounter += Game1.DeltaTime;

                var transitionSystem = (MapTransitionSystem)Game1.GameManager.GameSystems[typeof(MapTransitionSystem)];
                transitionSystem.SetColorMode(Color.White, MathHelper.Clamp(_instrumentCounter / 500f, 0, 1));

                if (_instrumentCounter > 2500)
                {
                    Direction = 3;
                    UpdateAnimation();

                    CurrentState = State.ShowInstrumentPart3;
                    ShowItem = null;
                    _drawInstrumentEffect = false;

                    Game1.GameManager.StartDialogPath($"instrument{_instrumentIndex}Collected");
                }
            }
            else if (CurrentState == State.ShowInstrumentPart3)
            {
                MapTransitionStart = EntityPosition.Position;
                MapTransitionEnd = MapTransitionStart;
                TransitionOutWalking = false;

                EndPickup();

                // append a map change
                ((MapTransitionSystem)Game1.GameManager.GameSystems[typeof(MapTransitionSystem)]).AppendMapChange(
                    "overworld.map", $"d{_instrumentIndex + 1}Finished", false, true, Color.White, true);
            }

            if (CurrentState == State.Teleporting)
            {
                if (_teleportCounterFull < 1250 || Direction <= 2)
                    _teleportCounter += Game1.DeltaTime;

                _teleportCounterFull += Game1.DeltaTime;
                var rotationSpeed = 150 - (float)Math.Sin((_teleportCounterFull / 2000f) * Math.PI) * 50;
                if (_teleportCounter > rotationSpeed)
                {
                    _teleportCounter -= rotationSpeed;
                    Direction = (Direction + 1) % 4;
                    UpdateAnimation();
                }

                var transitionSystem = (MapTransitionSystem)Game1.GameManager.GameSystems[typeof(MapTransitionSystem)];

                if (_teleportState == 0 && _teleportCounterFull >= 1250)
                {
                    if (_teleporter != null)
                    {
                        _teleportState = 1;

                        EntityPosition.Set(_teleporter.TeleportPosition);
                        _teleporter.Lock();

                        var goalPosition = Game1.GameManager.MapManager.GetCameraTarget();
                        MapManager.Camera.SoftUpdate(goalPosition);
                    }
                    else if (Direction == 3 && _teleportCounterFull >= 1450)
                    {
                        MapTransitionStart = EntityPosition.Position;
                        MapTransitionEnd = EntityPosition.Position;
                        TransitionOutWalking = false;

                        transitionSystem.AppendMapChange(_teleportMap, _teleporterId, false, true, Color.White, true);
                    }

                    transitionSystem.SetColorMode(Color.White, 1);
                }

                var fadeOutTime = 250.0f;
                var fadeoutStart = 1750;
                var fadeoutEnd = 1750 + fadeOutTime;

                // fading in
                if (_teleportCounterFull >= 750 && _teleportCounterFull < 1250)
                {
                    transitionSystem.SetColorMode(Color.White, (_teleportCounterFull - 750) / 500f);
                }
                // fading out
                else if (_teleportState == 1 && _teleportCounterFull >= fadeoutStart && _teleportCounterFull < fadeoutEnd)
                {
                    transitionSystem.SetColorMode(Color.White, 1 - (_teleportCounterFull - fadeoutStart) / fadeOutTime);
                }
                // finished?
                else if (_teleportState == 1 && _teleportCounterFull >= fadeoutEnd)
                {
                    _drawBody.Layer = Values.LayerPlayer;
                    transitionSystem.SetColorMode(Color.White, 0);
                    CurrentState = State.Idle;
                }
            }

            UpdateSwimming();

            UpdateIgnoresZ();

            // hinox should throw the player farther than normal
            if (CurrentState == State.Stunned)
                _body.DragAir = 0.95f;
            else
                _body.DragAir = 0.9f;

            // save the last position the player is grounded to use for the reset position if the player drowns
            if (CurrentState != State.Jumping && CurrentState != State.Drowning && CurrentState != State.Drowned && _body.IsGrounded)
            {
                var bodyCenter = new Vector2(EntityPosition.X, EntityPosition.Y - _body.Height / 2f);
                // center the position
                // can lead to the position being inside something
                bodyCenter.X = (int)(bodyCenter.X / 16) * 16 + 8;
                bodyCenter.Y = (int)(bodyCenter.Y / 16) * 16 + 8 + _body.Height / 2f;

                // found new reset position?
                if (!Map.GetFieldState(bodyCenter).HasFlag(MapStates.FieldStates.DeepWater))
                {
                    var bodyBox = new Box(
                        bodyCenter.X + _body.OffsetX,
                        bodyCenter.Y + _body.OffsetY, 0, _body.Width, _body.Height, _body.Depth);
                    var cBox = Box.Empty;

                    // check it the player is not standing inside something
                    if (!Map.Objects.Collision(bodyBox, Box.Empty, _body.CollisionTypes | Values.CollisionTypes.DrownExclude, 0, 0, ref cBox))
                        _drownResetPosition = bodyCenter;
                }
            }

            // walk
            UpdateWalking();

            if (CurrentState == State.Drowning)
            {
                if (_drownCounter < 300)
                {
                    _body.Velocity = Vector3.Zero;
                    // align the player to the pixel grid
                    EntityPosition.Set(new Vector2(
                        MathF.Round(EntityPosition.X), MathF.Round(EntityPosition.Y)));
                }

                _drownCounter -= Game1.DeltaTime;
                if (_drownCounter <= 0)
                {
                    IsVisible = false;
                    CurrentState = State.Drowned;
                    _drownResetCounter = 500;
                }
            }

            if (CurrentState == State.Drowned)
            {
                _drownResetCounter -= Game1.DeltaTime;
                if (_drownResetCounter <= 0)
                {
                    CurrentState = State.Idle;
                    CanWalk = true;
                    IsVisible = true;

                    _hitCount = CooldownTime;
                    Game1.GameManager.CurrentHealth -= 2;

                    _body.CurrentFieldState = MapStates.FieldStates.None;
                    EntityPosition.Set(_drownResetPosition);
                }
            }

            if (CurrentState == State.Swimming)
            {
                if (_diveCounter > -100)
                {
                    _diveCounter -= Game1.DeltaTime;

                    // stop diving
                    if (ControlHandler.ButtonPressed(CButtons.B))
                        _diveCounter = 0;
                }
                // start diving
                else if (ControlHandler.ButtonPressed(CButtons.B))
                {
                    StartDiving(1500);
                }

                if (_swimBoostCount > -300)
                    _swimBoostCount -= Game1.DeltaTime;
                else if (ControlHandler.ButtonPressed(CButtons.A))
                    _swimBoostCount = 300;

                if (_swimBoostCount > 0)
                    _moveVelocity *= SwimSpeedA;
                else
                    _moveVelocity *= SwimSpeed;

                var distance = _moveVelocity - _swimVelocity;
                var length = distance.Length();
                if (distance != Vector2.Zero)
                    distance.Normalize();

                if (length < 0.045f)
                    _swimVelocity = _moveVelocity;
                else
                    _swimVelocity += distance * (_swimBoostCount > 0 ? 0.06f : 0.045f) * Game1.TimeMultiplier;

                _moveVelocity = _swimVelocity;
            }

            // slows down the walk movement when the player is hit
            var moveMultiplier = MathHelper.Clamp(1f - _hitVelocity.Length(), 0, 1);

            // move the player
            if (CurrentState != State.Hookshot)
            {
                _body.VelocityTarget = _moveVelocity * moveMultiplier + _hitVelocity;
            }

            LastMoveVector = _moveVelocity;
            _moveVelocity = Vector2.Zero;

            if (_hitCount > 0 && _hitVelocity.Length() > 0.05f * Game1.TimeMultiplier)
            {
                var hitNormal = _hitVelocity;
                hitNormal.Normalize();

                var slowDownAmount = 0.05f + MathHelper.Clamp(_hitVelocity.Length() / 25f, 0, 0.05f);

                _hitVelocity -= hitNormal * slowDownAmount * Game1.TimeMultiplier;
            }
            else
                _hitVelocity = Vector2.Zero;

            // update the jump logic
            UpdateJump();

            // hole falling logic
            {
                // update position used to reset the player if he falls into a hole
                UpdateSavePosition();

                // change the room?
                if (_isFallingIntoHole)
                {
                    _holeFallCounter -= Game1.DeltaTime;

                    if (_holeFallCounter <= 0)
                    {
                        _isFallingIntoHole = false;

                        if (HoleResetRoom != null)
                        {
                            // append a map change
                            ((MapTransitionSystem)Game1.GameManager.GameSystems[
                                typeof(MapTransitionSystem)]).AppendMapChange(HoleResetRoom, HoleResetEntryId);
                        }
                        // teleport on hole fall?
                        else if (HoleTeleporterId >= 0)
                        {
                            _holeTeleportCounter = 0;
                            CurrentState = State.TeleporterUpWait;
                        }
                    }
                }

                HoleTeleporterId = -1;

                // finished falling down the hole?
                if (CurrentState == State.Falling && !Animation.IsPlaying)
                    OnHoleReset();
            }

            // update links animation
            UpdateAnimation();

            UpdateGhostSpawn();

            // stop push animation
            if (CurrentState == State.Pushing)
                CurrentState = State.Idle;

            _lastFieldState = _body.CurrentFieldState;
        }

        private void UpdateSwimming()
        {
            // we cant use the field state of the body because the raft updates the state while exiting
            var fieldState = SystemBody.GetFieldState(_body);

            // start/stop swimming or drowning
            if (!_isRafting && !_isFlying && fieldState.HasFlag(MapStates.FieldStates.DeepWater) && CurrentState != State.Dying)
            {
                if (CurrentState != State.Jumping && _body.IsGrounded && CurrentState != State.PickingUp)
                {
                    ReleaseCarriedObject();
                    var inLava = fieldState.HasFlag(MapStates.FieldStates.Lava);

                    if ((HasFlippers && !inLava) && CurrentState != State.Swimming)
                    {
                        CurrentState = State.Swimming;

                        // only push the player if he walks into the water and does not jump
                        if (!_lastFieldState.HasFlag(fieldState))
                            _body.Velocity = new Vector3(_body.VelocityTarget.X, _body.VelocityTarget.Y, 0) * 0.75f;

                        // splash effect
                        var splashAnimator = new ObjAnimator(Map, 0, 0, 0, 3, Values.LayerPlayer, "Particles/splash", "idle", true);
                        splashAnimator.EntityPosition.Set(new Vector2(
                            _body.Position.X + _body.OffsetX + _body.Width / 2f,
                            _body.Position.Y + _body.OffsetY + _body.Height - _body.Position.Z - 6));
                        Map.Objects.SpawnObject(splashAnimator);

                        Game1.GameManager.PlaySoundEffect("D360-14-0E");

                        _diveCounter = 0;
                        _swimBoostCount = 0;
                        _swimVelocity = Vector2.Zero;
                    }
                    else if (!HasFlippers || inLava)
                    {
                        if (CurrentState != State.Drowning && CurrentState != State.Drowned)
                        {
                            // only push the player if he walks into the water and does not jump
                            if (!_lastFieldState.HasFlag(fieldState))
                                _body.Velocity = new Vector3(_body.VelocityTarget.X, _body.VelocityTarget.Y, 0) * 0.5f;

                            // splash effect
                            var splashAnimator = new ObjAnimator(Map, 0, 0, 0, 3, Values.LayerPlayer, "Particles/splash", "idle", true);
                            splashAnimator.EntityPosition.Set(new Vector2(
                                _body.Position.X + _body.OffsetX + _body.Width / 2f,
                                _body.Position.Y + _body.OffsetY + _body.Height - _body.Position.Z - 6));
                            Map.Objects.SpawnObject(splashAnimator);

                            Game1.GameManager.PlaySoundEffect("D370-03-03");

                            CurrentState = State.Drowning;
                            _drownCounter = 650;

                            // blink in lava
                            _hitCount = inLava ? CooldownTime : 0;
                        }
                    }
                }
            }
            else if (CurrentState == State.Swimming && (!IsTransitioning || !Map.Is2dMap))
                CurrentState = State.Idle;

            if (CurrentState == State.Swimming)
            {
                EntityPosition.Z = 0;
                _body.IsGrounded = true;
            }
        }

        private void UpdateIgnoresZ()
        {
            if (CurrentState == State.Swimming ||
                CurrentState == State.Hookshot ||
                CurrentState == State.TeleporterUp ||
                CurrentState == State.TeleportFallWait || _isFlying || _isGrabbed || _isClimbing)
                _body.IgnoresZ = true;
            else
                _body.IgnoresZ = false;
        }

        private void UpdateWalking()
        {
            if (CurrentState != State.Idle && (CurrentState != State.Carrying || _isFlying) && CurrentState != State.Charging && CurrentState != State.Swimming &&
                CurrentState != State.CarryingItem && (CurrentState != State.MagicRod || _body.IsGrounded) && (CurrentState != State.Jumping || _railJump) &&
                CurrentState != State.Pushing && CurrentState != State.Blocking && CurrentState != State.Attacking ||
                !CanWalk || _isRafting) return;

            var walkVelocity = Vector2.Zero;
            if (!_isLocked && (CurrentState != State.Attacking || !_body.IsGrounded))
                walkVelocity = ControlHandler.GetMoveVector2();

            var walkVelLength = walkVelocity.Length();
            if (walkVelLength > 1)
                walkVelocity.Normalize();

            var vectorDirection = ToDirection(walkVelocity);

            if (_bootsRunning && (walkVelLength < Values.ControllerDeadzone || vectorDirection != (Direction + 2) % 4))
            {
                if (!_bootsStop)
                {
                    _moveVelocity = AnimationHelper.DirectionOffset[Direction] * BootsRunningSpeed;

                    // can move up or down while running
                    if (Direction % 2 != 0)
                        _moveVelocity.X += walkVelocity.X;
                    else if (Direction % 2 == 0)
                        _moveVelocity.Y += walkVelocity.Y;
                }
            }
            else if (walkVelLength > Values.ControllerDeadzone)
            {
                _bootsCounter %= _bootsParticleTime;
                _bootsRunning = false;

#if DEBUG
                if (InputHandler.KeyDown(Keys.LeftShift))
                    walkVelocity *= 0.25f;
#endif

                // slow down in the grass
                if (_body.CurrentFieldState.HasFlag(MapStates.FieldStates.Grass) && _body.IsGrounded)
                    _currentWalkSpeed *= 0.8f;

                // slow down in the water
                if (_body.CurrentFieldState.HasFlag(MapStates.FieldStates.Water) && _body.IsGrounded)
                {
                    _currentWalkSpeed *= 0.8f;

                    _waterSoundCounter += Game1.DeltaTime;
                    if (_waterSoundCounter > 250)
                    {
                        _waterSoundCounter -= 250;
                        Game1.GameManager.PlaySoundEffect("D360-14-0E", false);
                    }
                }

                // do not walk when trapped
                if (!_isTrapped)
                {
                    _isWalking = true;

                    if (_body.IsGrounded)
                    {
                        // after hitting the ground we still have _lastMoveVelocity
                        if (!_body.WasGrounded)
                            _moveVelocity = Vector2.Zero;

                        _moveVelocity += walkVelocity * _currentWalkSpeed;
                    }
                }

                // update the direction the player is facing
                if (CurrentState != State.Attacking && CurrentState != State.Charging)
                    Direction = vectorDirection;
            }

            _lastBaseMoveVelocity = _moveVelocity;

            // when we walk of a cliff set the air move vector
            // we need to make sure that the player did not started jumping
            if (!_startedJumping && !_hasStartedJumping && _body.WasGrounded && !_body.IsGrounded)
                _lastMoveVelocity = _moveVelocity;

            // the player has momentum when he is in the air and can not be controlled directly like on the ground
            if (!_body.IsGrounded || _body.Velocity.Z > 0)
            {
                var distance = (_lastMoveVelocity - walkVelocity * _currentWalkSpeed).Length();

                // trying to move in the air? => slowly change the direction in the air
                if (distance > 0 && walkVelocity != Vector2.Zero)
                {
                    var amount = Math.Clamp((0.05f / distance) * Game1.TimeMultiplier, 0, 1);
                    _lastMoveVelocity = Vector2.Lerp(_lastMoveVelocity, walkVelocity * _currentWalkSpeed, amount);
                }

                _moveVelocity = _lastMoveVelocity;
            }
        }

        private void UpdateAnimation()
        {
            if (Game1.GameManager.UseShockEffect)
                return;

            var shieldString = Game1.GameManager.ShieldLevel == 2 ? "ms_" : "s_";
            if (!CarryShield)
                shieldString = "_";

            if (_bootsHolding || _bootsRunning)
            {
                if (!_bootsRunning)
                    Animation.Play("walk" + shieldString + Direction);
                else
                {
                    // run while blocking with the shield
                    Animation.Play((CarryShield ? "walkb" : "walk") + shieldString + Direction);
                }

                Animation.SpeedMultiplier = 2.0f;
                return;
            }

            Animation.SpeedMultiplier = 1.0f;

            if (CurrentState == State.Idle && !_isWalking ||
                CurrentState == State.Charging && !_isWalking ||
                CurrentState == State.Rafting && !_isWalking ||
                CurrentState == State.Teleporting ||
                CurrentState == State.ShowInstrumentPart3 ||
                CurrentState == State.TeleportFall ||
                CurrentState == State.TeleporterUp ||
                CurrentState == State.FallRotateEntry)
                Animation.Play("stand" + shieldString + Direction);
            else if ((
                CurrentState == State.Idle ||
                CurrentState == State.Charging ||
                CurrentState == State.Rafting) && _isWalking)
                Animation.Play("walk" + shieldString + Direction);
            else if (CurrentState == State.Blocking)
                Animation.Play((!_isWalking ? "standb" : "walkb") + shieldString + Direction);
            else if ((CurrentState == State.Carrying || CurrentState == State.CarryingItem) && !_isFlying)
                Animation.Play((!_isWalking ? "standc_" : "walkc_") + Direction);
            else if (CurrentState == State.Carrying && _isFlying)
                Animation.Play("flying_" + Direction);
            else if (CurrentState == State.Pushing)
                Animation.Play("push_" + Direction);
            else if (CurrentState == State.Grabbing)
                Animation.Play("grab_" + Direction);
            else if (CurrentState == State.Pulling)
                Animation.Play("pull_" + Direction);
            else if (CurrentState == State.Swimming)
            {
                Animation.Play(_diveCounter > 0 ? "dive" : "swim_" + Direction);

                if (_swimVelocity.Length() < 0.1 && !IsTransitioning)
                    Animation.IsPlaying = false;
            }
            else if (CurrentState == State.Drowning)
                Animation.Play(_drownCounter > 300 ? "swim_" + Direction : "dive");
        }

        private void UpdateHeartWarningSound()
        {
            if (Game1.GameManager.CurrentHealth <= 4)
            {

            }

        }

        private void UpdateDive()
        {
            _diveCounter -= Game1.DeltaTime;
        }

        private void UpdateDamageShader()
        {
            if (_hitCount > 0)
                _sprite.SpriteShader = (CooldownTime - _hitCount) % (BlinkTime * 2) < BlinkTime ? Resources.DamageSpriteShader0 : null;
            else
                _sprite.SpriteShader = null;
        }

        private void UpdateSavePosition()
        {
            var bodyCenter = _body.BodyBox.Box.Center;
            var currentTilePosition = new Point(((int)bodyCenter.X - Map.MapOffsetX * 16) / 160, ((int)bodyCenter.Y - Map.MapOffsetY * 16) / 128);
            var tileDiff = currentTilePosition - _lastTilePosition;
            var newResetPosition = _holeResetPoint;
            _lastTilePosition = currentTilePosition;

            // update position?
            if (tileDiff != Point.Zero)
            {
                var tileSize = 16;
                _alternativeHoleResetPosition = Vector2.Zero;

                if (tileDiff.X == 0)
                    newResetPosition.X = EntityPosition.X;
                else
                {
                    if (tileDiff.X > 0)
                        newResetPosition.X = (int)(bodyCenter.X / tileSize) * tileSize;
                    else
                        newResetPosition.X = (int)(bodyCenter.X / tileSize + 1) * tileSize;
                }

                if (tileDiff.Y == 0)
                    newResetPosition.Y = EntityPosition.Y;
                else
                {
                    if (tileDiff.Y > 0)
                        newResetPosition.Y = (int)(bodyCenter.Y / tileSize) * tileSize;
                    else
                        newResetPosition.Y = (int)(bodyCenter.Y / tileSize + 1) * tileSize;
                }

                // check if there is no hole at the new position
                var bodyBox = new Box(newResetPosition.X + _body.BodyBox.OffsetX, newResetPosition.Y + _body.BodyBox.OffsetY, 0, _body.Width, _body.Height, 8);
                var outBox = Box.Empty;
                if (!Map.Objects.Collision(bodyBox, Box.Empty, Values.CollisionTypes.Hole, 0, 0, ref outBox))
                    _holeResetPoint = newResetPosition;
            }
        }

        public void UpdateSaveLocation()
        {
            MapManager.ObjLink.SaveMap = Map.MapName;
            MapManager.ObjLink.SavePosition = EntityPosition.Position;
            MapManager.ObjLink.SaveDirection = Direction;
        }

        private void SetHoleResetPosition(Vector2 newResetPosition)
        {
            _holeResetPoint = newResetPosition;

            var offset = Map != null ? new Point(Map.MapOffsetX, Map.MapOffsetY) : Point.Zero;
            _lastTilePosition = new Point(((int)newResetPosition.X - offset.X * 16) / 160, ((int)newResetPosition.Y - offset.Y * 16) / 128);
        }

        private void UpdateDrawComponents()
        {
            if (_drawInstrumentEffect)
                _drawBody.Layer = Values.LayerTop;
            else
                _drawBody.Layer = (CurrentState == State.Swimming && _diveCounter > 0) ? Values.LayerBottom : Values.LayerPlayer;

            if (CurrentState == State.Swimming && _diveCounter > 0 ||
               CurrentState == State.Drowning ||
               CurrentState == State.Drowned ||
               CurrentState == State.BedTransition ||
               _isTrapped)
                _shadowComponent.IsActive = false;
            else
                _shadowComponent.IsActive = true;
        }

        private void StartDiving(int diveTime)
        {
            // splash effect
            var splashAnimator = new ObjAnimator(Map, 0, 0, 0, 0, Values.LayerTop, "Particles/splash", "idle", true);
            splashAnimator.EntityPosition.Set(new Vector2(
                _body.Position.X + _body.OffsetX + _body.Width / 2f,
                _body.Position.Y + _body.OffsetY + _body.Height - _body.Position.Z - 3));
            Map.Objects.SpawnObject(splashAnimator);

            Game1.GameManager.PlaySoundEffect("D360-14-0E");

            _diveCounter = diveTime;
        }

        private void OnHoleReset()
        {
            // change the room?
            if (HoleResetRoom != null)
                return;

            _isFallingIntoHole = false;

            CurrentState = State.Idle;
            CanWalk = true;

            _hitCount = CooldownTime;
            Game1.GameManager.InflictDamage(2);

            MoveToHoleResetPosition();
        }

        private void MoveToHoleResetPosition()
        {
            WasHoleReset = true;
            EntityPosition.Set(_holeResetPoint);

            // alternative reset point
            var cBox = Box.Empty;
            if (_alternativeHoleResetPosition != Vector2.Zero &&
                Map.Objects.Collision(_body.BodyBox.Box, Box.Empty, _body.CollisionTypes, 0, 0, ref cBox))
            {
                EntityPosition.Set(_alternativeHoleResetPosition);
            }
        }

        private bool InteractWithObject()
        {
            var boxSize = 6;
            var interactionBox = new Box(
                EntityPosition.X + _walkDirection[Direction].X * (BodyRectangle.Width / 2 + boxSize / 2) - boxSize / 2,
                BodyRectangle.Center.Y + _walkDirection[Direction].Y * (BodyRectangle.Height / 2 + boxSize / 2) - boxSize / 2, 0,
                boxSize, boxSize, 16);

            return Map.Objects.InteractWithObject(interactionBox);
        }

        private void ReturnToIdle()
        {
            // Return to idle or to rafting if that was the player was rafting before
            if (_isRafting)
                CurrentState = State.Rafting;
            else
                CurrentState = State.Idle;
        }

        private void UpdateGhostSpawn()
        {
            if (!_spawnGhost || !Map.IsOverworld)
                return;

            var dungeonEntryPosition = new Vector2(1840, 272);
            var distance = MapManager.ObjLink.EntityPosition.Position - dungeonEntryPosition;
            if (MathF.Abs(distance.X) > 512 || MathF.Abs(distance.Y) > 256)
            {
                _spawnGhost = false;
                Game1.GameManager.SaveManager.RemoveString(_spawnGhostKey);
                Game1.GameManager.CollectItem(new GameItemCollected("ghost") { Count = 1 }, 0);
                UpdateFollower(false);
                _objGhost.StartFollowing();
            }
        }

        #region item stuff

        private void UpdateItem()
        {
            if (CurrentState == State.Blocking)
                CurrentState = State.Idle;
            else
                _wasBlocking = false;

            if (CurrentState == State.Grabbing || CurrentState == State.Pulling)
                CurrentState = State.Idle;

            _isPulling = false;
            _isHoldingSword = false;
            _bootsHolding = false;

            if (!_isLocked)
            {
                // interact with object
                if ((CurrentState == State.Idle || CurrentState == State.Pushing || CurrentState == State.Swimming || CurrentState == State.CarryingItem) &&
                    ControlHandler.ButtonPressed(CButtons.A) && InteractWithObject())
                    InputHandler.ResetInputState();

                if (_isTrapped && !_trappedDisableItems &&
                    (ControlHandler.ButtonPressed(CButtons.A) ||
                     ControlHandler.ButtonPressed(CButtons.B) ||
                     ControlHandler.ButtonPressed(CButtons.X) ||
                     ControlHandler.ButtonPressed(CButtons.Y)))
                {
                    _trapInteractionCount--;
                    if (_trapInteractionCount <= 0)
                        FreeTrappedPlayer();
                }

                // use/hold item
                if (!DisableItems && (!_isTrapped || !_trappedDisableItems))
                {
                    for (var i = 0; i < Values.HandItemSlots; i++)
                    {
                        if (Game1.GameManager.Equipment[i] != null &&
                            ControlHandler.ButtonPressed((CButtons)((int)CButtons.A * Math.Pow(2, i))))
                            UseItem(Game1.GameManager.Equipment[i]);

                        if (Game1.GameManager.Equipment[i] != null &&
                            ControlHandler.ButtonDown((CButtons)((int)CButtons.A * Math.Pow(2, i))))
                            HoldItem(Game1.GameManager.Equipment[i],
                                ControlHandler.LastButtonDown((CButtons)((int)CButtons.A * Math.Pow(2, i))));
                    }
                }
            }

            UpdatePegasusBoots();

            // shield pushing
            if (CurrentState == State.Blocking || _bootsRunning && CarryShield)
                UpdateShieldPush();

            // pick up animation
            if (CurrentState == State.PreCarrying)
            {
                _preCarryCounter += Game1.DeltaTime;

                // change the animation of the player depending on where the picked up object is
                if (_preCarryCounter > 100)
                    Animation.Play("standc_" + Direction);

                UpdatePositionCarriedObject(EntityPosition);
            }

            // stop attacking
            if (CurrentState == State.Attacking && !Animation.IsPlaying)
            {
                _isSwingingSword = false;

                if (!_isHoldingSword || _swordPoked || _stopCharging)
                    ReturnToIdle();
                else
                {
                    // start charging sword
                    CurrentState = State.Charging;
                    AnimatorWeapons.Play("stand_" + Direction);
                    _swordPokeCounter = _swordPokeTime;
                }
            }

            if (CurrentState == State.Charging)
                UpdateCharging();

            // hit stuff with the sword
            if (CurrentState == State.Attacking || _bootsRunning && CarrySword)
                UpdateAttacking();

            if (CurrentState == State.PickingUp)
                UpdatePickup();

            if (!Animation.IsPlaying &&
                (CurrentState == State.Powdering || CurrentState == State.Bombing || CurrentState == State.MagicRod || CurrentState == State.Throwing))
                ReturnToIdle();

            if (CurrentState == State.Hookshot)
                UpdateHookshot();

            if (CurrentState == State.Digging)
                UpdateDigging();

            _wasPulling = _isPulling;
        }

        private void UseItem(GameItemCollected item)
        {
            switch (item.Name)
            {
                case "sword1":
                case "sword2":
                    UseSword();
                    break;
                case "feather":
                    UseFeather();
                    break;
                case "toadstool":
                    UseToadstool();
                    break;
                case "powder":
                    UsePowder();
                    break;
                case "bomb":
                    UseBomb();
                    break;
                case "bow":
                    UseArrow();
                    break;
                case "shovel":
                    UseShovel();
                    break;
                case "stonelifter":
                case "stonelifter2":
                    UseStoneLifter();
                    break;
                case "hookshot":
                    UseHookshot();
                    break;
                case "boomerang":
                    UseBoomerang();
                    break;
                case "magicRod":
                    UseMagicRod();
                    break;
                case "ocarina":
                    UseOcarina();
                    break;
            }
        }

        private void HoldItem(GameItemCollected item, bool lastKeyDown)
        {
            switch (item.Name)
            {
                case "sword1":
                    HoldSword();
                    break;
                case "sword2":
                    HoldSword();
                    break;
                case "shield":
                case "mirrorShield":
                    HoldShield(lastKeyDown);
                    break;
                case "stonelifter":
                case "stonelifter2":
                    HoldStoneLifter();
                    break;
                case "pegasusBoots":
                    HoldPegasusBoots();
                    break;
            }
        }

        private void UseSword()
        {
            if (CurrentState != State.Idle && CurrentState != State.Pushing && CurrentState != State.Rafting &&
                (CurrentState != State.Jumping || _railJump) && (CurrentState != State.Swimming || !Map.Is2dMap))
                return;

            var slashSounds = new[] { "D378-02-02", "D378-20-14", "D378-21-15", "D378-24-18" };
            Game1.GameManager.PlaySoundEffect(slashSounds[Game1.RandomNumber.Next(0, 4)]);

            Animation.Play("attack_" + Direction);
            AnimatorWeapons.Play("attack_" + Direction);
            _swordChargeCounter = SwordChargeTime;
            IsPoking = false;
            _pokeStart = false;
            _stopCharging = false;
            _swordPoked = false;
            _shotSword = false;
            StopRaft();

            CurrentState = State.Attacking;
        }

        private void HoldSword()
        {
            _isHoldingSword = true;
        }

        private void UseFeather()
        {
            if (Is2DMode)
                Jump2D();
            else
                Jump();
        }

        private void UseToadstool()
        {
            CurrentState = State.ShowToadstool;
            Animation.Play("show2");
            Game1.GameManager.StartDialogPath("toadstool_hole");
        }

        private void UsePowder()
        {
            if (CurrentState != State.Idle &&
                CurrentState != State.Jumping &&
                CurrentState != State.Rafting &&
                (CurrentState != State.Swimming || !Map.Is2dMap))
                return;

            // remove one powder from the inventory
            if (!Game1.GameManager.RemoveItem("powder", 1))
                return;

            var spawnPosition = new Vector2(EntityPosition.X, EntityPosition.Y) + _powderOffset[Direction];
            Map.Objects.SpawnObject(new ObjPowder(Map, spawnPosition.X, spawnPosition.Y, EntityPosition.Z, true));

            if (CurrentState != State.Jumping)
            {
                StopRaft();

                CurrentState = State.Powdering;
                Animation.Play("powder_" + Direction);
            }
        }

        private void UseBomb()
        {
            // throw the object the player is currently carrying
            if (_carriedGameObject != null)
            {
                ThrowCarriedObject();
                return;
            }

            if (CurrentState != State.Idle &&
                CurrentState != State.Rafting &&
                (CurrentState != State.Swimming || !Map.Is2dMap))
                return;

            // pick up the bomb if there is one infront of the player
            var recInteraction = new RectangleF(
                EntityPosition.X + _walkDirection[Direction].X * (_body.Width / 2) - 4,
                EntityPosition.Y - _body.Height / 2 + _walkDirection[Direction].Y * (_body.Height / 2) - 4, 8, 8);

            // find a bomb to carry
            _bombList.Clear();
            Map.Objects.GetObjectsOfType(_bombList, typeof(ObjBomb),
                (int)recInteraction.X, (int)recInteraction.Y, (int)recInteraction.Width, (int)recInteraction.Height);

            // pick up the first bomb
            foreach (var objBomb in _bombList)
            {
                var carriableComponent = objBomb.Components[CarriableComponent.Index] as CarriableComponent;
                if (!carriableComponent.IsActive ||
                    !carriableComponent.Rectangle.Rectangle.Intersects(recInteraction))
                    continue;

                carriableComponent?.StartGrabbing?.Invoke();
                StartPickup(carriableComponent);

                Animation.Play("pull_" + Direction);

                return;
            }

            // remove one bomb from the inventory
            if (!Game1.GameManager.RemoveItem("bomb", 1))
                return;

            var spawnPosition = new Vector2(EntityPosition.X, EntityPosition.Y) + _bombOffset[Direction];
            Map.Objects.SpawnObject(new ObjBomb(Map, spawnPosition.X, spawnPosition.Y, true, false, 2000));

            CurrentState = State.Bombing;

            // play animation
            Animation.Play("powder_" + Direction);
        }

        private void UseArrow()
        {
            if (CurrentState != State.Idle &&
                CurrentState != State.Jumping &&
                CurrentState != State.Rafting &&
                CurrentState != State.Bombing &&
                (CurrentState != State.Swimming || !Map.Is2dMap))
                return;

            // remove one powder from the inventory
            if (!Game1.GameManager.RemoveItem("bow", 1))
                return;

            var spawnPosition = new Vector3(
                EntityPosition.X + _arrowOffset[Direction].X, EntityPosition.Y + _arrowOffset[Direction].Y + (Map.Is2dMap ? -4 : 0), EntityPosition.Z + (Map.Is2dMap ? 0 : 4));
            Map.Objects.SpawnObject(new ObjArrow(
                Map, spawnPosition, Direction, Game1.GameManager.PieceOfPowerIsActive ? ArrowSpeedPoP : ArrowSpeed));

            if (CurrentState != State.Jumping)
            {
                StopRaft();

                CurrentState = State.Powdering;
                Animation.Play("powder_" + Direction);
            }

            Game1.GameManager.PlaySoundEffect("D378-10-0A");
        }

        private void UseShovel()
        {
            if (CurrentState != State.Idle || _isClimbing)
                return;

            CurrentState = State.Digging;
            _hasDug = false;

            // play animation
            Animation.Play("dig_" + Direction);

            _digPosition = new Point(
                (int)((EntityPosition.X + _shovelOffset[Direction].X) / Values.TileSize),
                (int)((EntityPosition.Y + _shovelOffset[Direction].Y) / Values.TileSize));

            _canDig = Map.CanDig(_digPosition);

            if (_canDig)
                Game1.GameManager.PlaySoundEffect("D378-14-0E");
            else
                Game1.GameManager.PlaySoundEffect("D360-07-07");
        }

        private void UseStoneLifter()
        {
            if (_carriedComponent == null || CurrentState != State.Carrying)
                return;

            if (Map.Is2dMap && _isClimbing)
                return;

            ThrowCarriedObject();
        }

        private void HoldStoneLifter()
        {
            if (CurrentState != State.Idle)
                return;

            GameObject grabbedObject = null;

            if (_carriedComponent == null)
            {
                var recInteraction = new RectangleF(
                    EntityPosition.X + _walkDirection[Direction].X * (_body.Width / 2) - 1,
                    EntityPosition.Y - _body.Height / 2 + _walkDirection[Direction].Y * (_body.Height / 2) - 1, 2, 2);

                // find an object to carry
                grabbedObject = Map.Objects.GetCarryableObjects(recInteraction);
                if (grabbedObject != null)
                {
                    var carriableComponent = grabbedObject.Components[CarriableComponent.Index] as CarriableComponent;
                    if (carriableComponent.IsActive)
                    {
                        CurrentState = State.Grabbing;

                        if (!carriableComponent.IsHeavy || Game1.GameManager.StoneGrabberLevel > 1)
                            carriableComponent?.StartGrabbing?.Invoke();
                    }
                }
            }

            if (_wasPulling)
                _pullCounter += Game1.DeltaTime;
            else
                _pullCounter = 0;

            if (CurrentState == State.Grabbing)
            {
                var carriableComponent = grabbedObject.Components[CarriableComponent.Index] as CarriableComponent;

                // is the player pulling in the opposite direction?
                var moveVec = ControlHandler.GetMoveVector2();

                if (carriableComponent?.Pull != null)
                {
                    // do not continuously play the pull animation
                    if (!carriableComponent.Pull(_pullCounter > 0 ? moveVec : Vector2.Zero) && _pullCounter < 0)
                        _pullCounter = PullResetTime;
                }

                if (moveVec.Length() > 0.5)
                {
                    // pulling into the oposite direction
                    var moveDir = AnimationHelper.GetDirection(moveVec);
                    if ((moveDir + 2) % 4 == Direction)
                    {
                        // do not show the pull animation while resetting
                        if (_pullCounter >= 0)
                            CurrentState = State.Pulling;

                        _isPulling = true;

                        if (!carriableComponent.IsHeavy || Game1.GameManager.StoneGrabberLevel > 1)
                        {
                            // start carrying the object
                            if (_pullCounter >= PullTime && grabbedObject != null)
                                StartPickup(carriableComponent);

                            if (_pullCounter > PullMaxTime)
                                _pullCounter = PullResetTime;
                        }
                    }
                }
            }
        }

        private void UseHookshot()
        {
            if (CurrentState != State.Idle && CurrentState != State.Rafting && (!Map.Is2dMap || CurrentState != State.Swimming))
                return;

            var hookshotDirection = CurrentState == State.Swimming ? _swimDirection : Direction;

            var spawnPosition = new Vector3(
                EntityPosition.X + _hookshotOffset[hookshotDirection].X,
                EntityPosition.Y + _hookshotOffset[hookshotDirection].Y, EntityPosition.Z);
            Hookshot.Start(Map, spawnPosition, AnimationHelper.DirectionOffset[hookshotDirection]);
            Map.Objects.SpawnObject(Hookshot);

            CurrentState = State.Hookshot;
            _body.VelocityTarget = Vector2.Zero;
            _body.HoleAbsorption = Vector2.Zero;
            _body.IgnoreHoles = true;
            StopRaft();

            // play animation
            Animation.Play("powder_" + hookshotDirection);
        }

        private void UseBoomerang()
        {
            if ((CurrentState != State.Idle &&
                CurrentState != State.Jumping &&
                (CurrentState != State.Swimming || !Map.Is2dMap)) || !_boomerang.IsReady)
                return;

            var spawnPosition = new Vector3(EntityPosition.X + _boomerangOffset[Direction].X, EntityPosition.Y + _boomerangOffset[Direction].Y, EntityPosition.Z);

            // can throw into multiple directions
            var boomerangVector = _lastBaseMoveVelocity;
            if (boomerangVector != Vector2.Zero)
                boomerangVector.Normalize();
            else
                boomerangVector = _walkDirection[Direction];

            _boomerang.Start(Map, spawnPosition, boomerangVector);
            Map.Objects.SpawnObject(_boomerang);

            if (CurrentState != State.Jumping)
            {
                CurrentState = State.Powdering;
                Animation.Play("powder_" + Direction);
            }
        }

        private void UseMagicRod()
        {
            if (CurrentState != State.Idle &&
                CurrentState != State.Rafting &&
                (CurrentState != State.Swimming || !Map.Is2dMap) &&
                (CurrentState != State.Jumping || _railJump))
                return;

            var spawnPosition = new Vector3(EntityPosition.X + _magicRodOffset[Direction].X, EntityPosition.Y + _magicRodOffset[Direction].Y, EntityPosition.Z);
            Map.Objects.SpawnObject(new ObjMagicRodShot(Map, spawnPosition, AnimationHelper.DirectionOffset[Direction] *
                (Game1.GameManager.PieceOfPowerIsActive ? MagicRodSpeedPoP : MagicRodSpeed), Direction));

            CurrentState = State.MagicRod;
            _swordChargeCounter = SwordChargeTime;

            Game1.GameManager.PlaySoundEffect("D378-13-0D");
            StopRaft();

            // play animation
            Animation.Play("rod_" + Direction);
            AnimatorWeapons.Play("rod_" + Direction);
        }

        private void UseOcarina()
        {
            if (CurrentState != State.Idle || _isClimbing)
                return;

            _ocarinaNoteIndex = 0;
            _ocarinaCounter = 0;

            Game1.GbsPlayer.Pause();

            if (Game1.GameManager.SelectedOcarinaSong == 0)
                Game1.GameManager.PlaySoundEffect("D370-09-09");
            else if (Game1.GameManager.SelectedOcarinaSong == 1)
                Game1.GameManager.PlaySoundEffect("D370-11-0B");
            else if (Game1.GameManager.SelectedOcarinaSong == 2)
                Game1.GameManager.PlaySoundEffect("D370-10-0A");
            else
                Game1.GameManager.PlaySoundEffect("D370-21-15");

            _ocarinaSong = Game1.GameManager.SelectedOcarinaSong;
            CurrentState = State.Ocarina;
            Direction = 3;
            Animation.Play("ocarina");
        }

        private void UpdateOcarina()
        {
            if (CurrentState == State.Ocarina)
            {
                // finished playing the ocarina song?
                if (!Animation.IsPlaying)
                {
                    FinishedOcarinaSong();
                    return;
                }

                UpdateOcarinaAnimation();
            }
            else if (CurrentState == State.OcarinaTelport)
            {
                // show the animation while teleporting
                CurrentState = State.Idle;
            }
        }

        private void UpdateOcarinaAnimation()
        {
            if (CurrentState != State.Ocarina)
                return;

            _ocarinaCounter += Game1.DeltaTime;
            if (_ocarinaCounter > 100 + _ocarinaNoteIndex * 910)
            {
                _ocarinaNoteIndex++;

                var dir = _ocarinaNoteIndex % 2 == 1 ? -1 : 1;
                var objNote = new ObjNote(Map, new Vector2(EntityPosition.X + dir * 7, EntityPosition.Y), dir);
                Map.Objects.SpawnObject(objNote);
            }
        }

        private void FinishedOcarinaSong()
        {
            // continue playing music
            if (_ocarinaSong != 1)
                Game1.GbsPlayer.Play();

            if (_ocarinaSong == -1)
            {
                CurrentState = State.Idle;
                Game1.GameManager.StartDialogPath("ocarina_bad");
                return;
            }

            if (_ocarinaSong == 1)
            {
                CurrentState = State.OcarinaTelport;

                MapTransitionStart = EntityPosition.Position;
                MapTransitionEnd = EntityPosition.Position;
                TransitionOutWalking = false;

                Game1.GameManager.PlaySoundEffect("D360-44-2C");

                // load the map
                var transitionSystem = (MapTransitionSystem)Game1.GameManager.GameSystems[typeof(MapTransitionSystem)];

                if (Map.DungeonMode)
                {
                    // respawn at the dungeon entry
                    MapManager.ObjLink.SetNextMapPosition(MapManager.ObjLink.SavePosition);
                    transitionSystem.AppendMapChange(MapManager.ObjLink.SaveMap, null, false, false, Color.White, true);
                }
                else
                {
                    // append a map change
                    transitionSystem.AppendMapChange("overworld.map", "ocarina_entry", false, false, Color.White, true);
                }

                transitionSystem.StartTeleportTransition = true;

                return;
            }

            CurrentState = State.Idle;

            var recInteraction = new RectangleF(EntityPosition.X - 64, EntityPosition.Y - 64 - 8, 128, 128);

            _ocarinaList.Clear();
            Map.Objects.GetComponentList(_ocarinaList,
                (int)recInteraction.X, (int)recInteraction.Y, (int)recInteraction.Width, (int)recInteraction.Height, OcarinaListenerComponent.Mask);

            // notify ocarina listener components around the player
            foreach (var objOcarinaListener in _ocarinaList)
            {
                if (recInteraction.Contains(objOcarinaListener.EntityPosition.Position))
                {
                    var ocarinaComponent = (OcarinaListenerComponent)objOcarinaListener.Components[OcarinaListenerComponent.Index];
                    ocarinaComponent.OcarinaPlayedFunction(Game1.GameManager.SelectedOcarinaSong);
                }
            }
        }

        private void HoldShield(bool lastKeyDown)
        {
            if (CurrentState != State.Idle && CurrentState != State.Pushing)
                return;

            if (!_wasBlocking)
                Game1.GameManager.PlaySoundEffect("D378-22-16");

            _wasBlocking = true;
            CurrentState = State.Blocking;
        }

        private void HoldPegasusBoots()
        {
            if (CurrentState == State.BootKnockback || _isTrapped)
                return;

            _bootsHolding = true;
        }

        private void UpdateShieldPush()
        {
            if (Animation.CollisionRectangle.IsEmpty || _isTrapped)
                return;

            // push with the shield
            var shieldRectangle = new Box(
                EntityPosition.X + Animation.CollisionRectangle.X - 7,
                EntityPosition.Y + Animation.CollisionRectangle.Y - 16, 0,
                Animation.CollisionRectangle.Width,
                Animation.CollisionRectangle.Height, 12);

            var pushedRectangle = Map.Objects.PushObject(shieldRectangle,
                _walkDirection[Direction] + _body.VelocityTarget * 0.5f, PushableComponent.PushType.Impact);

            // get repelled from the pushed object
            if (pushedRectangle != null)
            {
                _bootsRunning = false;
                _bootsCounter = 0;

                _body.Velocity += new Vector3(
                    -_walkDirection[Direction].X * pushedRectangle.RepelMultiplier,
                    -_walkDirection[Direction].Y * pushedRectangle.RepelMultiplier, 0);

                if (pushedRectangle.RepelParticle)
                {
                    Game1.GameManager.PlaySoundEffect("D360-07-07");
                    // poke particle
                    Map.Objects.SpawnObject(new ObjAnimator(Map,
                        (int)(pushedRectangle.PushableBox.Box.X + pushedRectangle.PushableBox.Box.Width / 2),
                        (int)(pushedRectangle.PushableBox.Box.Y + pushedRectangle.PushableBox.Box.Height / 2),
                        Values.LayerTop, "Particles/swordPoke", "run", true));
                }
                else
                {
                    Game1.GameManager.PlaySoundEffect("D360-09-09");
                }
            }
        }

        private void UpdateCharging()
        {
            // stop charging
            if (_isHoldingSword)
            {
                // poke objects that walk into the sowrd
                RectangleF collisionRectangle = AnimatorWeapons.CollisionRectangle;
                var damageOrigin = BodyRectangle.Center;
                SwordDamageBox = new Box(
                    collisionRectangle.X + EntityPosition.X + _animationOffsetX,
                    collisionRectangle.Y + EntityPosition.Y - EntityPosition.Z + _animationOffsetY, 0,
                    collisionRectangle.Width,
                    collisionRectangle.Height, 4);

                var hitType = Game1.GameManager.SwordLevel == 1 ? HitType.Sword1 : HitType.Sword2;
                var damage = Game1.GameManager.SwordLevel == 1 ? 1 : 2;

                // red cloak doubles damage
                if (Game1.GameManager.CloakType == GameManager.CloakRed)
                    damage *= 2;
                // piece of power double the damage
                if (Game1.GameManager.PieceOfPowerIsActive)
                    damage *= 2;

                var pieceOfPower = Game1.GameManager.PieceOfPowerIsActive || Game1.GameManager.CloakType == GameManager.CloakRed;
                var hitCollision = Map.Objects.Hit(this, damageOrigin, SwordDamageBox, hitType | HitType.SwordHold, damage, pieceOfPower, out var direction, true);
                // start poking?
                if (hitCollision != Values.HitCollision.None &&
                    hitCollision != Values.HitCollision.NoneBlocking)
                {
                    _swordPoked = true;
                    Animation.Play("poke_" + Direction);
                    AnimatorWeapons.Play("poke_" + Direction);
                    CurrentState = State.Attacking;

                    // get repelled
                    RepelPlayer(hitCollision, direction);
                }
                else if (_swordChargeCounter > 0)
                {
                    _swordChargeCounter -= Game1.DeltaTime;

                    // finished charging?
                    if (_swordChargeCounter <= 0)
                        Game1.GameManager.PlaySoundEffect("D360-04-04");
                }
            }
            else
            {
                // start charge attack
                if (_swordChargeCounter <= 0)
                    StartSwordSpin();
                else
                    ReturnToIdle();
            }
        }

        private void StartSwordSpin()
        {
            CurrentState = State.Attacking;

            Animation.Play("swing_" + Direction);
            AnimatorWeapons.Play("swing_" + Direction);

            Game1.GameManager.PlaySoundEffect("D378-03-03");

            _swordChargeCounter = SwordChargeTime;
            _isSwingingSword = true;
        }

        private void UpdateAttacking()
        {
            if (_bootsRunning && CarrySword)
                AnimatorWeapons.Play("stand_" + Direction);

            if (AnimatorWeapons.CollisionRectangle.IsEmpty)
                return;

            var damageOrigin = BodyRectangle.Center;
            if (Map.Is2dMap)
                damageOrigin.Y -= 4;

            RectangleF collisionRectangle = AnimatorWeapons.CollisionRectangle;

            // this lerps the collision box between frames
            // a rotation collision box would probably be a better option
            if (AnimatorWeapons.CurrentAnimation.Frames.Length > AnimatorWeapons.CurrentFrameIndex + 1)
            {
                var frameState = (float)(AnimatorWeapons.FrameCounter / AnimatorWeapons.CurrentFrame.FrameTime);
                var collisionRectangleNextFrame = AnimatorWeapons.GetCollisionBox(
                    AnimatorWeapons.CurrentAnimation.Frames[AnimatorWeapons.CurrentFrameIndex + 1]);
                collisionRectangle = new RectangleF(
                    MathHelper.Lerp(collisionRectangle.X, collisionRectangleNextFrame.X, frameState),
                    MathHelper.Lerp(collisionRectangle.Y, collisionRectangleNextFrame.Y, frameState),
                    MathHelper.Lerp(collisionRectangle.Width, collisionRectangleNextFrame.Width, frameState),
                    MathHelper.Lerp(collisionRectangle.Height, collisionRectangleNextFrame.Height, frameState));
            }

            SwordDamageBox = new Box(
                collisionRectangle.X + EntityPosition.X + _animationOffsetX,
                collisionRectangle.Y + EntityPosition.Y - EntityPosition.Z + _animationOffsetY, 0,
                collisionRectangle.Width,
                collisionRectangle.Height, 4);

            var hitType = _bootsRunning ? HitType.PegasusBootsSword :
                (Game1.GameManager.SwordLevel == 1 ? HitType.Sword1 : HitType.Sword2);

            var damage = Game1.GameManager.SwordLevel == 1 ? 1 : 2;

            if (_isSwingingSword)
            {
                damage *= 2;
                hitType |= HitType.SwordSpin;
            }

            if (_bootsRunning)
                damage *= 2;

            // piece of power double the damage
            if (Game1.GameManager.PieceOfPowerIsActive)
                damage *= 2;

            // red cloak doubles the damage
            if (Game1.GameManager.CloakType == GameManager.CloakRed)
                damage *= 2;

            var pieceOfPower = Game1.GameManager.PieceOfPowerIsActive || Game1.GameManager.SwordLevel == 2;
            var hitCollision = Map.Objects.Hit(this, damageOrigin, SwordDamageBox, hitType, damage, pieceOfPower, out var direction, true);

            if (_pokeStart)
            {
                _pokeStart = false;

                if (hitCollision != Values.HitCollision.NoneBlocking)
                {
                    var swordRectangle = AnimatorWeapons.CollisionRectangle;
                    var swordBox = new Box(
                        swordRectangle.X + EntityPosition.X + _animationOffsetX,
                        swordRectangle.Y + EntityPosition.Y - EntityPosition.Z + _animationOffsetY, 0,
                        swordRectangle.Width, swordRectangle.Height, 4);
                    var destroyableWall = DestroyableWall(swordBox);

                    if (destroyableWall)
                        Game1.GameManager.PlaySoundEffect("D378-23-17");
                    else
                        Game1.GameManager.PlaySoundEffect("D360-07-07");

                    var pokeParticle = new ObjAnimator(Map, 0, 0, Values.LayerTop, "Particles/swordPoke", "run", true);
                    pokeParticle.EntityPosition.X = EntityPosition.X + _pokeAnimationOffset[Direction].X;
                    pokeParticle.EntityPosition.Y = EntityPosition.Y + _pokeAnimationOffset[Direction].Y;
                    Map.Objects.SpawnObject(pokeParticle);
                }
            }

            if (hitCollision != Values.HitCollision.None && hitCollision != Values.HitCollision.NoneBlocking)
                _stopCharging = true;

            // shoot the sword if the player has the l2 sword and full health
            if (!_shotSword && Game1.GameManager.SwordLevel == 2 && Game1.GameManager.CurrentHealth >= Game1.GameManager.MaxHearths * 4 && AnimatorWeapons.CurrentFrameIndex == 2)
            {
                _shotSword = true;

                var spawnPosition = new Vector3(EntityPosition.X + _shootSwordOffset[Direction].X, EntityPosition.Y + _shootSwordOffset[Direction].Y - EntityPosition.Z, 0);
                var objSwordShot = new ObjSwordShot(Map, spawnPosition, Direction);
                Map.Objects.SpawnObject(objSwordShot);
            }

            // spawn hit particle?
            if ((hitCollision & Values.HitCollision.Particle) != 0 && _hitParticleTime + 225 < Game1.TotalGameTime)
            {
                _hitParticleTime = Game1.TotalGameTime;
                SwordPoke(collisionRectangle);
            }

            RepelPlayer(hitCollision, direction);
        }

        private void RepelPlayer(Values.HitCollision collisionType, Vector2 direction)
        {
            // repel the player
            if ((collisionType & Values.HitCollision.Repelling) != 0 &&
                _hitRepelTime + 225 < Game1.TotalGameTime)
            {
                _hitRepelTime = Game1.TotalGameTime;

                var multiplier = Map.Is2dMap ? 1.5f : (_bootsRunning ? 1.5f : 1.0f);

                if ((collisionType & Values.HitCollision.Repelling0) != 0)
                    multiplier = 3.00f;
                if ((collisionType & Values.HitCollision.Repelling1) != 0)
                    multiplier = 2.25f;

                if (_bootsRunning)
                    _bootsStop = true;

                _body.Velocity += new Vector3(-direction.X, -direction.Y, 0) * multiplier;
            }
        }

        private void SwordPoke(RectangleF collisionRectangle)
        {
            Game1.GameManager.PlaySoundEffect("D360-07-07");

            // poke particle
            Map.Objects.SpawnObject(new ObjAnimator(Map,
                (int)(EntityPosition.X - 8 + collisionRectangle.X + collisionRectangle.Width / 2),
                (int)(EntityPosition.Y - 15 + collisionRectangle.Y + collisionRectangle.Height / 2),
                Values.LayerTop, "Particles/swordPoke", "run", true));
        }

        private void UpdatePickup()
        {
            if (ShowItem == null)
                return;

            _itemShowCounter -= Game1.DeltaTime;

            if (_itemShowCounter <= 0)
            {
                // show pick up text
                if (_showItem && CurrentState == State.PickingUp)
                {
                    _showItem = false;

                    // show pickup dialog
                    if (ShowItem.PickUpDialog != null)
                    {
                        if (string.IsNullOrEmpty(_pickupDialogOverride))
                            Game1.GameManager.StartDialogPath(ShowItem.PickUpDialog);
                        else
                        {
                            Game1.GameManager.StartDialogPath(_pickupDialogOverride);
                            _pickupDialogOverride = null;
                        }

                        if (!string.IsNullOrEmpty(_additionalPickupDialog))
                        {
                            Game1.GameManager.StartDialogPath(_additionalPickupDialog);
                            _additionalPickupDialog = null;
                        }
                    }

                    _itemShowCounter = 250;

                    if (ShowItem.Name == "sword1")
                        _itemShowCounter = 5850;
                    else if (ShowItem.Name.StartsWith("instrument"))
                        _itemShowCounter = 1000;
                }
                else
                {
                    Game1.GameManager.SaveManager.SetString("player_shows_item", "0");

                    // add the item to the inventory
                    if (_collectedShowItem != null)
                    {
                        Game1.GameManager.CollectItem(_collectedShowItem, 0);
                        _collectedShowItem = null;
                    }

                    // spawn the follower if one was picked up
                    UpdateFollower(false);

                    // sword spin
                    if (ShowItem.Name == "sword1")
                    {
                        Game1.GameManager.PlaySoundEffect("D378-03-03");
                        Animation.Play("swing_3");
                        AnimatorWeapons.Play("swing_3");
                        CurrentState = State.SwordShow0;
                        _swordChargeCounter = 1; // don't blink
                        ShowItem = null;
                    }
                    else if (ShowItem.Name.StartsWith("instrument"))
                    {
                        // make sure that the music is not playing
                        Game1.GameManager.StopPieceOfPower();
                        Game1.GameManager.StopGuardianAcorn();

                        _instrumentCounter = 0;
                        CurrentState = State.ShowInstrumentPart0;
                    }
                    else
                    {
                        ShowItem = null;
                        if (CurrentState == State.PickingUp)
                            CurrentState = State.Idle;
                    }
                }
            }
        }

        private void EndPickup()
        {
            _savedPreItemPickup = false;
            SaveGameSaveLoad.ClearSaveState();
            Game1.GameManager.SaveManager.DisableHistory();
        }

        private void UpdateHookshot()
        {
            if (Hookshot.IsMoving)
                return;

            _body.IgnoreHoles = false;
            ReturnToIdle();
        }

        private void UpdateDigging()
        {
            if (Animation.CurrentFrameIndex > 0 && !_hasDug)
            {
                _hasDug = true;
                if (_canDig)
                    Map.Dig(_digPosition, EntityPosition.Position, Direction);
            }

            if (!Animation.IsPlaying)
                CurrentState = State.Idle;
        }

        private void UpdatePegasusBoots()
        {
            _wasBootsRunning = _bootsRunning;
            if (CurrentState != State.Idle || _isClimbing || Map.Is2dMap && Direction % 2 != 0)
            {
                _bootsHolding = false;
                _bootsRunning = false;
                _bootsCounter = 0;

                return;
            }

            // stop running but start charging with a time boost
            if (_bootsStop && _body.Velocity.Length() < 0.25f)
            {
                _bootsStop = false;
                _bootsRunning = false;
                _bootsCounter = _bootsRunTime - 300;
            }

            if (_bootsHolding || _bootsRunning)
            {
                var lastCounter = _bootsCounter;
                _bootsCounter += Game1.DeltaTime;

                // spawn particles
                if (_bootsCounter % _bootsParticleTime < lastCounter % _bootsParticleTime)
                {
                    // water splash effect while running water?
                    if (_body.CurrentFieldState.HasFlag(MapStates.FieldStates.Water))
                    {
                        Game1.GameManager.PlaySoundEffect("D360-14-0E");

                        var splashAnimator = new ObjAnimator(_body.Owner.Map, 0, 0, 0, 3, 1, "Particles/splash", "idle", true);
                        splashAnimator.EntityPosition.Set(new Vector2(
                            _body.Position.X + _body.OffsetX + _body.Width / 2f,
                            _body.Position.Y + _body.OffsetY + _body.Height - _body.Position.Z - 3));
                        Map.Objects.SpawnObject(splashAnimator);
                    }
                    else
                    {
                        Game1.GameManager.PlaySoundEffect("D378-07-07");

                        var animator = new ObjAnimator(Map, (int)EntityPosition.X, (int)(EntityPosition.Y + 1),
                            0, -1 - (int)EntityPosition.Z, Values.LayerPlayer, "Particles/run", "spawn", true);
                        Map.Objects.SpawnObject(animator);
                    }
                }

                // start running
                if (!_bootsRunning && _bootsCounter > _bootsRunTime)
                {
                    _bootsRunning = true;
                    _wasBootsRunning = true;
                    _bootsStop = false;
                }
            }
            else
            {
                _bootsCounter = 0;
            }
        }

        private bool Jump(bool force = false, bool playSoundEffect = true)
        {
            if ((!force && (
                CurrentState != State.Idle &&
                CurrentState != State.Attacking &&
                CurrentState != State.Charging &&
                CurrentState != State.Pushing &&
                CurrentState != State.Blocking &&
                CurrentState != State.Rafting)) ||
                _isTrapped || !_canJump)
            {
                if (_isTrapped && playSoundEffect)
                    Game1.GameManager.PlaySoundEffect("D360-13-0D");

                return false;
            }

            if (!_body.IsGrounded)
                return false;

            // release the carried object if the player is carrying something
            ReleaseCarriedObject();

            if (playSoundEffect)
                Game1.GameManager.PlaySoundEffect("D360-13-0D");

            if (_isRafting)
            {
                // do not move while jumping
                _moveVelocity = Vector2.Zero;
                _lastMoveVelocity = Vector2.Zero;
                StopRaft();
            }
            else
            {
                // base move velocity does not contain the velocity added in the air
                // so when we hit the floor and directly jump afterwards we do not get the velocity of the previouse jump
                _lastMoveVelocity = _lastBaseMoveVelocity;
            }

            _startedJumping = true;
            _body.Velocity.Z = JumpAcceleration;

            // while attacking the player can still jump but without the animation
            if (CurrentState != State.Attacking &&
                CurrentState != State.Charging)
            {
                // start the jump animation
                Animation.Play("jump_" + Direction);

                CurrentState = State.Jumping;
            }

            return true;
        }

        private void UpdateJump()
        {
            if (CurrentState != State.Jumping)
                return;

            if (_railJump)
            {
                _railJumpPercentage += Game1.TimeMultiplier * _railJumpSpeed;
                var amount = MathF.Sin(_railJumpPercentage * (MathF.PI * 0.3f)) / MathF.Sin(MathF.PI * 0.3f);
                var newPosition = Vector2.Lerp(_railJumpStartPosition, _railJumpTargetPosition, amount);
                EntityPosition.Set(newPosition);

                EntityPosition.Z = MathF.Sin(_railJumpPercentage * MathF.PI) * _railJumpHeight + _railJumpPercentage * _railJumpPositionZ;

                if (_railJumpPercentage >= 1)
                {
                    _railJump = false;
                    _body.IgnoreHeight = false;
                    _body.IgnoresZ = false;
                    _body.Velocity.Z = -1f;
                    _body.JumpStartHeight = _railJumpPositionZ;
                    EntityPosition.Set(_railJumpTargetPosition);
                    EntityPosition.Z = _railJumpPositionZ;
                    _lastMoveVelocity = Vector2.Zero;
                }
            }

            // touched the ground
            if (!_railJump && _body.IsGrounded && _body.Velocity.Z <= 0)
            {
                if ((_body.CurrentFieldState & (MapStates.FieldStates.Water | MapStates.FieldStates.DeepWater)) == 0)
                    Game1.GameManager.PlaySoundEffect("D378-07-07");
                if ((_body.CurrentFieldState & MapStates.FieldStates.DeepWater) == 0)
                    Game1.GameManager.PlaySoundEffect("D360-14-0E");

                ReturnToIdle();
            }
        }

        private void ThrowCarriedObject()
        {
            Game1.GameManager.PlaySoundEffect("D360-08-08");

            // play a little throw animation
            Animation.Play("throw_" + Direction);
            CurrentState = State.Throwing;

            _carriedComponent.Throw(_walkDirection[Direction] * 3f);
            RemoveCarriedObject();
        }

        private void StartPickup(CarriableComponent carriableComponent)
        {
            if (carriableComponent?.Init == null)
                return;

            _carriedComponent = carriableComponent;

            Game1.GameManager.PlaySoundEffect("D370-02-02");

            _carryStartPosition = _carriedComponent.Init();
            _carriedComponent.IsPickedUp = true;
            CurrentState = State.PreCarrying;
            _preCarryCounter = 0;

            _carriedGameObject = carriableComponent.Owner;
            _carriedObjDrawComp = carriableComponent.Owner.Components[DrawComponent.Index] as DrawComponent;
            if (_carriedObjDrawComp != null)
                _carriedObjDrawComp.IsActive = false;
        }

        private void UpdatePositionCarriedObject(CPosition newPosition)
        {
            if (_carriedComponent == null)
                return;

            var targetPosition = new Vector3(EntityPosition.X, EntityPosition.Y, EntityPosition.Z + _carriedComponent.CarryHeight);

            if (CurrentState == State.PreCarrying)
            {
                // finished pickup animation?
                if (_preCarryCounter >= PreCarryTime)
                {
                    _preCarryCounter = PreCarryTime;
                    CurrentState = State.Carrying;
                }

                var pickupTime = 1 - MathF.Cos((_preCarryCounter / PreCarryTime) * (MathF.PI / 2));

                var carryPositionXY = Vector2.Lerp(
                    new Vector2(_carryStartPosition.X, _carryStartPosition.Y),
                    new Vector2(targetPosition.X, targetPosition.Y),
                    1 - MathF.Cos(pickupTime * (MathF.PI / 2)));
                var carryPositionZ = MathHelper.Lerp(_carryStartPosition.Z, targetPosition.Z,
                    MathF.Sin(pickupTime * (MathF.PI / 2)));

                if (!_carriedComponent.UpdatePosition(new Vector3(carryPositionXY.X, carryPositionXY.Y, carryPositionZ)))
                {
                    CurrentState = State.Idle;
                    ReleaseCarriedObject();
                }
            }
            else if (!_isFlying)
            {
                // move the carried object up/down with the walk animation
                if (Direction % 2 == 0)
                    targetPosition.Z += _isWalking ? Animation.CurrentFrameIndex : 1;
                else if (Map.Is2dMap)
                    targetPosition.Z += 1;

                if (!_carriedComponent.UpdatePosition(targetPosition))
                {
                    CurrentState = State.Idle;
                    ReleaseCarriedObject();
                }
            }
        }

        #endregion

        private void StopRaft()
        {
            if (_isRafting)
            {
                _objRaft.Body.VelocityTarget = Vector2.Zero;
                _objRaft.Body.AdditionalMovementVT = Vector2.Zero;
                _objRaft.Body.LastAdditionalMovementVT = Vector2.Zero;
            }
        }

        private void StealItem()
        {
            StopHoldingItem();

            // used in ObjStoreItem to not return the item to the shelf
            Game1.GameManager.SaveManager.SetString("result", "0");

            Game1.GameManager.SaveName = "Thief";

            // add the item to the inventory
            var strItem = Game1.GameManager.SaveManager.GetString("itemShopItem");
            var strCount = Game1.GameManager.SaveManager.GetString("itemShopCount");

            var item = new GameItemCollected(strItem)
            {
                Count = int.Parse(strCount)
            };

            // gets picked up
            PickUpItem(item, false, false);

            Game1.GameManager.SaveManager.SetString("stoleItem", "1");
            _showStealMessage = true;
        }

        private void OnHoleAbsorb()
        {
            if (CurrentState == State.Falling ||
                CurrentState == State.TeleporterUpWait ||
                CurrentState == State.TeleporterUp ||
                CurrentState == State.PickingUp ||
                CurrentState == State.Dying)
                return;

            CurrentState = State.Falling;

            FreeTrappedPlayer();
            ReleaseCarriedObject();

            _railJump = false;
            _isFallingIntoHole = true;
            _holeFallCounter = 350;

            Animation.Play("fall");
            Game1.GameManager.PlaySoundEffect("D370-12-0C");
        }

        private void OnDeath()
        {
            if (CurrentState == State.Dying)
                return;

            // has potion?
            var potion = Game1.GameManager.GetItem("potion");
            if (potion != null && potion.Count >= 1)
            {
                Game1.GameManager.RemoveItem("potion", 1);
                Game1.GameManager.HealPlayer(99);
                ItemDrawHelper.EnableHeartAnimationSound();
                return;
            }

            Game1.GameManager.StopMusic(true);
            Game1.GameManager.PlaySoundEffect("D370-08-08");

            CurrentState = State.Dying;
            Animation.Play("dying");

            // set the correct start frame depending on the direction the player is facing
            int[] dirToFrame = { 0, 2, 1, 3 };
            Animation.SetFrame(dirToFrame[Direction]);

            ((GameOverSystem)Game1.GameManager.GameSystems[typeof(GameOverSystem)]).StartDeath();
        }

        private void ReleaseCarriedObject()
        {
            // let the carried item fall down
            if (_carriedComponent == null)
                return;

            _carriedComponent.Throw(new Vector2(0, 0));
            RemoveCarriedObject();
        }

        private void RemoveCarriedObject()
        {
            _carriedComponent.IsPickedUp = false;
            _carriedComponent = null;

            _carriedGameObject = null;

            if (_carriedObjDrawComp != null)
            {
                _carriedObjDrawComp.IsActive = true;
                _carriedObjDrawComp = null;
            }
        }

        private void UpdateFollower(bool mapInit)
        {
            var hasFollower = false;

            // check if marin is following the player
            var itemMarin = Game1.GameManager.GetItem("marin");
            if (itemMarin != null && itemMarin.Count > 0)
            {
                _objFollower = _objMaria;
                hasFollower = true;
            }

            // check if the rooster is following the player
            var itemRooster = Game1.GameManager.GetItem("rooster");
            if (itemRooster != null && itemRooster.Count > 0)
            {
                _objFollower = _objRooster;
                hasFollower = true;
            }

            // check if the ghost is following the player
            var itemGhost = Game1.GameManager.GetItem("ghost");
            if (itemGhost != null && itemGhost.Count > 0)
            {
                _objFollower = _objGhost;
                hasFollower = true;
            }

            if (hasFollower)
            {
                // check if the follower is already spawned
                if (_objFollower.Map != Map)
                {
                    if (mapInit && NextMapPositionStart.HasValue)
                        _objFollower.EntityPosition.Set(NextMapPositionStart.Value);
                    else
                        _objFollower.EntityPosition.Set(EntityPosition.Position);

                    _objFollower.Map = Map;
                    Map.Objects.SpawnObject(_objFollower);
                }
            }
            // remove the current follower from the map
            else if (_objFollower != null)
            {
                Map.Objects.DeleteObjects.Add(_objFollower);
                _objFollower = null;
            }
        }

        private void UpdateStoreItemPosition(CPosition position)
        {
            _storePickupPosition.X = position.X - _storeItemWidth / 2f;
            _storePickupPosition.Y = position.Y - EntityPosition.Z - 14 - _storeItemHeight;
        }

        #region public

        public void InitGame()
        {
            Animation.Play((CarryShield ? "stands_" : "stand_") + Direction);
            _spriteTransparency = 1;

            _inDungeon = false;

            NextMapFallStart = false;
            NextMapFallRotateStart = false;

            Game1.GameManager.SwordLevel = 0;
            Game1.GameManager.ShieldLevel = 0;
            Game1.GameManager.StoneGrabberLevel = 0;

            Game1.GameManager.SelectedOcarinaSong = -1;
            Game1.GameManager.OcarinaSongs[0] = 0;
            Game1.GameManager.OcarinaSongs[1] = 0;
            Game1.GameManager.OcarinaSongs[2] = 0;

            Game1.GameManager.HasMagnifyingLens = false;

            _spawnGhost = false;
            HasFlippers = false;
            StoreItem = null;

            _body.IsActive = true;

            _objMaria = new ObjMarin(Map, 0, 0);
            _objRooster = new ObjCock(Map, 0, 0, null);
            _objGhost = new ObjGhost(Map, 0, 0);

            MapInit();

            CurrentState = State.Idle;
        }

        public void MapInit()
        {
            if (CurrentState != State.Swimming && CurrentState != State.OcarinaTelport)
                CurrentState = State.Idle;

            _boomerang.Reset();
            Hookshot.Reset();

            _hookshotPull = false;

            _railJump = false;
            IsVisible = true;

            _isRafting = false;
            _isFlying = false;

            _isClimbing = false;

            _isTrapped = false;
            _shadowComponent.IsActive = true;

            _isGrabbed = false;

            ShowItem = null;
            _collectedShowItem = null;
            _objFollower = null;

            _hitRepelTime = 0;
            _hitParticleTime = 0;

            _hitCount = 0;
            _sprite.SpriteShader = null;

            _moveVelocity = Vector2.Zero;
            _lastMoveVelocity = Vector2.Zero;
            _hitVelocity = Vector2.Zero;
            _body.Velocity = Vector3.Zero;

            _body.IgnoreHeight = false;
            _body.IgnoreHoles = false;
            _body.DeepWaterOffset = -3;
            _body.Level = 0;
            _body.IsGrounded = true;

            _bootsHolding = false;
            _bootsRunning = false;
            _bootsCounter = 0;

            _carriedGameObject = null;
            _carriedComponent = null;
            _carriedObjDrawComp = null;

            _drawInstrumentEffect = false;

            _diveCounter = 0;
            _swimVelocity = Vector2.Zero;

            if (NextMapFallStart)
            {
                EntityPosition.Z = 64;

                _body.Velocity.Z = -3.75f;
                _body.IgnoresZ = false;
                _body.JumpStartHeight = EntityPosition.Z;

                NextMapFallStart = false;
            }

            if (NextMapFallRotateStart)
            {
                EntityPosition.Z = 160;

                _body.Velocity.Z = -3.75f;
                _body.IgnoresZ = false;
                _body.IsGrounded = false;
                _body.JumpStartHeight = EntityPosition.Z;

                _fallEntryCounter = 0;
                CurrentState = State.FallRotateEntry;

                NextMapFallRotateStart = false;
            }

            if (NextMapPositionEnd.HasValue)
                SetHoleResetPosition(NextMapPositionEnd.Value);

            if (Is2DMode)
                MapInit2D();

            // reset guardian acorn and piece of power except when in a dungeon
            if (!_inDungeon || Map == null || !Map.DungeonMode)
            {
                Game1.GameManager.StopGuardianAcorn();
                Game1.GameManager.StopPieceOfPower();
            }

            if (Map != null && Map.DungeonMode)
                _inDungeon = true;
            else
                _inDungeon = false;

            Game1.GameManager.UseShockEffect = false;
        }

        public void InitEnding()
        {
            CurrentState = State.Sequence;
            Animation.Play("stand_1");
        }

        public void FinishLoadingMap(Map.Map map)
        {
            Map = map;
            Is2DMode = map.Is2dMap;

            if (NextMapPositionStart.HasValue)
                SetPosition(NextMapPositionStart.Value);

            MapInit();

            UpdateFollower(true);

            if (_objFollower != null)
                _objFollower.EntityPosition.Set(NextMapPositionStart.Value);
        }

        public void Respawn()
        {
            Animation.Play((CarryShield ? "stands_" : "stand_") + Direction);

            StoreItem = null;
            _body.IsActive = true;

            var hearts = 3;
            if (Game1.GameManager.MaxHearths >= 14)
                hearts = 10;
            else if (Game1.GameManager.MaxHearths >= 10)
                hearts = 7;
            else if (Game1.GameManager.MaxHearths >= 6)
                hearts = 5;

            Game1.GameManager.CurrentHealth = hearts * 4;

            Game1.GameManager.DeathCount++;

            MapInit();
        }

        public void StartIntro()
        {
            // set the music
            Game1.GameManager.SetMusic(27, 2);

            CurrentState = State.Intro;

            Animation.Play("intro");

            NextMapPositionStart = null;
            NextMapPositionEnd = null;
            SetPosition(new Vector2(56, 51));
            MapManager.Camera.ForceUpdate(Game1.GameManager.MapManager.GetCameraTarget());

            MapManager.ObjLink.SaveMap = Map.MapName;
            MapManager.ObjLink.SavePosition = new Vector2(70, 70);
            MapManager.ObjLink.SaveDirection = 3;
        }

        public void SetPosition(Vector2 newPosition)
        {
            _body.VelocityTarget = Vector2.Zero;
            EntityPosition.Set(new Vector2(newPosition.X, newPosition.Y));
        }

        public void FreezePlayer()
        {
            UpdatePlayer = false;

            _isWalking = false;
            _bootsRunning = false;

            // stop movement
            // on the boat the player should still move up/down while playing the sequence
            if (Map != null && !Map.Is2dMap)
            {
                // make sure to fall down when jumping into a game sequence
                _body.Velocity.X = 0;
                _body.Velocity.Y = 0;
                if (CurrentState == State.Jumping || CurrentState == State.Powdering)
                    CurrentState = State.Idle;
            }

            _body.VelocityTarget = Vector2.Zero;
            _moveVelocity = Vector2.Zero;
            _hitVelocity = Vector2.Zero;
            _swimVelocity = Vector2.Zero;

            // stop push animation
            if (CurrentState == State.Pushing)
                CurrentState = State.Idle;

            if (Map != null && Map.Is2dMap)
                UpdateAnimation2D();
            else
                UpdateAnimation();
        }

        public bool HitPlayer(Box box, HitType type, int damage, float pushMultiplier = 1.75f)
        {
            var boxDir = BodyRectangle.Center - box.Center;

            // if the player is standing inside the box the hit is not blockable
            var blockable = Math.Abs(boxDir.X) > box.Width / 2 ||
                            Math.Abs(boxDir.Y) > box.Height / 2;

            var intersection = BodyRectangle.GetIntersection(box.Rectangle());
            var direction = BodyRectangle.Center - intersection.Center;

            if (direction == Vector2.Zero)
                direction = boxDir;
            if (direction != Vector2.Zero)
                direction.Normalize();

            return HitPlayer(direction * pushMultiplier, type, damage, blockable);
        }

        public bool HitPlayer(Vector2 direction, HitType type, int damage, bool blockable, int damageCooldown = CooldownTime)
        {
            if (_hitCount > 0 ||
                CurrentState == State.Dying ||
                CurrentState == State.PickingUp ||
                CurrentState == State.Drowning ||
                CurrentState == State.Drowned ||
                CurrentState == State.Knockout ||
                IsDiving() ||
                Game1.GameManager.UseShockEffect ||
                !UpdatePlayer ||
                _isTrapped)
                return false;

            // block the attack?
            if (blockable && (CurrentState == State.Blocking || _bootsRunning && CarryShield))
            {
                _bootsHolding = false;
                _bootsRunning = false;
                _bootsCounter = 0;

                // is the player blocking this direction
                var vectorDirection = ToDirection(-direction);
                if (Direction == vectorDirection)
                    return false;
            }

            // jump a little if we get hit by a spike
            if ((type & HitType.Spikes) != 0)
            {
                _body.Velocity.Z = 1.0f;
            }

            // redirect the down force to the sides
            if (Map.Is2dMap && _body.IsGrounded && direction.Y > 0)
            {
                direction.X += Math.Sign(direction.X) * Math.Abs(direction.Y) * 0.5f;
                direction.Y = 0;
            }

            // fall down on damage taken while climbing
            if (Map.Is2dMap && _isClimbing)
                _isClimbing = false;

            if (!_isRafting)
                _hitVelocity += direction;

            if (_hitCount > 0)
                return false;

            Game1.GameManager.PlaySoundEffect("D370-03-03");

            _hitCount = damageCooldown;
            Game1.GameManager.InflictDamage(damage);

            // TODO_2: this should be optional (in config file or game settings?)
            //if(false)
            {
                // freeze the screen and shake
                var freezeTime = 67;
                var shakeMult = (100.0f / freezeTime) * MathF.PI;
                Game1.FreezeTime = Game1.TotalGameTime + freezeTime;
                Game1.GameManager.ShakeScreen(freezeTime, (int)(direction.X * 2), (int)(direction.Y * 2), shakeMult, shakeMult);
                UpdateDamageShader();
            }

            return true;
        }

        public void FreezeAnimationState()
        {
            CurrentState = State.Frozen;
            Animation.Pause();
        }

        public void StartOcarinaDuo()
        {
            CurrentState = State.Ocarina;

            _ocarinaNoteIndex = 0;
            _ocarinaCounter = 0;

            Animation.Play("ocarina_duo");
        }

        public void StopOcarinaDuo()
        {
            CurrentState = State.Idle;
        }

        public void StartFlying(ObjCock objCock)
        {
            _isFlying = true;
            _objRooster = objCock;
        }

        public void StopFlying()
        {
            _isFlying = false;

            _body.IgnoresZ = false;
            _body.IsGrounded = false;
            _body.JumpStartHeight = 0;

            _lastMoveVelocity = Vector2.Zero;

            if (_objRooster != null)
                _objRooster.StopFlying();
        }

        public void SeqLockPlayer()
        {
            UpdatePlayer = false;

            if (Map.Is2dMap)
                UpdateAnimation2D();
            else
                UpdateAnimation();
        }

        public void LockPlayer()
        {
            _isLocked = true;
        }

        public void TrapPlayer(bool disableItems = false)
        {
            _isTrapped = true;
            _trappedDisableItems = disableItems;
            _trapInteractionCount = 8;
        }

        public bool StealShield()
        {
            // steal the shield if it is in the first 4 slots
            for (var i = 0; i < 4; i++)
            {
                if (Game1.GameManager.Equipment[i] != null &&
                    Game1.GameManager.Equipment[i].Name == "shield")
                {
                    Game1.GameManager.RemoveItem("shield", 1);
                    return true;
                }
            }

            return false;
        }

        public void FreeTrappedPlayer()
        {
            _isTrapped = false;
        }

        public void ShortenDive()
        {
            _diveCounter = 350;
        }

        public void StartRaftRiding(ObjRaft objRaft)
        {
            if (CurrentState != State.Jumping)
                CurrentState = State.Rafting;

            _isRafting = true;
            _objRaft = objRaft;
            _body.VelocityTarget = Vector2.Zero;
            _body.IgnoreHeight = true;
        }

        public void RaftJump(Vector2 targetPosition)
        {
            if (CurrentState == State.Jumping)
                return;

            CurrentState = State.Jumping;

            Game1.GameManager.PlaySoundEffect("D360-13-0D");

            Direction = 3;
            Animation.Play("jump_" + Direction);

            if (_objRaft != null)
            {
                _objRaft.Jump(targetPosition, 100);
            }
        }

        public void ExitRaft()
        {
            CurrentState = State.Idle;

            _isRafting = false;
            _objRaft = null;

            EntityPosition.Set(new Vector2(EntityPosition.X, EntityPosition.Y - 1));
        }

        public void SetHoleResetPosition(Vector2 position, int direction)
        {
            if (direction == 0)
                _alternativeHoleResetPosition = new Vector2(position.X + MathF.Ceiling(_body.Width / 2f), position.Y + 8 + MathF.Ceiling(_body.Height / 2f));
            else if (direction == 1)
                _alternativeHoleResetPosition = new Vector2(position.X + 8, position.Y + _body.Height);
            else if (direction == 2)
                _alternativeHoleResetPosition = new Vector2(position.X + 16 - MathF.Ceiling(_body.Width / 2f), position.Y + 8 + MathF.Ceiling(_body.Height / 2f));
            else if (direction == 3)
                _alternativeHoleResetPosition = new Vector2(position.X + 8, position.Y + 16);

            // also used for the drown reseet point
            _drownResetPosition = _alternativeHoleResetPosition;
        }

        public void Knockout(Vector2 direction, string resetDoor)
        {
            if (CurrentState == State.Knockout)
                return;

            CurrentState = State.Knockout;

            MapTransitionStart = MapManager.ObjLink.EntityPosition.Position;
            MapTransitionEnd = MapManager.ObjLink.EntityPosition.Position + direction * 80;
            TransitionOutWalking = false;

            // append a map change
            var transitionSystem = ((MapTransitionSystem)Game1.GameManager.GameSystems[typeof(MapTransitionSystem)]);
            transitionSystem.AppendMapChange(Map.MapName, resetDoor, false, false, Color.White, false);
            transitionSystem.StartKnockoutTransition = true;
        }

        public void GroundStun(int stunTime = 1250)
        {
            // do not stun the player when he is in the air
            if (_body.IsGrounded && CurrentState != State.Jumping)
                Stun(stunTime);
        }

        public void Stun(int stunTime, bool particle = false)
        {
            if (CurrentState == State.Dying)
                return;

            CurrentState = State.InitStunned;

            _stunnedParticles = particle;
            _stunnedCounter = stunTime;
        }

        public void StartGrab()
        {
            _isGrabbed = true;
        }

        public void EndGrab()
        {
            _isGrabbed = false;
        }

        public void StartThrow(Vector3 direction)
        {
            _body.Velocity = direction;
            _body.IsGrounded = false;
            _body.JumpStartHeight = 0;
        }

        public void StartHoldingItem(GameItem item)
        {
            CurrentState = State.CarryingItem;

            StoreItem = item;

            _storeItemWidth = item.SourceRectangle.Value.Width;
            _storeItemHeight = item.SourceRectangle.Value.Height;

            EntityPosition.AddPositionListener(typeof(ObjLink), UpdateStoreItemPosition);
            UpdateStoreItemPosition(EntityPosition);

            Game1.GameManager.SaveManager.SetString("holdItem", "1");
        }

        public void StopHoldingItem()
        {
            CurrentState = State.Idle;

            StoreItem = null;

            // this removes all listeners with the ObjLink as a key
            EntityPosition.PositionChangedDict.Remove(typeof(ObjLink));

            Game1.GameManager.SaveManager.SetString("holdItem", "0");
        }

        public void SlowDown(float speed)
        {
            if (CurrentState != State.Jumping)
                _currentWalkSpeed = speed;
        }

        public void StartBedTransition()
        {
            _startBedTransition = true;
        }

        public void StartJump()
        {
            if (CurrentState != State.Dying && CurrentState != State.PickingUp)
                Jump(true);
        }

        public void StartRailJump(Vector2 goalPosition, float jumpHeightMultiply, float jumpSpeedMultiply, float goalPositionZ = 0)
        {
            if (CurrentState == State.Swimming)
                CurrentState = State.Idle;

            if (!Jump(false, false))
                return;

            Game1.GameManager.PlaySoundEffect("D360-08-08");

            _railJump = true;

            _railJumpStartPosition = EntityPosition.Position;
            _railJumpTargetPosition = goalPosition;

            // values for distance of 16
            _railJumpSpeed = 0.045f * jumpSpeedMultiply;
            _railJumpHeight = 12 * jumpHeightMultiply;
            _railJumpPositionZ = goalPositionZ;

            _railJumpPercentage = 0;

            _body.IgnoreHeight = true;
            _body.IgnoresZ = true;
            _body.Velocity.Z = 0;
        }

        public Vector2 RailJumpTarget()
        {
            return _railJumpTargetPosition;
        }

        public float RailJumpSpeed()
        {
            return _railJumpSpeed;
        }

        public float RailJumpHeight()
        {
            return _railJumpHeight;
        }

        public float GetRailJumpAmount()
        {
            if (!_railJump)
                return 0;

            return _railJumpPercentage;
        }

        public void RotatePlayer()
        {
            if (_bootsRunning)
                return;

            _rotationCounter += Game1.DeltaTime;
            // 8 frames per direction
            if (_rotationCounter > 133)
            {
                _rotationCounter -= 133;
                if (!_isWalking)
                {
                    Direction = (Direction + 1) % 4;
                    // rotate the sword if the player is currently charging
                    if (CurrentState == State.Charging)
                        AnimatorWeapons.Play("stand_" + Direction);
                }
            }
        }

        public void StartTeleportation(ObjDungeonTeleporter teleporter)
        {
            _teleporter = teleporter;

            CurrentState = State.Teleporting;
            _drawBody.Layer = Values.LayerTop;

            _teleportState = 0;
            _teleportCounter = 0;
            _teleportCounterFull = 0;

        }

        public void ShockPlayer(int time)
        {
            // stop running to not continuously run into the enemy
            _bootsHolding = false;
            _bootsRunning = false;
            _bootsCounter = 0;

            CurrentState = State.Idle;

            // shock the player
            Game1.GameManager.UseShockEffect = true;

            Game1.GameManager.ShakeScreen(time, 4, 0, 8.5f, 0);

            Game1.GameManager.InflictDamage(4);
        }

        public void StartHookshotPull()
        {
            _hookshotPull = true;
            if (Map.Is2dMap)
            {
                _body.Velocity.Y = 0;
                _body.LastVelocityCollision = Values.BodyCollision.None;
            }

            // if the player is on the upper level he will not get pulled through water and we can move through colliders
            if ((_body.CurrentFieldState & MapStates.FieldStates.UpperLevel) != 0)
            {
                _body.IsGrounded = false;
                _body.Level = MapStates.GetLevel(_body.CurrentFieldState);
            }
        }

        public bool UpdateHookshotPull()
        {
            var distance = _body.BodyBox.Box.Center - Hookshot.HookshotPosition.Position;
            var pullVector = AnimationHelper.DirectionOffset[Direction];

            // reached the end of the hook or collided with an object before
            if (distance.Length() < (distance + pullVector).Length() ||
                (_body.LastVelocityCollision != Values.BodyCollision.None && (_body.SlideOffset == Vector2.Zero || _body.BodyBox.Box.Contains(Hookshot.HookshotPosition.Position))) ||
                CurrentState == State.Dying)
            {
                _hookshotPull = false;
                _body.IgnoresZ = false;
                _body.IgnoreHoles = false;
                _body.Level = 0;
                return false;
            }

            _body.VelocityTarget = pullVector * 3;

            return true;
        }

        public void StartTeleportation(string teleportMap, string teleporterId)
        {
            _teleporter = null;

            CurrentState = State.Teleporting;
            _drawBody.Layer = Values.LayerTop;

            _teleportMap = teleportMap;
            _teleporterId = teleporterId;
            _teleportState = 0;
            _teleportCounter = 0;
            _teleportCounterFull = 0;

            ReleaseCarriedObject();
        }

        public void StartWorldTelportation(Vector2 newPosition)
        {
            CurrentState = State.TeleportFallWait;

            var positionDistance = EntityPosition.Position - newPosition;
            var fallPosition = new Vector3(newPosition.X, newPosition.Y, 128);
            EntityPosition.Set(fallPosition);

            if (_objFollower != null)
            {
                var itemGhost = Game1.GameManager.GetItem("ghost");
                if (itemGhost != null && itemGhost.Count >= 0)
                    _objFollower.EntityPosition.Set(new Vector2(fallPosition.X, fallPosition.Y));
                else
                    _objFollower.EntityPosition.Set(fallPosition);
            }

            // only jump to the new position if it is a different teleporter at a different location
            if (positionDistance.Length() > 64)
                MapManager.Camera.ForceUpdate(Game1.GameManager.MapManager.GetCameraTarget());
        }

        public void SetWalkingDirection(int direction)
        {
            Direction = direction;
            UpdateAnimation();
        }

        public void PickUpItem(GameItemCollected itemCollected, bool showItem, bool showDialog = true, bool playSound = true)
        {
            if (itemCollected == null)
                return;

            var item = Game1.GameManager.ItemManager[itemCollected.Name];
            // the base item has the max count and other information
            var baseItem = Game1.GameManager.ItemManager[item.Name];

            // save the game before entering the show animation to support exiting the game while the item is shown
            _savedPreItemPickup = true;
            if (item.PickUpDialog != null && !Game1.GameManager.SaveManager.HistoryEnabled)
            {
                SaveGameSaveLoad.FillSaveState(Game1.GameManager);
                Game1.GameManager.SaveManager.EnableHistory();
            }

            _showItem = false;
            _pickingUpInstrument = false;
            _pickingUpSword = false;

            // upgrade the sword
            var equipmentPosition = 0;
            if (item.Name == "sword1")
            {
                _pickingUpSword = true;
                Game1.GameManager.SetMusic(14, 2);
            }
            else if (item.Name == "sword2")
            {
                equipmentPosition = Game1.GameManager.GetEquipmentSlot("sword1");
                Game1.GameManager.RemoveItem("sword1", 99);
                Game1.GameManager.CollectItem(itemCollected, equipmentPosition);
                Game1.GameManager.SetMusic(14, 2);
            }
            else if (item.Name == "mirrorShield")
            {
                equipmentPosition = Game1.GameManager.GetEquipmentSlot("shield");
                Game1.GameManager.RemoveItem("shield", 99);
                Game1.GameManager.CollectItem(itemCollected, equipmentPosition);
            }
            else if (baseItem.Name == "shield")
            {
                var mirrorShield = Game1.GameManager.GetItem("mirrorShield");
                if (mirrorShield != null)
                {
                    Game1.GameManager.PlaySoundEffect(item.SoundEffectName, true, 1, 0, item.TurnDownMusic);
                    return;
                }
            }
            else if (itemCollected.Name == "stonelifter2")
            {
                equipmentPosition = Game1.GameManager.GetEquipmentSlot("stonelifter");
                Game1.GameManager.RemoveItem("stonelifter", 99);
                Game1.GameManager.CollectItem(itemCollected, equipmentPosition);
            }
            else if (itemCollected.Name == "heartMeterFull")
            {
                Game1.GameManager.SetMusic(36, 2);
            }
            else if (itemCollected.Name == "heartMeter")
            {
                var heart = Game1.GameManager.GetItem("heartMeter");
                // hearts was expanded => show different dialog
                if (heart?.Count == 3)
                    _additionalPickupDialog = "heartMeterFilled";
            }

            // hearth
            if (item.Name == "heart")
            {
                Game1.GameManager.CurrentHealth += itemCollected.Count * 4;

                if (Game1.GameManager.CurrentHealth > Game1.GameManager.MaxHearths * 4)
                    Game1.GameManager.CurrentHealth = Game1.GameManager.MaxHearths * 4;
            }
            // pick up item is an accessory
            else if ((item.ShowAnimation == 1 || item.ShowAnimation == 2) && showItem)
            {
                // stop player movement
                _body.Velocity = Vector3.Zero;
                _body.VelocityTarget = Vector2.Zero;
                _moveVelocity = Vector2.Zero;
                _hitVelocity = Vector2.Zero;

                // pick up and show an item
                ShowItem = item;

                // hold the item over the head with one or two hands (to the left side or the middle)
                if (item.ShowAnimation == 1)
                    _showItemOffset.X = 0;
                else
                    _showItemOffset.X = -4;

                _showItemOffset.Y = -15;

                if (ShowItem.Name == "guardianAcorn")
                    Game1.GameManager.InitGuardianAcorn();
                else if (ShowItem.Name == "pieceOfPower")
                    Game1.GameManager.InitPieceOfPower();

                // @HACK: piece of power shows the sword image when picked up
                if (ShowItem.Name == "pieceOfPower")
                {
                    var swordItem = Game1.GameManager.GetItem("sword1");
                    if (swordItem != null && swordItem.Count > 0)
                        ShowItem = Game1.GameManager.ItemManager["sword1PoP"];
                    else
                        ShowItem = Game1.GameManager.ItemManager["sword2PoP"];
                }

                // make sure to use the right source rectangle if the shown item does not have one
                var sourceRectangle = ShowItem.SourceRectangle ?? baseItem.SourceRectangle.Value;
                if (ShowItem.MapSprite != null)
                    sourceRectangle = ShowItem.MapSprite.SourceRectangle;
                else if (baseItem.MapSprite != null)
                    sourceRectangle = baseItem.MapSprite.SourceRectangle;

                // spawn pickup animation
                if (item.ShowEffect)
                    Map.Objects.SpawnObject(new ObjPickupAnimation(Map,
                        EntityPosition.X + _showItemOffset.X, EntityPosition.Y - EntityPosition.Z + _showItemOffset.Y - sourceRectangle.Height / 2));

                _showItemOffset -= new Vector2(sourceRectangle.Width / 2f, sourceRectangle.Height);

                CurrentState = State.PickingUp;
                Game1.GameManager.SaveManager.SetString("player_shows_item", "1");
                Animation.Play("show" + item.ShowAnimation);
                _itemShowCounter = item.ShowTime;
                _showItem = true;

                // make sure to collect the item the player is currently showing
                if (_collectedShowItem != null)
                    Game1.GameManager.CollectItem(_collectedShowItem, 0);

                _collectedShowItem = itemCollected;

                if (ShowItem.Name == "sword2")
                {
                    _shownSwordLv2Dialog = false;
                    _showSwordL2ParticleCounter = 0;
                    CurrentState = State.SwordShowLv2;
                }

                // not sure if this is what should happen here
                ReleaseCarriedObject();
            }
            else
            {
                Game1.GameManager.CollectItem(itemCollected, equipmentPosition);
            }

            if (item.Name.StartsWith("instrument"))
            {
                // stop playing music
                Game1.GameManager.SetMusic(26, 2);

                _instrumentPickupTime = Game1.TotalGameTime;

                _instrumentIndex = int.Parse(item.Name.Replace("instrument", ""));
                _pickingUpInstrument = true;
            }

            if (item.PickUpDialog != null && !_showItem && showDialog)
            {
                Game1.GameManager.StartDialogPath(item.PickUpDialog);
            }

            // play sound
            if (playSound && item.SoundEffectName != null)
                Game1.GameManager.PlaySoundEffect(item.SoundEffectName, true, 1, 0, item.TurnDownMusic);
            if (item.MusicName >= 0)
                Game1.GameManager.SetMusic(item.MusicName, 1);
        }

        #region map change

        public void SetNextMapPosition(Vector2 playerPosition)
        {
            // this will be used to set the position of the player after loading the map
            // one of them should always be null
            // the playerPosition is used after loading a savestate
            NextMapPositionStart = playerPosition;
            NextMapPositionEnd = playerPosition;

            NextMapPositionId = null;
        }

        public void SetNextMapPosition(string nextMapPositionId)
        {
            // this will be used to set the position of the player after loading the map
            // one of them should always be null
            // the nextMapPositionId is used after going though a door
            NextMapPositionId = nextMapPositionId;

            NextMapPositionStart = null;
            NextMapPositionEnd = null;
        }

        public void OnAppendMapChange()
        {
            if (_objMaria != null)
                _objMaria.OnAppendMapChange();
        }

        public void StartTransitioning()
        {
            IsTransitioning = true;

            _drawBody.Layer = Values.LayerTop;

            // if the transitioning starts from a jump the player would have no animation otherwise
            //_moved = true;
            _isWalking = true;
            _bootsRunning = false;

            // stole item?
            if (StoreItem != null)
                StealItem();

            ReleaseCarriedObject();

            // release the cock if link is flying
            if (MapManager.ObjLink.IsFlying())
                MapManager.ObjLink.StopFlying();

            // make sure the player walks
            if (MapTransitionStart.HasValue && MapTransitionEnd.HasValue &&
                CurrentState != State.Swimming && CurrentState != State.BedTransition && CurrentState != State.Knockout && CurrentState != State.OcarinaTelport)
                CurrentState = State.Idle;

            _body.VelocityTarget = Vector2.Zero;

            if (Map.Is2dMap)
            {
                if (_ladderCollision)
                {
                    _isClimbing = true;
                    Direction = 1;
                }

                // prefent the player from falling down while climbing up a ladder
                //if ((Direction % 2) != 0)
                _body.IgnoresZ = true;
                // fall down
                //else if (_body.Velocity.Y < 0)
                _body.Velocity.Y = 0.0f;
            }
            else
            {
                _body.Velocity = Vector3.Zero;
            }
        }

        public void UpdateMapTransitionOut(float state)
        {
            if (MapTransitionStart.HasValue && MapTransitionEnd.HasValue)
            {
                var newPosition = Vector2.Lerp(MapTransitionStart.Value, MapTransitionEnd.Value, state);

                // fall down to the ground
                //if (Map.Is2dMap && (Direction % 2) == 0)
                //    newPosition.Y = EntityPosition.Y;

                SetPosition(newPosition);
            }

            // lock the camera while transitioning
            if (!Map.Is2dMap || Direction == 1)
                Game1.GameManager.MapManager.UpdateCameraY = MapTransitionStart == MapTransitionEnd;

            _isWalking = TransitionOutWalking;

            if (Is2DMode)
                UpdateAnimation2D();
            else
                UpdateAnimation();
        }

        public void UpdateMapTransitionIn(float state)
        {
            // make sure to not start falling while transitioning into a 2d map with a ladder
            if (state == 0 && Map.Is2dMap)
                _body.IgnoresZ = true;

            if (DirectionEntry >= 0)
                Direction = DirectionEntry;

            if (NextMapPositionStart.HasValue && NextMapPositionEnd.HasValue)
            {
                var newPosition = Vector2.Lerp(NextMapPositionStart.Value, NextMapPositionEnd.Value, state);
                SetPosition(newPosition);

                // transition the follower out
                if (_objFollower != null && NextMapPositionStart.Value != NextMapPositionEnd.Value)
                {
                    var followerPosition = Vector2.Lerp(NextMapPositionStart.Value, NextMapPositionEnd.Value, state * 0.5f);
                    _objFollower.SetPosition(followerPosition);
                }
            }

            // lock the camera while transitioning
            if (!Map.Is2dMap || Direction == 1)
                Game1.GameManager.MapManager.UpdateCameraY = NextMapPositionStart == NextMapPositionEnd;

            _isWalking = TransitionInWalking;

            // set the hole and water reset position to be at the transition entrance
            _holeResetPoint = EntityPosition.Position;
            _drownResetPosition = EntityPosition.Position;

            UpdateSwimming();

            UpdateIgnoresZ();

            if (Is2DMode)
                UpdateAnimation2D();
            else
                UpdateAnimation();
        }

        public void EndTransitioning()
        {
            _body.HoleAbsorption = Vector2.Zero;

            IsTransitioning = false;

            if (!Map.Is2dMap)
            {
                _body.Velocity.X = 0;
                _body.Velocity.Y = 0;
            }

            // this is because the water is deeper than 0
            if ((SystemBody.GetFieldState(_body) & MapStates.FieldStates.DeepWater) == 0 && CurrentState != State.Swimming && !_isClimbing)
                _body.IgnoresZ = false;

            _drawBody.Layer = Values.LayerPlayer;

            MapManager.Camera.CameraFollowMultiplier = 1.0f;

            if (_showStealMessage)
            {
                _showStealMessage = false;
                Game1.GameManager.StartDialogPath("shopkeeper_steal");
            }

            // restart the music
            if (Game1.GameManager.PieceOfPowerIsActive || Game1.GameManager.GuardianAcornIsActive)
                Game1.GameManager.StartPieceOfPowerMusic();
        }

        #endregion

        public Vector2 GetSwimVelocity()
        {
            return _swimVelocity;
        }

        public ObjMarin GetMarin()
        {
            return _objMaria;
        }

        #endregion

        #region is functions

        public bool IsDiving()
        {
            return _diveCounter > 0;
        }

        public bool IsGrounded()
        {
            return _body.IsGrounded && !_railJump && !_isFlying;
        }

        public bool IsJumping()
        {
            return CurrentState == State.Jumping;
        }

        public bool IsRailJumping()
        {
            return _railJump;
        }

        public bool IsHoleAbsorb()
        {
            return _isFallingIntoHole;
        }

        public bool IsDashing()
        {
            return _bootsRunning;
        }

        public bool IsStunned()
        {
            return CurrentState == State.Stunned;
        }

        public bool IsTrapped()
        {
            return _isTrapped;
        }

        public bool IsFlying()
        {
            return _isFlying && CurrentState == State.Carrying;
        }

        public bool IsUsingHookshot()
        {
            return CurrentState == State.Hookshot;
        }

        #endregion
    }
}
