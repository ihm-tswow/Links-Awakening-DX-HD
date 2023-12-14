using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Bosses
{
    class BossFinalBossBat : GameObject
    {
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly Animator _animator;
        private readonly CSprite _sprite;

        public BossFinalBossBat(Map.Map map, int posX, int posY) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(-8, -8, 16, 16);

            _animator = AnimatorSaveLoad.LoadAnimator("Nightmares/nightmare bat");

            _sprite = new CSprite(EntityPosition);
            var animatorComponent = new AnimationComponent(_animator, _sprite, Vector2.Zero);

            _body = new BodyComponent(EntityPosition, -5, -4, 10, 8, 8)
            {
                IgnoresZ = true,
                IgnoreHoles = true,
                CollisionTypes = Values.CollisionTypes.None
            };

            _aiComponent = new AiComponent();

            var stateIdle = new AiState() { Init = InitIdle };
            stateIdle.Trigger.Add(new AiTriggerCountdown(400, null, () => _aiComponent.ChangeState("fire")));
            var stateFire = new AiState() { Init = InitFire };
            stateFire.Trigger.Add(new AiTriggerCountdown(400, null, () => _aiComponent.ChangeState("bat")));
            var stateBat = new AiState() { Init = InitBat };
            stateBat.Trigger.Add(new AiTriggerCountdown(550, null, () => _aiComponent.ChangeState("flying")));
            var stateFlying = new AiState() { Init = InitFlying };
            stateFlying.Trigger.Add(new AiTriggerCountdown(2000, FadeOut, Despawn));

            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("fire", stateFire);
            _aiComponent.States.Add("bat", stateBat);
            _aiComponent.States.Add("flying", stateFlying);

            _aiComponent.ChangeState("idle");

            var damageCollider = new CBox(EntityPosition, -5, -4, 0, 10, 8, 8);
            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageCollider, HitType.Enemy, 2));
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BodyComponent.Index, _body);
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(BaseAnimationComponent.Index, animatorComponent);
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(_sprite, Values.LayerTop));
        }

        private void Update()
        {
            _sprite.SpriteShader =
                Game1.TotalGameTime % (AiDamageState.BlinkTime * 2) < AiDamageState.BlinkTime ? Resources.DamageSpriteShader0 : null;
        }

        private void InitIdle()
        {
            _animator.Play("idle");
        }

        private void InitFire()
        {
            _animator.Play("fire");
        }

        private void InitBat()
        {
            _animator.Play("bat");
        }

        private void InitFlying()
        {
            var playerDirection = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            if (playerDirection != Vector2.Zero)
                playerDirection.Normalize();
            _body.VelocityTarget = playerDirection * 1.75f;

            Game1.GameManager.PlaySoundEffect("D378-40-28");
        }

        private void FadeOut(double time)
        {
            var percentage = MathHelper.Clamp((float)time / 75, 0, 1);
            _sprite.Color = Color.White * percentage;
        }

        private void Despawn()
        {
            Map.Objects.DeleteObjects.Add(this);
        }
    }
}