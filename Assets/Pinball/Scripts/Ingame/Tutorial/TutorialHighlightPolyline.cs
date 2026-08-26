using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.Ingame.Tutorial
{
    public class TutorialHighlightPolyline : MaskableGraphic
    {
        const int MIN_POINT_COUNT = 2;
        const float MIN_SEGMENT_SQR_LENGTH = 0.0001f;
        const float HALF_RATIO = 0.5f;
        const int ROUND_SEGMENT_COUNT = 8;
        const float FULL_CIRCLE_RADIAN = Mathf.PI * 2f;

        readonly List<Vector2> _points = new List<Vector2>();

        float _width = 1f;
        bool _hasRoundCorner = false;
        bool _hasRoundCap = false;

        public void SetLine(List<Vector2> localPoints, float width, Color lineColor, bool hasRoundCorner, bool hasRoundCap)
        {
            _points.Clear();
            if (null != localPoints)
                _points.AddRange(localPoints);

            _width = width;
            _hasRoundCorner = hasRoundCorner;
            _hasRoundCap = hasRoundCap;
            color = lineColor;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_points.Count < MIN_POINT_COUNT)
                return;

            float halfWidth = _width * HALF_RATIO;
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

            AddRoundEdges(vh, halfWidth);
        }

        void AddRoundEdges(VertexHelper vh, float halfWidth)
        {
            int lastIndex = _points.Count - 1;
            for (int i = 0; i <= lastIndex; ++i)
            {
                bool isEnd = i == 0 || i == lastIndex;
                if (isEnd ? !_hasRoundCap : !_hasRoundCorner)
                    continue;

                AddDisc(vh, _points[i], halfWidth);
            }
        }

        void AddDisc(VertexHelper vh, Vector2 center, float radius)
        {
            int startIndex = vh.currentVertCount;
            Color32 color32 = color;
            vh.AddVert(center, color32, Vector2.zero);
            for (int i = 0; i <= ROUND_SEGMENT_COUNT; ++i)
            {
                float angle = FULL_CIRCLE_RADIAN * i / ROUND_SEGMENT_COUNT;
                vh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, color32, Vector2.zero);
            }

            for (int i = 0; i < ROUND_SEGMENT_COUNT; ++i)
                vh.AddTriangle(startIndex, startIndex + 1 + i, startIndex + 2 + i);
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
