using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Pinball.Ingame.Map
{
    public class LaneInteractable : MapInteractable
    {
        const int MIN_POINT_COUNT = 2;
        const float MIN_SEGMENT_LENGTH = 0.0001f;

        [SerializeField] LaneEntrance _startEntrance = null;
        [SerializeField] LaneEntrance _endEntrance = null;
        [SerializeField] Transform _pathRoot = null;
        [SerializeField] Vector2[] _bakedLocalPoints = null;
        [SerializeField] int _scoreReward = 0;
        [SerializeField] float _minTravelSpeed = 6f;
        [SerializeField] float _maxTravelSpeed = 22f;
        [SerializeField] float _exitSpeedMultiplier = 1f;

        Vector2[] _pathPoints = null;
        float[] _pathDistances = null;
        List<Rigidbody2D> _travelingBodies = null;
        CancellationTokenSource _cancellationTokenSource = null;

        public override EGimmickType GimmickType => EGimmickType.Lane;
        public override bool IsDeactivatable => false;

        protected override void OnInit()
        {
            LoadBakedPath();
            _travelingBodies = new List<Rigidbody2D>();
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            _startEntrance.Init(OnStartEntranceEntered);

            if (null != _endEntrance)
                _endEntrance.Init(OnEndEntranceEntered);
        }

        protected override void OnRelease()
        {
            if (null != _endEntrance)
                _endEntrance.Release();

            _startEntrance.Release();

            if (null != _cancellationTokenSource)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }

            if (null != _travelingBodies)
            {
                for (int i = 0; i < _travelingBodies.Count; ++i)
                    ReleaseBody(_travelingBodies[i], Vector2.zero);

                _travelingBodies.Clear();
                _travelingBodies = null;
            }

            _pathPoints = null;
            _pathDistances = null;
        }

        void LoadBakedPath()
        {
            if (null == _bakedLocalPoints || _bakedLocalPoints.Length < MIN_POINT_COUNT)
            {
                Debug.LogError($"[{name}] 베이크된 경로가 없습니다. 맵 프리팹 루트에서 Bind Serialized Field를 실행하세요.", this);
                return;
            }

            _pathPoints = new Vector2[_bakedLocalPoints.Length];
            for (int i = 0; i < _bakedLocalPoints.Length; ++i)
                _pathPoints[i] = transform.TransformPoint(_bakedLocalPoints[i]);

            _pathDistances = new float[_pathPoints.Length];
            for (int i = 1; i < _pathPoints.Length; ++i)
                _pathDistances[i] = _pathDistances[i - 1] + Vector2.Distance(_pathPoints[i - 1], _pathPoints[i]);
        }

        void OnStartEntranceEntered(Collider2D other)
        {
            TryEnterLane(other, true);
        }

        void OnEndEntranceEntered(Collider2D other)
        {
            TryEnterLane(other, false);
        }

        void TryEnterLane(Collider2D other, bool isForward)
        {
            if (null == _pathPoints)
                return;

            if (!IsBallLayer(other.gameObject.layer))
                return;

            Rigidbody2D body = other.attachedRigidbody;
            if (null == body)
                return;

            if (body.bodyType != RigidbodyType2D.Dynamic)
                return;

            if (_travelingBodies.Contains(body))
                return;

            if (Vector2.Dot(body.linearVelocity, GetEnterDirection(isForward)) <= 0f)
                return;

            PlayGimmickHit();

            float entrySpeed = body.linearVelocity.magnitude;
            TravelAsync(body, Mathf.Clamp(entrySpeed, _minTravelSpeed, _maxTravelSpeed), isForward, _cancellationTokenSource.Token).Forget();
        }

        async UniTaskVoid TravelAsync(Rigidbody2D body, float travelSpeed, bool isForward, CancellationToken cancellationToken)
        {
            _travelingBodies.Add(body);
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.bodyType = RigidbodyType2D.Kinematic;

            float totalLength = _pathDistances[_pathDistances.Length - 1];
            float traveledLength = 0f;

            while (traveledLength < totalLength)
            {
                await UniTask.Yield(PlayerLoopTiming.FixedUpdate, cancellationToken);

                traveledLength += travelSpeed * Time.deltaTime;
                float sampled = Mathf.Min(traveledLength, totalLength);
                body.MovePosition(SamplePath(isForward ? sampled : totalLength - sampled));
            }

            _travelingBodies.Remove(body);
            ReleaseBody(body, GetExitDirection(isForward) * travelSpeed * _exitSpeedMultiplier);
            GainScore(_scoreReward);
        }

        Vector2 SamplePath(float distance)
        {
            for (int i = 1; i < _pathDistances.Length; ++i)
            {
                if (_pathDistances[i] < distance)
                    continue;

                float segmentLength = _pathDistances[i] - _pathDistances[i - 1];
                if (segmentLength < MIN_SEGMENT_LENGTH)
                    return _pathPoints[i];

                return Vector2.Lerp(_pathPoints[i - 1], _pathPoints[i], (distance - _pathDistances[i - 1]) / segmentLength);
            }

            return _pathPoints[_pathPoints.Length - 1];
        }

        Vector2 GetEnterDirection(bool isForward)
        {
            if (isForward)
                return (_pathPoints[1] - _pathPoints[0]).normalized;

            int lastIndex = _pathPoints.Length - 1;
            return (_pathPoints[lastIndex - 1] - _pathPoints[lastIndex]).normalized;
        }

        Vector2 GetExitDirection(bool isForward)
        {
            if (!isForward)
                return (_pathPoints[0] - _pathPoints[1]).normalized;

            int lastIndex = _pathPoints.Length - 1;
            return (_pathPoints[lastIndex] - _pathPoints[lastIndex - 1]).normalized;
        }

        void ReleaseBody(Rigidbody2D body, Vector2 exitVelocity)
        {
            if (null == body)
                return;

            body.bodyType = RigidbodyType2D.Dynamic;
            body.linearVelocity = exitVelocity;
        }

#if UNITY_EDITOR
        const int SAMPLES_PER_SEGMENT = 10;

        public void BakePath()
        {
            _bakedLocalPoints = BuildLocalPath();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        protected override void OnBindSerializedField()
        {
            base.OnBindSerializedField();
            BakePath();
            BindEntrances();
        }

        void BindEntrances()
        {
            _startEntrance = null;
            _endEntrance = null;

            if (null == _bakedLocalPoints || _bakedLocalPoints.Length < MIN_POINT_COUNT)
                return;

            LaneEntrance[] entrances = GetComponentsInChildren<LaneEntrance>(true);
            Vector2 startPoint = _bakedLocalPoints[0];
            Vector2 endPoint = _bakedLocalPoints[_bakedLocalPoints.Length - 1];

            for (int i = 0; i < entrances.Length; ++i)
            {
                Vector2 local = transform.InverseTransformPoint(entrances[i].transform.position);
                if ((local - startPoint).sqrMagnitude <= (local - endPoint).sqrMagnitude)
                    _startEntrance = entrances[i];
                else
                    _endEntrance = entrances[i];
            }
        }

        Vector2[] BuildLocalPath()
        {
            if (null == _pathRoot)
                return null;

            int waypointCount = _pathRoot.childCount;
            if (waypointCount < MIN_POINT_COUNT)
                return null;

            List<Vector2> sampledPoints = new List<Vector2>();
            for (int i = 0; i < waypointCount - 1; ++i)
            {
                Vector2 previous = GetLocalWaypoint(Mathf.Max(i - 1, 0));
                Vector2 start = GetLocalWaypoint(i);
                Vector2 end = GetLocalWaypoint(i + 1);
                Vector2 next = GetLocalWaypoint(Mathf.Min(i + 2, waypointCount - 1));

                for (int sample = 0; sample < SAMPLES_PER_SEGMENT; ++sample)
                    sampledPoints.Add(SampleCatmullRom(previous, start, end, next, (float)sample / SAMPLES_PER_SEGMENT));
            }
            sampledPoints.Add(GetLocalWaypoint(waypointCount - 1));
            return sampledPoints.ToArray();
        }

        Vector2 GetLocalWaypoint(int index)
        {
            return transform.InverseTransformPoint(_pathRoot.GetChild(index).position);
        }

        static Vector2 SampleCatmullRom(Vector2 previous, Vector2 start, Vector2 end, Vector2 next, float t)
        {
            float squared = t * t;
            float cubed = squared * t;
            return 0.5f * ((2f * start)
                + (-previous + end) * t
                + (2f * previous - 5f * start + 4f * end - next) * squared
                + (-previous + 3f * start - 3f * end + next) * cubed);
        }
#endif // UNITY_EDITOR
    }
}
