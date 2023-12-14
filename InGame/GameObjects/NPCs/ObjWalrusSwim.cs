using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;
using System;

namespace ProjectZ.InGame.GameObjects.NPCs
{
    internal class ObjWalrusSwim : GameObject
    {
        private readonly Animator _animator;
        private readonly AiComponent _aiComponent;
        private readonly BodyComponent _body;
        private readonly CSprite _sprite;

        private readonly Vector2 _spawnPosition;

        private string _spawnKey;
        private float _moveCounter;
        private float _despawnTime;
        private bool _dialog;
        private bool _init;

        public ObjWalrusSwim() : base("walrus") { }

        public ObjWalrusSwim(Map.Map map, int posX, int posY, string strSpawnKey) : base(map)
        {
            _animator = AnimatorSaveLoad.LoadAnimator("NPCs/walrus");
            _animator.Play("sleep");

            EntityPosition = new CPosition(posX + 16, posY + 29, 0);
            EntitySize = new Rectangle(-16, -32, 32, 32);

            _spawnPosition = EntityPosition.Position;

            _sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, _sprite, Vector2.Zero);

            _body = new BodyComponent(EntityPosition, -16, -12, 32, 12, 8);
            _spawnKey = strSpawnKey;

            _aiComponent = new AiComponent();

            var stateHidden = new AiState() { Init = InitHidden };
            var stateSwim = new AiState(UpdateSwim) { Init = InitSwim };

            _aiComponent.States.Add("hidden", stateHidden);
            _aiComponent.States.Add("swim", stateSwim);

            _aiComponent.ChangeState("hidden");

            AddComponent(BodyComponent.Index, _body);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(OcarinaListenerComponent.Index, new OcarinaListenerComponent(OnSongPlayed));
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(_sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(_body, _sprite));
        }

        private void OnSongPlayed(int songIndex)
        {
            if (Game1.GameManager.SaveManager.GetString(_spawnKey) != "1")
                return;

            if (songIndex == 0 && _aiComponent.CurrentStateId == "hidden")
                _aiComponent.ChangeState("swim");
        }

        private void SpawnSplash()
        {
            if (_init)
                Game1.GameManager.PlaySoundEffect("D378-36-24");
            _init = true;

            var splashAnimator0 = new ObjAnimator(Map, (int)EntityPosition.X - 6, (int)EntityPosition.Y + 1, 0, 0, Values.LayerPlayer, "Particles/fishingSplash", "idle", true);
            var splashAnimator1 = new ObjAnimator(Map, (int)EntityPosition.X + 6, (int)EntityPosition.Y + 2, 0, 0, Values.LayerPlayer, "Particles/fishingSplash", "idle", true);
            Map.Objects.SpawnObject(splashAnimator0);
            Map.Objects.SpawnObject(splashAnimator1);
        }

        private void InitHidden()
        {
            _moveCounter = 0;
            _sprite.IsVisible = false;
            SpawnSplash();
        }

        private void InitSwim()
        {
            _despawnTime = 0;
            _dialog = false;
            _sprite.IsVisible = true;
            EntityPosition.Set(_spawnPosition);
            SpawnSplash();
        }

        private void UpdateSwim()
        {
            _moveCounter += Game1.DeltaTime;
            if (!Game1.GameManager.InGameOverlay.TextboxOverlay.IsOpen)
            {
                _despawnTime += Game1.DeltaTime;
                if (_despawnTime > 5000)
                    _aiComponent.ChangeState("hidden");
            }

            if (!_dialog && _moveCounter > 2000)
            {
                var playerDistance = EntityPosition.Position - MapManager.ObjLink.EntityPosition.Position;
                if (playerDistance.Length() < 80)
                {
                    _dialog = true;
                    Game1.GameManager.StartDialogPath("walrus_swim");
                }
            }

            var offset = new Vector2(0, (int)MathF.Round(MathF.Cos(_moveCounter / 2000 * MathF.PI * 2)));
            EntityPosition.Set(_spawnPosition + offset);

            _animator.Play("swim_" + (offset.Y > 0 ? "up" : "down"));
        }
    }
}