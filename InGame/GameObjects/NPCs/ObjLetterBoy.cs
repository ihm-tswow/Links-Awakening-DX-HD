using Microsoft.Xna.Framework;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.NPCs
{
    class ObjLetterBoy : GameObject
    {
        private readonly Animator _animator;
        private readonly string _dialogId;

        private float _lookCounter;
        private bool _look;

        public ObjLetterBoy() : base("letter_boy") { }

        public ObjLetterBoy(Map.Map map, int posX, int posY, string dialogId) : base(map)
        {
            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-12, -24, 24, 40);

            _dialogId = dialogId;

            _animator = AnimatorSaveLoad.LoadAnimator("NPCs/npc_letter_boy");
            _animator.Play("idle");

            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, sprite, Vector2.Zero);
            var body = new BodyComponent(EntityPosition, -8, -13, 16, 12, 8);

            AddComponent(BodyComponent.Index, body);
            AddComponent(CollisionComponent.Index, new BodyCollisionComponent(body, Values.CollisionTypes.Normal | Values.CollisionTypes.PushIgnore));
            AddComponent(InteractComponent.Index, new InteractComponent(new CBox(EntityPosition, -7, -10, 14, 24, 8), Interact));
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new BodyDrawComponent(body, sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(body, sprite));
        }

        private void Update()
        {
            // look at the player
            var box = new RectangleF(EntityPosition.X - 14, EntityPosition.Y - 8, 28, 16);
            if (MapManager.ObjLink.BodyRectangle.Intersects(box))
            {
                if (!_look)
                {
                    _lookCounter = 250;
                    _look = true;
                    var _direction = EntityPosition.X < MapManager.ObjLink.PosX ? 1 : -1;
                    _animator.Play("look_" + _direction);
                }
            }
            else if (_look)
            {
                if (_lookCounter > 0)
                {
                    _lookCounter -= Game1.DeltaTime;
                }
                else
                {
                    _look = false;
                    _animator.Play("idle");
                }
            }
        }

        private bool Interact()
        {
            Game1.GameManager.StartDialogPath(_dialogId);
            return true;
        }
    }
}