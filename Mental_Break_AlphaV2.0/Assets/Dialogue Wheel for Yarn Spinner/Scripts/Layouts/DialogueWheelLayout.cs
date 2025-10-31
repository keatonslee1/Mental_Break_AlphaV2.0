namespace Yarn.Unity.Addons.DialogueWheel
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using Yarn.Unity;

    #nullable enable

    /// <summary>
    /// Represents a layout of options around a wheel.
    /// <remarks>
    /// As each wheel will often require its own layout this provides a common interface for the <see cref="WheelDialogueView"/> to use.
    ///</remarks>
    /// </summary>
    public abstract class DialogueWheelLayout : MonoBehaviour
    {
        #if UNITY_EDITOR
        [SerializeField] float previewScale = 1f;
        internal float PreviewScale => previewScale;
        [SerializeField] Transform? previewRoot = null;
        internal Transform? PreviewRoot => previewRoot;
        #endif

        /// <summary>
        /// Called by the dialogue view when it is time to setup the wheel for display.
        /// </summary>
        /// <remarks>
        /// This will be called and allowed to return before the canvas group containing the wheel is alpha'd up to 100%.
        /// </remarks>
        public abstract OptionRegion[] CreateDialogueRegions(DialogueOption[] dialogueOptions);

        /// <summary>
        /// Gets rid of all the options that were added as part of the presentation of options.
        /// </summary>
        /// <remarks>
        /// This will be called after the canvas group containing the wheel is alpha'd down to 0%.
        /// </remarks>
        public abstract void DestroyDialogueOptionViews();

        /// <summary>
        /// Returns the option at an index at a particular region.
        /// </summary>
        /// <remarks>
        /// Because the active regions change depending on the current number of options or custom positioning this is NOT the index of the dialogue option.
        /// Or rather, often won't be the same as just the index into the list of options to be presented by the runner.
        /// </remarks>
        /// <param name="index">The index of the option region</param>
        /// <returns>The <see cref="WheelOptionView"/> at corresponds to the region index.</returns>
        public abstract WheelOptionView Option(int index);

        public abstract IEnumerable<WheelOptionView> GetAllOptionViews();

#pragma warning disable CS8618
        [field: SerializeField]
        public virtual OptionRegion[] regions { get; set; }
#pragma warning restore CS8618
    }

    /// <summary>
    /// Represents a region of a circle and used primarily to work out which option the user wants to select.
    /// </summary>
    /// <remarks>
    /// Each region of a circle can be determined by an angle, theta, around the circle and a spread,
    /// that is how many degrees either side of theta are to be considered within the circle.
    /// </remarks>
    [Serializable]
    public struct OptionRegion
    {
        /// <summary>
        /// the angle around the circle in degrees
        /// </summary>
        public float theta;
        
        /// <summary>
        /// The range of the region.
        /// This is the total spread, so the total spread of region is
        /// (theta - range/2) -> (theta + range/2)
        /// </summary>
        public float range;
    }
}
