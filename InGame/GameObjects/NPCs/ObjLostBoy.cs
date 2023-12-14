using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.NPCs
{
    class ObjLostBoy : GameObject
    {
        private readonly BodyComponent _body;
        private readonly Animator _animator;
        private readonly CSprite _sprite;

        private int _direction;
        private bool _eating;

        public ObjLostBoy() : base("lost_boy") { }

        public ObjLostBoy(Map.Map map, int posX, int posY) : base(map)
        {
            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            _animator = AnimatorSaveLoad.LoadAnimator("NPCs/npc_lost_boy");

            _sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, _sprite, Vector2.Zero);

            _body = new BodyComponent(EntityPosition, -7, -12, 14, 12, 8);

            AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(KeyChanged));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(CollisionComponent.Index, new BodyCollisionComponent(_body, Values.CollisionTypes.Normal));
            AddComponent(InteractComponent.Index, new InteractComponent(_body.BodyBox, Interact));
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, _sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(_body, _sprite));
        }

        private void Update()
        {
            if (_eating)
                return;

            var playerDirection = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            var playerDistance = playerDirection.Length();

            if (playerDistance < 32)
            {
                if (MapManager.ObjLink.EntityPosition.X > EntityPosition.X - 4 - _direction * 4)
                    _direction = 2;
                else
                    _direction = 0;

                _animator.Play("hand_" + _direction);
            }
            else
            {
                _animator.Play("wave_" + _direction);
            }
        }

        private void KeyChanged()
        {
            // start eating animation?
            var strEat = "npc_lost_boy_eat";
            var eatValue = Game1.GameManager.SaveManager.GetString(strEat);
            if (eatValue != null)
            {
                _eating = true;
                _animator.Play("eat_0");
                Game1.GameManager.SaveManager.RemoveString(strEat);
            }
        }

        private bool Interact()
        {
            Game1.GameManager.StartDialogPath("npc_lost_boy");
            return true;
        }
    }
}