using UnityEngine;

namespace Creator
{
    // Size the board's own area to its square cells, rather than leaving padding inside it.
    [ExecuteAlways]
    public sealed class CreatorBoardLayout : MonoBehaviour
    {
        static readonly string[] Categories = { "PERSONAJES", "BLOQUES", "ENEMIGOS", "EXTRAS" };
        void LateUpdate() => Apply();
        public void Apply()
        {
            var frame = (RectTransform)transform;
            var root = transform.parent as RectTransform;
            if (root == null || root.rect.height <= 0) return;
            float bottom = .92f - root.rect.width * CreatorBoard.Rows / CreatorBoard.Columns / root.rect.height;
            var min = new Vector2(0,bottom);
            var max = new Vector2(1,.92f);
            if (frame.anchorMin != min) frame.anchorMin = min;
            if (frame.anchorMax != max) frame.anchorMax = max;
            if (frame.offsetMin != Vector2.zero) frame.offsetMin = Vector2.zero;
            if (frame.offsetMax != Vector2.zero) frame.offsetMax = Vector2.zero;
            foreach (var name in Categories)
            {
                var panel = root.Find(name) as RectTransform;
                if (panel == null) continue;
                var top = new Vector2(panel.anchorMax.x,bottom-.015f);
                if (panel.anchorMax != top) panel.anchorMax = top;
            }
        }
    }
}
