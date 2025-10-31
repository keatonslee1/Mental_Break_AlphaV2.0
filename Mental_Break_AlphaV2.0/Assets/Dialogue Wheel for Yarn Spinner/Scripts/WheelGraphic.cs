namespace Yarn.Unity.Addons.DialogueWheel
{
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Basic graphic to draw an annulus.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class WheelGraphic : Graphic
    {
        /// <summary>
        /// the difference in graphics space between the inner and outer radius.
        /// How thick the ring is to be.
        /// </summary>
        public float innerRadiusDiff = 10f;
        
        /// <summary>
        /// The number of segments that make up the annulus.
        /// </summary>
        public int density = 25;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            base.OnPopulateMesh(vh);
            vh.Clear();

            var centre = rectTransform.rect.center;
            var outerRadius = rectTransform.rect.height < rectTransform.rect.width ? rectTransform.rect.height / 2f : rectTransform.rect.width / 2f;
            var innerRadius = outerRadius - innerRadiusDiff;
            var sweepAngle = Mathf.Deg2Rad * 360f / density;

            var vert = UIVertex.simpleVert;

            // we draw the section with both its inner and outer radius at once
            // the outer radius vertex is always the lesser indexed vertex
            for (int i = 0; i < density; i++)
            {
                // adding the outer vertex first
                var x = centre.x + outerRadius * Mathf.Cos(sweepAngle * i);
                var y = centre.y + outerRadius * Mathf.Sin(sweepAngle * i);

                vert.position = new Vector2(x, y);
                vert.color = color;
                vh.AddVert(vert);

                // then the inner vertex
                x = centre.x + innerRadius * Mathf.Cos(sweepAngle * i);
                y = centre.y + innerRadius * Mathf.Sin(sweepAngle * i);

                vert.position = new Vector2(x, y);
                vert.color = color;
                vh.AddVert(vert);
            }

            // adding in all the triangles
            // we draw each segment of the circle as a quad
            // both the inner and outer section at once
            for (int i = 0; i < density; i++)
            {
                int a = i * 2;
                int b = (a + 1) % (density * 2);
                int c = (a + 2) % (density * 2);
                int d = (a + 3) % (density * 2);

                vh.AddTriangle(a, b, c);
                vh.AddTriangle(a + 1, d, c);
            }
        }
    }
}
