using System.Collections.Generic;
using Pinball.Ingame.Map;
using Pinball.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Pinball.Ingame.Tutorial
{
    public class TutorialHighlightLayer : MonoBehaviour
    {
        [SerializeField] Image _template = null;
        [SerializeField] TutorialHighlightPolyline _lineTemplate = null;
        [SerializeField] RectTransform _cloneRoot = null;

        readonly List<Image> _images = new List<Image>();
        readonly List<TutorialHighlightPolyline> _lines = new List<TutorialHighlightPolyline>();
        readonly List<RectTransform> _clones = new List<RectTransform>();
        readonly List<Vector2> _localPointBuffer = new List<Vector2>();

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
        }

        void AddUIClone(RectTransform target)
        {
            if (null == target || !target.gameObject.activeInHierarchy)
                return;

            Vector2 targetSize = target.rect.size;
            RectTransform clone = Instantiate(target, _cloneRoot);
            clone.anchorMin = new Vector2(0.5f, 0.5f);
            clone.anchorMax = new Vector2(0.5f, 0.5f);
            clone.sizeDelta = targetSize;
            clone.position = target.position;
            clone.rotation = target.rotation;
            Vector3 parentScale = _cloneRoot.lossyScale;
            Vector3 targetScale = target.lossyScale;
            clone.localScale = new Vector3(
                GetScaleRatio(targetScale.x, parentScale.x),
                GetScaleRatio(targetScale.y, parentScale.y),
                GetScaleRatio(targetScale.z, parentScale.z));
            _clones.Add(clone);
        }

        void BindSprite(Image image, MapInteractable.SpriteSnapshot snapshot)
        {
            Bounds spriteBounds = snapshot.Sprite.bounds;
            Vector3 localCenter = spriteBounds.center;
            if (snapshot.FlipX)
                localCenter.x = -localCenter.x;
            if (snapshot.FlipY)
                localCenter.y = -localCenter.y;

            Vector3 worldCenter = snapshot.Position + snapshot.Rotation * Vector3.Scale(localCenter, snapshot.LossyScale);
            Vector3 halfX = snapshot.Rotation * new Vector3(spriteBounds.extents.x * snapshot.LossyScale.x, 0f, 0f);
            Vector3 halfY = snapshot.Rotation * new Vector3(0f, spriteBounds.extents.y * snapshot.LossyScale.y, 0f);

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
            line.SetLine(_localPointBuffer, localWidth, snapshot.Color);
        }

        Vector2 WorldToLocal(Vector3 worldPoint)
        {
            Vector2 screenPoint = CameraController.WorldToScreenPoint(worldPoint);
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, screenPoint, null, out Vector2 localPoint);
            return localPoint;
        }

        static float GetScaleRatio(float targetScale, float parentScale)
        {
            return Mathf.Approximately(parentScale, 0f) ? targetScale : targetScale / parentScale;
        }
    }
}
