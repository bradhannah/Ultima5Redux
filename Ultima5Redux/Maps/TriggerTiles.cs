using System.Collections.Generic;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.Maps
{
    public class TriggerTiles
    {
        private readonly Dictionary<Point2D, List<TriggerTileData>> _triggerTilesByPosition = new();

        public bool HasTriggerAtPosition(Point2D position) => _triggerTilesByPosition.ContainsKey(position);

        public List<TriggerTileData> GetTriggerTileDataByPosition(Point2D position) =>
            HasTriggerAtPosition(position) ? _triggerTilesByPosition[position] : null;

        public void AddNewTrigger(TileReference triggerSprite, Point2D triggeredPosition,
            Point2D triggerChangePosition1, Point2D triggerChangePosition2)
        {
            if (!_triggerTilesByPosition.ContainsKey(triggeredPosition))
            {
                _triggerTilesByPosition.Add(triggeredPosition, new());
                // throw new Ultima5ReduxException(
                //     $"Tried to put the triggers that trigger at the same position: {triggeredPosition.X}, {triggeredPosition.Y}");
            }

            _triggerTilesByPosition[triggeredPosition].Add(new TriggerTileData(triggerSprite, triggeredPosition,
                triggerChangePosition1, triggerChangePosition2));
        }
    }

    public class TriggerTileData
    {
        public bool Triggered { get; private set; }

        public void Trigger() => Triggered = true;
        public void ResetTrigger() => Triggered = false;

        public TileReference TriggerSprite { get; }
        public Point2D TriggeredPosition { get; }
        public IReadOnlyList<Point2D> TriggerChangePositions => _triggerChangePositions.AsReadOnly();

        private readonly List<Point2D> _triggerChangePositions = new();

        public TriggerTileData(TileReference triggerSprite, Point2D triggeredPosition,
            Point2D triggerChangePosition1, Point2D triggerChangePosition2)
        {
            TriggerSprite = triggerSprite;
            TriggeredPosition = triggeredPosition;
            _triggerChangePositions.Add(triggerChangePosition1);
            _triggerChangePositions.Add(triggerChangePosition2);
        }
    }
}