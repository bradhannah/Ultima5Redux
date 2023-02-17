using System.Collections.Generic;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.Maps
{
    public class TriggerTiles
    {
        private readonly Dictionary<Point2D, List<TriggerTileData>> _triggerTilesByPosition = new();

        public void AddNewTrigger(TileReference triggerSprite, Point2D triggeredPosition,
            Point2D triggerChangePosition1, Point2D triggerChangePosition2)
        {
            if (!_triggerTilesByPosition.ContainsKey(triggeredPosition))
            {
                _triggerTilesByPosition.Add(triggeredPosition, new List<TriggerTileData>());
            }

            _triggerTilesByPosition[triggeredPosition].Add(new TriggerTileData(triggerSprite, triggeredPosition,
                triggerChangePosition1, triggerChangePosition2));
        }

        public List<TriggerTileData> GetTriggerTileDataByPosition(Point2D position) =>
            HasTriggerAtPosition(position) ? _triggerTilesByPosition[position] : null;

        public bool HasTriggerAtPosition(Point2D position) => _triggerTilesByPosition.ContainsKey(position);
    }

    public class TriggerTileData
    {
        private readonly List<Point2D> _triggerChangePositions = new();
        public IReadOnlyList<Point2D> TriggerChangePositions => _triggerChangePositions.AsReadOnly();
        public bool Triggered { get; private set; }
        public Point2D TriggeredPosition { get; }

        public TileReference TriggerSprite { get; }

        public TriggerTileData(TileReference triggerSprite, Point2D triggeredPosition,
            Point2D triggerChangePosition1, Point2D triggerChangePosition2)
        {
            TriggerSprite = triggerSprite;
            TriggeredPosition = triggeredPosition;
            _triggerChangePositions.Add(triggerChangePosition1);
            _triggerChangePositions.Add(triggerChangePosition2);
        }

        public void ResetTrigger()
        {
            Triggered = false;
        }

        public void Trigger()
        {
            Triggered = true;
        }
    }
}