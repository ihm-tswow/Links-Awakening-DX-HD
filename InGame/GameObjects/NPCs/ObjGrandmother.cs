using Microsoft.Xna.Framework;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.NPCs
{
    class ObjGrandmother : GameObject
    {
        private ObjAnimator _objBroom;

        private readonly Animator _animator;
        private readonly string _dialogId;

        private int _direction = -1;
        private bool _showBroom;
        private bool _missingBroomState;

        public ObjGrandmother() : base("grandmother") { }

        public ObjGrandmother(Map.Map map, int posX, int posY, string spawnCondition, string dialogId) : base(map)
        {
            var condition = SaveCondition.GetConditionNode(spawnCondition);
            if (!condition.Check())
            {
                IsDead = true;
                return;
            }

            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            _dialogId = dialogId;

            _animator = AnimatorSaveLoad.LoadAnimator("NPCs/npc_woman_broom");
            _animator.Play("stand_-1");

            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, sprite, Vector2.Zero);
            var body = new BodyComponent(EntityPosition, -7, -12, 14, 12, 8);

            AddComponent(BodyComponent.Index, body);
            AddComponent(CollisionComponent.Index, new BodyCollisionComponent(body, Values.CollisionTypes.Normal | Values.CollisionTypes.PushIgnore));
            AddComponent(InteractComponent.Index, new InteractComponent(new CBox(EntityPosition, -7, -12, 14, 12, 8), Interact));
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new BodyDrawComponent(body, sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(body, sprite));
            AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(OnKeyChange));

            if (Game1.GameManager.SaveManager.GetString("missing_broom", "0") == "1")
            {
                _missingBroomState = true;
                _animator.Play("missing_broom");
                _objBroom = new ObjAnimator(map, posX + 8, posY + 16, Values.LayerPlayer, "NPCs/broom", "show", false);
            }
        }

        private void OnKeyChange()
        {
            var showBroomValue = Game1.GameManager.SaveManager.GetString("show_broom", "0");
            if (!_showBroom && showBroomValue == "1")
            {
                _showBroom = true;
                _animator.Play("show");
                Map.Objects.SpawnObject(_objBroom);
            }
            if (_showBroom && showBroomValue == "0")
            {
                _missingBroomState = false;
                _showBroom = false;
                _animator.Play("stand_" + _direction);
                Map.Objects.DeleteObjects.Add(_objBroom);
            }
        }

        private void Update()
        {
            if (_missingBroomState)
                return;

            // look at the player
            var box = new RectangleF(EntityPosition.X - 16 * _direction - 8, EntityPosition.Y - 32, 16, 48);
            if (MapManager.ObjLink.BodyRectangle.Intersects(box))
            {
                _direction = EntityPosition.X < MapManager.ObjLink.PosX ? 1 : -1;
                _animator.Play("stand_" + _direction);
            }
        }

        private bool Interact()
        {
            Game1.GameManager.StartDialogPath(_dialogId);
            return true;
        }
    }
}