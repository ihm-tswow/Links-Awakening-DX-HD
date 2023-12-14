using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.NPCs
{
    class ObjPainter : GameObject
    {
        private readonly Animator _animator;

        public ObjPainter() : base("painter") { }

        public ObjPainter(Map.Map map, int posX, int posY) : base(map)
        {
            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-12, -24, 24, 24);

            _animator = AnimatorSaveLoad.LoadAnimator("NPCs/npc_painter");
            _animator.Play("idle");

            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, sprite, Vector2.Zero);

            var body = new BodyComponent(EntityPosition, -9, -13, 18, 12, 8);

            AddComponent(BodyComponent.Index, body);
            AddComponent(CollisionComponent.Index, new BodyCollisionComponent(body, Values.CollisionTypes.Normal));
            AddComponent(InteractComponent.Index, new InteractComponent(new CBox(EntityPosition, -9, -5, 18, 5, 8), Interact));
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new BodyDrawComponent(body, sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(body, sprite));
        }

        private void Update()
        {
            if (!Game1.GameManager.InGameOverlay.TextboxOverlay.IsOpen)
                _animator.Play("idle");
        }

        private bool Interact()
        {
            if (EntityPosition.X < MapManager.ObjLink.EntityPosition.X)
                _animator.Play("talk_1");
            else
                _animator.Play("talk_-1");

            Game1.GameManager.StartDialogPath("npc_painter");
            return true;
        }
    }
}