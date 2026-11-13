using UnityEngine;
using UnityEngine.UI;

namespace Creator
{
    // Draw actual lines instead of relying on sub-pixel gaps between cell images.
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CreatorGridLines : MaskableGraphic
    {
        float previousScale;

        protected override void OnEnable()
        {
            base.OnEnable(); raycastTarget = false;
        }

        void LateUpdate()
        {
            float scale = canvas != null ? canvas.scaleFactor : 1;
            if (Mathf.Approximately(scale, previousScale)) return;
            previousScale = scale; SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var rect = rectTransform.rect;
            float thickness = 1.5f / Mathf.Max(.01f, canvas != null ? canvas.scaleFactor : 1);
            for (int x = 0; x <= CreatorBoard.Columns; x++)
            {
                float center = rect.xMin + rect.width * x / CreatorBoard.Columns;
                Quad(mesh, Mathf.Max(rect.xMin, center - thickness / 2), rect.yMin,
                    Mathf.Min(rect.xMax, center + thickness / 2), rect.yMax);
            }
            for (int y = 0; y <= CreatorBoard.Rows; y++)
            {
                float center = rect.yMin + rect.height * y / CreatorBoard.Rows;
                Quad(mesh, rect.xMin, Mathf.Max(rect.yMin, center - thickness / 2),
                    rect.xMax, Mathf.Min(rect.yMax, center + thickness / 2));
            }
        }

        void Quad(VertexHelper mesh, float x0, float y0, float x1, float y1)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(new Vector3(x0,y0),color,Vector2.zero);
            mesh.AddVert(new Vector3(x0,y1),color,Vector2.zero);
            mesh.AddVert(new Vector3(x1,y1),color,Vector2.zero);
            mesh.AddVert(new Vector3(x1,y0),color,Vector2.zero);
            mesh.AddTriangle(start,start+1,start+2); mesh.AddTriangle(start,start+2,start+3);
        }
    }
}
