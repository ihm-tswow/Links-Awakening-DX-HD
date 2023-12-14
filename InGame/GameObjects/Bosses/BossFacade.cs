using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.GameObjects.Enemies;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Bosses
{
    class BossFacade : GameObject
    {
        private ObjStone[] _objPots = new ObjStone[4];
        private float[] _potCounter = new float[4];
        private bool[] _potIsActive = new bool[4];

        private Vector2[] _potPositions = { new Vector2(-4, -2), new Vector2(3, 3), new Vector2(-4, 3), new Vector2(3, -2) };
        private Vector2[] _tilePositions =
        {
            new Vector2(-4, -1), new Vector2(-4, 0), new Vector2(-4, 1), new Vector2(-4, 2),
            new Vector2(3, -1), new Vector2(3, 0), new Vector2(3, 1), new Vector2(3, 2),
            new Vector2(-3, -2), new Vector2(-2, -2), new Vector2(-1, -2), new Vector2(0, -2), new Vector2(1, -2), new Vector2(2, -2),
            new Vector2(-3, 3), new Vector2(-2, 3), new Vector2(-1, 3), new Vector2(0, 3), new Vector2(1, 3), new Vector2(2, 3)
        };

        private int[] _tileOrder =
        {
            12, 16, 18, 15,
            14, 19, 17, 13,
            0, 2, 4, 6, 8, 10,
            11, 9, 7, 5, 3, 1
        };

        private readonly Animator _animatorEyes;
        private Animator _animatorMouth;
        private AiComponent _aiComponent;
        private SpriteShader _drawEffect;

        private RectangleF _triggerField;
        private readonly AiTriggerCountdown _blinkTigger;

        private readonly string _saveKey;
        private readonly string _saveKeyHeart;
        private readonly string _tileString;

        private int _blinkCount;
        private int _currentLives = 5;

        private const int DespawnTime = 1150;

        private bool _hittable;
        private bool _wasHit;

        private bool _spawnHoles;
        private float _holeCounter = -3000;

        private float _deathCount = -1000;

        private bool _shakeScreen;

        public BossFacade() : base("facade") { }

        public BossFacade(Map.Map map, int posX, int posY, string saveKey, string saveKeyHeart) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 16, posY, 0);
            EntitySize = new Rectangle(-24, -6, 48, 40);

            _saveKey = saveKey;
            _saveKeyHeart = saveKeyHeart;

            _tileString = saveKey + "tiles";

            _triggerField = Map.GetField(posX, posY, 16);

            SpawnTilesAndPots();

            if (!string.IsNullOrWhiteSpace(saveKey) && Game1.GameManager.SaveManager.GetString(saveKey) == "1")
            {
                // respawn the heart if the player died after he killed the boss without collecting the heart
                SpawnHeart();

                IsDead = true;
                return;
            }

            _animatorEyes = AnimatorSaveLoad.LoadAnimator("Nightmares/facade");
            _animatorMouth = AnimatorSaveLoad.LoadAnimator("Nightmares/facade");

            var hittableRectangle = new CBox(EntityPosition, -12, 4, 24, 24, 8);
            var damageCollider = new CBox(EntityPosition, -12, -12, 24, 24, 8);

            var stateInit = new AiState(UpdateInit);
            var statePre = new AiState();
            statePre.Trigger.Add(new AiTriggerCountdown(3500, null, () => _aiComponent.ChangeState("spawn")));
            var stateSpawn = new AiState { Init = InitSpawn };
            stateSpawn.Trigger.Add(new AiTriggerCountdown(1000, null, () => _aiComponent.ChangeState("preBlink")));
            var statePreBlink = new AiState { Init = InitPreBlink };
            statePreBlink.Trigger.Add(new AiTriggerCountdown(1800, null, () => _aiComponent.ChangeState("blink")));
            var stateBlink = new AiState(UpdateBlink) { Init = InitBlink };
            var statePostBlink = new AiState(UpdatePostBlink);
            statePostBlink.Trigger.Add(new AiTriggerCountdown(1000, null, ToDialog));
            var stateDialog = new AiState(UpdateDialog);
            var stateIdle = new AiState(UpdateIdle);
            stateIdle.Trigger.Add(new AiTriggerRandomTime(BlinkAnimation, 1000, 2500));
            var stateDespawn = new AiState();
            stateDespawn.Trigger.Add(new AiTriggerCountdown(DespawnTime, DespawnTick, EndDespawn));
            var stateHidden = new AiState();
            stateHidden.Trigger.Add(new AiTriggerCountdown(2750, null, () => _aiComponent.ChangeState("respawn")));
            var stateRespawn = new AiState(UpdateRespawn) { Init = InitRespawn };
            var statePreDeath = new AiState();
            statePreDeath.Trigger.Add(new AiTriggerCountdown(1500, null, () => _aiComponent.ChangeState("death")));
            var stateDeath = new AiState(UpdateDeath);
            stateDeath.Trigger.Add(new AiTriggerCountdown(3000 / AiDamageState.BlinkTime * AiDamageState.BlinkTime, UpdateBlink, DeathAnimationEnd));

            _aiComponent = new AiComponent();
            _aiComponent.Trigger.Add(new AiTriggerUpdate(UpdateAnimations));
            _aiComponent.Trigger.Add(_blinkTigger = new AiTriggerCountdown(AiDamageState.BlinkTime * 4 * 2, BlinkTick, null));
            _aiComponent.Trigger.Add(new AiTriggerUpdate(Update));

            _aiComponent.States.Add("init", stateInit);
            _aiComponent.States.Add("preSpawn", statePre);
            _aiComponent.States.Add("spawn", stateSpawn);
            _aiComponent.States.Add("preBlink", statePreBlink);
            _aiComponent.States.Add("blink", stateBlink);
            _aiComponent.States.Add("postBlink", statePostBlink);
            _aiComponent.States.Add("dialog", stateDialog);
            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("despawn", stateDespawn);
            _aiComponent.States.Add("hidden", stateHidden);
            _aiComponent.States.Add("respawn", stateRespawn);
            _aiComponent.States.Add("preDeath", statePreDeath);
            _aiComponent.States.Add("death", stateDeath);

            _aiComponent.ChangeState("init");

            AddComponent(AiComponent.Index, _aiComponent);
            //AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageCollider, HitType.Enemy, 2));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableRectangle, OnHit));
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerBottom, EntityPosition));
            AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(OnKeyChange));

            _potCounter[0] = -600;
            _potCounter[1] = -300;
            _potCounter[2] = -300;
            _potCounter[3] = -300;
        }

        private void OnKeyChange()
        {
            var tileState = Game1.GameManager.SaveManager.GetString(_tileString);
            if (tileState == "17")
            {
                // get the first pot that is still on the floor
                for (var i = 0; i < _objPots.Length; i++)
                {
                    if (!_objPots[i].MakeFlyingStone())
                        continue;
                    _potIsActive[i] = true;

                    break;
                }
            }

            if (tileState == "20")
            {
                _spawnHoles = true;
            }
        }

        private void Update()
        {
            if (_shakeScreen)
                Game1.GameManager.ShakeScreenContinue(50, 1, 0, 0.55f, 0);

            // update pot
            for (var i = 0; i < _objPots.Length; i++)
            {
                if (!_potIsActive[i])
                    continue;

                _potCounter[i] += Game1.DeltaTime;

                // move up
                if (_potCounter[i] > 0)
                    _objPots[i].EntityPosition.Z += 0.25f * Game1.TimeMultiplier;

                if (_objPots[i].EntityPosition.Z > 12)
                {
                    _objPots[i].EntityPosition.Z = 12;

                    // start the throw?
                    if (_potCounter[i] > 1600)
                    {
                        _potIsActive[i] = false;
                        ThrowPot(_objPots[i]);

                        // activate the next pot
                        for (var j = i + 1; j < _objPots.Length; j++)
                        {
                            if (!_objPots[j].MakeFlyingStone())
                                continue;
                            _potIsActive[j] = true;
                            break;
                        }
                    }
                }
            }

            if (_spawnHoles)
            {
                _holeCounter -= Game1.DeltaTime;
                if (_holeCounter < 0)
                {
                    _holeCounter = Game1.RandomNumber.Next(1600, 3500);

                    var posX = EntityPosition.X - 40 + Game1.RandomNumber.Next(0, 80);
                    var posY = EntityPosition.Y - 8 + Game1.RandomNumber.Next(0, 48);
                    var objHole = new BossFacadeHole(Map, new Vector2(posX, posY));
                    Map.Objects.SpawnObject(objHole);
                }
            }
        }

        private void ThrowPot(ObjStone objPot)
        {
            if (_currentLives <= 0)
            {
                objPot.LetGo();
                return;
            }

            var playerDirection = MapManager.ObjLink.EntityPosition.Position -
                                  new Vector2(objPot.EntityPosition.X, objPot.EntityPosition.Y - objPot.EntityPosition.Z + 2);
            if (playerDirection != Vector2.Zero)
                playerDirection.Normalize();

            objPot.ThrowStone(playerDirection * 2f);
        }

        private void SpawnTilesAndPots()
        {
            for (var i = 0; i < _potPositions.Length; i++)
            {
                var parameter = MapData.GetParameter("pot", null);
                parameter[1] = (int)(EntityPosition.X + _potPositions[i].X * Values.TileSize);
                parameter[2] = (int)(EntityPosition.Y + _potPositions[i].Y * Values.TileSize);

                _objPots[i] = (ObjStone)ObjectManager.GetGameObject(Map, "pot", parameter);
                Map.Objects.SpawnObject(_objPots[i]);
            }

            Game1.GameManager.SaveManager.SetString(_tileString, "0");

            for (var i = 0; i < _tilePositions.Length; i++)
            {
                var posX = (int)(EntityPosition.X + _tilePositions[i].X * Values.TileSize);
                var posY = (int)(EntityPosition.Y + _tilePositions[i].Y * Values.TileSize);
                // tile index starts at 1 so that they do not start automatically
                var flyingTile = new EnemyFlyingTile(Map, posX, posY, _tileString, _tileOrder[i] + 1, 1);
                Map.Objects.SpawnObject(flyingTile);
            }
        }

        private void UpdateDeath()
        {
            _deathCount += Game1.DeltaTime;
            if (_deathCount < 100)
                return;

            _deathCount -= 100;

            Game1.GameManager.PlaySoundEffect("D378-19-13");

            var posX = (int)EntityPosition.X + Game1.RandomNumber.Next(0, 32) - 8 - 16;
            var posY = (int)EntityPosition.Y - (int)EntityPosition.Z + Game1.RandomNumber.Next(0, 32) - 8;

            // spawn explosion effect
            Map.Objects.SpawnObject(new ObjAnimator(Map, posX, posY, Values.LayerTop, "Particles/spawn", "run", true));
        }

        private void UpdateBlink(double time)
        {
            var blinkTime = AiDamageState.BlinkTime;
            _drawEffect = time % (blinkTime * 2) < blinkTime ? Resources.DamageSpriteShader0 : null;
        }

        private void UpdateInit()
        {
            if (_triggerField.Contains(MapManager.ObjLink.BodyRectangle))
            {
                Game1.GameManager.SetMusic(24, 2);
                _aiComponent.ChangeState("preSpawn");
            }
        }

        private void BlinkTick(double time)
        {
            var blinkTime = AiDamageState.BlinkTime;
            _drawEffect = time % (blinkTime * 2) >= blinkTime ? Resources.DamageSpriteShader0 : null;
        }

        private void DespawnTick(double counter)
        {
            _animatorEyes.Play(counter > (DespawnTime * (16 / 70f)) ? "eye_half" : "eye_closed");
            _animatorMouth.Play(counter > (DespawnTime * (32 / 70f)) ? "mouth_opened" : "mouth_closed");
        }

        private void EndDespawn()
        {
            _aiComponent.ChangeState("hidden");
        }

        private void InitSpawn()
        {
            _hittable = true;
            _animatorEyes.Play("eye_closed");
            _animatorMouth.Play("mouth_closed");
        }

        private void InitPreBlink()
        {
            _animatorEyes.Play("eye_half");
        }

        private void InitBlink()
        {
            _blinkCount = 0;
            _animatorEyes.Play("eye_blink");
        }

        private void UpdateBlink()
        {
            // blink 2 times and change to eyes opened state
            if (!_animatorEyes.IsPlaying)
            {
                _blinkCount++;
                if (_blinkCount >= 2)
                {
                    _animatorEyes.Play("eye_half");
                    _animatorMouth.Play("mouth_opened");
                    _aiComponent.ChangeState("postBlink");
                    return;
                }

                _animatorEyes.Play("eye_blink");
            }
        }

        private void UpdatePostBlink()
        {
            if (!_animatorEyes.IsPlaying)
            {
                _animatorEyes.Play("eye");
            }
        }

        private void ToDialog()
        {
            _aiComponent.ChangeState("dialog");
            Game1.GameManager.StartDialogPath("facade_opening");
        }

        private void UpdateDialog()
        {
            // finished the dialog
            if (!Game1.GameManager.InGameOverlay.TextboxOverlay.IsOpen)
            {
                // start the tile action
                Game1.GameManager.SaveManager.SetString(_tileString, "1");

                _shakeScreen = true;

                _aiComponent.ChangeState("idle");
            }
        }

        private void UpdateAnimations()
        {
            _animatorEyes.Update();
            _animatorMouth.Update();
        }

        private void BlinkAnimation()
        {
            _animatorEyes.Play("eye_blink_full");
        }

        private void UpdateIdle()
        {
            if (!_animatorEyes.IsPlaying)
            {
                _animatorEyes.Play("eye");
            }

            // was hit => despawn
            if (_wasHit)
            {
                _wasHit = false;
                ToDespawn();
            }
        }

        private void ToDespawn()
        {
            _hittable = false;
            _aiComponent.ChangeState("despawn");
        }

        private void InitRespawn()
        {
            _animatorEyes.Play("eye_respawn");
            _animatorMouth.Play("mouth_respawn");
        }

        private void UpdateRespawn()
        {
            if (!_animatorEyes.IsPlaying)
            {
                _hittable = true;
                _aiComponent.ChangeState("idle");
            }
        }

        private bool IsVisible()
        {
            return _aiComponent.CurrentStateId != "init" &&
                   _aiComponent.CurrentStateId != "preSpawn" &&
                   _aiComponent.CurrentStateId != "hidden";
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            if (!IsVisible())
                return;

            if (_drawEffect != null)
            {
                spriteBatch.End();
                ObjectManager.SpriteBatchBegin(spriteBatch, _drawEffect);
            }

            // draw the eye and the mouth
            _animatorEyes.Draw(spriteBatch, new Vector2(EntityPosition.X, EntityPosition.Y), Color.White);
            _animatorMouth.Draw(spriteBatch, new Vector2(EntityPosition.X, EntityPosition.Y + 23), Color.White);

            if (_drawEffect != null)
            {
                spriteBatch.End();
                ObjectManager.SpriteBatchBegin(spriteBatch, null);
            }
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (_blinkTigger.IsRunning() || !_hittable || !IsVisible() || (damageType & HitType.Bomb) == 0)
                return Values.HitCollision.None;

            _wasHit = true;

            _currentLives--;

            if (_currentLives <= 0)
            {
                _spawnHoles = false;

                // stop the flying tiles from activating
                Game1.GameManager.SaveManager.SetString(_tileString, "-1");

                _hittable = false;
                _shakeScreen = false;

                _aiComponent.ChangeState("preDeath");
                Game1.GameManager.StartDialogPath("facade_death");
            }

            _blinkTigger.OnInit();

            return Values.HitCollision.None;
        }


        private void DeathAnimationEnd()
        {
            if (!string.IsNullOrEmpty(_saveKey))
                Game1.GameManager.SaveManager.SetString(_saveKey, "1");

            Game1.GameManager.PlaySoundEffect("D378-26-1A");

            SpawnHeart();

            Map.Objects.DeleteObjects.Add(this);
        }

        private void SpawnHeart()
        {
            // spawn big heart
            Map.Objects.SpawnObject(new ObjItem(Map, (int)EntityPosition.X - 8, (int)EntityPosition.Y + 8, "j", _saveKeyHeart, "heartMeterFull", null));
        }
    }
}