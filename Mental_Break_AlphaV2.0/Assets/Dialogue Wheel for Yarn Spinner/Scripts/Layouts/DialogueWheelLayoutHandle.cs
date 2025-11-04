#if UNITY_EDITOR
namespace Yarn.Unity.Addons.DialogueWheel
{
    using UnityEngine;
    using UnityEditor;
    using System;

#nullable enable

    /// <summary>
    /// Basic editor handle to visualise the regions that each option will later be occupying.
    /// </summary>
    [CustomEditor(typeof(DialogueWheelLayout), true)]
    public class DialogueWheelLayoutHandle : Editor
    {
        void OnSceneGUI()
        {
            DialogueWheelLayout layout = (DialogueWheelLayout)target;
            if (layout.regions == null || layout.regions.Length == 0)
            {
                return;
            }

            int i = 0;
            foreach (var region in layout.regions)
            {
                var range = region.range;
                var angle = region.theta - (range / 2f);

                var hx = Mathf.Cos(angle * Mathf.Deg2Rad);
                var hy = Mathf.Sin(angle * Mathf.Deg2Rad);
                var startPos = new Vector3(hx, hy, 0);

                var c = Color.HSVToRGB(1f / layout.regions.Length * i, 1, 1);

                Transform? previewRoot = layout.PreviewRoot;
                if (previewRoot == null) {
                    previewRoot = layout.transform;
                }

                Handles.matrix = previewRoot.localToWorldMatrix;

                Handles.color = c;
                Handles.DrawWireArc(Vector3.zero, Vector3.forward, startPos, range, 100f * layout.PreviewScale, 5 * layout.PreviewScale);

                Handles.color = new Color(c.r, c.g, c.b, 0.25f);
                Handles.DrawSolidArc(Vector3.zero, Vector3.forward, startPos, range, 100f * layout.PreviewScale);

                i++;
            }
        }
    }

    /// <summary>
    /// Similar to the <see cref="DialogueWheelLayoutHandle"/> but for the specific <see cref="DialogueWheelAutomaticLayout"/> sample.
    /// </summary>
    [CustomEditor(typeof(DialogueWheelAutomaticLayout))]
    public class DialogueWheelLayoutAutomaticHandle : Editor
    {
        void OnSceneGUI()
        {
            DialogueWheelAutomaticLayout layout = (DialogueWheelAutomaticLayout)target;
            if (layout.previewRegionCount < 1)
            {
                return;
            }

            var numberOfRegions = layout.previewRegionCount;
            
            for (int i = 0; i < numberOfRegions; i++)
            {
                layout.GetDialogueRegion(layout.previewRegionCount, i, out float theta, out float range);
                
                var hx = Mathf.Cos((theta - range / 2) * Mathf.Deg2Rad);
                var hy = Mathf.Sin((theta - range / 2) * Mathf.Deg2Rad);
                var startPos = new Vector3(hx, hy, 0);

                var c = Color.HSVToRGB(1f / numberOfRegions * i, 1, 1);

                Transform? previewRoot = layout.PreviewRoot;
                if (previewRoot == null) {
                    previewRoot = layout.transform;
                }

                Handles.matrix = previewRoot.localToWorldMatrix;

                Handles.color = c;
                Handles.DrawWireArc(Vector3.zero, Vector3.forward, startPos, range, 100f * layout.PreviewScale, 5 * layout.PreviewScale);

                Handles.color = new Color(c.r, c.g, c.b, 0.25f);
                Handles.DrawSolidArc(Vector3.zero, Vector3.forward, startPos, range, 100f * layout.PreviewScale);
            }
        }
    }
}
#endif
