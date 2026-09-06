using UnityEngine;
using UnityEngine.UI;

namespace Cutrium.Presentation.Frontend
{
    /// A soft diagonal light band drawn across its own rect, used as a
    /// looping "shine" sweep over icons (a sun-glint reflection) instead of
    /// a scale pulse. Progress 0/1 both place the band fully outside the
    /// rect so a looping animator can wrap without a visible pop.
    [DisallowMultipleComponent]
    public sealed class FrontEndShineSweepGraphic : MaskableGraphic
    {
        [SerializeField, Range(0f, 1f)] private float _progress;
        [SerializeField, Range(0f, 85f)] private float _angleDegrees = 22f;
        [SerializeField, Range(0.02f, 0.5f)] private float _bandWidthFraction =
            0.16f;
        [SerializeField] private Color _coreColor =
            new Color(1f, 1f, 1f, 0.55f);

        public void ConfigureForSetup(
            Color coreColor,
            float angleDegrees,
            float bandWidthFraction)
        {
            _coreColor = coreColor;
            _angleDegrees = Mathf.Clamp(angleDegrees, 0f, 85f);
            _bandWidthFraction = Mathf.Clamp(bandWidthFraction, 0.02f, 0.5f);
            raycastTarget = false;
            SetVerticesDirty();
        }

        public void SetProgress(float progress)
        {
            float clamped = Mathf.Clamp01(progress);
            if (Mathf.Approximately(clamped, _progress))
            {
                return;
            }

            _progress = clamped;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            Rect rect = GetPixelAdjustedRect();
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            float shear = Mathf.Tan(_angleDegrees * Mathf.Deg2Rad)
                * rect.height;
            float halfBand = Mathf.Max(2f, _bandWidthFraction * rect.width);
            float centerX = Mathf.Lerp(
                rect.xMin - halfBand - Mathf.Abs(shear),
                rect.xMax + halfBand + Mathf.Abs(shear),
                _progress);

            float topX = centerX + shear * 0.5f;
            float bottomX = centerX - shear * 0.5f;

            Color32 core = _coreColor;
            Color32 edge = new Color32(core.r, core.g, core.b, 0);

            // Six verts: left edge (transparent) / center (bright core) /
            // right edge (transparent), each at the rect's top and bottom,
            // sheared sideways by height so the band reads as a diagonal
            // streak rather than a straight vertical bar.
            vertexHelper.AddVert(
                new Vector3(topX - halfBand, rect.yMax), edge, Vector2.zero);
            vertexHelper.AddVert(
                new Vector3(bottomX - halfBand, rect.yMin), edge, Vector2.zero);
            vertexHelper.AddVert(
                new Vector3(topX, rect.yMax), core, Vector2.zero);
            vertexHelper.AddVert(
                new Vector3(bottomX, rect.yMin), core, Vector2.zero);
            vertexHelper.AddVert(
                new Vector3(topX + halfBand, rect.yMax), edge, Vector2.zero);
            vertexHelper.AddVert(
                new Vector3(bottomX + halfBand, rect.yMin), edge, Vector2.zero);

            vertexHelper.AddTriangle(0, 1, 3);
            vertexHelper.AddTriangle(0, 3, 2);
            vertexHelper.AddTriangle(2, 3, 5);
            vertexHelper.AddTriangle(2, 5, 4);
        }
    }
}
