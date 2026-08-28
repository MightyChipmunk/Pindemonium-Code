using System.Collections.Generic;
using Pinball.Ingame.Map;
using Pinball.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.Ingame.Tutorial
{
    public class TutorialHighlightLayer : MonoBehaviour
    {
        const int RECT_CORNER_COUNT = 4;
        const int BOTTOM_LEFT_CORNER = 0;
        const int TOP_LEFT_CORNER = 1;
        const int TOP_RIGHT_CORNER = 2;
        const int BOTTOM_RIGHT_CORNER = 3;
        const float HALF_RATIO = 0.5f;
        const float DEFAULT_SCALE = 1f;
        const float FULL_ALPHA = 1f;

        static readonly Vector2 CENTER_ANCHOR = new Vector2(0.5f, 0.5f);

        [SerializeField] Image _template = null;
        [SerializeField] TutorialHighlightPolyline _lineTemplate = null;
        [SerializeField] RectTransform _cloneRoot = null;

        readonly List<Image> _images = new List<Image>();
        readonly List<TutorialHighlightPolyline> _lines = new List<TutorialHighlightPolyline>();
        readonly List<RectTransform> _clones = new List<RectTransform>();
        readonly List<RectTransform> _cloneTargets = new List<RectTransform>();
        readonly List<Vector2> _localPointBuffer = new List<Vector2>();
        readonly Vector3[] _worldCornerBuffer = new Vector3[RECT_CORNER_COUNT];
        readonly List<Behaviour> _behaviourBuffer = new List<Behaviour>();
        readonly List<Graphic> _graphicBuffer = new List<Graphic>();

        public void ShowWorldTargets(List<MapInteractable.SpriteSnapshot> spriteSnapshots, List<MapInteractable.LineSnapshot> lineSnapshots)
        {
            gameObject.SetActive(true);
            DestroyClones();
            ApplySprites(spriteSnapshots);
            ApplyLines(lineSnapshots);
        }

        public void ShowUITarget(RectTransform target)
        {
            gameObject.SetActive(true);
            ApplySprites(null);
            ApplyLines(null);
            DestroyClones();
            AddUIClone(target);
        }

        public void ShowUITargets(List<RectTransform> targets)
        {
            gameObject.SetActive(true);
            ApplySprites(null);
            ApplyLines(null);
            DestroyClones();
            if (null == targets)
                return;

            for (int i = 0; i < targets.Count; ++i)
                AddUIClone(targets[i]);
        }

        public void Clear()
        {
            ApplySprites(null);
            ApplyLines(null);
            DestroyClones();
            gameObject.SetActive(false);
        }

        void ApplySprites(List<MapInteractable.SpriteSnapshot> snapshots)
        {
            int count = null == snapshots ? 0 : snapshots.Count;
            while (_images.Count < count)
                _images.Add(Instantiate(_template, _template.transform.parent));

            for (int i = 0; i < _images.Count; ++i)
            {
                bool isUsed = i < count;
                _images[i].gameObject.SetActive(isUsed);
                if (isUsed)
                    BindSprite(_images[i], snapshots[i]);
            }
        }

        void ApplyLines(List<MapInteractable.LineSnapshot> snapshots)
        {
            int count = null == snapshots ? 0 : snapshots.Count;
            while (_lines.Count < count)
                _lines.Add(Instantiate(_lineTemplate, _lineTemplate.transform.parent));

            for (int i = 0; i < _lines.Count; ++i)
            {
                bool isUsed = i < count;
                _lines[i].gameObject.SetActive(isUsed);
                if (isUsed)
                    BindLine(_lines[i], snapshots[i]);
            }
        }

        void DestroyClones()
        {
            for (int i = 0; i < _clones.Count; ++i)
                if (null != _clones[i])
                    Destroy(_clones[i].gameObject);

            _clones.Clear();
            _cloneTargets.Clear();
        }

        void AddUIClone(RectTransform target)
        {
            if (null == target || !target.gameObject.activeInHierarchy)
                return;

            RectTransform clone = Instantiate(target, _cloneRoot);
            PrepareCloneVisual(clone);
            clone.anchorMin = CENTER_ANCHOR;
            clone.anchorMax = CENTER_ANCHOR;
            clone.pivot = CENTER_ANCHOR;
            ApplyCloneGeometry(clone, target);
            LayoutRebuilder.ForceRebuildLayoutImmediate(clone);
            _clones.Add(clone);
            _cloneTargets.Add(target);
        }

        void PrepareCloneVisual(RectTransform clone)
        {
            clone.GetComponentsInChildren(true, _behaviourBuffer);
            for (int i = 0; i < _behaviourBuffer.Count; ++i)
            {
                Behaviour behaviour = _behaviourBuffer[i];
                if (IsKeptBehaviour(behaviour))
                    continue;

                if (behaviour is CanvasGroup canvasGroup)
                    canvasGroup.alpha = FULL_ALPHA;

                behaviour.enabled = false;
            }

            _behaviourBuffer.Clear();

            clone.GetComponentsInChildren(true, _graphicBuffer);
            for (int i = 0; i < _graphicBuffer.Count; ++i)
            {
                _graphicBuffer[i].canvasRenderer.SetColor(Color.white);
                _graphicBuffer[i].canvasRenderer.SetAlpha(FULL_ALPHA);
            }

            _graphicBuffer.Clear();
        }

        static bool IsKeptBehaviour(Behaviour behaviour)
        {
            return behaviour is Graphic
                || behaviour is Mask
                || behaviour is RectMask2D
                || behaviour is ILayoutElement
                || behaviour is ILayoutController;
        }

        void ApplyCloneGeometry(RectTransform clone, RectTransform target)
        {
            target.GetWorldCorners(_worldCornerBuffer);
            Vector2 bottomLeft = WorldToLocal(_worldCornerBuffer[BOTTOM_LEFT_CORNER]);
            Vector2 topLeft = WorldToLocal(_worldCornerBuffer[TOP_LEFT_CORNER]);
            Vector2 topRight = WorldToLocal(_worldCornerBuffer[TOP_RIGHT_CORNER]);
            Vector2 bottomRight = WorldToLocal(_worldCornerBuffer[BOTTOM_RIGHT_CORNER]);
            Vector2 widthAxis = bottomRight - bottomLeft;
            Vector2 heightAxis = topLeft - bottomLeft;

            Vector2 targetSize = target.rect.size;
            clone.sizeDelta = targetSize;
            clone.anchoredPosition = (bottomLeft + topRight) * HALF_RATIO;
            clone.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(widthAxis.y, widthAxis.x) * Mathf.Rad2Deg);
            clone.localScale = new Vector3(
                GetScaleRatio(widthAxis.magnitude, targetSize.x),
                GetScaleRatio(heightAxis.magnitude, targetSize.y),
                DEFAULT_SCALE);
        }

        void LateUpdate()
        {
            for (int i = 0; i < _clones.Count; ++i)
            {
                if (null == _clones[i] || null == _cloneTargets[i])
                    continue;

                ApplyCloneGeometry(_clones[i], _cloneTargets[i]);
            }
        }

        void BindSprite(Image image, MapInteractable.SpriteSnapshot snapshot)
        {
            Vector3 localCenter = snapshot.LocalCenter;
            if (snapshot.FlipX)
                localCenter.x = -localCenter.x;
            if (snapshot.FlipY)
                localCenter.y = -localCenter.y;

            Vector3 worldCenter = snapshot.Position + snapshot.Rotation * Vector3.Scale(localCenter, snapshot.LossyScale);
            Vector3 halfX = snapshot.Rotation * new Vector3(snapshot.LocalSize.x * HALF_RATIO * snapshot.LossyScale.x, 0f, 0f);
            Vector3 halfY = snapshot.Rotation * new Vector3(0f, snapshot.LocalSize.y * HALF_RATIO * snapshot.LossyScale.y, 0f);

            Vector2 layerCenter = WorldToLocal(worldCenter);
            float width = (WorldToLocal(worldCenter + halfX) - layerCenter).magnitude * 2f;
            float height = (WorldToLocal(worldCenter + halfY) - layerCenter).magnitude * 2f;

            bool isMirroredX = snapshot.FlipX != (snapshot.LossyScale.x < 0f);
            bool isMirroredY = snapshot.FlipY != (snapshot.LossyScale.y < 0f);
            RectTransform imageTransform = (RectTransform)image.transform;
            imageTransform.anchoredPosition = layerCenter;
            imageTransform.sizeDelta = new Vector2(width, height);
            imageTransform.localRotation = Quaternion.Euler(0f, 0f, snapshot.Rotation.eulerAngles.z);
            imageTransform.localScale = new Vector3(isMirroredX ? -1f : 1f, isMirroredY ? -1f : 1f, 1f);
            image.sprite = snapshot.Sprite;
            image.color = snapshot.Color;
        }

        void BindLine(TutorialHighlightPolyline line, MapInteractable.LineSnapshot snapshot)
        {
            _localPointBuffer.Clear();
            for (int i = 0; i < snapshot.Points.Length; ++i)
                _localPointBuffer.Add(WorldToLocal(snapshot.Points[i]));

            Vector2 widthOrigin = WorldToLocal(snapshot.Points[0]);
            Vector2 widthOffset = WorldToLocal(snapshot.Points[0] + Vector3.up * snapshot.Width);
            float localWidth = (widthOffset - widthOrigin).magnitude;
            line.SetLine(_localPointBuffer, localWidth, snapshot.Color, snapshot.HasRoundCorner, snapshot.HasRoundCap);
        }

        Vector2 WorldToLocal(Vector3 worldPoint)
        {
            Vector2 screenPoint = CameraController.WorldToScreenPoint(worldPoint);
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, screenPoint, null, out Vector2 localPoint);
            return localPoint;
        }

        static float GetScaleRatio(float measuredLength, float sourceLength)
        {
            return Mathf.Approximately(sourceLength, 0f) ? DEFAULT_SCALE : measuredLength / sourceLength;
        }
    }
}
