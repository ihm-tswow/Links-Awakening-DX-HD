using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;
using static ProjectZ.InGame.GameObjects.ObjLink;

namespace ProjectZ.InGame.GameObjects.MidBoss
{
    internal class MBossBlainoGlove : GameObject
    {
        private readonly MBossBlaino _blaino;
        private readonly DamageFieldComponent _damageFieldComponent;
        private readonly string _resetDoor;

        private int _hitDirection;
        private bool _knockoutMode;
        private bool _stunMode;

        public MBossBlainoGlove(Map.Map map, MBossBlaino blaino, Vector2 position, string resetDoor) : base(map)
        {
            EntityPosition = new CPosition(position.X, position.Y, 0);
            EntitySize = new Rectangle(0, 0, 11, 11);

            var damageCollider = new CBox(EntityPosition, 0, 0, 0, 11, 11, 8);
            AddComponent(DamageFieldComponent.Index, _damageFieldComponent = new DamageFieldComponent(damageCollider, HitType.Enemy, 4) { OnDamage = DamagePlayer, PushMultiplier = 2.25f });
            AddComponent(HittableComponent.Index, new HittableComponent(damageCollider, OnHit));
            AddComponent(PushableComponent.Index, new PushableComponent(damageCollider, OnPush));

            _blaino = blaino;
            _resetDoor = resetDoor;
        }

        public void SetHitDirection(int direction)
        {
            _hitDirection = direction;
        }

        public void SetKnockoutMode(bool knockoutMode)
        {
            _knockoutMode = knockoutMode;
        }

        public void SetStunMode(bool stunMode)
        {
            _stunMode = stunMode;
        }

        private bool DamagePlayer()
        {
            // is the player blocking?
            if (_stunMode &&
                MapManager.ObjLink.CurrentState == State.Blocking &&
                ((_hitDirection == -1 && MapManager.ObjLink.Direction != 0) ||
                 (_hitDirection == 1 && MapManager.ObjLink.Direction != 2)))
            {
                _blaino.GlovePush(new Vector2(-_hitDirection * 3.5f, 0));

                // push the player back
                MapManager.ObjLink._body.Velocity += new Vector3(_hitDirection * 3.5f, 0, 0);

                return false;
            }

            var damagedPlayer = _damageFieldComponent.DamagePlayer();

            if (_knockoutMode)
            {
                _knockoutMode = false;
                Game1.GameManager.PlaySoundEffect("D360-11-0B");

                MapManager.ObjLink.Knockout(new Vector2(_hitDirection * 0.75f, -1), _resetDoor);
                return true;
            }

            if (_stunMode)
                MapManager.ObjLink.Stun(3500, true);

            return damagedPlayer;
        }

        public void SetPosition(Vector2 newPosition)
        {
            EntityPosition.Set(newPosition);
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            return Values.HitCollision.RepellingParticle;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (_stunMode)
                return false;

            _blaino.OnPush(direction, type);
            return true;
        }
    }
}