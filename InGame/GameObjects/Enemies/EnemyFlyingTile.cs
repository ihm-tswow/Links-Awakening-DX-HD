using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyFlyingTile : GameObject
    {
        private readonly Animator _animator;
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly DrawCSpriteComponent _drawComponent;
        private readonly BodyDrawShadowComponent _shadowComponent;
        private readonly DamageFieldComponent _damageField;
        private readonly AiDamageState _damageState;

        private ObjHole _objHole;

        private RectangleF _triggerField;
        private const float FlySpeed = 1.5f;

        private readonly string _strFly = "fly";
        // we use the key and the index to indicate when the tile should start moving
        private readonly string _strKey;
        private readonly int _index;

        private float _soundCounter;

        // initial time for the activation
        private float _activationCounter = 1500;

        private bool _stringSet;
        private bool _wasActivated;

        public EnemyFlyingTile() : base("flying tile") { }

        public EnemyFlyingTile(Map.Map map, int posX, int posY, string strKey, int index, int mode) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 8, 0);
            EntitySize = new Rectangle(-8, -24, 16, 32);

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/flyingTile");
            _animator.Play("idle_" + mode);

            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, sprite, new Vector2(0, 0));

            _body = new BodyComponent(EntityPosition, -5, -5, 10, 5, 8)
            {
                Gravity = -0.15f,
                IgnoresZ = true,
                CollisionTypes = Values.CollisionTypes.Normal,
                MoveCollision = OnCollision
            };

            // the player needs to be inside the room
            // mode == 1 is for the tiles used for the facade boss; in this mode the tiles should always attack
            _triggerField = map.GetField(posX, posY, mode == 0 ? 16 : 0);

            // mode == 2 is just the gray version for dungeon 7
            if (mode == 2)
                _strFly = "fly_1";

            _strKey = strKey;
            _index = index;

            var stateIdle = new AiState(UpdateIdle);
            var stateAscent = new AiState(UpdateAscent) { Init = InitAscent };
            var stateWait = new AiState();
            stateWait.Trigger.Add(new AiTriggerCountdown(700, null, () => _aiComponent.ChangeState("flying")));
            var stateFlying = new AiState(UpdateFlying) { Init = InitFlying };

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("ascent", stateAscent);
            _aiComponent.States.Add("wait", stateWait);
            _aiComponent.States.Add("flying", stateFlying);
            _damageState = new AiDamageState(this, _body, _aiComponent, sprite, 1) { OnBurn = OnBurn, FlameOffset = new Point(0, 7), ExplosionOffsetY = 7 };
            _aiComponent.ChangeState("idle");

            var damageCollider = new CBox(EntityPosition, -3, -3, 0, 6, 6, 4, true);
            var hittableBox = new CBox(EntityPosition, -5, -5, 0, 10, 10, 8, true);

            AddComponent(DamageFieldComponent.Index, _damageField = new DamageFieldComponent(damageCollider, HitType.Enemy, 2) { OnDamagedPlayer = OnDamagePlayer, IsActive = false });
            AddComponent(BodyComponent.Index, _body);
            AddComponent(PushableComponent.Index, new PushableComponent(hittableBox, OnPush));
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, _drawComponent = new DrawCSpriteComponent(sprite, Values.LayerBottom));
            AddComponent(DrawShadowComponent.Index, _shadowComponent = new BodyDrawShadowComponent(_body, sprite) { IsActive = false, OffsetY = 5 });
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));

            if (!string.IsNullOrEmpty(_strKey))
            {
                if (_index != 0)
                    AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(OnKeyChange));
                else
                    Game1.GameManager.SaveManager.SetString(_strKey, "0");
            }
        }

        public override void Init()
        {
            // get the hole that is under the tile
            var holeList = new List<GameObject>();
            Map.Objects.GetGameObjectsWithTag(holeList, Values.GameObjectTag.Hole,
                (int)EntityPosition.X - 8, (int)EntityPosition.Y - 8, 16, 16);

            if (holeList.Count > 0)
            {
                // there should only be one hole normally
                _objHole = (ObjHole)holeList[0];
                // disable the hole under the tile
                _objHole.SetActive(false);
            }
        }

        private void OnBurn()
        {
            SetSaveString();

            _animator.Pause();
            _body.IgnoresZ = false;
        }

        private void OnDamagePlayer()
        {
            OnDeath(new Vector3(0, 0, 0.25f));
        }

        private void OnKeyChange()
        {
            var stateValue = Game1.GameManager.SaveManager.GetString(_strKey, "-");
            if (stateValue == _index.ToString())
                _wasActivated = true;

            // stop the tile from activating the next tile
            if (stateValue == "-1")
                _wasActivated = false;
        }

        private void SetSaveString()
        {
            if (_stringSet || !_wasActivated)
                return;

            // set the string to activate the next flying tile
            _stringSet = true;
            if (!string.IsNullOrEmpty(_strKey))
                Game1.GameManager.SaveManager.SetString(_strKey, (_index + 1).ToString());
        }

        private void UpdateIdle()
        {
            // check if the player is inside the room
            if ((_index == 0 || _wasActivated) && _triggerField.Contains(MapManager.ObjLink.BodyRectangle))
            {
                _activationCounter -= Game1.DeltaTime;

                if (_activationCounter < 0 || _wasActivated)
                {
                    _wasActivated = true;
                    _aiComponent.ChangeState("ascent");
                }
            }
        }

        private void InitAscent()
        {
            _drawComponent.Layer = Values.LayerPlayer;
            _shadowComponent.IsActive = true;
            _damageField.IsActive = true;
            _animator.Play(_strFly);

            // if there is a hole under the tile enable it
            _objHole?.SetActive(true);
        }

        private void UpdateAscent()
        {
            PlaySound();

            EntityPosition.Z += 0.25f * Game1.TimeMultiplier;

            if (EntityPosition.Z > 12)
            {
                EntityPosition.Z = 12;
                SetSaveString();
                _aiComponent.ChangeState("wait");
            }
        }

        private void InitFlying()
        {
            // start flying towards the player
            var velocity = MapManager.ObjLink.EntityPosition.Position - new Vector2(EntityPosition.X, EntityPosition.Y - 4);
            if (velocity != Vector2.Zero)
                velocity.Normalize();

            _body.VelocityTarget = velocity * FlySpeed;
        }

        private void UpdateFlying()
        {
            PlaySound();
        }

        private void PlaySound()
        {
            _soundCounter -= Game1.DeltaTime;

            if (_soundCounter < 0)
            {
                _soundCounter += 225;
                // @TODO: faster loop
                Game1.GameManager.PlaySoundEffect("D360-63-3F");
            }
        }

        private Values.HitCollision OnHit(GameObject originObject, Vector2 direction, HitType type, int damage, bool pieceOfPower)
        {
            if (_aiComponent.CurrentStateId == "idle")
                return Values.HitCollision.None;

            // burn
            if (type == HitType.MagicRod || type == HitType.MagicPowder)
                return _damageState.OnHit(originObject, direction, type, damage, pieceOfPower);

            OnDeath(new Vector3(direction * 1.0f, 0.1f));

            return Values.HitCollision.Enemy;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (_aiComponent.CurrentStateId == "idle")
                return false;
            if (_aiComponent.CurrentStateId != "flying")
                return true;

            if (type == PushableComponent.PushType.Impact)
                OnDeath(new Vector3(direction * 0.35f, 0.1f));

            return true;
        }

        private void OnCollision(Values.BodyCollision collision)
        {
            if (_aiComponent.CurrentStateId != "flying")
                return;

            OnDeath(new Vector3(0, 0, 0.25f));
        }

        private void OnDeath(Vector3 bodyVelocity)
        {
            SetSaveString();

            Game1.GameManager.PlaySoundEffect("D378-09-09");

            // spawn stone particles
            var rndMin = 50;
            var rndMax = 75;
            var diff = 200f;

            var vector0 = new Vector3(-1, -1, 0) * Game1.RandomNumber.Next(rndMin, rndMax) / diff + bodyVelocity;
            var vector1 = new Vector3(-1, 0, 0) * Game1.RandomNumber.Next(rndMin, rndMax) / diff + bodyVelocity;
            var vector2 = new Vector3(1, -1, 0) * Game1.RandomNumber.Next(rndMin, rndMax) / diff + bodyVelocity;
            var vector3 = new Vector3(1, 0, 0) * Game1.RandomNumber.Next(rndMin, rndMax) / diff + bodyVelocity;

            var stone0 = new ObjSmallStone(Map, (int)EntityPosition.X - 2, (int)EntityPosition.Y - 7, (int)EntityPosition.Z, vector0, true);
            var stone1 = new ObjSmallStone(Map, (int)EntityPosition.X - 2, (int)EntityPosition.Y - 2, (int)EntityPosition.Z, vector1, true);
            var stone2 = new ObjSmallStone(Map, (int)EntityPosition.X + 3, (int)EntityPosition.Y - 7, (int)EntityPosition.Z, vector2, false);
            var stone3 = new ObjSmallStone(Map, (int)EntityPosition.X + 3, (int)EntityPosition.Y - 2, (int)EntityPosition.Z, vector3, false);

            Map.Objects.SpawnObject(stone0);
            Map.Objects.SpawnObject(stone1);
            Map.Objects.SpawnObject(stone2);
            Map.Objects.SpawnObject(stone3);

            // remove the object
            Map.Objects.DeleteObjects.Add(this);
        }
    }
}