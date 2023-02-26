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

        public IEnumerable<TriggerTileData> GetTriggerTileDataByPosition(Point2D position) =>
            HasTriggerAtPosition(position) ? _triggerTilesByPosition[position] : null;

        public bool HasTriggerAtPosition(Point2D position) => _triggerTilesByPosition.ContainsKey(position);
    }
}