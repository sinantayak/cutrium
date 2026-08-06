using System;
using System.Collections.Generic;
using Cutrium.Gameplay.Barriers;
using Cutrium.Gameplay.Geometry;
using Cutrium.Gameplay.Threats;

namespace Cutrium.Gameplay.Board
{
    public sealed class CaptureBoardState
    {
        private readonly GeometryTolerancePolicy _tolerance;
        private readonly List<RoomState> _activeRooms = new List<RoomState>();
        private readonly List<RoomState> _capturedRooms = new List<RoomState>();
        private readonly List<ThreatState> _threats = new List<ThreatState>();
        private readonly List<BarrierState> _completedBarriers =
            new List<BarrierState>();
        private readonly HashSet<int> _completedBarrierIds = new HashSet<int>();
        private int _nextRoomId;

        public CaptureBoardState(
            LogicalRect initialBounds,
            IReadOnlyList<ThreatState> threats,
            GeometryTolerancePolicy tolerance)
        {
            if (initialBounds.Width <= 0f || initialBounds.Height <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(initialBounds));
            }

            if (threats == null || threats.Count == 0)
            {
                throw new ArgumentException(
                    "A capture board needs at least one threat.",
                    nameof(threats));
            }

            _tolerance = tolerance;
            InitialBounds = initialBounds;
            var initialRoom = new RoomState(new RoomId(1), initialBounds);
            _activeRooms.Add(initialRoom);
            var threatIds = new HashSet<int>();
            for (int index = 0; index < threats.Count; index++)
            {
                ThreatState threat = threats[index];
                if (threat.RoomId != initialRoom.Id
                    || !ContainsCircle(initialBounds, threat))
                {
                    throw new ArgumentException(
                        "Every initial threat must fit inside the initial room.",
                        nameof(threats));
                }

                if (!threatIds.Add(threat.Id.Value))
                {
                    throw new ArgumentException(
                        "Initial threat IDs must be unique.",
                        nameof(threats));
                }

                _threats.Add(threat);
            }

            _nextRoomId = 2;
            ValidateInvariants(
                _activeRooms,
                _capturedRooms,
                _threats,
                0f);
        }

        public LogicalRect InitialBounds { get; }

        public IReadOnlyList<RoomState> ActiveRooms => _activeRooms;

        public IReadOnlyList<RoomState> CapturedRooms => _capturedRooms;

        public IReadOnlyList<ThreatState> Threats => _threats;

        public IReadOnlyList<BarrierState> CompletedBarriers =>
            _completedBarriers;

        public float ActiveArea => SumArea(_activeRooms);

        public float CapturedArea => SumArea(_capturedRooms);

        public float CapturedFraction => 1f - (ActiveArea / InitialBounds.Area);

        public bool TryGetActiveRoom(RoomId id, out RoomState room)
        {
            int index = FindRoomIndex(_activeRooms, id);
            if (index >= 0)
            {
                room = _activeRooms[index];
                return true;
            }

            room = default;
            return false;
        }

        public bool TryGetActiveRoomAt(LogicalPoint point, out RoomState room)
        {
            for (int index = 0; index < _activeRooms.Count; index++)
            {
                RoomState candidate = _activeRooms[index];
                if (_tolerance.Contains(candidate.Bounds, point))
                {
                    room = candidate;
                    return true;
                }
            }

            room = default;
            return false;
        }

        public ThreatState GetThreat(ThreatId id)
        {
            for (int index = 0; index < _threats.Count; index++)
            {
                if (_threats[index].Id == id)
                {
                    return _threats[index];
                }
            }

            throw new ArgumentOutOfRangeException(nameof(id));
        }

        public void UpdateThreat(ThreatState threat)
        {
            for (int index = 0; index < _threats.Count; index++)
            {
                if (_threats[index].Id != threat.Id)
                {
                    continue;
                }

                if (!TryGetActiveRoom(threat.RoomId, out RoomState room)
                    || !ContainsCircle(room.Bounds, threat))
                {
                    throw new InvalidOperationException(
                        "A moving threat must remain inside its active room.");
                }

                _threats[index] = threat;
                return;
            }

            throw new ArgumentOutOfRangeException(nameof(threat));
        }

        public RoomSplitApplyResult TryApplyLockedBarrier(
            BarrierState barrier)
        {
            if (barrier.Lifecycle != BarrierLifecycle.Locked
                || !barrier.IsComplete)
            {
                return Reject(RoomSplitDiagnostic.BarrierNotLocked);
            }

            if (_completedBarrierIds.Contains(barrier.Id.Value))
            {
                return Reject(RoomSplitDiagnostic.BarrierAlreadyApplied);
            }

            int parentIndex = FindRoomIndex(_activeRooms, barrier.ParentRoomId);
            if (parentIndex < 0)
            {
                return Reject(RoomSplitDiagnostic.StaleParentRoom);
            }

            RoomState parent = _activeRooms[parentIndex];
            if (!TryCreateChildren(
                    parent,
                    barrier,
                    out RoomState negative,
                    out RoomState positive))
            {
                return Reject(RoomSplitDiagnostic.InvalidSplit);
            }

            var reassignedThreats = new List<ThreatState>(_threats);
            int negativeThreatCount = 0;
            int positiveThreatCount = 0;
            RoomSplitDiagnostic diagnostic = RoomSplitDiagnostic.None;
            for (int index = 0; index < reassignedThreats.Count; index++)
            {
                ThreatState threat = reassignedThreats[index];
                if (threat.RoomId != parent.Id)
                {
                    continue;
                }

                Classification classification = Classify(
                    threat,
                    barrier,
                    out RoomSplitDiagnostic threatDiagnostic);
                if (classification == Classification.Invalid)
                {
                    return Reject(threatDiagnostic);
                }

                if (threatDiagnostic != RoomSplitDiagnostic.None)
                {
                    diagnostic = threatDiagnostic;
                }

                bool goesNegative = classification == Classification.Negative;
                RoomId childId = goesNegative ? negative.Id : positive.Id;
                reassignedThreats[index] = threat.WithRoom(childId);
                if (goesNegative)
                {
                    negativeThreatCount++;
                }
                else
                {
                    positiveThreatCount++;
                }
            }

            var nextActive = new List<RoomState>(_activeRooms);
            var nextCaptured = new List<RoomState>(_capturedRooms);
            nextActive.RemoveAt(parentIndex);
            AddChild(
                negative,
                negativeThreatCount,
                nextActive,
                nextCaptured);
            AddChild(
                positive,
                positiveThreatCount,
                nextActive,
                nextCaptured);

            float previousCapturedFraction = CapturedFraction;
            if (!ValidateInvariants(
                    nextActive,
                    nextCaptured,
                    reassignedThreats,
                    previousCapturedFraction))
            {
                return Reject(RoomSplitDiagnostic.InvariantViolation);
            }

            _activeRooms.Clear();
            _activeRooms.AddRange(nextActive);
            _capturedRooms.Clear();
            _capturedRooms.AddRange(nextCaptured);
            _threats.Clear();
            _threats.AddRange(reassignedThreats);
            _completedBarriers.Add(barrier);
            _completedBarrierIds.Add(barrier.Id.Value);
            _nextRoomId += 2;
            return new RoomSplitApplyResult(
                true,
                diagnostic,
                negative,
                positive);
        }

        public bool ValidateCurrentInvariants() =>
            ValidateInvariants(
                _activeRooms,
                _capturedRooms,
                _threats,
                CapturedFraction);

        private bool TryCreateChildren(
            RoomState parent,
            BarrierState barrier,
            out RoomState negative,
            out RoomState positive)
        {
            LogicalRect bounds = parent.Bounds;
            LogicalRect negativeBounds;
            LogicalRect positiveBounds;
            if (barrier.Orientation == BarrierOrientation.Vertical)
            {
                float split = barrier.Origin.X;
                if (_tolerance.IsLessThanOrApproximatelyEqualDistance(
                        split,
                        bounds.MinX)
                    || _tolerance.IsGreaterThanOrApproximatelyEqualDistance(
                        split,
                        bounds.MaxX))
                {
                    negative = default;
                    positive = default;
                    return false;
                }

                negativeBounds = LogicalRect.FromMinMax(
                    bounds.MinX,
                    bounds.MinY,
                    split,
                    bounds.MaxY);
                positiveBounds = LogicalRect.FromMinMax(
                    split,
                    bounds.MinY,
                    bounds.MaxX,
                    bounds.MaxY);
            }
            else
            {
                float split = barrier.Origin.Y;
                if (_tolerance.IsLessThanOrApproximatelyEqualDistance(
                        split,
                        bounds.MinY)
                    || _tolerance.IsGreaterThanOrApproximatelyEqualDistance(
                        split,
                        bounds.MaxY))
                {
                    negative = default;
                    positive = default;
                    return false;
                }

                negativeBounds = LogicalRect.FromMinMax(
                    bounds.MinX,
                    bounds.MinY,
                    bounds.MaxX,
                    split);
                positiveBounds = LogicalRect.FromMinMax(
                    bounds.MinX,
                    split,
                    bounds.MaxX,
                    bounds.MaxY);
            }

            if (!_tolerance.IsAreaApproximatelyEqual(
                    negativeBounds.Area + positiveBounds.Area,
                    bounds.Area))
            {
                negative = default;
                positive = default;
                return false;
            }

            negative = new RoomState(new RoomId(_nextRoomId), negativeBounds);
            positive = new RoomState(
                new RoomId(_nextRoomId + 1),
                positiveBounds);
            return true;
        }

        private Classification Classify(
            ThreatState threat,
            BarrierState barrier,
            out RoomSplitDiagnostic diagnostic)
        {
            float center = barrier.Orientation == BarrierOrientation.Vertical
                ? threat.Position.X
                : threat.Position.Y;
            float velocity = barrier.Orientation == BarrierOrientation.Vertical
                ? threat.Velocity.X
                : threat.Velocity.Y;
            float split = barrier.Orientation == BarrierOrientation.Vertical
                ? barrier.Origin.X
                : barrier.Origin.Y;
            bool negative = _tolerance.IsLessThanOrApproximatelyEqualDistance(
                center + threat.Radius,
                split);
            bool positive = _tolerance.IsGreaterThanOrApproximatelyEqualDistance(
                center - threat.Radius,
                split);
            if (negative != positive)
            {
                diagnostic = RoomSplitDiagnostic.None;
                return negative
                    ? Classification.Negative
                    : Classification.Positive;
            }

            float penetration = threat.Radius - Math.Abs(center - split);
            if (penetration > _tolerance.DistanceTolerance
                && !_tolerance.IsDistanceApproximatelyEqual(penetration, 0f))
            {
                diagnostic = RoomSplitDiagnostic.ThreatStraddlesSplit;
                return Classification.Invalid;
            }

            diagnostic = RoomSplitDiagnostic.ThreatTieFallback;
            if (!_tolerance.IsDistanceApproximatelyEqual(center, split))
            {
                return center < split
                    ? Classification.Negative
                    : Classification.Positive;
            }

            return velocity <= 0f
                ? Classification.Negative
                : Classification.Positive;
        }

        private bool ValidateInvariants(
            IReadOnlyList<RoomState> active,
            IReadOnlyList<RoomState> captured,
            IReadOnlyList<ThreatState> threats,
            float minimumCapturedFraction)
        {
            float activeArea = SumArea(active);
            float capturedArea = SumArea(captured);
            if (!_tolerance.IsAreaApproximatelyEqual(
                    activeArea + capturedArea,
                    InitialBounds.Area))
            {
                return false;
            }

            float fractionFromActive = 1f - (activeArea / InitialBounds.Area);
            float fractionFromCaptured = capturedArea / InitialBounds.Area;
            if (!_tolerance.IsAreaApproximatelyEqual(
                    fractionFromActive * InitialBounds.Area,
                    fractionFromCaptured * InitialBounds.Area)
                || fractionFromActive + _tolerance.AreaTolerance
                    < minimumCapturedFraction)
            {
                return false;
            }

            var allRooms = new List<RoomState>(active.Count + captured.Count);
            for (int index = 0; index < active.Count; index++)
            {
                allRooms.Add(active[index]);
            }

            for (int index = 0; index < captured.Count; index++)
            {
                allRooms.Add(captured[index]);
            }

            for (int left = 0; left < allRooms.Count; left++)
            {
                for (int right = left + 1; right < allRooms.Count; right++)
                {
                    if (HasPositiveAreaOverlap(
                            allRooms[left].Bounds,
                            allRooms[right].Bounds))
                    {
                        return false;
                    }
                }
            }

            var threatIds = new HashSet<int>();
            for (int index = 0; index < threats.Count; index++)
            {
                ThreatState threat = threats[index];
                if (!threatIds.Add(threat.Id.Value))
                {
                    return false;
                }

                int containingRooms = 0;
                for (int roomIndex = 0; roomIndex < active.Count; roomIndex++)
                {
                    RoomState room = active[roomIndex];
                    if (room.Id == threat.RoomId
                        && ContainsCircle(room.Bounds, threat))
                    {
                        containingRooms++;
                    }
                }

                if (containingRooms != 1)
                {
                    return false;
                }
            }

            return true;
        }

        private bool ContainsCircle(LogicalRect room, ThreatState threat) =>
            _tolerance.IsGreaterThanOrApproximatelyEqualDistance(
                threat.Position.X - threat.Radius,
                room.MinX)
            && _tolerance.IsLessThanOrApproximatelyEqualDistance(
                threat.Position.X + threat.Radius,
                room.MaxX)
            && _tolerance.IsGreaterThanOrApproximatelyEqualDistance(
                threat.Position.Y - threat.Radius,
                room.MinY)
            && _tolerance.IsLessThanOrApproximatelyEqualDistance(
                threat.Position.Y + threat.Radius,
                room.MaxY);

        private bool HasPositiveAreaOverlap(LogicalRect left, LogicalRect right)
        {
            float overlapX = Math.Min(left.MaxX, right.MaxX)
                - Math.Max(left.MinX, right.MinX);
            float overlapY = Math.Min(left.MaxY, right.MaxY)
                - Math.Max(left.MinY, right.MinY);
            return overlapX > _tolerance.DistanceTolerance
                && overlapY > _tolerance.DistanceTolerance;
        }

        private static int FindRoomIndex(
            IReadOnlyList<RoomState> rooms,
            RoomId id)
        {
            for (int index = 0; index < rooms.Count; index++)
            {
                if (rooms[index].Id == id)
                {
                    return index;
                }
            }

            return -1;
        }

        private static float SumArea(IReadOnlyList<RoomState> rooms)
        {
            float area = 0f;
            for (int index = 0; index < rooms.Count; index++)
            {
                area += rooms[index].Bounds.Area;
            }

            return area;
        }

        private static void AddChild(
            RoomState child,
            int threatCount,
            ICollection<RoomState> active,
            ICollection<RoomState> captured)
        {
            if (threatCount == 0)
            {
                captured.Add(child);
            }
            else
            {
                active.Add(child);
            }
        }

        private static RoomSplitApplyResult Reject(
            RoomSplitDiagnostic diagnostic) =>
            new RoomSplitApplyResult(false, diagnostic, default, default);

        private enum Classification
        {
            Invalid = 0,
            Negative = 1,
            Positive = 2
        }
    }
}
