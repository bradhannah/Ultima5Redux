using System.Collections.Generic;

namespace Ultima5Redux.MapUnits.CombatMapUnits
{
    /// <summary>
    ///     Represents a stack of items that can be picked up
    /// </summary>
    public class ItemStack : NonAttackingUnit
    {
        public override string FriendlyName { get; }
        public override string PluralName { get; }
        public override string SingularName { get; }
        public override string Name { get; }

        private readonly Stack<StackableItem> _stackableItems = new();

        public void PushStackableItem(StackableItem item) => _stackableItems.Push(item);

        public StackableItem PopStackableItem()
        {
            if (_stackableItems.Count == 0)
                throw new Ultima5ReduxException("Tried to pop a StackableItem but non were left");
            return _stackableItems.Pop();
        }

        public bool AreStackableItems => _stackableItems.Count > 0;
    }
}