namespace Yarn.Unity.Addons.DialogueWheel
{
    using UnityEngine;

    #nullable enable

    /// <summary>
    /// A basic option which moves itself slightly further out and draws a small
    /// circle at the start of the option.
    /// </summary>
    /// <remarks>
    /// This class is intended to be used with the <see
    /// cref="DialogueWheelAutomaticLayout"/> class.
    /// </remarks>
    public class AutomaticWheelOptionView : WheelOptionView
    {
        [SerializeField] private int verticalRange = 10;

        /// <summary>
        /// the angle away from the centre the text will need to be placed
        /// </summary>
        public float Theta { get; set; } = 0f;

        /// <summary>
        /// the distance away from the centre the text will need to be placed
        /// </summary>
        public float GoOutDistance = 20;

        /// <inheritdoc />
        /// <remarks>
        /// Moves the text out slightly further than the position set by the
        /// layout so that there is space for the graphic to be drawn. Also
        /// calculates and adjusts pivots based on angle around the circle.
        /// </remarks>
        public override void Configure()
        {
            base.Configure();

            if (optionText == null)
            {
                return;
            }

            // setting the text of the option
            var line = "No Option Set";
            var colour = this.normalColor;
            if (option != null)
            {
                line = option.Line.TextWithoutCharacterName.Text;
                if (!option.IsAvailable)
                {
                    colour = this.disabledColor;
                }
            }

            if (optionText != null)
            {
                optionText.text = line;
                optionText.color = colour;
            }

            if (this.targetGraphic != null)
            {
                this.targetGraphic.color = colour;
            }

            if (this.optionText == null) {
                // We don't have a text to adjust the pivot of, so stop here
                return;
            }

            // working out the appropriate point the text will rest at
            var x = GoOutDistance * Mathf.Cos(Theta);
            var y = GoOutDistance * Mathf.Sin(Theta);
            var finalPoint = new Vector2(x, y);

            // based on the position around the circle we need a different anchor
            // this calculates which of the four "regions" this option occupies
            // this is then used to work out the pivot for the textfield
            var angle = Mathf.Rad2Deg * Theta;
            var bottom = Mathf.Abs(Mathf.DeltaAngle(angle, 270));
            var top = Mathf.Abs(Mathf.DeltaAngle(angle, 90));

            // the pivot for the text
            // defaults to right side appropriate point
            var pivot = new Vector2(0, 0.5f);

            if (top < verticalRange)
            {
                pivot = new Vector2(0.5f, 0);
            }
            else if (bottom < verticalRange)
            {
                pivot = new Vector2(0.5f, 1);
            }
            else if (angle > (90 + verticalRange) && angle < (270 - verticalRange))
            {
                pivot = new Vector2(1, 0.5f);
            }

            var textRect = optionText.GetComponent<RectTransform>();
            textRect.pivot = pivot;
            textRect.anchoredPosition = finalPoint;
        
        }
    }
}
