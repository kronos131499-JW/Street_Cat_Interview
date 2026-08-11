using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace StreetCat.UI
{
    /// <summary>
    /// Legacy letter-spacing mesh hack for Unity UI <see cref="Text"/>.
    /// Runtime GameUI now uses TextMeshPro <c>characterSpacing</c> via <see cref="VnText.ApplyLetterSpacing"/>.
    /// Kept for any remaining editor-only / non-migrated Text components.
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class UILetterSpacing : BaseMeshEffect
    {
        [SerializeField] float spacing;

        public float Spacing
        {
            get => spacing;
            set
            {
                if (Mathf.Approximately(spacing, value)) return;
                spacing = value;
                if (graphic != null)
                    graphic.SetVerticesDirty();
            }
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0 || Mathf.Approximately(spacing, 0f))
                return;

            // Never post-process wrapped text — it breaks wrap and overflows the dialog.
            var uiText = graphic as Text;
            if (uiText != null && uiText.horizontalOverflow == HorizontalWrapMode.Wrap)
                return;

            var verts = new List<UIVertex>();
            vh.GetUIVertexStream(verts);
            // UI Text: 6 verts per glyph after GetUIVertexStream (two triangles).
            const int vertsPerGlyph = 6;
            int glyphCount = verts.Count / vertsPerGlyph;
            if (glyphCount <= 1)
            {
                vh.Clear();
                vh.AddUIVertexTriangleStream(verts);
                return;
            }

            // Left-aligned tracking: push following glyphs right.
            for (int g = 0; g < glyphCount; g++)
            {
                float offset = g * spacing;
                for (int i = 0; i < vertsPerGlyph; i++)
                {
                    int idx = g * vertsPerGlyph + i;
                    var v = verts[idx];
                    v.position.x += offset;
                    verts[idx] = v;
                }
            }

            vh.Clear();
            vh.AddUIVertexTriangleStream(verts);
        }
    }
}
