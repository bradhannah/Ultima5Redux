using System.Collections.Generic;
using Ultima5Redux.References.Maps;

namespace Ultima5Redux.Maps
{
    public class TriggerTileData
    {
        private readonly List<Point2D> _triggerChangePositions = new();
        public IEnumerable<Point2D> TriggerChangePositions => _triggerChangePositions.AsReadOnly();
        public bool Triggered { get; private set; }

        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
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