using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.NPCs
{
    internal class ObjFairy : GameObject
    {
        private readonly CSprite _sprite;
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly ShadowBodyDrawComponent _shadowComponent;

        private readonly DictAtlasEntry _heartSource;

        private readonly string _strDialogPath;

        private const int DespawnTime = 1000;
        private const int DespawnStart = 4000;
        private const int HealingStart = 500;
        private const int HealingStepTime = 250;

        private Color _color;

        private double _hiddenStartTime;
        
        private float _spawnState;
        private float _heartTimer;
        private float _healCounter;
        private float _heartSpeed = 300;
        private float _despawnCounter;

        private int _healStepAmount;

        private bool _shownDialog;
        private bool _healMode;

        public ObjFairy() : base("npc_fairy") { }

        public ObjFairy(Map.Map map, int posX, int posY, string strDialogPath) : base(map)
        {
            EntityPosition = new CPosition(posX + 8, posY + 16, 8);
            EntitySize = new Rectangle(-11, -35, 22, 35 + 32);

            // the fairy in the color dungeon does not heal but shows a custom dialog instead
            _strDialogPath = strDialogPath;

            _healMode = string.IsNullOrEmpty(strDialogPath);

            _body = new BodyComponent(EntityPosition, -5, -16, 10, 16, 8)
            {
                IgnoresZ = true
            };

            _heartSource = Resources.GetSprite("heart");

            var animator = AnimatorSaveLoad.LoadAnimator("NPCs/fairy");
            animator.Play("idle");

            _aiComponent = new AiComponent();

            var stateHidde = new AiState(UpdateHidden) { Init = InitHidden };
            var stateIdle = new AiState(UpdateIdle);
            var stateHealing = new AiState(UpdateHealing) { Init = InitHealing };
            var stateDespawning = new AiState(UpdateDespawning) { Init = InitDespawning };

            _aiComponent.States.Add("hidden", stateHidde);
            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("healing", stateHealing);
            _aiComponent.States.Add("despawning", stateDespawning);

            _aiComponent.ChangeState("idle");

            _sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(animator, _sprite, new Vector2(-11, -25));

            AddComponent(BodyComponent.Index, _body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(ObjectCollisionComponent.Index, new ObjectCollisionComponent(new Rectangle(posX + 8 - 3, posY + 16, 6, 30), OnCollision));
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerTop, EntityPosition));
            AddComponent(DrawShadowComponent.Index, _shadowComponent = new ShadowBodyDrawComponent(EntityPosition));
        }

        private void UpdatePosition()
        {
            // move up down
            EntityPosition.Z = 12.0f + MathF.Sin((float)Game1.TotalGameTime / 1100.0f * MathF.PI) * 4.0f;
        }

        private void InitHidden()
        {
            _hiddenStartTime = Game1.TotalGameTime;
        }

        private void UpdateHidden()
        {
            // fade out
            _spawnState = AnimationHelper.MoveToTarget(_spawnState, 0, 0.05f * Game1.TimeMultiplier);
            _color = Color.White * _spawnState;
            _shadowComponent.Transparency = _spawnState;

            // delay the spawning a little bit
            if (_hiddenStartTime + 10000 < Game1.TotalGameTime &&
                Game1.GameManager.CurrentHealth < Game1.GameManager.MaxHearths * 4)
                _aiComponent.ChangeState("idle");
        }

        private void UpdateIdle()
        {
            // fade in
            _spawnState = AnimationHelper.MoveToTarget(_spawnState, 1, 0.05f * Game1.TimeMultiplier);
            _color = Color.White * _spawnState;
            _shadowComponent.Transparency = _spawnState;

            UpdatePosition();

            // hide when the player has full health
            if (_healMode && Game1.GameManager.CurrentHealth >= Game1.GameManager.MaxHearths * 4)
                _aiComponent.ChangeState("hidden");
        }

        private void InitHealing()
        {
            _heartTimer = -1;

            // different speeds depending on the health of the player
            var healingSteps = (DespawnStart - HealingStart) / HealingStepTime;
            var neededSteps = Game1.GameManager.MaxHearths * 4 - Game1.GameManager.CurrentHealth;
            _healStepAmount = Math.Clamp((int)Math.Ceiling(neededSteps / (float)healingSteps), 1, 8);

            Game1.GameManager.SetMusic(11, 2);
            Game1.GameManager.StartDialogPath("fairy");
        }

        private void UpdateHealing()
        {
            UpdatePosition();

            if (Game1.GameManager.InGameOverlay.TextboxOverlay.IsOpen)
                return;

            MapManager.ObjLink.FreezePlayer();

            _heartTimer += Game1.DeltaTime;

            // start healing
            if (_heartTimer > HealingStart)
                _healCounter += Game1.DeltaTime;

            Game1.GameManager.PlaySoundEffect("D370-06-06", false);

            if (_healCounter > HealingStepTime)
            {
                _healCounter -= HealingStepTime;
                if (Game1.GameManager.CurrentHealth < Game1.GameManager.MaxHearths * 4)
                {
                    Game1.GameManager.PlaySoundEffect("D370-06-06", true);
                    Game1.GameManager.HealPlayer(_healStepAmount);
                }
            }

            if (_heartTimer > DespawnStart)
            {
                Game1.GameManager.PlaySoundEffect("D360-38-26");
                _aiComponent.ChangeState("despawning");
            }
        }

        private void InitDespawning()
        {
            _despawnCounter = DespawnTime;
            Game1.GameManager.SetMusic(-1, 2);
        }

        private void UpdateDespawning()
        {
            UpdatePosition();

            // remove the fairy
            _despawnCounter -= Game1.DeltaTime;
            if (_despawnCounter <= 0)
            {
                _spawnState = 0;
                _aiComponent.ChangeState("hidden");
            }

            // fade out with a blinking effect
            var despawnState = MathHelper.Clamp(_despawnCounter / 500.0f, 0, 1) *
                                (0.75f + (float)Math.Cos(_despawnCounter / 15) * 0.25f);

            _color = Color.White * despawnState;
            _shadowComponent.Transparency = despawnState;
        }

        private void OnCollision(GameObject gameObject)
        {
            if (!string.IsNullOrEmpty(_strDialogPath))
            {
                if (!_shownDialog)
                    Game1.GameManager.StartDialogPath(_strDialogPath);
                _shownDialog = true;
                return;
            }

            if (_aiComponent.CurrentStateId == "idle" && _spawnState >= 1)
                _aiComponent.ChangeState("healing");
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            // draw the sprite
            _sprite.Color = _color;
            _sprite.Draw(spriteBatch);

            // draw the hearts
            if (_aiComponent.CurrentStateId == "healing" ||
                _aiComponent.CurrentStateId == "despawning")
                DrawHearts(spriteBatch);
        }

        private void DrawHearts(SpriteBatch spriteBatch)
        {
            for (var i = 0; i < 10; i++)
            {
                if (_heartTimer < (i * _heartSpeed))
                    return;

                var position = new Vector2(EntityPosition.Position.X - 3.5f, EntityPosition.Position.Y + 20);
                var angle = i / 5.0 * Math.PI - (_heartTimer / _heartSpeed * Math.PI / 5.0);

                position += new Vector2((float)Math.Sin(angle), -(float)Math.Cos(angle)) * 36;

                DrawHelper.DrawNormalized(spriteBatch, _heartSource, position, _color);
            }
        }
    }
}