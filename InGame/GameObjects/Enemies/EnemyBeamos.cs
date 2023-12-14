using System;
using Microsoft.Xna.Framework;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyBeamos : GameObject
    {
        private readonly AiComponent _aiComponent;
        private readonly Animator _animator;
        private readonly AiTriggerSwitch _shootCooldown;
        private readonly CSprite _sprite;

        private readonly Rectangle _fieldRectangle;
        
        private Vector2 _shootDirection;
        private Vector2 _shootOrigin;

        private float _shootCounter;
        private float _beamosRotation;

        private int _shootsFired;

        public EnemyBeamos() : base("beamos") { }

        public EnemyBeamos(Map.Map map, int posX, int posY) : base(map)
        {
            Tags = Values.GameObjectTag.Trap;

            EntityPosition = new CPosition(posX + 8, posY + 14, 0);
            EntitySize = new Rectangle(-8, -14, 16, 16);

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/beamos");
            _animator.Play("idle");

            _sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, _sprite, Vector2.Zero);

            var body = new BodyComponent(EntityPosition, -7, -12, 14, 14, 8);

            _fieldRectangle = Map.GetField(posX, posY);

            var stateIdle = new AiState(UpdateIdle);
            stateIdle.Trigger.Add(_shootCooldown = new AiTriggerSwitch(1000));
            var statePreShoot = new AiState();
            statePreShoot.Trigger.Add(new AiTriggerCountdown(16000 / 60, TickPreShoot, PreShootEnd));
            var stateShoot = new AiState(UpdateShoot);

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("preShoot", statePreShoot);
            _aiComponent.States.Add("shoot", stateShoot);
            _aiComponent.ChangeState("idle");

            var hittableBox = new CBox(EntityPosition, -5, -12, 0, 10, 14, 8);
            var damageBox = new CBox(EntityPosition, -5, -12, 0, 10, 14, 4);

            AddComponent(BodyComponent.Index, body);
            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageBox, HitType.Enemy, 1));
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(PushableComponent.Index, new PushableComponent(damageBox, OnPush));
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(_sprite, Values.LayerPlayer));
        }

        private Vector2 GetOrigin()
        {
            return new Vector2(EntityPosition.X, EntityPosition.Y - 8) +
                   new Vector2(-MathF.Cos(_beamosRotation), -MathF.Sin(_beamosRotation)) * 4;
        }

        private void UpdateIdle()
        {
            if (!_shootCooldown.State)
                return;

            // get the direction of the beamos from the current animation frame (8 frames for a full rotation)
            var animationFrame = _animator.CurrentFrameIndex;
            _beamosRotation = animationFrame * (MathF.PI * 2) / 8f;
            _shootOrigin = new Vector2(EntityPosition.X, EntityPosition.Y - 8) +
                           new Vector2(-MathF.Cos(_beamosRotation), -MathF.Sin(_beamosRotation)) * 4;

            var playerPosition = MapManager.ObjLink.EntityPosition.Position;
            var targetPosition = new Vector2(playerPosition.X, playerPosition.Y - 6);
            var playerDirection = targetPosition - _shootOrigin;

            // if we normalize a zero vector we crash
            // it is really unlikely (probably impossible) but we need to be careful
            if (playerDirection != Vector2.Zero &&
                (playerDirection.Length() < 72 || _fieldRectangle.Contains(MapManager.ObjLink.EntityPosition.Position)))
            {
                playerDirection.Normalize();

                // there is probably a nicer way to calculate the difference between two angles
                var playerRotation = MathF.Atan2(-playerDirection.Y, -playerDirection.X);
                if (playerRotation < 0)
                    playerRotation += MathF.PI * 2;

                var rotationDistance = MathF.Abs(_beamosRotation - playerRotation);
                if (rotationDistance > MathF.PI)
                    rotationDistance = MathF.PI - rotationDistance % MathF.PI;

                // start shooting if the beamos is looking at the player
                // we do not divide by 8 because we offset the shoot origin and do not get 8 perfect angles
                if (rotationDistance < MathF.PI / 7.5f)
                {
                    _shootDirection = playerDirection;

                    if (!CheckForCollision(_shootOrigin, targetPosition))
                        ToPreShoot();
                }
            }
        }

        private bool CheckForCollision(Vector2 startPosition, Vector2 endPosition)
        {
            var direction = endPosition - startPosition;
            var distance = direction.Length();
            direction.Normalize();

            var currentPosition = startPosition;
            var steps = 15;

            while (true)
            {
                steps--;

                var modX = currentPosition.X % Values.TileSize;
                var modY = currentPosition.Y % Values.TileSize;

                // move to the next tile edge on the x or y axis
                // could probably be written in a nicer way
                var stepX = Math.Abs((((int)(currentPosition.X / Values.TileSize) +
                                       (modX == 0 ? Math.Sign(direction.X) : direction.X < 0 ? 0 : 1)) * Values.TileSize - currentPosition.X) / direction.X);
                var stepY = Math.Abs((((int)(currentPosition.Y / Values.TileSize) +
                                       (modY == 0 ? Math.Sign(direction.Y) : direction.Y < 0 ? 0 : 1)) * Values.TileSize - currentPosition.Y) / direction.Y);

                currentPosition += direction * (stepX < stepY ? stepX : stepY);

                // reached the end position without encountering anything?
                var traveledDistance = (startPosition - currentPosition).Length();
                if (distance < traveledDistance || steps < 0)
                    return false;

                // check for a colliding box on the line of sight
                var outBox = Box.Empty;
                var tileX = (int)((currentPosition.X + (direction.X < 0 ? -1 : 1)) / Values.TileSize) * Values.TileSize;
                var tileY = (int)((currentPosition.Y + (direction.Y < 0 ? -1 : 1)) / Values.TileSize) * Values.TileSize;
                if (Map.Objects.Collision(new Box(tileX, tileY, 2, 16, 16, 4), Box.Empty, Values.CollisionTypes.Normal, 0, 1, ref outBox))
                    return true;
            }
        }

        private void TickPreShoot(double count)
        {
            _sprite.SpriteShader = count % (8000 / 60f) >= (4000 / 60f) ? Resources.DamageSpriteShader0 : null;
        }

        private void PreShootEnd()
        {
            Game1.GameManager.PlaySoundEffect("D378-08-08");
            _aiComponent.ChangeState("shoot");
        }

        private void ToPreShoot()
        {
            _shootCounter = 0;
            _shootsFired = 0;

            _animator.Pause();
            _aiComponent.ChangeState("preShoot");
        }

        private void UpdateShoot()
        {
            // 16 projectiles
            // ~4 pixels/frame
            // 60 projectiles/second
            _shootCounter += Game1.DeltaTime;

            // spawn projectiles
            // this is a little complicated because the game can run at different framerates
            var projectileInterval = 1 / 60f * 1000;
            while (_shootCounter > projectileInterval * _shootsFired)
            {
                _shootOrigin = GetOrigin();
                // we calculate the position of the first projectile
                var spawnPosition = _shootOrigin + _shootDirection * (4 + _shootCounter / 1000f * 60 * 4);
                // now we go back in the opposite direction to find the exact position of the current projectile
                spawnPosition -= _shootDirection * _shootsFired * 4;

                // spawn the projectile
                var newProjectile = new EnemyBeamosProjectile(Map, spawnPosition, _shootDirection * 4, _shootsFired == 0);
                Map.Objects.SpawnObject(newProjectile);

                _shootsFired++;

                if (_shootsFired >= 16)
                {
                    _animator.Continue();
                    _aiComponent.ChangeState("idle");
                    _shootCooldown.Reset();
                }
            }
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType pushType)
        {
            if (pushType == PushableComponent.PushType.Impact)
                return true;

            return false;
        }

        private Values.HitCollision OnHit(GameObject originObject, Vector2 direction, HitType type, int damage, bool pieceOfPower)
        {
            return Values.HitCollision.RepellingParticle;
        }
    }
}