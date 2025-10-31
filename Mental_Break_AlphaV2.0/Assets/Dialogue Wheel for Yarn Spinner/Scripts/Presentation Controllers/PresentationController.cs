namespace Yarn.Unity.Addons.DialogueWheel
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    #nullable enable

    public abstract class PresentationController : MonoBehaviour
    {
        public abstract void PresentOptionViews(IEnumerable<WheelOptionView> enumerable);
        public abstract void Dismiss();

        public abstract void SetHighlightedOptionView(WheelOptionView? selectedOption);
        public abstract void SetCurrentAngle(float angleInDegrees);
    }
}