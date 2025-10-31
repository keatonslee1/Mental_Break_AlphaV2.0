namespace Yarn.Unity.Addons.DialogueWheel
{
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.EventSystems;
    using Yarn.Unity;
    using Yarn.Unity.Addons.DialogueWheel.Extensions;

#nullable enable

    public enum HighlightMode {
        Immediate,
        Crossfade
    }

    public class ImageWheelOptionView : WheelOptionView
    {
        [SerializeField] Sprite? selectedLineSprite;
        [SerializeField] Sprite? unselectedLineSprite;
        [SerializeField] Sprite? wheelSprite;

        internal Sprite? WheelSprite => wheelSprite;

        private Coroutine? crossfadeCoroutine = null;
        private Sprite? crossfadeTargetSprite = null;

        public override void SetHighlighted(bool isHighlighted)
        {
            if (gameObject.activeInHierarchy == false)
            {
                // Nothing to do.
                return;
            }

            Sprite? lineSprite = isHighlighted ? selectedLineSprite : unselectedLineSprite;
            if (lineSprite == null)
            {
                Debug.LogWarning("Unable to determine a sprite for the line.");
                return;
            }

            if (targetGraphic is Image image)
            {
                switch (graphicHighlightMode)
                {
                    case HighlightMode.Immediate:
                        image.sprite = lineSprite;

                        break;
                    case HighlightMode.Crossfade:
                        if (crossfadeTargetSprite == lineSprite)
                        {
                            // We're already crossfading to this sprite. Nothing to do.
                            break;
                        }

                        if (crossfadeCoroutine != null)
                        {
                            targetGraphic.StopCoroutine(crossfadeCoroutine);
                        }
                        crossfadeTargetSprite = lineSprite;
                        crossfadeCoroutine = image.CrossfadeImage(
                            lineSprite,
                            crossfadeDuration,
                            () =>
                            {
                                crossfadeCoroutine = null;
                                crossfadeTargetSprite = null;
                            });

                        break;
                }
            }
            base.SetHighlighted(isHighlighted);
        }

        public override void Configure()
        {
            base.Configure();
        }

    }
}