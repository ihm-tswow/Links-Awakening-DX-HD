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

namespace ProjectZ.InGame.GameObjects.Things
{
    class ObjArrow : GameObject
    {
        private readonly Animator _animator;
        private readonly AiComponent _aiComponent;
        private readonly ShadowBodyDrawComponent _shadowBody;
        private readonly CSprite _sprite;
        private readonly BodyComponent _body;
        private readonly BodyDrawComponent _drawComponent;

        private readonly CBox _damageBox;

        private const int DespawnTime = 375;
        private const int FadeOutTime = 75;

        private float _despawnPercentage = 1;
        private int _dir;
        private bool _isFalling;

        private Vector2[] _bombOffset = new Vector2[] { new Vector2(-4, 0), new Vector2(0, -4), new Vector2(4, 0), new Vector2(0, 6) };
        private ObjBomb _objBomb;
        private bool _bombMode;

        private Vector2 _startPosition;
        private Point[] _collisionBoxSize = { new Point(2, 2), new Point(2, 2), new Point(2, 2), new Point(2, 2) };

        public ObjArrow(Map.Map map, Vector3 position, int dir, float speed) : base(map)
        {
            EntityPosition = new CPosition(position.X, position.Y, position.Z);
            EntitySize = new Rectangle(-8, -12, 16, 16);

            _startPosition = new Vector2(position.X, position.Y);

            _dir = dir;
            var velocity = AnimationHelper.DirectionOffset[_dir] * speed;

            _animator = AnimatorSaveLoad.LoadAnimator("Objects/spear");
            _animator.Play(dir.ToString());

            _sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, _sprite, Vector2.Zero);

            _body = new BodyComponent(EntityPosition,
                -_collisionBoxSize[dir].X / 2, -_collisionBoxSize[dir].Y / 2,
                _collisionBoxSize[dir].X, _collisionBoxSize[dir].Y, 8)
            {
                CollisionTypes = Values.CollisionTypes.Normal,
                CollisionTypesIgnore = Values.CollisionTypes.ThrowWeaponIgnore,
                MoveCollision = OnCollision,
                VelocityTarget = velocity,
                Bounciness = 0.35f,
                Drag = 0.75f,
                DragAir = 0.95f,
                Gravity = -0.025f,
                IgnoreHeight = true,
                IgnoresZ = true,
                IgnoreInsideCollision = false,
                Level = MapStates.GetLevel(MapManager.ObjLink._body.CurrentFieldState)
            };

            _damageBox = new CBox(EntityPosition,
                -_collisionBoxSize[dir].X / 2 - 1, -_collisionBoxSize[dir].Y - 1, 0,
                _collisionBoxSize[dir].X + 2, _collisionBoxSize[dir].Y + 2, 8);

            var stateIdle = new AiState(UpdateIdle);
            var stateDespawn = new AiState() { Init = InitDespawn };
            stateDespawn.Trigger.Add(new AiTriggerCountdown(DespawnTime, TickDespawn, () => TickDespawn(0)));

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("despawn", stateDespawn);
            _aiComponent.ChangeState("idle");

            _drawComponent = new BodyDrawComponent(_body, _sprite, Values.LayerPlayer);

            AddComponent(BodyComponent.Index, _body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerPlayer, EntityPosition));
            AddComponent(DrawShadowComponent.Index, _shadowBody = new ShadowBodyDrawComponent(EntityPosition));
        }

        private void UpdateIdle()
        {
            var distance = _startPosition - EntityPosition.Position;
            if (Math.Abs(distance.X) > 112 || Math.Abs(distance.Y) > 96)
            {
                _isFalling = true;
                _body.IgnoresZ = false;

                ExplodeBomb();
            }

            DealDamage();
        }

        private void ExplodeBomb()
        {
            if (!_bombMode)
                return;

            _bombMode = false;
            _objBomb.Explode();
            Map.Objects.DeleteObjects.Add(this);
        }

        private void InitDespawn()
        {
            _body.Gravity = -0.1f;
            _body.IgnoresZ = false;

            if (!_isFalling)
                _body.Velocity = new Vector3(-_body.VelocityTarget.X * 0.35f, -_body.VelocityTarget.Y * 0.35f, 1f);
            else
                _body.Velocity = new Vector3(_body.VelocityTarget.X * 0.35f, _body.VelocityTarget.Y * 0.35f, 1f);

            _body.VelocityTarget = Vector2.Zero;

            _animator.Play(_dir == 2 ? "rotatel" : "rotate");
            _animator.SetFrame((_dir + 1) % 4);

            Game1.GameManager.PlaySoundEffect("D360-07-07");
        }

        private void TickDespawn(double time)
        {
            if (_animator.CurrentFrameIndex == (_dir + 2) % 4)
                _animator.Pause();

            _despawnPercentage = (float)(time / FadeOutTime);
            if (_despawnPercentage > 1)
                _despawnPercentage = 1;

            _sprite.Color = Color.White * _despawnPercentage;
            _shadowBody.Transparency = _despawnPercentage;

            if (time <= 0)
                Map.Objects.DeleteObjects.Add(this);
        }

        private void OnCollision(Values.BodyCollision collision)
        {
            if (_aiComponent.CurrentStateId == "despawn")
                return;

            _aiComponent.ChangeState("despawn");

            // make sure the deal damage one last time
            DealDamage();

            ExplodeBomb();
        }

        private void DealDamage()
        {
            var collision = Map.Objects.Hit(this, EntityPosition.Position, _damageBox.Box, HitType.Bow, 2, false, false);
            if ((collision & (Values.HitCollision.Blocking | Values.HitCollision.Enemy)) != 0)
            {
                Map.Objects.DeleteObjects.Add(this);

                ExplodeBomb();
            }

            if ((collision & Values.HitCollision.Repelling) != 0)
            {
                _aiComponent.ChangeState("despawn");

                ExplodeBomb();
            }
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            _drawComponent.Draw(spriteBatch);

            // make sure to draw the bomb ontop of the arrow
            if (_objBomb != null)
                _objBomb.Draw(spriteBatch);
        }

        public void InitBombMode(ObjBomb bomb)
        {
            _bombMode = true;
            _objBomb = bomb;
            _body.CollisionTypesIgnore = Values.CollisionTypes.None;

            EntityPosition.AddPositionListener(typeof(ObjBomb), (position) => bomb.EntityPosition.Set(position.Position + _bombOffset[_dir]));
        }
    }
}