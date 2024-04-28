using UnityEngine;

public partial class PlayerInteractor
{
    public class PlayerInteraction
    {
        public PlayerInteraction(string name, InteractionInput input, Visibility visibility, string iconSpriteName)
        {
            IsEnabled = true;
            IsActive = false;
            CanInteract = true;

            this.Name = name;
            this.Input = input;
            this.VisibilityState = visibility;

            blockedSprite = SpriteSet.GetSprite("cross");
            spriteInputInactive = SpriteSet.GetSprite(this.Input.name + "_inactive");
            spriteInputActive = SpriteSet.GetSprite(this.Input.name + "_active");
            if (iconSpriteName != null)
            {
                spriteIconInactive = SpriteSet.GetSprite(iconSpriteName + "_inactive");
                spriteIconActive = SpriteSet.GetSprite(iconSpriteName + "_active");
            }
        }

        public enum Visibility
        { Hidden, Input, Icon, Text }

        public bool IsEnabled { get; protected set; }
        public bool IsActive { get; protected set; }
        public bool CanInteract { get; protected set; }
        public string Name { get; private set; }
        public InteractionInput Input { get; private set; }
        public Visibility VisibilityState { get; protected set; }

        public void Update()
        {
            if (!IsEnabled) return;
            PollPlayerInput();
            if (IsActive) UpdateAction();
        }

        public Sprite GetCurrentSpriteInput()
        {
            if (!CanInteract) return blockedSprite;
            if (!IsActive) return spriteInputInactive;
            return spriteInputActive;
        }

        public Sprite GetCurrentSpriteIcon()
        {
            if (!CanInteract) return blockedSprite;
            if (!IsActive) return spriteIconInactive;
            return spriteIconActive;
        }

        protected PlayerInteractor PlayerInteractor => PlayerInteractor.Instance;
        protected PlayerMovement PlayerController => PlayerInteractor.playerMovement;
        protected PlayerLegs PlayerLegs => PlayerInteractor.playerLegs;
        protected CompositeObject TargetComposable => PlayerInteractor.target;
        protected LineHelper TargetDirLH => PlayerInteractor.targetDirLH;
        protected Color LegDirInteractColor => PlayerInteractor.legDirInteractColor;
        protected float InteractSlowdown => PlayerInteractor.interactCharacterSlowdown;

        protected virtual void OnHold()
        { }

        protected virtual void OnInputDown()
        { }

        protected virtual void OnInputUp()
        { }

        protected virtual void UpdateAction()
        { }

        private Sprite blockedSprite;
        private Sprite spriteInputInactive;
        private Sprite spriteInputActive;
        private Sprite spriteIconInactive;
        private Sprite spriteIconActive;

        private bool PollPlayerInput()
        {
            if (Input.CheckInputDown()) OnInputDown();
            else if (Input.CheckInput()) OnHold();
            else if (Input.CheckInputUp()) OnInputUp();
            else return false;
            return true;
        }
    }
}
