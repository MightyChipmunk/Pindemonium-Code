using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.Ingame.Tutorial
{
    public class TutorialHighlightPolyline : MaskableGraphic
    {
        const int MIN_POINT_COUNT = 2;
        const float MIN_SEGMENT_SQR_LENGTH = 0.0001f;

        readonly List<Vector2> _points = new List<Vector2>();

        float _width = 1f;

        public void SetLine(List<Vector2> localPoints, float width, Color lineColor)
        {
            _points.Clear();
            if (null != localPoints)
                _points.AddRange(localPoints);

            _width = width;
            color = lineColor;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_points.Count < MIN_POINT_COUNT)
                return;

            float halfWidth = _width * 0.5f;
            for (int i = 0; i < _points.Count - 1; ++i)
            {
                Vector2 start = _points[i];
                Vector2 end = _points[i + 1];
                Vector2 segment = end - start;
                if (segment.sqrMagnitude <= MIN_SEGMENT_SQR_LENGTH)
                    continue;

                Vector2 normal = new Vector2(-segment.y, segment.x).normalized * halfWidth;
                AddQuad(vh, start - normal, start + normal, end + normal, end - normal);
            }
        }

        void AddQuad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            int startIndex = vh.currentVertCount;
            Color32 color32 = color;
            vh.AddVert(a, color32, Vector2.zero);
            vh.AddVert(b, color32, Vector2.zero);
            vh.AddVert(c, color32, Vector2.zero);
            vh.AddVert(d, color32, Vector2.zero);
            vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
            vh.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
        }
    }
}
