using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A simple line graph for world-space UI screens: a line of a set thickness through the given points, with an
/// optional filled area under it that fades from the line colour down to transparent (like the screen designs).
/// Points are in 0..1 inside this RectTransform (x: left to right, y: bottom to top). Call SetPoints to update.
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class UILineGraph : MaskableGraphic
{
    [Tooltip("Line thickness in canvas units.")]
    public float thickness = 6f;
    [Tooltip("Draw a filled area under the line.")]
    public bool fillArea = true;
    [Tooltip("Colour of the fill right under the line (it fades to transparent at the bottom).")]
    public Color fillTop = new Color(0.25f, 0.85f, 0.95f, 0.35f);
    public Color fillBottom = new Color(0.25f, 0.85f, 0.95f, 0f);

    readonly List<Vector2> points = new List<Vector2>();

    /// <summary>Replace the points (0..1 coordinates) and redraw.</summary>
    public void SetPoints(IList<Vector2> newPoints)
    {
        points.Clear();
        points.AddRange(newPoints);
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (points.Count < 2) return;
        Rect r = rectTransform.rect;
        Vector2 P(Vector2 p) => new Vector2(r.xMin + p.x * r.width, r.yMin + p.y * r.height);

        if (fillArea)
        {
            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector2 a = P(points[i]), b = P(points[i + 1]);
                // vertical gradient: full fillTop at the top of the plot, fading to fillBottom at the bottom
                Color ca = Color.Lerp(fillBottom, fillTop, points[i].y);
                Color cb = Color.Lerp(fillBottom, fillTop, points[i + 1].y);
                int idx = vh.currentVertCount;
                vh.AddVert(new Vector3(a.x, r.yMin), fillBottom, Vector4.zero);
                vh.AddVert(new Vector3(a.x, a.y), ca, Vector4.zero);
                vh.AddVert(new Vector3(b.x, b.y), cb, Vector4.zero);
                vh.AddVert(new Vector3(b.x, r.yMin), fillBottom, Vector4.zero);
                vh.AddTriangle(idx, idx + 1, idx + 2);
                vh.AddTriangle(idx, idx + 2, idx + 3);
            }
        }

        float half = thickness * 0.5f;
        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector2 a = P(points[i]), b = P(points[i + 1]);
            Vector2 dir = (b - a).normalized;
            Vector2 n = new Vector2(-dir.y, dir.x) * half;
            // extend each piece slightly so the joints between pieces have no gaps
            Vector2 ext = dir * half * 0.5f;
            int idx = vh.currentVertCount;
            vh.AddVert(a - ext - n, color, Vector4.zero);
            vh.AddVert(a - ext + n, color, Vector4.zero);
            vh.AddVert(b + ext + n, color, Vector4.zero);
            vh.AddVert(b + ext - n, color, Vector4.zero);
            vh.AddTriangle(idx, idx + 1, idx + 2);
            vh.AddTriangle(idx, idx + 2, idx + 3);
        }
    }
}
