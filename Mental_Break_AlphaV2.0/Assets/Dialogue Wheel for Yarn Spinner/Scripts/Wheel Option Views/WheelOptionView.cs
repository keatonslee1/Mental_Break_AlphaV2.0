namespace Yarn.Unity.Addons.DialogueWheel
{
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.EventSystems;
    using Yarn.Unity;

    #nullable enable

    /// <summary>
    /// Represents an individual option placed somewhere around the wheel.
    /// </summary>
    public class WheelOptionView : MonoBehaviour
    {
        /// <summary>
        /// The TMP text field for this option.
        /// </summary>
        [SerializeField] protected TMPro.TMP_Text? optionText;
        
        /// <summary>
        /// the <see cref="DialogueOption"/> for this option view
        /// </summary>
        public DialogueOption? option;

        /// <summary>
        /// If true, then <see cref="optionText"/> will be tinted when this option is selected.
        /// </summary>
        [SerializeField] protected bool tintText = true;

        /// <summary>
        /// If true, then <see cref="targetGraphic"/> will be tinted when this option is selected.
        /// </summary>
        [SerializeField] protected bool tintGraphic = true;

        [SerializeField] protected Color normalColor = Color.white;
        [SerializeField] protected Color disabledColor = Color.gray;
        [SerializeField] protected Color highlightedColor = Color.yellow;
        [SerializeField] protected Graphic? targetGraphic;

        [SerializeField] protected HighlightMode textHighlightMode = HighlightMode.Immediate;
        [SerializeField] protected HighlightMode graphicHighlightMode = HighlightMode.Immediate;
        [SerializeField] protected float crossfadeDuration = 0.1f;

        protected virtual void OnEnable()
        {
            Color colour = normalColor;
            if (option != null)
            {
                if (!option.IsAvailable)
                {
                    colour = disabledColor;
                }
            }
            if (optionText != null && tintText)
            {
                optionText.color = colour;
            }

            if (targetGraphic != null && tintGraphic)
            {
                this.targetGraphic.color = colour;
            }

        }

        public virtual void SetHighlighted(bool isHighlighted)
        {
            if (gameObject.activeInHierarchy == false) {
                // Nothing to do.
                return;
            }

            if (option != null && !option.IsAvailable)
            {
                return;
            }

            var color = isHighlighted ? highlightedColor : normalColor;

            if (optionText != null && tintText)
            {
                switch (textHighlightMode)
                {
                    case HighlightMode.Immediate:
                        optionText.color = color;
                        break;
                    case HighlightMode.Crossfade:
                        optionText.CrossFadeColor(color, crossfadeDuration, true, true);
                        break;
                }
            }

            if (targetGraphic != null && tintGraphic)
            {
                switch (graphicHighlightMode)
                {
                    case HighlightMode.Immediate:
                        targetGraphic.color = color;
                        break;
                    case HighlightMode.Crossfade:
                        targetGraphic.CrossFadeColor(color, crossfadeDuration, true, true);
                        break;
                }
            }

        }

        /// <summary>
        /// Called by the WheelDialogueView after giving the WheelOption its necessary values.
        /// </summary>
        /// <remarks>
        /// Intended to be used to perform any additional setup necessary for display.
        /// </remarks>
        public virtual void Configure()
        {
            if (optionText == null)
            {
                return;
            }

            // setting the text of the option
            var line = "No Option Set";
            if (option != null)
            {
                line = option.Line.TextWithoutCharacterName.Text;
            }
            optionText.text = line;
        }
    }
}
