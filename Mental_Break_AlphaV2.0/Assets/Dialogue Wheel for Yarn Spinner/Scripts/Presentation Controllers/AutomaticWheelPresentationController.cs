namespace Yarn.Unity.Addons.DialogueWheel
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    using Yarn.Unity.Addons.DialogueWheel.Extensions;

#nullable enable

    /// <summary>
    /// A <see cref="PresentationController"/> that rotates an arrow based on
    /// the current wheel angle, and manages the highlight state of a collection
    /// of <see cref="WheelOptionView"/> objects based on the wheel's current
    /// selection.
    /// </summary>
    public class AutomaticWheelPresentationController : PresentationController
    {   
        // the arrow inside the centre of the wheel.
        // also represents the centre of wheel itself for angle calculations
        [SerializeField] private Transform? arrow;
        
        [SerializeField] CanvasGroup? canvasGroup;
        private IEnumerable<WheelOptionView> options = Array.Empty<WheelOptionView>();

        [SerializeField] bool showCursor = false;

        public void Awake()
        {
            if (canvasGroup == null)
            {
                Debug.LogWarning($"{this.name}'s {nameof(canvasGroup)} is null, unable to continue.");
                return;
            }
            // Hide the canvas group when the object awakens.
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0;
        }

        public override void Dismiss()
        {
            if (canvasGroup == null)
            {
                Debug.LogWarning($"{this.name}'s {nameof(canvasGroup)} is null, unable to continue.");
                return;
            }
            // Hide the canvas group when the object awakens.
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0;

            // Cursor.lockState = CursorLockMode.Locked;
            // Cursor.visible = false;
        }

        public override void PresentOptionViews(IEnumerable<WheelOptionView> enumerable)
        {
            if (canvasGroup == null)
            {
                Debug.LogWarning($"{this.name}'s {nameof(canvasGroup)} is null, unable to continue.");
                return;
            }
            canvasGroup.alpha = 1;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            if (showCursor == false) {
                Cursor.visible = false;
            }

            this.options = enumerable;
        }

        public override void SetCurrentAngle(float angleInDegrees)
        {
            if (arrow == null)
            {
                return;
            }
            arrow.localRotation = Quaternion.AngleAxis(angleInDegrees, Vector3.forward);
        }

        public override void SetHighlightedOptionView(WheelOptionView? selectedOption)
        {
            foreach (var option in this.options) {
                if (option == selectedOption) {
                    option.SetHighlighted(true);
                } else {
                    option.SetHighlighted(false);
                }
            }
        }
    }
}