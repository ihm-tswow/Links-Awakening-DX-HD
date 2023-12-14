using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyMadBomber : GameObject
    {
        private readonly AiComponent _aiComponent;
        private readonly Animator _animator;
        private readonly CSprite _sprite;
        private readonly AiDamageState _damageState;
        private readonly AiStunnedState _aiStunnedState;
        private readonly DamageFieldComponent _damageField;

        private readonly Vector2[] _holeOffsets = {
            new Vector2(0, 0), new Vector2(3, -1), new Vector2(-3, -1),
            new Vector2(0, -3), new Vector2(-2, 2), new Vector2(2, 2)
        };

        private const string _leafSaveKey = "ow_goldLeafMadBomber";

        private readonly Vector2 _spawnPosition;
        private bool _wasHit;

        public EnemyMadBomber() : base("madBomber") { }

        public EnemyMadBomber(Map.Map map, int posX, int posY) : base(map)
        {
            // abort spawn if the player already has the leaf
            if (Game1.GameManager.SaveManager.GetString(_leafSaveKey) == "1")
            {
                IsDead = true;
                return;
            }

            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 15, 0);
            EntitySize = new Rectangle(-8, -15, 16, 16);

            _spawnPosition = new Vector2(posX + 8, posY + 15);

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/mad bomber");

            _sprite = new CSprite(EntityPosition) { IsVisible = false };
            var animationComponent = new AnimationComponent(_animator, _sprite, new Vector2(-8, -15));

            var body = new BodyComponent(EntityPosition, -7, -14, 14, 14, 8)
            {
                IgnoreHoles = true
            };

            var stateCooldown = new AiState();
            stateCooldown.Trigger.Add(new AiTriggerRandomTime(() => _aiComponent.ChangeState("hidden"), 1000, 1500));
            var stateHidden = new AiState(UpdateHidden);
            var stateComing = new AiState(UpdateComing);
            var stateLeaving = new AiState(UpdateLeaving);

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("cooldown", stateCooldown);
            _aiComponent.States.Add("hidden", stateHidden);
            _aiComponent.States.Add("coming", stateComing);
            _aiComponent.States.Add("leaving", stateLeaving);
            _aiStunnedState = new AiStunnedState(_aiComponent, animationComponent, 3300, 900);

            _aiComponent.ChangeState("hidden");

            _damageState = new AiDamageState(this, body, _aiComponent, _sprite, 4, false)
            {
                IsActive = false,
                SpawnItems = false,
                HitMultiplierX = 0,
                HitMultiplierY = 0,
                OnDeath = OnDeath
            };

            var damageBox = new CBox(EntityPosition, -7, -14, 0, 14, 14, 8);
            AddComponent(DamageFieldComponent.Index, _damageField = new DamageFieldComponent(damageBox, HitType.Enemy, 2));
            AddComponent(HittableComponent.Index, new HittableComponent(body.BodyBox, OnHit));
            AddComponent(BodyComponent.Index, body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new BodyDrawComponent(body, _sprite, Values.LayerPlayer));
        }

        private void OnDeath(bool pieceofpower)
        {
            var playerDirection = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            if (playerDirection != Vector2.Zero)
                playerDirection.Normalize();
            playerDirection *= 2.25f;

            // spawn the golden leaf jumping towards the player
            var objLeaf = new ObjItem(Map, 0, 0, null, _leafSaveKey, "goldLeaf", null);
            objLeaf.EntityPosition.Set(new Vector3(EntityPosition.X, EntityPosition.Y + 1, 1));
            objLeaf.SetVelocity(new Vector3(playerDirection.X, playerDirection.Y, 1.5f));
            objLeaf.Collectable = false;
            Map.Objects.SpawnObject(objLeaf);

            _damageState.BaseOnDeath(pieceofpower);
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (_damageState.IsInDamageState())
                return Values.HitCollision.None;

            if (_aiComponent.CurrentStateId == "coming")
                _wasHit = true;

            // stun state
            if (damageType == HitType.Hookshot)
            {
                _damageState.SetDamageState(false);

                _aiStunnedState.StartStun();
                _animator.Pause();

                return Values.HitCollision.Enemy;
            }

            if (damageType == HitType.Bow || damageType == HitType.MagicRod)
                damage = 1;
            if (damageType == HitType.MagicPowder || damageType == HitType.Boomerang)
                damage = 0;

            var hitReturn = _damageState.OnHit(gameObject, direction, damageType, damage, pieceOfPower);

            // make sure to not disapear while moving out of the hole with piece of power active
            if (_damageState.CurrentLives <= 0)
            {
                _animator.Pause();
                _damageState.HasDamageState = true;
            }

            return hitReturn;
        }

        private void ToCooldown()
        {
            _aiComponent.ChangeState("cooldown");
            _sprite.IsVisible = false;
        }

        private void UpdateHidden()
        {
            // only spawn if the player is close enough
            var playerDirection = MapManager.ObjLink.EntityPosition.Position - new Vector2(_spawnPosition.X, _spawnPosition.Y - 15);
            if (playerDirection.Length() < 64)
                ToComing();
        }

        private void ToComing()
        {
            // find a hole to come out
            var tryCounter = 0;
            while (tryCounter < 10)
            {
                tryCounter++;

                var randomNum = Game1.RandomNumber.Next(0, _holeOffsets.Length);
                var holePosition = _spawnPosition + _holeOffsets[randomNum] * 16;

                // check the distance to not spawn next to the player
                var direction = MapManager.ObjLink.EntityPosition.Position - holePosition;
                if (direction.Length() > 48)
                {
                    _aiComponent.ChangeState("coming");

                    EntityPosition.Set(holePosition);

                    var playerOnTheRight = EntityPosition.X < MapManager.ObjLink.EntityPosition.X;
                    _animator.Play(playerOnTheRight ? "come_1" : "come_0");

                    _sprite.IsVisible = true;
                    _damageState.IsActive = true;
                    _damageField.IsActive = true;
                    _wasHit = false;

                    return;
                }
            }
        }

        private void UpdateComing()
        {
            // finished the enter animation
            if (!_animator.IsPlaying)
            {
                if (!_wasHit)
                    ThrowBomb();

                ToLeaving();
            }
        }

        private void ToLeaving()
        {
            _aiComponent.ChangeState("leaving");
            _animator.Play("leave");
            _damageState.IsActive = false;
            _damageField.IsActive = false;
        }

        private void ThrowBomb()
        {
            var throwDirection = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            if (throwDirection != Vector2.Zero)
                throwDirection.Normalize();
            throwDirection *= 0.8f;

            // spawn a bomb
            var bomb = new ObjBomb(Map, 0, 0, false, true);
            bomb.EntityPosition.Set(new Vector3(EntityPosition.X, EntityPosition.Y + 1, 6));
            bomb.Body.Velocity = new Vector3(throwDirection.X, throwDirection.Y, 1.5f);
            bomb.Body.Gravity = -0.1f;
            bomb.Body.DragAir = 1.0f;
            bomb.Body.Bounciness = 0.25f;
            Map.Objects.SpawnObject(bomb);

            Game1.GameManager.PlaySoundEffect("D360-08-08");
        }

        private void UpdateLeaving()
        {
            if (!_animator.IsPlaying)
                ToCooldown();
        }
    }
}