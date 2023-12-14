namespace ProjectZ.InGame.GameObjects.Base.Components
{
    class AnimationSheetComponent : BaseAnimationComponent
    {
        public SheetAnimator Animator;

        public AnimationSheetComponent(SheetAnimator animator)
        {
            Animator = animator;
        }

        public override void UpdateAnimation()
        {
            Animator.Update();
        }
    }
}
