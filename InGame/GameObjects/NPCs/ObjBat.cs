using System;
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

namespace ProjectZ.InGame.GameObjects.NPCs
{
    internal class ObjBat : GameObject
    {
        private readonly Animator _animator;
        private readonly AiComponent _aiComponent;
        private readonly CSprite _sprite;

        private readonly Rectangle _thunderTop = new Rectangle(444, 118, 14, 16);
        private readonly Rectangle _thunderBottom = new Rectangle(476, 107, 32, 32);

        private readonly Vector2 _spawnPosition;
        private readonly Vector2 _goalPosition;

        private readonly string _strKey;
        private const int SpawnTime = 2500;
        private const int DespawnTime = 750;

        private float _punishCount;
        private bool _showThunder;

        public ObjBat() : base("npc_bat") { }

        public ObjBat(Map.Map map, int posX, int posY, string strKey) : base(map)
        {
            EntityPosition = new CPosition(posX + 8, posY + 8, 0);
            EntitySize = new Rectangle(-8, -8, 16, 16);

            // was already spawned?
            _strKey = strKey;
            if (!string.IsNullOrEmpty(_strKey) &&
                Game1.GameManager.SaveManager.GetString(_strKey) == "1")
            {
                IsDead = true;
                return;
            }

            _animator = AnimatorSaveLoad.LoadAnimator("NPCs/bat");
            _animator.Play("spawn");

            _spawnPosition = new Vector2(posX + 8, posY + 8);
            _goalPosition = new Vector2(_spawnPosition.X, _spawnPosition.Y - 35);

            _sprite = new CSprite(EntityPosition) { IsVisible = false };
            var animationComponent = new AnimationComponent(_animator, _sprite, Vector2.Zero) { UpdateWithOpenDialog = true };

            var stateIdle = new AiState();
            var stateSpawning = new AiState(UpdateLockPlayer) { Init = InitSpawn };
            stateSpawning.Trigger.Add(new AiTriggerCountdown(SpawnTime, TickSpawn, () => TickSpawn(0)));
            var stateWaiting = new AiState(UpdateLockPlayer);
            stateWaiting.Trigger.Add(new AiTriggerCountdown(1500, null, ToBat));
            var stateBat = new AiState(UpdateLockPlayer);
            stateBat.Trigger.Add(new AiTriggerCountdown(1300, null, StartDialog));
            var stateThunder = new AiState(UpdateThunder);
            var statePreDespawn = new AiState(UpdateLockPlayer);
            statePreDespawn.Trigger.Add(new AiTriggerCountdown(125, null, () => _aiComponent.ChangeState("despawn")));
            var stateDespawn = new AiState() { Init = InitDespawn };
            stateDespawn.Trigger.Add(new AiTriggerCountdown(DespawnTime, TickDespawn, () => TickDespawn(0)));

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("spawning", stateSpawning);
            _aiComponent.States.Add("waiting", stateWaiting);
            _aiComponent.States.Add("bat", stateBat);
            _aiComponent.States.Add("thunder", stateThunder);
            _aiComponent.States.Add("preDespawn", statePreDespawn);
            _aiComponent.States.Add("despawn", stateDespawn);
            _aiComponent.ChangeState("idle");

            var hitBox = new CBox(EntityPosition, -8, -8, 0, 16, 16, 8);
            AddComponent(HittableComponent.Index, new HittableComponent(hitBox, OnHit));
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerPlayer, EntityPosition));
            //AddComponent(DrawShadowComponent.Index, new DrawShadowCSpriteComponent(_sprite));
            AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(OnKeyChange));

            Game1.GameManager.SaveManager.SetString("npcBatThunder", "0");
        }

        private void OnKeyChange()
        {
            var thunderState = Game1.GameManager.SaveManager.GetString("npcBatThunder");
            if (_aiComponent.CurrentStateId == "bat" && thunderState == "1")
                ToThunder();
            else if (_aiComponent.CurrentStateId == "thunder" && thunderState == "0")
                EndThunder();
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            // spawn the bat?
            if (_aiComponent.CurrentStateId == "idle" && damageType == HitType.MagicPowder)
            {
                _aiComponent.ChangeState("spawning");

                return Values.HitCollision.Enemy;
            }

            return Values.HitCollision.None;
        }

        private void InitSpawn()
        {
            SaveGameSaveLoad.FillSaveState(Game1.GameManager);
            Game1.GameManager.SaveManager.EnableHistory();

            if (!string.IsNullOrEmpty(_strKey))
                Game1.GameManager.SaveManager.SetString(_strKey, "1");

            Game1.GameManager.PlaySoundEffect("D360-06-06");

            _sprite.IsVisible = true;

            // spawn effect
            var objAnimation = new ObjAnimator(Map,
                (int)_spawnPosition.X - 8, (int)_spawnPosition.Y - 8, Values.LayerBottom, "Particles/spawn", "run", true);
            Map.Objects.SpawnObject(objAnimation);
        }

        private void TickSpawn(double time)
        {
            // update fairy position
            if (time > 0)
            {
                var amount = (float)(time / SpawnTime);
                var newPosition = Vector2.Lerp(_goalPosition, _spawnPosition, amount);
                newPosition.X -= MathF.Sin(amount * 2 * MathF.PI * 4.5f) * 1.5f;
                EntityPosition.Set(newPosition);
            }
            else
            {
                EntityPosition.Set(_goalPosition);

                _aiComponent.ChangeState("waiting");
            }
        }

        private void UpdateLockPlayer()
        {
            MapManager.ObjLink.LockPlayer();
        }

        private void ToBat()
        {
            _aiComponent.ChangeState("bat");
            _animator.Play("bat");

            Game1.GameManager.PlaySoundEffect("D378-12-0C");

            // spawn effect
            var objAnimation = new ObjAnimator(Map,
                (int)EntityPosition.X, (int)EntityPosition.Y, Values.LayerTop, "Particles/explosionBomb", "run", true);
            Map.Objects.SpawnObject(objAnimation);
        }

        private void StartDialog()
        {
            Game1.GameManager.StartDialogPath("npcBat");
        }

        private void ToThunder()
        {
            _aiComponent.ChangeState("thunder");
            _animator.Play("attack");
            _showThunder = true;

            Game1.GameManager.PlaySoundEffect("D378-38-26");
        }

        private void UpdateThunder()
        {
            if (_showThunder)
            {
                _punishCount += Game1.DeltaTime;
                Game1.GameManager.UseShockEffect = _punishCount % 100 < 50;
            }
        }

        private void EndThunder()
        {
            _aiComponent.ChangeState("preDespawn");
            _animator.Play("bat");
            _showThunder = false;
            Game1.GameManager.UseShockEffect = false;
        }

        private void InitDespawn()
        {
            Game1.GameManager.PlaySoundEffect("D360-59-3B");
        }

        private void TickDespawn(double state)
        {
            if (state > 0)
            {
                var amount = (float)(state / DespawnTime);
                var newPosition = Vector2.Lerp(new Vector2(_goalPosition.X, _goalPosition.Y - 32), _goalPosition, MathF.Sin(amount * (MathF.PI / 2)));
                EntityPosition.Set(newPosition);
                _sprite.Color = Color.White * (float)Math.Clamp(amount / 0.25, 0, 1);
            }
            else
            {
                Map.Objects.DeleteObjects.Add(this);
            }
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            // draw the alligator
            _sprite.Draw(spriteBatch);

            // draw the thunder effect
            if (!_showThunder)
                return;

            var offsetY = 3;
            var animationOffset = _punishCount % 133 < 66;
            if (_punishCount > 0)
                spriteBatch.Draw(Resources.SprNpCs, new Vector2(EntityPosition.X - 7, EntityPosition.Y + offsetY),
                    new Rectangle(_thunderTop.X + (animationOffset ? _thunderTop.Width + 1 : 0), _thunderTop.Y, _thunderTop.Width, _thunderTop.Height), Color.White);
            if (_punishCount > 66)
                spriteBatch.Draw(Resources.SprNpCs, new Vector2(EntityPosition.X - 7, EntityPosition.Y + offsetY + 16),
                    new Rectangle(_thunderTop.X + (animationOffset ? _thunderTop.Width + 1 : 0), _thunderTop.Y, _thunderTop.Width, _thunderTop.Height), Color.White);
            if (_punishCount > 133)
                spriteBatch.Draw(Resources.SprNpCs, new Vector2(EntityPosition.X - 16, EntityPosition.Y + offsetY + 32),
                    new Rectangle(_thunderBottom.X + (animationOffset ? _thunderBottom.Width + 1 : 0), _thunderBottom.Y, _thunderBottom.Width, _thunderBottom.Height), Color.White);
        }
    }
}