namespace Yarn.Unity.Addons.DialogueWheel
{
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using Yarn.Unity;

#nullable enable

    /// <summary>
    /// Places an arbitrary number of <see cref="AutomaticWheelOptionView"/>
    /// options around the wheel evenly.
    /// </summary>
    /// <remarks>
    /// While this is a functional layout, it is intended more as an example of
    /// making custom layouts.
    /// </remarks>
    public class DialogueWheelAutomaticLayout : DialogueWheelLayout
    {
#if UNITY_EDITOR
        // exists solely to visualise how the option regions will be placed.
        // not actually used at runtime.
        [Range(1, 32)]
        public int previewRegionCount;
#endif

        public AutomaticWheelOptionView? optionPrefab;
        public RectTransform? rectangle;

        public float startAngle = 0f;
        public float endAngle = 360f;

        public enum LayoutDirection
        {
            CounterClockwise,
            Clockwise
        }

        public LayoutDirection layoutDirection = LayoutDirection.CounterClockwise;

        public float distanceFromRect = 10f;

        public float offsetAngle = 0f;

        internal float AngleRange => Mathf.Min((startAngle < endAngle) ? endAngle - startAngle : startAngle - endAngle, 360f);
        internal float StartAngle => (startAngle < endAngle) ? startAngle : endAngle;
        internal float EndAngle => (startAngle < endAngle) ? endAngle : startAngle;

        private List<AutomaticWheelOptionView> wheelOptions = new List<AutomaticWheelOptionView>();

        public void GetDialogueRegion(int count, int index, out float theta, out float range)
        {
            var sweepAngle = AngleRange / count;
            var regionStart = (sweepAngle * index) + StartAngle;
            var regionEnd = (sweepAngle * (index + 1)) + StartAngle;

            theta = (regionStart + regionEnd) / 2f;
            theta += offsetAngle;
            range = sweepAngle;
        }

        /// <inheritdoc />
        /// <remarks>
        /// All options are evenly distributed around the wheel starting at right middle and spinning counter-clockwise.
        /// </remarks>
        public override OptionRegion[] CreateDialogueRegions(DialogueOption[] dialogueOptions)
        {
            if (optionPrefab == null)
            {
                Debug.LogWarning($"{nameof(optionPrefab)} is null, unable to continue.");
                return System.Array.Empty<OptionRegion>();
            }

            if (rectangle == null)
            {
                Debug.LogWarning($"{nameof(rectangle)} is null, unable to continue.");
                return System.Array.Empty<OptionRegion>();
            }

            var centre = rectangle.rect.center;
            var radius = (rectangle.rect.height < rectangle.rect.width ? rectangle.rect.height / 2 : rectangle.rect.width / 2) + distanceFromRect;

            var regions = new OptionRegion[dialogueOptions.Length];

            for (int i = 0; i < dialogueOptions.Length; i++)
            {
                int segmentIndex = i;

                if (layoutDirection == LayoutDirection.Clockwise)
                {
                    segmentIndex = dialogueOptions.Length - i - 1;
                }

                GetDialogueRegion(dialogueOptions.Length, segmentIndex, out float theta, out float range);

                var x = centre.x + radius * Mathf.Cos(theta * Mathf.Deg2Rad);
                var y = centre.y + radius * Mathf.Sin(theta * Mathf.Deg2Rad);

                var optionView = GameObject.Instantiate<AutomaticWheelOptionView>(optionPrefab, rectangle.transform);
                optionView.gameObject.SetActive(true);
                optionView.transform.localPosition = new Vector3(x, y, rectangle.transform.position.z);

                optionView.option = dialogueOptions[i];
                optionView.Theta = theta * Mathf.Deg2Rad;
                optionView.GoOutDistance = distanceFromRect;
                optionView.Configure();
                wheelOptions.Add(optionView);

                var region = new OptionRegion
                {
                    range = range,
                    theta = theta
                };
                regions[i] = region;
            }
            return regions;
        }

        /// <inheritdoc />
        public override void DestroyDialogueOptionViews()
        {
            foreach (var opt in wheelOptions)
            {
                GameObject.Destroy(opt.gameObject);
            }
            wheelOptions.Clear();
        }

        /// <inheritdoc />
        public override WheelOptionView Option(int index)
        {
            return wheelOptions[index];
        }

        public override IEnumerable<WheelOptionView> GetAllOptionViews()
        {
            return wheelOptions;
        }

        [YarnCommand("set_layout_direction")]
        public void SetWinding(string direction)
        {
            direction = direction.ToLower();
            if (direction == "clockwise" || direction == "sunwise" || direction == "cw")
            {
                layoutDirection = LayoutDirection.Clockwise;
            }
            else if (direction == "counterclockwise" || direction == "counter-clockwise" || direction == "widdershins" || direction == "ccw")
            {
                layoutDirection = LayoutDirection.CounterClockwise;
            }
        }
        [YarnCommand("set_option_zone")]
        public void SetZone(float start, float end)
        {
            startAngle = start;
            endAngle = end;
        }
        [YarnCommand("set_offset_angle")]
        public void SetOffsetAngle(float angle)
        {
            offsetAngle = angle;
        }
    }
}
