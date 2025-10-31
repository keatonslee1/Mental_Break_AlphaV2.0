namespace Yarn.Unity.Addons.DialogueWheel
{
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Draws a small circle as the cap point of the option just so there is an indicator of it's position
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class WheelOptionGraphic : Graphic
    {
        /// <summary>
        /// the width of the end cap
        /// </summary>
        public float width = 5;
        /// <summary>
        /// how many pieces the end cap should have
        /// </summary>
        public int endCapDensity = 20;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            base.OnPopulateMesh(vh);
            vh.Clear();

            var centre = rectTransform.rect.center;
            var vert = UIVertex.simpleVert;
            vert.position = centre;
            vert.color = color;
            vh.AddVert(vert);

            for (int i = 0; i < endCapDensity; i++)
            {
                var x = centre.x + width * 2 * Mathf.Cos(i * 360f / endCapDensity * Mathf.Deg2Rad);
                var y = centre.y + width * 2 * Mathf.Sin(i * 360f / endCapDensity * Mathf.Deg2Rad);

                vert.position = new Vector2(x, y);
                vert.color = color;
                vh.AddVert(vert);
            }

            for (int i = 0; i < endCapDensity; i++)
            {
                // 0, 2, 1
                // 0, 3, 2
                // etc etc
                var a = (i + 1) % endCapDensity + 1;
                var b = i + 1;
                vh.AddTriangle(0, a, b);
            }
        }
    }
}
