using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;
using System;

namespace ProjectZ.InGame.GameObjects.Bosses
{
    class BossFinalBoss : GameObject
    {
        public readonly CSprite Sprite;

        private readonly BodyDrawShadowComponent _bodyShadow;
        private readonly DamageFieldComponent _damageField;
        private readonly HittableComponent _hittableComponent;
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;

        private readonly Animator _animator;
        private readonly Animator _animatorBody;
        private readonly Animator _animatorWeapon;
        private readonly Animator _animatorEye;

        private readonly DrawComponent _drawComponent;
        private readonly AiDamageState _aiDamageState;

        private readonly CBox _hittableBoxMan;

        private CBox _hittableBox;

        private DictAtlasEntry _spriteBody;

        private BossFinalBossFireball _objFireball;

        private Rectangle _roomRectangle;

        private Vector2 _bodyPosition;
        private Vector2 _bodyStartPosition;
        private Vector2 _bodyTargetPosition;

        private Vector2 _manTargetPosition;
        private Vector2[] _fireballOffset = new Vector2[] { new Vector2(-15, -8), new Vector2(0, -24), new Vector2(15, -8), new Vector2(0, -4) };

        private Vector2[] _bodyParts = new Vector2[6];

        private string _saveKey;

        // state slime
        private const int SlimeDamageTime = 2200;
        private const int RotateTime = 2500;
        private int _slimeLives = 3;
        private bool _slimeForm;

        // state man
        private bool _manInit = true;
        private int _manLives = 4;

        // state ganon
        private int _ganonLives = 12;

        private Vector2 _ganonTargetPosition;

        private const int _ganonDeathTime = 2200;
        private bool _ganonForm;

        private float _batCounter;
        private int _batIndex;
        private int _batIndexStart;

        // state moldorm
        private int _moldormLives = 16;

        private const int TailMult = 8;
        private BossFinalBossTail[] _moldormTails = new BossFinalBossTail[4];
        private AiTriggerSwitch _moldormSpeedUp;
        private Vector2[] _moldormPositions = new Vector2[6 * TailMult];
        private Vector2[] _moldormPositionsNew = new Vector2[6 * TailMult];
        private float[] _partDist = new float[4];
        private float _moldormRadiant;
        private float _moldormChangeCounter;
        private float _moldormSpeed;
        private float _directionChangeMultiplier;
        private int _moldormDirection = 1;
        private bool _moldormHit;

        private const float MoldormSpeedNormal = 1.0f;
        private const float MoldormSpeedFast = 1.75f;

        private int _direction;
        private int _sideIndex;

        private int _moveDist = 40;
        private float _moveSpeed = 0.25f;
        private float _moldormSoundCounter;

        private int _moveCounter;

        // state face
        private float _faceParticleCounter;

        // state final
        // objects used to deal damage
        private readonly BossFinalBossFinalTail[] _finalParts = new BossFinalBossFinalTail[8];
        private readonly string[] _spriteFinalParts = new string[] { "final_part0", "final_part1", "final_part1", "final_part2" };
        private float[] _finalPartDistance = new float[] { 18, 10, 10, 10 };

        private Vector2 _targetPosition;

        private float _finalPartCounter;
        private float _finalPart0 = -MathF.PI / 2;
        private float _finalPart1 = -MathF.PI / 2;
        private float _finalPartSpeed0 = 1 / 2500.0f * MathF.PI * 2;
        private float _finalPartSpeed1 = 1 / 2500.0f * MathF.PI * 2;

        private int _finalStateLives = 16;
        private int _finalStateDeathCounter = 2500;

        private float _finalEyeCounter;
        private int _finalEyeState;
        private bool _finalState;

        private bool _hideBody;
        private bool _hideHead;
        private bool _drawGanonWeapon;
        private bool _drawMoldormTail;

        private bool _pushRepel;

        public BossFinalBoss() : base("nightmare_head") { }

        public BossFinalBoss(Map.Map map, int posX, int posY, string saveKey) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 8, 0);
            EntityPosition.AddPositionListener(typeof(BossFinalBoss), OnUpdatePosition);
            EntitySize = new Rectangle(-16, -24, 32, 40);

            _saveKey = saveKey;
            if (!string.IsNullOrEmpty(_saveKey) &&
                Game1.GameManager.SaveManager.GetString(_saveKey) == "1")
            {
                IsDead = true;
                return;
            }

            _bodyStartPosition = EntityPosition.Position;
            _bodyTargetPosition = _bodyStartPosition - new Vector2(0, _moveDist);

            _animator = AnimatorSaveLoad.LoadAnimator("nightmares/nightmare");
            _animator.Play("head");

            _animatorBody = AnimatorSaveLoad.LoadAnimator("nightmares/nightmare");
            _animatorBody.Play("idle");

            _animatorWeapon = AnimatorSaveLoad.LoadAnimator("nightmares/nightmare ganon weapon");
            _animatorEye = AnimatorSaveLoad.LoadAnimator("nightmares/nightmare final");

            _spriteBody = Resources.GetSprite("nightmare_body");

            _bodyPosition = EntityPosition.Position;

            Sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, Sprite, Vector2.Zero);

            for (var i = 0; i < _finalParts.Length; i++)
            {
                _finalParts[i] = new BossFinalBossFinalTail(map, this, _spriteFinalParts[i % 4], EntityPosition.Position);
                map.Objects.SpawnObject(_finalParts[i]);
            }

            _roomRectangle = map.GetField(posX, posY);

            _body = new BodyComponent(EntityPosition, -7, -7, 14, 14, 8)
            {
                MoveCollision = OnCollision,
                AbsorbPercentage = 1f,
                Gravity = -0.085f,
                DragAir = 1.0f,
                Drag = 0.8f,
                FieldRectangle = map.GetField(posX, posY),
                CollisionTypes = Values.CollisionTypes.Normal
            };

            _aiComponent = new AiComponent();

            // floor
            var stateIdle = new AiState(UpdateIdle) { Init = InitIdle };
            var stateMoveBody = new AiState(UpdateMoveBody) { Init = InitMoveBody };
            var stateMoveHead = new AiState(UpdateMoveHead);
            var stateWobble = new AiState(UpdateWobble) { Init = InitWobble };
            var stateDespawn = new AiState(UpdateDespawn) { Init = InitStartDespawn };

            // slime
            var stateSlimeSpawn = new AiState(UpdateSlimeSpawn) { Init = InitSlimeSpawn };
            var stateSlimeJump = new AiState(UpdateSlimeJump) { Init = InitSlimeJump };
            var stateSlimeWait = new AiState() { Init = InitSlimeWait };
            stateSlimeWait.Trigger.Add(new AiTriggerCountdown(800, null, () => _aiComponent.ChangeState("slimeJump")));
            var stateSlimeDespawn = new AiState(UpdateSlimeDespawn) { Init = InitSlimeDespawn };
            var stateSlimeHidden = new AiState() { Init = InitSlimeHidden };
            stateSlimeHidden.Trigger.Add(new AiTriggerCountdown(2200, null, EndSlimeHidden));
            var stateSlimeDamaged = new AiState() { Init = InitSlimeDamaged };
            stateSlimeDamaged.Trigger.Add(new AiTriggerCountdown(SlimeDamageTime, TickSlimeDamaged, EndSlimeDamge));
            var stateSlimeHideExplode = new AiState() { Init = InitSlimeHideExplode };
            stateSlimeHideExplode.Trigger.Add(new AiTriggerCountdown(2200, null, EndSlimeHidden));
            var stateSlimeExplode = new AiState() { Init = InitSlimeExplode };
            stateSlimeExplode.Trigger.Add(new AiTriggerCountdown(2800, null, SlimeEnd));

            // man
            var stateManPreAttack = new AiState(UpdateManPreAttack);
            stateManPreAttack.Trigger.Add(new AiTriggerCountdown(1100, null, () => _aiComponent.ChangeState("manAttack")));
            var stateManAttack = new AiState(UpdateManAttack) { Init = InitManAttack };
            var stateManPostAttack = new AiState(UpdateManPostAttack) { Init = InitPostAttack };
            stateManPostAttack.Trigger.Add(new AiTriggerCountdown(1300, null, () => _aiComponent.ChangeState("manDespawn")));
            var stateManDespawn = new AiState(UpdateManDespawn) { Init = InitManDespawn };
            var stateManMove = new AiState(UpdateManMove) { Init = InitManMove };
            var stateManMoveWait = new AiState();
            stateManMoveWait.Trigger.Add(new AiTriggerCountdown(650, null, () => _aiComponent.ChangeState("manSpawn")));
            var stateManSpawn = new AiState(UpdateManSpawn) { Init = InitManSpawn };
            var stateManRotate = new AiState() { Init = InitManRotate };
            stateManRotate.Trigger.Add(new AiTriggerCountdown(RotateTime, TickRotate, EndRotate));

            // explode
            var stateExplode = new AiState() { Init = InitExplode };
            stateExplode.Trigger.Add(new AiTriggerCountdown(950, null, () => _aiComponent.ChangeState("explodeDespawn")));
            var stateExplodeDespawn = new AiState(UpdateDepawn) { Init = InitDespawn };
            var stateMove = new AiState(UpdateMove) { Init = InitMove };
            var stateMoveWait = new AiState();
            stateMoveWait.Trigger.Add(new AiTriggerCountdown(650, null, EndMoveWait));

            // moldorm
            var stateMoldormSpawn = new AiState(UpdateMoldormSpawn) { Init = InitMoldormSpawn };
            var stateMoldorm = new AiState(UpdateMoldorm) { Init = InitMoldorm };
            stateMoldorm.Trigger.Add(_moldormSpeedUp = new AiTriggerSwitch(3000));
            var stateMoldormDying = new AiState(UpdateMoldormDying) { Init = InitMoldormDying };
            stateMoldormDying.Trigger.Add(new AiTriggerCountdown(1250, null, MoldormExplode));
            var stateMoldormDespawn = new AiState(UpdateMoldormDespawn);

            // ganon
            var stateGanonSpawn = new AiState(UpdateGanonSpawn) { Init = InitGanonSpawn };
            var stateGanonPreSpawnWeapon = new AiState(UpdateGanonPreSpawnWeapon);
            stateGanonPreSpawnWeapon.Trigger.Add(new AiTriggerCountdown(850, null, () => _aiComponent.ChangeState("ganonSpawnWeapon")));
            var stateGanonSpawnWeapon = new AiState(UpdateGanonSpawnWeapon) { Init = InitGanonSpawnWeapon };
            var stateGanon = new AiState(UpdateGanon) { Init = InitGanon };
            stateGanon.Trigger.Add(new AiTriggerCountdown(1000, null, () => _aiComponent.ChangeState("ganonBats")));
            var stateGanonBats = new AiState(UpdateGanonBats) { Init = InitGanonBats };
            var stateGanonThrow = new AiState(UpdateGanonThrow) { Init = InitGanonThrow };
            stateGanonThrow.Trigger.Add(new AiTriggerCountdown(800, null, ThrowWeapon));
            var stateGanonPostThrow = new AiState() { Init = InitGanonPostThrow };
            stateGanonPostThrow.Trigger.Add(new AiTriggerCountdown(1100, null, () => _aiComponent.ChangeState("ganonMove")));
            var stateGanonMove = new AiState(UpdateGanonMove) { Init = InitGanonMove };
            var stateGanonCatchWeapon = new AiState(UpdateGanonCatchWeapon) { Init = InitGanonCatchWeapon };
            var stateGanonWait = new AiState(UpdateGanonWait);
            stateGanonWait.Trigger.Add(new AiTriggerCountdown(1000, null, () => _aiComponent.ChangeState("ganonBats")));
            var stateGanonDeath = new AiState() { Init = InitGanonDeath };
            stateGanonDeath.Trigger.Add(new AiTriggerCountdown(_ganonDeathTime, TickGanonDamage, () => _aiComponent.ChangeState("ganonExplode")));
            var stateGanonExplode = new AiState() { Init = InitGanonExplode };
            stateGanonExplode.Trigger.Add(new AiTriggerCountdown(_ganonDeathTime, null, () => _aiComponent.ChangeState("face")));

            // face
            var stateFace = new AiState(UpdateFace) { Init = InitFace };
            var stateFaceExplode = new AiState() { Init = InitFaceExplose };
            stateFaceExplode.Trigger.Add(new AiTriggerCountdown(2200, null, () => _aiComponent.ChangeState("faceMove")));
            var stateFaceMove = new AiState(UpdateFaceMove) { Init = InitFaceMove };
            var stateFaceHidden = new AiState() { Init = InitFaceHidden };
            stateFaceHidden.Trigger.Add(new AiTriggerCountdown(1000, null, () => _aiComponent.ChangeState("faceDespawn")));
            var stateFaceDespawn = new AiState(UpdateFaceDespawn) { Init = InitFaceDespawn };

            // final
            var stateFinalSpawn = new AiState(UpdateFianalSpawn) { Init = InitFinalSpawn };
            var stateFinal = new AiState(UpdateFinal) { Init = InitFinal };
            var stateFinalBlink = new AiState() { Init = InitFinalBlink };
            stateFinalBlink.Trigger.Add(new AiTriggerCountdown(_finalStateDeathCounter, TickFinalDespawn, () => _aiComponent.ChangeState("finalDeath")));
            var stateFinalDeath = new AiState(UpdateFinalDeath) { Init = InitFinalDeath };

            var stateTest = new AiState();
            stateTest.Trigger.Add(new AiTriggerCountdown(1500, null, TestProjectileSpawn) { ResetAfterEnd = true });

            // spawning
            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("moveBody", stateMoveBody);
            _aiComponent.States.Add("moveHead", stateMoveHead);
            _aiComponent.States.Add("wobble", stateWobble);
            _aiComponent.States.Add("despawn", stateDespawn);

            // slime
            _aiComponent.States.Add("slimeSpawn", stateSlimeSpawn);
            _aiComponent.States.Add("slimeJump", stateSlimeJump);
            _aiComponent.States.Add("slimeWait", stateSlimeWait);
            _aiComponent.States.Add("slimeDespawn", stateSlimeDespawn);
            _aiComponent.States.Add("slimeHidden", stateSlimeHidden);
            _aiComponent.States.Add("slimeDamaged", stateSlimeDamaged);
            _aiComponent.States.Add("slimeHideExplode", stateSlimeHideExplode);
            _aiComponent.States.Add("slimeExplode", stateSlimeExplode);

            // man
            _aiComponent.States.Add("manPreAttack", stateManPreAttack);
            _aiComponent.States.Add("manAttack", stateManAttack);
            _aiComponent.States.Add("manPostAttack", stateManPostAttack);
            _aiComponent.States.Add("manDespawn", stateManDespawn);
            _aiComponent.States.Add("manMove", stateManMove);
            _aiComponent.States.Add("manMoveWait", stateManMoveWait);
            _aiComponent.States.Add("manSpawn", stateManSpawn);
            _aiComponent.States.Add("manRotate", stateManRotate);

            // moldorm
            _aiComponent.States.Add("moldormSpawn", stateMoldormSpawn);
            _aiComponent.States.Add("moldorm", stateMoldorm);
            _aiComponent.States.Add("moldormDying", stateMoldormDying);
            _aiComponent.States.Add("moldormDespawn", stateMoldormDespawn);

            // ganon
            _aiComponent.States.Add("ganonSpawn", stateGanonSpawn);
            _aiComponent.States.Add("ganonPreSpawnWeapon", stateGanonPreSpawnWeapon);
            _aiComponent.States.Add("ganonSpawnWeapon", stateGanonSpawnWeapon);
            _aiComponent.States.Add("ganon", stateGanon);
            _aiComponent.States.Add("ganonBats", stateGanonBats);
            _aiComponent.States.Add("ganonThrow", stateGanonThrow);
            _aiComponent.States.Add("ganonPostThrow", stateGanonPostThrow);
            _aiComponent.States.Add("ganonMove", stateGanonMove);
            _aiComponent.States.Add("ganonCatchWeapon", stateGanonCatchWeapon);
            _aiComponent.States.Add("ganonWait", stateGanonWait);
            _aiComponent.States.Add("ganonDeath", stateGanonDeath);
            _aiComponent.States.Add("ganonExplode", stateGanonExplode);

            // face
            _aiComponent.States.Add("face", stateFace);
            _aiComponent.States.Add("faceExplode", stateFaceExplode);
            _aiComponent.States.Add("faceMove", stateFaceMove);
            _aiComponent.States.Add("faceHidden", stateFaceHidden);
            _aiComponent.States.Add("faceDespawn", stateFaceDespawn);

            // final
            _aiComponent.States.Add("finalSpawn", stateFinalSpawn);
            _aiComponent.States.Add("final", stateFinal);
            _aiComponent.States.Add("finalBlink", stateFinalBlink);
            _aiComponent.States.Add("finalDeath", stateFinalDeath);

            _aiComponent.States.Add("explode", stateExplode);
            _aiComponent.States.Add("explodeDespawn", stateExplodeDespawn);
            _aiComponent.States.Add("move", stateMove);
            _aiComponent.States.Add("moveWait", stateMoveWait);

            _aiDamageState = new AiDamageState(this, _body, _aiComponent, Sprite, 8, false);
            _aiDamageState.OnDeath = OnZeroLives;
            _aiDamageState.BossHitSound = true;

            var damageCollider = new CBox(EntityPosition, -6, -6, 0, 12, 12, 8);
            _hittableBox = new CBox(EntityPosition, -7, -7, 0, 14, 14, 8);
            _hittableBoxMan = new CBox(EntityPosition, -6, -12, 0, 12, 18, 8);

            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BodyComponent.Index, _body);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(PushableComponent.Index, new PushableComponent(_body.BodyBox, OnPush));
            AddComponent(HittableComponent.Index, _hittableComponent = new HittableComponent(_hittableBox, OnHit));
            AddComponent(DamageFieldComponent.Index, _damageField = new DamageFieldComponent(damageCollider, HitType.Enemy, 2) { IsActive = false });
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, _drawComponent = new DrawComponent(Draw, Values.LayerPlayer, EntityPosition));
            AddComponent(DrawShadowComponent.Index, _bodyShadow = new BodyDrawShadowComponent(_body, Sprite) { ShadowWidth = 14, ShadowHeight = 5 });

            _aiComponent.ChangeState("idle");

            //DebugMan();
        }

        #region Debug

        private void DebugSlimeEnd()
        {
            _slimeLives = 1;
            _aiComponent.ChangeState("slimeHidden");
        }

        private void DebugMan()
        {
            _manLives = 2;
            _aiComponent.ChangeState("slimeExplode");
        }

        private void DebugMoldrom()
        {
            _moveCounter = 1;
            _moldormLives = 1;
            _aiComponent.ChangeState("moldormSpawn");
        }

        private void DebugGanon()
        {
            _ganonLives = 1;
            _aiComponent.ChangeState("ganonSpawn");
        }

        private void DebugFace()
        {
            _finalStateLives = 4;
            _aiComponent.ChangeState("face");
        }

        #endregion

        #region state final

        private void InitFinalDeath()
        {
            Sprite.SpriteShader = null;

            Game1.GameManager.PlaySoundEffect("D370-16-10");
            Game1.GameManager.PlaySoundEffect("D378-60-3D");
        }

        private void UpdateFinalDeath()
        {
            // spawn the parts
            _finalPartCounter -= Game1.DeltaTime;

            // despawn?
            if (_finalPartCounter < -300)
                FinalExplose();

            for (var i = 0; i < 4; i++)
            {
                if (i * 300 >= _finalPartCounter)
                {
                    _finalParts[i].SetActive(false);
                    _finalParts[i + 4].SetActive(false);
                }
            }
        }

        private void FinalExplose()
        {
            if (!string.IsNullOrEmpty(_saveKey))
                Game1.GameManager.SaveManager.SetString(_saveKey, "1");

            Map.Objects.DeleteObjects.Add(this);

            ExplodeAnimation();
        }

        private void InitFinalBlink()
        {
            Game1.GameManager.SetMusic(93, 2);

            Game1.GameManager.StopSoundEffect("D360-61-3D");
            Game1.GameManager.PlaySoundEffect("D370-16-10");

            // hack to not allow anymore attacks
            _body.VelocityTarget = Vector2.Zero;
            Game1.GameManager.StartDialog("nightmareFinal0");

            // deactivate the damge fields
            _damageField.IsActive = false;
            for (var i = 0; i < 8; i++)
                _finalParts[i].DeactivateDamageField();
        }

        private void TickFinalDespawn(double counter)
        {
            var time = _finalStateDeathCounter - counter;
            Sprite.SpriteShader = time % (AiDamageState.BlinkTime * 2) < AiDamageState.BlinkTime ? Resources.DamageSpriteShader0 : null;
        }

        private void InitFinalSpawn()
        {
            Game1.GameManager.PlaySoundEffect("D370-35-23");
            Game1.GameManager.SetMusic(79, 2);

            _targetPosition = EntityPosition.Position;
            _animator.Play("final_spawn");
            _damageField.IsActive = true;
            _damageField.CollisionBox = new CBox(EntityPosition, -8, -12, 16, 16, 8);
            _hittableComponent.HittableBox = new CBox(EntityPosition, -6, -8, 12, 14, 8);
        }

        private void UpdateFianalSpawn()
        {
            if (!_animator.IsPlaying)
                _aiComponent.ChangeState("final");
        }

        private Vector2 RandomRoomPositionFinal()
        {
            var posY = 0;
            // make it more likeley to move around at the top
            if (Game1.RandomNumber.Next(1, 10) <= 6)
                posY = Game1.RandomNumber.Next(8, 6 * 8);
            else if (Game1.RandomNumber.Next(1, 10) <= 6)
                posY = Game1.RandomNumber.Next(8, 9 * 8);

            int posX;
            if (16 < posY || posY < 64 - 16)
                posX = Game1.RandomNumber.Next(16, 6 * 16);
            else
                posX = Game1.RandomNumber.Next(0, 8 * 16);

            return new Vector2(_roomRectangle.X + 24 + posX, _roomRectangle.Y + 24 + posY);
        }

        private void InitFinal()
        {
            _finalState = true;
            _animator.Play("final");
        }

        private void UpdateFinal()
        {
            _animatorEye.Update();

            Game1.GameManager.PlaySoundEffect("D360-61-3D", false);

            // move to the target position
            var distance = _targetPosition - EntityPosition.Position;
            if (distance.Length() > 4)
            {
                distance.Normalize();
                _body.VelocityTarget = AnimationHelper.MoveToTarget(_body.VelocityTarget, distance * 0.5f, 0.0125f * Game1.TimeMultiplier);
            }
            else
            {
                // generate new target position
                _targetPosition = RandomRoomPositionFinal();
            }

            if (!_animatorEye.IsPlaying)
                _finalEyeCounter -= Game1.DeltaTime;

            if (_finalEyeCounter <= 0)
            {
                // open the eye
                if (_finalEyeState == 0)
                {
                    _finalEyeState = 1;
                    _finalEyeCounter += 1250 + Game1.RandomNumber.Next(750);
                    _animatorEye.Play("eye_open");
                }
                // close the eye
                else if (_finalEyeState == 1)
                {
                    _finalEyeState = 0;
                    _finalEyeCounter += 2500 + Game1.RandomNumber.Next(2500);
                    _animatorEye.Play("eye_close");
                }
            }

            // spawn the parts
            if (_finalPartCounter < 4 * 300)
                _finalPartCounter += Game1.DeltaTime;

            if (_finalPartCounter > 300)
            {
                _finalPart0 += Game1.DeltaTime * _finalPartSpeed0;
                _finalPart1 += Game1.DeltaTime * _finalPartSpeed1;

                var directionPart0 = new Vector2(MathF.Cos(_finalPart0), MathF.Sin(_finalPart0));
                SetFinalPartsPosition(directionPart0, 0);
                var directionPart1 = new Vector2(-MathF.Cos(_finalPart1), MathF.Sin(_finalPart1));
                SetFinalPartsPosition(directionPart1, 1);
            }
        }

        private void SetFinalPartsPosition(Vector2 direction, int index)
        {
            var position = new Vector2(EntityPosition.X, EntityPosition.Y - 5);

            for (var i = 0; i < 4; i++)
            {
                if ((i + 1) * 300 > _finalPartCounter)
                    break;

                _finalParts[i + index * 4].SetActive(true);

                position += direction * _finalPartDistance[i];
                _finalParts[i + index * 4].EntityPosition.Set(position);
            }
        }

        #endregion

        #region state face

        private void InitFaceDespawn()
        {
            _damageField.IsActive = false;
            _animator.Play("slime_despawn");
        }

        private void UpdateFaceDespawn()
        {
            // TODO: delay
            if (!_animator.IsPlaying)
                _aiComponent.ChangeState("finalSpawn");
        }

        private void InitFaceHidden()
        {
            _animator.Play("face_hidden");
        }

        private void InitFaceExplose()
        {
            Game1.GameManager.SetMusic(-1, 2);
            Game1.GameManager.PlaySoundEffect("D370-16-10");
            _body.VelocityTarget = Vector2.Zero;
            ExplodeAnimation();
        }

        private void InitFaceMove()
        {
            Game1.GameManager.PlaySoundEffect("D360-53-35");
        }

        private void UpdateFaceMove()
        {
            var targetPosition = new Vector2(_body.FieldRectangle.X + 80, _body.FieldRectangle.Y + 40);
            var direction = targetPosition - EntityPosition.Position;
            var moveSpeed = 1.25f;

            if (direction.Length() > moveSpeed * Game1.TimeMultiplier)
            {
                direction.Normalize();
                _body.VelocityTarget = AnimationHelper.MoveToTarget(_body.VelocityTarget, direction * moveSpeed, 0.075f * Game1.TimeMultiplier);
            }
            else
            {
                _body.VelocityTarget = Vector2.Zero;
                _aiComponent.ChangeState("faceHidden");
            }
        }

        private void InitFace()
        {
            _damageField.IsActive = true;
            _damageField.CollisionBox = new CBox(EntityPosition, -6, -6, 0, 12, 12, 8);
            _hittableComponent.HittableBox = new CBox(EntityPosition, -6, -6, 0, 12, 12, 8);
            _drawComponent.Layer = Values.LayerPlayer;
        }

        private void UpdateFace()
        {
            // spawn paticles
            _faceParticleCounter -= Game1.DeltaTime;
            if (_faceParticleCounter < 0)
            {
                _faceParticleCounter += 125;
                var objParticle = new ObjAnimator(Map, (int)EntityPosition.X, (int)EntityPosition.Y, Values.LayerBottom, "Nightmares/nightmare particle", "face_particle", true);
                Map.Objects.SpawnObject(objParticle);
            }

            // move towards the player
            var playerDirection = MapManager.ObjLink.EntityPosition.Position - new Vector2(EntityPosition.Position.X, EntityPosition.Position.Y + 4);
            if (playerDirection != Vector2.Zero)
            {
                playerDirection.Normalize();
                _body.VelocityTarget = AnimationHelper.MoveToTarget(_body.VelocityTarget, playerDirection * 1.5f, Game1.TimeMultiplier * 0.035f);
            }
        }

        #endregion

        #region state ganon

        public void CatchWeapon()
        {
            _body.VelocityTarget = Vector2.Zero;

            Game1.GameManager.PlaySoundEffect("D378-25-19");

            if (_ganonLives > 0)
                _aiComponent.ChangeState("ganonCatchWeapon");
        }

        private void InitGanonDeath()
        {
            _ganonForm = false;
            _animator.Pause();
            _animatorWeapon.Pause();

            Game1.GameManager.PlaySoundEffect("D370-16-10");
        }

        private void InitGanonExplode()
        {
            _drawGanonWeapon = false;
            EntityPosition.Set(new Vector2(EntityPosition.X, EntityPosition.Y - 4));

            Game1.GameManager.PlaySoundEffect("D378-55-37");
            ExplodeAnimation();
        }

        private void TickGanonDamage(double counter)
        {
            var time = _ganonDeathTime - counter;
            Sprite.SpriteShader = time % (AiDamageState.BlinkTime * 2) < AiDamageState.BlinkTime ? Resources.DamageSpriteShader0 : null;
        }

        private void UpdateGanonWait()
        {
            var playerDirection = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            var dir = playerDirection.X < 0 ? -1 : 1;

            _animator.Play("ganon_" + dir);
            _animatorWeapon.Play("ganon_" + dir);
        }

        private void InitGanonCatchWeapon()
        {
            _drawGanonWeapon = true;

            var playerDirection = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            var dir = playerDirection.X < 0 ? -1 : 1;

            _animator.Play("ganon_weapon_spawn_" + dir);
            _animatorWeapon.Play("ganon_weapon_spawn_" + dir);
        }

        private void UpdateGanonCatchWeapon()
        {
            if (!_animatorWeapon.IsPlaying)
                _aiComponent.ChangeState("ganonWait");
        }

        private void InitGanonMove()
        {
            _ganonTargetPosition = RandomRoomPosition();
        }

        private void UpdateGanonMove()
        {
            var moveDirection = _ganonTargetPosition - EntityPosition.Position;
            if (moveDirection.Length() > 8)
            {
                if (moveDirection != Vector2.Zero)
                    moveDirection.Normalize();

                _body.VelocityTarget = AnimationHelper.MoveToTarget(_body.VelocityTarget, moveDirection * 1.5f, 0.075f * Game1.TimeMultiplier);

                _direction = moveDirection.X < 0 ? -1 : 1;
                _animator.Play("ganon_" + _direction);
            }
            else
            {
                _body.VelocityTarget = Vector2.Zero;
            }
        }

        private void InitGanonPostThrow()
        {
            _drawGanonWeapon = false;
            _animator.Play("ganon_throw_" + _direction);
        }

        private void InitGanonThrow()
        {
            _drawGanonWeapon = true;
        }

        private void UpdateGanonThrow()
        {
            UpdateDirection();
            _animator.Play("ganon_swing_up_" + _direction);
            _animatorWeapon.Play("ganon_swing_up_" + _direction);
        }

        private void ThrowWeapon()
        {
            _aiComponent.ChangeState("ganonPostThrow");

            var position = EntityPosition.Position - new Vector2(-_direction * 12, 22);
            var objWeapon = new BossFinalBossWeapon(Map, this, (int)position.X, (int)position.Y, _direction);
            Map.Objects.SpawnObject(objWeapon);
        }

        private void UpdateDirection()
        {
            var playerDirection = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            _direction = playerDirection.X < 0 ? -1 : 1;
        }

        private void InitGanonBats()
        {
            _batIndex = 0;
            _batCounter = 550 - 350;

            UpdateDirection();

            _batIndexStart = _direction < 0 ? 3 : 0;

            _animator.Play("ganon_swing_" + _direction);
            _animatorWeapon.Play("ganon_swing_" + _direction);
        }

        private void UpdateGanonBats()
        {
            _batCounter += Game1.DeltaTime;
            if (_batCounter > 550)
            {
                _batIndex++;
                _batCounter -= 550;

                if (_batIndex > 8)
                {
                    _aiComponent.ChangeState("ganonThrow");
                    return;
                }
                if (_batIndex >= 7)
                    return;

                var radians = (_batIndexStart + _batIndex - 1) / 3f * MathF.PI;
                var position = EntityPosition.Position - new Vector2(_direction * 9, 26) + new Vector2(MathF.Cos(radians), _direction * -MathF.Sin(radians)) * 24;
                var objBat = new BossFinalBossBat(Map, (int)position.X, (int)position.Y);
                Map.Objects.SpawnObject(objBat);
            }
        }

        private void InitGanonSpawn()
        {
            _ganonForm = true;

            _pushRepel = true;
            _damageField.IsActive = true;
            _damageField.CollisionBox = new CBox(EntityPosition, -12, 7 - 24, 24, 24, 8);
            _hittableComponent.HittableBox = new CBox(EntityPosition, -12, 7 - 24, 24, 24, 8);

            var playerDirection = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            var dir = playerDirection.X < 0 ? -1 : 1;

            Game1.GameManager.PlaySoundEffect("D370-35-23");

            _animator.Play("ganon_spawn_" + dir);
        }

        private void UpdateGanonSpawn()
        {
            if (!_animator.IsPlaying)
                _aiComponent.ChangeState("ganonPreSpawnWeapon");
        }

        private void UpdateGanonPreSpawnWeapon()
        {
            var playerDirection = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            var dir = playerDirection.X < 0 ? -1 : 1;

            _animator.Play("ganon_weapon_" + dir);
        }

        private void InitGanonSpawnWeapon()
        {
            _drawGanonWeapon = true;

            var playerDirection = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            var dir = playerDirection.X < 0 ? -1 : 1;

            Game1.GameManager.PlaySoundEffect("D360-57-39");

            _animator.Play("ganon_weapon_spawn_" + dir);
            _animatorWeapon.Play("ganon_weapon_spawn_" + dir);
        }

        private void UpdateGanonSpawnWeapon()
        {
            if (!_animatorWeapon.IsPlaying)
                _aiComponent.ChangeState("ganon");
        }

        private void InitGanon()
        {
        }

        private void UpdateGanon()
        {
            var playerDirection = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            var dir = playerDirection.X < 0 ? -1 : 1;

            _animator.Play("ganon_" + dir);
            _animatorWeapon.Play("ganon_" + dir);
        }

        #endregion

        #region state moldorm

        private void InitMoldormSpawn()
        {
            _animator.Play("meldorm_spawn");
        }

        private void UpdateMoldormSpawn()
        {
            if (!_animator.IsPlaying)
                _aiComponent.ChangeState("moldorm");
        }

        private void InitMoldorm()
        {
            _pushRepel = true;
            _body.CollisionTypes = Values.CollisionTypes.Normal;
            _drawMoldormTail = true;
            _damageField.IsActive = true;

            for (var i = 0; i < _moldormPositions.Length; i++)
                _moldormPositions[i] = EntityPosition.Position;

            for (var i = 0; i < _moldormTails.Length; i++)
            {
                string animationId;
                if (i == _moldormTails.Length - 1)
                    animationId = "moldorm_tail";
                else if (i == _moldormTails.Length - 2)
                    animationId = "moldorm_body_2";
                else
                    animationId = "moldorm_body";

                _moldormTails[i] = new BossFinalBossTail(Map, this, animationId, i == _moldormTails.Length - 1);
                Map.Objects.SpawnObject(_moldormTails[i]);
            }
        }

        private void UpdateMoldormTail()
        {
            // blinking tail
            _moldormTails[3].Sprite.SpriteShader = Game1.TotalGameTime % (AiDamageState.BlinkTime * 2) < AiDamageState.BlinkTime ? Resources.DamageSpriteShader0 : null;
        }

        private void UpdateMoldorm()
        {
            UpdateMoldormTail();

            // change the direction?
            _moldormChangeCounter -= Game1.DeltaTime;
            if (_moldormChangeCounter < 0)
            {
                _moldormChangeCounter += Game1.RandomNumber.Next(500, 2000);
                _moldormDirection = -_moldormDirection;
            }

            _moldormRadiant += Game1.TimeMultiplier * 0.065f * _moldormDirection;

            _moldormSpeed = _moldormSpeedUp.State ? MoldormSpeedNormal : MoldormSpeedFast;
            if (_moldormHit)
            {
                _moldormSpeed = 0;
                UpdateMoldormTail(EntityPosition);
            }
            else
            {
                _moldormSoundCounter -= Game1.DeltaTime * _moldormSpeed;
                if (_moldormSoundCounter < 0)
                {
                    _moldormSoundCounter += 250;
                    Game1.GameManager.PlaySoundEffect("D360-56-38");
                }
            }

            _body.VelocityTarget = new Vector2(MathF.Sin(_moldormRadiant), MathF.Cos(_moldormRadiant)) * _moldormSpeed;

            _directionChangeMultiplier = AnimationHelper.MoveToTarget(_directionChangeMultiplier, 1, 0.1f * Game1.TimeMultiplier);
        }

        private void InitMoldormDying()
        {
            _body.VelocityTarget = Vector2.Zero;
        }

        private void UpdateMoldormDying()
        {
            UpdateMoldormTail();

            _moldormRadiant += Game1.TimeMultiplier * 0.065f * _moldormDirection;

            UpdateMoldormTail(EntityPosition);
        }

        private void MoldormExplode()
        {
            _aiComponent.ChangeState("moldormDespawn");
            _animator.Play("slime_despawn");

            _drawMoldormTail = false;

            _pushRepel = false;
            _damageField.IsActive = false;
            _bodyShadow.IsActive = false;
            _drawComponent.Layer = Values.LayerBottom;

            Game1.GameManager.PlaySoundEffect("D378-55-37");

            ExplosionParticle();
        }

        private void UpdateMoldormDespawn()
        {
            if (!_animator.IsPlaying)
                _aiComponent.ChangeState("move");
        }

        private void OnUpdatePosition(CPosition position)
        {
            if (_aiComponent.CurrentStateId == "moldorm")
                UpdateMoldormTail(position);
        }

        private void UpdateMoldormTail(CPosition position)
        {
            // set the rotation to be 0 < rotation < Pi * 2 to allow for correct calculations
            while (_moldormRadiant < MathF.PI * 2)
                _moldormRadiant += MathF.PI * 2;
            _moldormRadiant %= MathF.PI * 2;

            var radiantIndex = (int)(((_moldormRadiant + MathF.PI / 8) / (2 * MathF.PI)) * 8) % 8;
            _animator.Play("moldorm_head_" + (8 - radiantIndex) % 8);

            _moldormPositions[0] = position.Position;

            if (!_aiDamageState.IsInDamageState())
            {
                if (_moldormHit)
                {
                    _moldormHit = false;
                    _moldormSpeedUp.Reset();
                }

                _partDist = new float[] { 12, 12, 12, 10 };
            }
            else
            {
                for (int i = 0; i < _partDist.Length; i++)
                {
                    if (_partDist[i] > 0)
                    {
                        _partDist[i] -= Game1.TimeMultiplier * 1.75f;
                        if (_partDist[i] < 0)
                            _partDist[i] = 0;
                        break;
                    }
                }
            }

            var partPos = 0f;
            var partIndex = 0;
            var targetDist = _partDist[0] / TailMult;

            for (int i = 1; i < _moldormPositions.Length; i++)
            {
                // this loop is only used to make sure to not have and endless loop incase of a problem with the code below
                while (partIndex + 1 < _moldormPositions.Length)
                {
                    var dist = (_moldormPositions[partIndex + 1] - _moldormPositions[partIndex]).Length();
                    if (dist - partPos >= targetDist)
                    {
                        var percentage = dist > 0 ? (dist - partPos - targetDist) / dist : 1;
                        var newPosition = Vector2.Lerp(_moldormPositions[partIndex + 1], _moldormPositions[partIndex], percentage);
                        partPos += targetDist;
                        if (i / TailMult < _partDist.Length)
                            targetDist = _partDist[i / TailMult] / TailMult;

                        _moldormPositionsNew[i] = newPosition;

                        break;
                    }
                    else
                    {
                        partIndex++;
                        targetDist -= (dist - partPos);
                        partPos = 0;
                    }
                }

                // the tail is not expaneded
                if (partIndex + 1 >= _moldormPositions.Length)
                    _moldormPositionsNew[i] = _moldormPositions[_moldormPositions.Length - 1];
            }

            for (int i = 1; i < _moldormPositions.Length; i++)
                _moldormPositions[i] = _moldormPositionsNew[i];

            // set the newly calculated positions
            for (var i = 0; i < _moldormTails.Length; i++)
                _moldormTails[i].EntityPosition.Set(_moldormPositions[(i + 1) * TailMult]);
        }

        #endregion

        #region state init

        private void InitIdle()
        {
            Game1.GameManager.StartDialogPath("final_boss_intro");
        }

        private void UpdateIdle()
        {
            if (!Game1.GameManager.DialogIsRunning() &&
                !Game1.GameManager.InGameOverlay.TextboxOverlay.IsOpen)
            {
                _aiComponent.ChangeState("moveBody");
            }
        }

        private void InitMoveBody()
        {
            Game1.GameManager.PlaySoundEffect("D360-53-35");
        }

        private void UpdateBodyPartPosition(float state)
        {
            var direction = _bodyPosition - EntityPosition.Position;

            // this is supposed to make it look better by moving the first element more than the following elements
            // TODO: should be replaced by constant speed up moving up?
            var stateRad = MathF.PI / 2 - state * MathF.PI / 2;
            var max = MathF.Sin(stateRad);
            var min = MathF.Sin(stateRad - MathF.PI / 2);
            for (var i = 0; i < _bodyParts.Length; i++)
            {
                var percentage = stateRad - (float)(i + 1) / (_bodyParts.Length + 1) * MathF.PI / 2;
                var sinState = (MathF.Sin(percentage) - min) / (max - min);
                _bodyParts[i] = EntityPosition.Position + direction * (1 - sinState);
            }
        }

        private void UpdateMoveBody()
        {
            _moveSpeed = AnimationHelper.MoveToTarget(_moveSpeed, 1.75f, 0.05f * Game1.TimeMultiplier);
            _bodyPosition = AnimationHelper.MoveToTarget(_bodyPosition, _bodyTargetPosition, _moveSpeed * Game1.TimeMultiplier);

            var bodyState = Math.Clamp(0.5f - (_bodyPosition.Y - _bodyTargetPosition.Y) / (_moveDist * 0.5f), 0, 0.5f);
            UpdateBodyPartPosition(bodyState);

            if (_bodyPosition == _bodyTargetPosition)
            {
                _moveSpeed = 0.25f;
                _animatorBody.Play("spawn");
                _aiComponent.ChangeState("moveHead");
            }
        }

        private void UpdateMoveHead()
        {
            _moveSpeed = AnimationHelper.MoveToTarget(_moveSpeed, 1.75f, 0.05f * Game1.TimeMultiplier);
            var newPosition = AnimationHelper.MoveToTarget(EntityPosition.Position, _bodyTargetPosition, _moveSpeed * Game1.TimeMultiplier);
            EntityPosition.X = newPosition.X;
            EntityPosition.Y = newPosition.Y;

            var bodyState = Math.Clamp(2.5f - (EntityPosition.Y - _bodyTargetPosition.Y) / (_moveDist * 0.5f), 0.5f, 1.0f);
            UpdateBodyPartPosition(bodyState);

            if (EntityPosition.Position == _bodyTargetPosition)
                _aiComponent.ChangeState("wobble");
        }

        private void InitWobble()
        {
            _animatorBody.Play("wobble");
        }

        private void UpdateWobble()
        {
            if (!_animatorBody.IsPlaying)
                _aiComponent.ChangeState("despawn");
        }

        private void InitStartDespawn()
        {
            _hideBody = true;
            _animator.Play("despawn");
            Game1.GameManager.PlaySoundEffect("D360-04-04");
        }

        private void UpdateDespawn()
        {
            if (!_animator.IsPlaying)
                _aiComponent.ChangeState("slimeSpawn");
        }

        #endregion

        #region state explode

        private void InitExplode()
        {
            _pushRepel = false;
            Game1.GameManager.PlaySoundEffect("D378-55-37");
            ExplodeAnimation();
        }

        private void ExplodeAnimation()
        {
            _animator.Play("head");

            _damageField.IsActive = false;
            _bodyShadow.IsActive = false;
            _drawComponent.Layer = Values.LayerBottom;

            ExplosionParticle();
        }

        private void ExplosionParticle()
        {
            for (int i = 0; i < 8; i++)
            {
                var radiant = i / 4f * MathF.PI;
                var velocity = new Vector2(MathF.Sin(radiant), MathF.Cos(radiant)) * 2.5f;
                var objParticle0 = new BossFinalBossParticle(Map, new Vector2(EntityPosition.X, EntityPosition.Y - EntityPosition.Z), velocity);
                Map.Objects.SpawnObject(objParticle0);
            }
        }

        private void InitDespawn()
        {
            _animator.Play("slime_despawn");
        }

        private void UpdateDepawn()
        {
            if (!_animator.IsPlaying)
                _aiComponent.ChangeState("move");
        }

        private void InitMove()
        {
            Game1.GameManager.PlaySoundEffect("D360-53-35");
            _body.CollisionTypes = Values.CollisionTypes.None;
        }

        private void UpdateMove()
        {
            var direction = new Vector2(_roomRectangle.Center.X, _roomRectangle.Y + 43) - EntityPosition.Position;

            if (direction.Length() < 2)
            {
                _body.CollisionTypes = Values.CollisionTypes.Normal;
                _aiComponent.ChangeState("moveWait");
            }
            else
            {
                direction.Normalize();
                var targetVelocity = AnimationHelper.MoveToTarget(new Vector2(_body.Velocity.X, _body.Velocity.Y), direction, 0.15f * Game1.TimeMultiplier);
                _body.Velocity.X = targetVelocity.X;
                _body.Velocity.Y = targetVelocity.Y;
            }
        }

        private void EndMoveWait()
        {
            if (_moveCounter == 0)
                _aiComponent.ChangeState("moldormSpawn");
            else if (_moveCounter == 1)
                _aiComponent.ChangeState("ganonSpawn");

            _moveCounter++;
        }

        #endregion

        #region state slime

        private void InitSlimeSpawn()
        {
            _slimeForm = true;
            _hideHead = false;
            _pushRepel = true;
            _bodyShadow.IsActive = true;
            _damageField.IsActive = true;

            _hittableComponent.HittableBox = new CBox(EntityPosition, -8, -8, 0, 16, 16, 8, true);
            _hittableComponent.IsActive = true;

            _drawComponent.Layer = Values.LayerPlayer;

            _animator.Play("slime_spawn");
        }

        private void UpdateSlimeSpawn()
        {
            if (!_animator.IsPlaying)
                _aiComponent.ChangeState("slimeJump");
        }

        private void InitSlimeJump()
        {
            _animator.Play("slime_jump");

            var playerDirection = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            if (playerDirection != Vector2.Zero)
                playerDirection.Normalize();

            _body.IsGrounded = false;
            _body.Velocity.X = playerDirection.X * 0.5f;
            _body.Velocity.Y = playerDirection.Y * 0.5f;
            _body.Velocity.Z = 1.75f;
        }

        private void UpdateSlimeJump()
        {
            if (_body.IsGrounded)
            {
                Game1.GameManager.PlaySoundEffect("D360-32-20");

                if (Game1.RandomNumber.Next(0, 2) == 0)
                    _aiComponent.ChangeState("slimeWait");
                else
                    _aiComponent.ChangeState("slimeDespawn");
            }
            else if (_body.Velocity.Y < -0.5f)
            {
                _animator.Play("slime_land");
            }
        }

        private void InitSlimeWait()
        {
            _animator.Play("slime");
        }

        private void InitSlimeDespawn()
        {
            _bodyShadow.IsActive = false;
            _damageField.IsActive = false;
            _hittableComponent.IsActive = false;
            _pushRepel = false;

            _animator.Play("slime_despawn");

            EntityPosition.Offset(new Vector2(0, -2));
        }

        private void UpdateSlimeDespawn()
        {
            if (!_animator.IsPlaying)
                _aiComponent.ChangeState("slimeHidden");
        }

        private void InitSlimeHidden()
        {
            _hideHead = true;
        }

        private void EndSlimeHidden()
        {
            // random new position
            EntityPosition.Set(RandomRoomPosition());

            _aiComponent.ChangeState("slimeSpawn");
        }

        private void InitSlimeDamaged()
        {
            _animator.Play("slime_damaged");
            Game1.GameManager.PlaySoundEffect("D360-55-37");
        }

        private void TickSlimeDamaged(double counter)
        {
            var time = SlimeDamageTime - counter;
            if (time < AiDamageState.CooldownTime && time % (AiDamageState.BlinkTime * 2) < AiDamageState.BlinkTime)
                Sprite.SpriteShader = Resources.DamageSpriteShader0;
            else
                Sprite.SpriteShader = null;
        }

        private void EndSlimeDamge()
        {
            _slimeForm = false;

            if (_slimeLives <= 0)
                _aiComponent.ChangeState("slimeExplode");
            else
                _aiComponent.ChangeState("slimeDespawn");
        }

        private void InitSlimeHideExplode()
        {
            _pushRepel = false;
            _hideHead = true;
            _bodyShadow.IsActive = false;
            _damageField.IsActive = false;
            _hittableComponent.IsActive = false;

            InitSlimeExplode();
        }

        private void InitSlimeExplode()
        {
            EntityPosition.Offset(new Vector2(0, -2));
            Game1.GameManager.PlaySoundEffect("D370-33-21");

            _body.VelocityTarget = Vector2.Zero;
            ExplodeAnimation();
        }

        #endregion

        #region state man

        // @TODO:
        // type 2 spawn rate

        private void SlimeEnd()
        {
            _aiComponent.ChangeState("manMove");
            _manTargetPosition = new Vector2(_roomRectangle.Center.X, _roomRectangle.Y + 43);
        }

        private void InitManSpawn()
        {
            _animator.Play("man_spawn");
        }

        private void UpdateManSpawn()
        {
            if (_animator.IsPlaying)
                return;

            _pushRepel = true;
            _damageField.IsActive = true;
            Game1.GameManager.PlaySoundEffect("D370-35-23");
            _aiComponent.ChangeState("manPreAttack");
        }

        private void TestProjectileSpawn()
        {
            _animator.Play("man_" + _direction);
            _objFireball = new BossFinalBossFireball(this, EntityPosition.Position + _fireballOffset[_direction]);
            Map.Objects.SpawnObject(_objFireball);

            _objFireball.Fire();
        }

        private void UpdateManPreAttack()
        {
            var playerDirection = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            _direction = AnimationHelper.GetDirection(playerDirection);

            if (_manInit)
            {
                _animator.Play("man_" + _direction);
                _animator.Pause();
            }
            else
            {
                _animator.Play("man_attack_" + _direction);
            }
        }

        private void InitManAttack()
        {
            _manInit = false;
            _animator.Play("man_" + _direction);
            Game1.GameManager.PlaySoundEffect("D370-34-22");

            _objFireball = new BossFinalBossFireball(this, EntityPosition.Position + _fireballOffset[_direction]);
            Map.Objects.SpawnObject(_objFireball);
        }

        private void UpdateManAttack()
        {
            var playerDirection = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            var newDirection = AnimationHelper.GetDirection(playerDirection);

            if (newDirection != _direction)
            {
                _direction = newDirection;
                _animator.Play("man_" + _direction);
                _objFireball.EntityPosition.Set(EntityPosition.Position + _fireballOffset[_direction]);
            }

            if (_objFireball.IsReady)
            {
                _aiComponent.ChangeState("manPostAttack");
                _objFireball.Fire();
            }
        }

        private void InitPostAttack()
        {
            _animator.Play("man_attack_" + _direction);
        }

        private void UpdateManPostAttack() { }

        private void InitManDespawn()
        {
            _pushRepel = false;
            _damageField.IsActive = false;
            _animator.Play("man_despawn");
        }

        private void UpdateManDespawn()
        {
            if (!_animator.IsPlaying)
                _aiComponent.ChangeState("manMove");
        }

        private void InitManMove()
        {
            Game1.GameManager.PlaySoundEffect("D360-53-35");
            _body.CollisionTypes = Values.CollisionTypes.None;
            _manTargetPosition = RandomRoomPositionSide();
        }

        private void UpdateManMove()
        {
            var direction = _manTargetPosition - EntityPosition.Position;

            if (direction.Length() < 2)
            {
                _body.CollisionTypes = Values.CollisionTypes.Normal;
                _aiComponent.ChangeState("manMoveWait");
            }
            else
            {
                direction.Normalize();
                var targetVelocity = AnimationHelper.MoveToTarget(new Vector2(_body.Velocity.X, _body.Velocity.Y), direction, 0.15f * Game1.TimeMultiplier);
                _body.Velocity.X = targetVelocity.X;
                _body.Velocity.Y = targetVelocity.Y;
            }
        }

        private void InitManRotate()
        {
            Game1.GameManager.PlaySoundEffect("D360-54-36");
            _animator.Play("man_rotate");
            // first frame = up
            // make sure to not jump to a different direction
            _animator.SetFrame((_direction + 3) % 4);
        }

        private void TickRotate(double time)
        {
            // speed up the rotation speed
            var rotateSpeed = Math.Clamp((RotateTime - (float)time) / (RotateTime * 0.65f), 0, 1);
            _animator.SpeedMultiplier = rotateSpeed * 3 + 1;
        }

        private void EndRotate()
        {
            _pushRepel = false;
            EntityPosition.Set(new Vector2(EntityPosition.X, EntityPosition.Y - 4));

            _animator.SpeedMultiplier = 1;
            _aiComponent.ChangeState("explode");
        }

        #endregion

        public bool HitBoss(GameObject origin, Vector2 direction, CBox damageBox)
        {
            // getting hit by the fireball?
            if (!_aiDamageState.IsInDamageState() && _aiComponent.CurrentStateId == "manPostAttack" && damageBox.Box.Intersects(_hittableBoxMan.Box))
            {
                _aiDamageState.OnHit(origin, direction, HitType.Boss, 1, false);

                _manLives--;
                if (_manLives <= 0)
                    _aiComponent.ChangeState("manRotate");

                return true;
            }

            return false;
        }

        private void OnZeroLives(bool pieceOfPower) { }

        private Vector2 RandomRoomPositionSide()
        {
            // always change the side
            _sideIndex = (_sideIndex + Game1.RandomNumber.Next(1, 4)) % 4;
            if (_sideIndex == 0)
                return new Vector2(_roomRectangle.X + 16 + 16, _roomRectangle.Y + 32 + 16 + Game1.RandomNumber.Next(0, 2 * 16));
            else if (_sideIndex == 2)
                return new Vector2(_roomRectangle.X + 128, _roomRectangle.Y + 32 + 16 + Game1.RandomNumber.Next(0, 2 * 16));
            else if (_sideIndex == 1)
                return new Vector2(_roomRectangle.X + 32 + 16 + Game1.RandomNumber.Next(0, 4 * 16), _roomRectangle.Y + 32);
            else
                return new Vector2(_roomRectangle.X + 32 + 16 + Game1.RandomNumber.Next(0, 4 * 16), _roomRectangle.Y + 100);
        }

        private Vector2 RandomRoomPosition()
        {
            var posY = Game1.RandomNumber.Next(0, 5 * 16);
            int posX;
            if (16 < posY || posY < 64 - 16)
                posX = Game1.RandomNumber.Next(16, 6 * 16);
            else
                posX = Game1.RandomNumber.Next(0, 8 * 16);

            return new Vector2(_roomRectangle.X + 24 + posX, _roomRectangle.Y + 24 + posY);
        }

        private void Update()
        {
            _animatorBody.Update();

            if (_drawGanonWeapon)
                _animatorWeapon.Update();
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            // draw the tail
            if (_drawMoldormTail)
                for (var i = _moldormTails.Length - 1; i >= 0; i--)
                {
                    if (i != _moldormTails.Length - 1)
                        _moldormTails[i].Sprite.SpriteShader = Sprite.SpriteShader;
                    _moldormTails[i].Sprite.Draw(spriteBatch);
                }

            // draw the body parts
            if (!_hideBody)
                for (var i = 0; i < _bodyParts.Length; i++)
                    DrawHelper.DrawNormalized(spriteBatch, _spriteBody, _bodyParts[i], Color.White);

            if (!_hideBody)
                _animatorBody.Draw(spriteBatch, _bodyPosition, Color.White);

            if (Sprite.SpriteShader != null)
            {
                spriteBatch.End();
                ObjectManager.SpriteBatchBegin(spriteBatch, Sprite.SpriteShader);
            }

            // draw the weapon of ganon
            if (_drawGanonWeapon)
                _animatorWeapon.Draw(spriteBatch, EntityPosition.Position, Color.White);

            if (!_hideHead)
                Sprite.Draw(spriteBatch);

            if (Sprite.SpriteShader != null)
            {
                spriteBatch.End();
                ObjectManager.SpriteBatchBegin(spriteBatch, Sprite.SpriteShader);
            }

            if (_finalState && (_finalEyeState == 1 || (_finalEyeState == 0 && _animatorEye.IsPlaying)))
                _animatorEye.Draw(spriteBatch, new Vector2(EntityPosition.X, EntityPosition.Y + 1), Color.White);

            if (Sprite.SpriteShader != null)
            {
                spriteBatch.End();
                ObjectManager.SpriteBatchBegin(spriteBatch, null);
            }
        }

        public Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            // slime attacked by powder
            if (_slimeForm && (damageType & HitType.MagicPowder) != 0)
            {
                _body.Velocity.X = direction.X;
                _body.Velocity.Y = direction.Y;
                if (_body.Velocity.Z > 0)
                    _body.Velocity.Z = 0;

                _aiComponent.ChangeState("slimeDamaged");
                _slimeLives--;

                return Values.HitCollision.Enemy;
            }

            if ((damageType & (HitType.Sword | HitType.SwordHold)) != 0 &&
                (_aiComponent.CurrentStateId == "slimeJump" || _aiComponent.CurrentStateId == "slimeWait"))
            {
                _aiComponent.ChangeState("slimeHideExplode");
                return Values.HitCollision.Enemy;
            }

            if (_aiComponent.CurrentStateId == "moldorm")
            {
                return Values.HitCollision.RepellingParticle;
            }

            // ganon damage form
            if (_ganonForm && !_aiDamageState.IsInDamageState() && (damageType & HitType.PegasusBootsSword) != 0)
            {
                _ganonLives -= damage;
                if (_ganonLives <= 0)
                    _aiComponent.ChangeState("ganonDeath");

                Game1.GameManager.PlaySoundEffect("D370-07-07");
                _aiDamageState.SetDamageState();

                return Values.HitCollision.Repelling;
            }
            if (_ganonForm && (damageType & HitType.Sword) != 0)
            {
                return Values.HitCollision.Repelling;
            }

            if (_aiComponent.CurrentStateId == "face" && !_aiDamageState.IsInDamageState())
            {
                if ((damageType & (HitType.Hookshot | HitType.MagicRod | HitType.Bomb | HitType.Boomerang | HitType.Bow)) != 0)
                {
                    _aiDamageState.SetDamageState();
                    _aiComponent.ChangeState("faceExplode");
                    return Values.HitCollision.Enemy;
                }
                else
                {
                    _aiDamageState.SetDamageState(false);

                    _body.Velocity.X += direction.X;
                    _body.Velocity.Y += direction.Y;

                    Game1.GameManager.PlaySoundEffect("D360-09-09");
                    Game1.GameManager.PlaySoundEffect("D370-17-11");

                    return Values.HitCollision.Repelling | Values.HitCollision.Repelling1;
                }
            }

            if (_aiComponent.CurrentStateId == "final")
            {
                // bow hit from below with an open eye
                if (!_aiDamageState.IsInDamageState() && (
                    (_finalEyeState == 0 && _animatorEye.CurrentFrameIndex < 1) ||
                    (_finalEyeState == 1 && _animatorEye.CurrentFrameIndex >= 1)) && (damageType & HitType.Bow) != 0 &&
                    MathF.Abs(direction.Y) > MathF.Abs(direction.X) && direction.Y < 0)
                {
                    _aiDamageState.SetDamageState();

                    _finalStateLives--;
                    if (_finalStateLives <= 0)
                        _aiComponent.ChangeState("finalBlink");

                    Game1.GameManager.PlaySoundEffect("D370-07-07");

                    // randomly change the speed of the two parts
                    _finalPartSpeed0 = (1 / 2500.0f * MathF.PI * 2) * (1 + (Game1.RandomNumber.Next(0, 101) - 50) / 500f);
                    _finalPartSpeed1 = (1 / 2500.0f * MathF.PI * 2) * (1 + (Game1.RandomNumber.Next(0, 101) - 50) / 500f);

                    return Values.HitCollision.Enemy;
                }
                else
                {
                    return Values.HitCollision.RepellingParticle;
                }
            }

            return Values.HitCollision.None;
        }

        public Values.HitCollision HitTail(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (!_aiDamageState.IsInDamageState() && _aiComponent.CurrentStateId == "moldorm" && ((damageType & HitType.Sword) != 0))
            {
                _moldormLives -= damage;
                if (_moldormLives <= 0)
                    _aiComponent.ChangeState("moldormDying");

                Game1.GameManager.PlaySoundEffect("D370-07-07");
                Game1.GameManager.StopSoundEffect("D360-56-38");

                _moldormHit = true;
                _aiDamageState.SetDamageState(true);
                return Values.HitCollision.Enemy;
            }

            return Values.HitCollision.None;
        }

        private void OnCollision(Values.BodyCollision collision)
        {
            if (_aiComponent.CurrentStateId == "moldorm")
            {
                if (Game1.RandomNumber.Next(0, 2) == 0)
                    _moldormDirection = -_moldormDirection;

                if ((collision & Values.BodyCollision.Horizontal) != 0)
                    _moldormRadiant = (float)Math.Atan2(-_body.VelocityTarget.X * _directionChangeMultiplier, _body.VelocityTarget.Y);
                else if ((collision & Values.BodyCollision.Vertical) != 0)
                    _moldormRadiant = (float)Math.Atan2(_body.VelocityTarget.X, -_body.VelocityTarget.Y * _directionChangeMultiplier);

                _directionChangeMultiplier *= 0.75f;
            }

            if (_aiComponent.CurrentStateId == "face")
            {
                if ((collision & Values.BodyCollision.Horizontal) != 0)
                {
                    _body.Velocity.X = -_body.VelocityTarget.X * 0.125f;
                    _body.VelocityTarget.X = 0;
                }
                else if ((collision & Values.BodyCollision.Vertical) != 0)
                {
                    _body.Velocity.Y = -_body.VelocityTarget.Y * 0.125f;
                    _body.VelocityTarget.Y = 0;
                }
            }
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (!_pushRepel)
                return false;

            return true;
        }
    }
}