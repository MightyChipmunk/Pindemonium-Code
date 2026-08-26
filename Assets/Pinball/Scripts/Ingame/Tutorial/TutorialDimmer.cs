using System.Collections.Generic;
using Pinball.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.Ingame.Tutorial
{
    public class TutorialDimmer : MaskableGraphic
    {
        enum EFocusMode
        {
            Full = 0,
            UITargets = 1,
            WorldBounds = 2,
        }

        const int RECT_CORNER_COUNT = 4;
        const float EDGE_EPSILON = 0.01f;
        const float HOLE_CHANGE_EPSILON = 0.5f;

        readonly List<RectTransform> _uiTargets = new List<RectTransform>();
        readonly List<Bounds> _worldBoundsList = new List<Bounds>();
        readonly List<Rect> _holes = new List<Rect>();
        readonly List<Rect> _holeBuffer = new List<Rect>();
        readonly List<float> _stripEdges = new List<float>();
        readonly List<Vector2> _yIntervals = new List<Vector2>();
        readonly Vector3[] _cornerBuffer = new Vector3[RECT_CORNER_COUNT];

        EFocusMode _mode = EFocusMode.Full;
        float _padding = 0f;

        public bool IsShowing()
        {
            return gameObject.activeSelf;
        }

        public void ShowFull()
        {
            _mode = EFocusMode.Full;
            _uiTargets.Clear();
            _worldBoundsList.Clear();
            gameObject.SetActive(true);
            Refresh();
        }

        public void FocusUI(RectTransform target, float padding)
        {
            _uiTargets.Clear();
            if (null != target)
                _uiTargets.Add(target);

            BeginUIFocus(padding);
        }

        public void FocusUITargets(List<RectTransform> targets, float padding)
        {
            _uiTargets.Clear();
            if (null != targets)
                for (int i = 0; i < targets.Count; ++i)
                    if (null != targets[i])
                        _uiTargets.Add(targets[i]);

            BeginUIFocus(padding);
        }

        public void FocusWorldBounds(List<Bounds> worldBoundsList, float padding)
        {
            _worldBoundsList.Clear();
            if (null != worldBoundsList)
                _worldBoundsList.AddRange(worldBoundsList);

            _uiTargets.Clear();
            _padding = padding;
            _mode = _worldBoundsList.Count > 0 ? EFocusMode.WorldBounds : EFocusMode.Full;
            gameObject.SetActive(true);
            Refresh();
        }

        public void Hide()
        {
            _uiTargets.Clear();
            _worldBoundsList.Clear();
            gameObject.SetActive(false);
        }

        void BeginUIFocus(float padding)
        {
            _worldBoundsList.Clear();
            _padding = padding;
            _mode = _uiTargets.Count > 0 ? EFocusMode.UITargets : EFocusMode.Full;
            gameObject.SetActive(true);
            Refresh();
        }

        void LateUpdate()
        {
            Refresh();
        }

        void Refresh()
        {
            RebuildHoleBuffer();
            if (!HasHoleChanged())
                return;

            _holes.Clear();
            _holes.AddRange(_holeBuffer);
            SetVerticesDirty();
        }

        void RebuildHoleBuffer()
        {
            _holeBuffer.Clear();
            if (_mode == EFocusMode.UITargets)
            {
                for (int i = 0; i < _uiTargets.Count; ++i)
                    if (null != _uiTargets[i] && _uiTargets[i].gameObject.activeInHierarchy)
                        _holeBuffer.Add(Expand(CalculateUILocalRect(_uiTargets[i]), _padding));
            }
            else if (_mode == EFocusMode.WorldBounds)
            {
                for (int i = 0; i < _worldBoundsList.Count; ++i)
                    _holeBuffer.Add(Expand(CalculateWorldLocalRect(_worldBoundsList[i]), _padding));
            }
        }

        bool HasHoleChanged()
        {
            if (_holes.Count != _holeBuffer.Count)
                return true;

            for (int i = 0; i < _holes.Count; ++i)
            {
                Rect previous = _holes[i];
                Rect current = _holeBuffer[i];
                if (Mathf.Abs(previous.xMin - current.xMin) > HOLE_CHANGE_EPSILON
                    || Mathf.Abs(previous.yMin - current.yMin) > HOLE_CHANGE_EPSILON
                    || Mathf.Abs(previous.xMax - current.xMax) > HOLE_CHANGE_EPSILON
                    || Mathf.Abs(previous.yMax - current.yMax) > HOLE_CHANGE_EPSILON)
                    return true;
            }

            return false;
        }

        Rect CalculateUILocalRect(RectTransform target)
        {
            target.GetWorldCorners(_cornerBuffer);
            Camera worldCamera = CameraController.GetMainCamera();
            Vector2 min = Vector2.positiveInfinity;
            Vector2 max = Vector2.negativeInfinity;
            for (int i = 0; i < RECT_CORNER_COUNT; ++i)
            {
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(worldCamera, _cornerBuffer[i]);
                Vector2 localPoint = ScreenToLocal(screenPoint);
                min = Vector2.Min(min, localPoint);
                max = Vector2.Max(max, localPoint);
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        Rect CalculateWorldLocalRect(Bounds worldBounds)
        {
            Vector2 min = Vector2.positiveInfinity;
            Vector2 max = Vector2.negativeInfinity;
            for (int i = 0; i < RECT_CORNER_COUNT; ++i)
            {
                float x = (i & 1) == 0 ? worldBounds.min.x : worldBounds.max.x;
                float y = (i & 2) == 0 ? worldBounds.min.y : worldBounds.max.y;
                Vector2 localPoint = ScreenToLocal(CameraController.WorldToScreenPoint(new Vector3(x, y, 0f)));
                min = Vector2.Min(min, localPoint);
                max = Vector2.Max(max, localPoint);
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        Vector2 ScreenToLocal(Vector2 screenPoint)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, null, out Vector2 localPoint);
            return localPoint;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect area = rectTransform.rect;
            if (_holes.Count <= 0)
            {
                AddQuad(vh, area.xMin, area.yMin, area.xMax, area.yMax);
                return;
            }

            BuildStripEdges(area);
            for (int stripIndex = 0; stripIndex < _stripEdges.Count - 1; ++stripIndex)
            {
                float stripMin = _stripEdges[stripIndex];
                float stripMax = _stripEdges[stripIndex + 1];
                if (stripMax - stripMin <= EDGE_EPSILON)
                    continue;

                CollectStripIntervals(area, stripMin, stripMax);
                FillStripComplement(vh, area, stripMin, stripMax);
            }
        }

        void BuildStripEdges(Rect area)
        {
            _stripEdges.Clear();
            _stripEdges.Add(area.xMin);
            _stripEdges.Add(area.xMax);
            for (int i = 0; i < _holes.Count; ++i)
            {
                float xMin = Mathf.Clamp(_holes[i].xMin, area.xMin, area.xMax);
                float xMax = Mathf.Clamp(_holes[i].xMax, area.xMin, area.xMax);
                AddStripEdge(xMin);
                AddStripEdge(xMax);
            }

            _stripEdges.Sort();
        }

        void AddStripEdge(float value)
        {
            for (int i = 0; i < _stripEdges.Count; ++i)
                if (Mathf.Abs(_stripEdges[i] - value) <= EDGE_EPSILON)
                    return;

            _stripEdges.Add(value);
        }

        void CollectStripIntervals(Rect area, float stripMin, float stripMax)
        {
            _yIntervals.Clear();
            for (int i = 0; i < _holes.Count; ++i)
            {
                Rect hole = _holes[i];
                if (hole.xMin >= stripMax - EDGE_EPSILON || hole.xMax <= stripMin + EDGE_EPSILON)
                    continue;

                float yMin = Mathf.Clamp(hole.yMin, area.yMin, area.yMax);
                float yMax = Mathf.Clamp(hole.yMax, area.yMin, area.yMax);
                if (yMax - yMin <= EDGE_EPSILON)
                    continue;

                InsertInterval(new Vector2(yMin, yMax));
            }
        }

        void InsertInterval(Vector2 interval)
        {
            for (int i = _yIntervals.Count - 1; i >= 0; --i)
            {
                Vector2 existing = _yIntervals[i];
                if (interval.x > existing.y || interval.y < existing.x)
                    continue;

                interval = new Vector2(Mathf.Min(interval.x, existing.x), Mathf.Max(interval.y, existing.y));
                _yIntervals.RemoveAt(i);
            }

            int insertIndex = _yIntervals.Count;
            for (int i = 0; i < _yIntervals.Count; ++i)
            {
                if (_yIntervals[i].x <= interval.x)
                    continue;

                insertIndex = i;
                break;
            }

            _yIntervals.Insert(insertIndex, interval);
        }

        void FillStripComplement(VertexHelper vh, Rect area, float stripMin, float stripMax)
        {
            float cursor = area.yMin;
            for (int i = 0; i < _yIntervals.Count; ++i)
            {
                Vector2 interval = _yIntervals[i];
                AddQuad(vh, stripMin, cursor, stripMax, interval.x);
                cursor = Mathf.Max(cursor, interval.y);
            }

            AddQuad(vh, stripMin, cursor, stripMax, area.yMax);
        }

        void AddQuad(VertexHelper vh, float xMin, float yMin, float xMax, float yMax)
        {
            if (xMax - xMin <= EDGE_EPSILON || yMax - yMin <= EDGE_EPSILON)
                return;

            int startIndex = vh.currentVertCount;
            Color32 color32 = color;
            vh.AddVert(new Vector3(xMin, yMin), color32, Vector2.zero);
            vh.AddVert(new Vector3(xMin, yMax), color32, Vector2.zero);
            vh.AddVert(new Vector3(xMax, yMax), color32, Vector2.zero);
            vh.AddVert(new Vector3(xMax, yMin), color32, Vector2.zero);
            vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
            vh.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
        }

        static Rect Expand(Rect rect, float padding)
        {
            return Rect.MinMaxRect(rect.xMin - padding, rect.yMin - padding, rect.xMax + padding, rect.yMax + padding);
        }
    }
}
