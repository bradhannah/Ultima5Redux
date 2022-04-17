using System.Collections.Generic;
using System.Linq;
using Ultima5Redux.PlayerCharacters.Inventory;
using Ultima5Redux.References;
using Ultima5Redux.References.PlayerCharacters.Inventory;

namespace Ultima5Redux.MapUnits.CombatMapUnits
{
    public static class NonAttackingUnitFactory
    {
        private enum OtherSprites { Chest = 257, DeadBody = 286, BloodSpatter = 287 }

        // 257 = Chest
        // 271 = ItemFood
        public static NonAttackingUnit Create(int nSprite, MapUnitPosition mapUnitPosition = null)
        {
            switch (nSprite)
            {
                // case < (int)OtherSprites.Chest or > (int)ProvisionReferences.SpecificProvisionSpritesType.Food when (nSprite != (int)OtherSprites.DeadBody && nSprite != (int)OtherSprites.BloodSpatter):
                //     throw new Ultima5ReduxException(
                //         $"Cannot create {nSprite} with the NonAttackingUnitFactory. It must be between 257 and 271");
                case (int)OtherSprites.DeadBody:
                    return new DeadBody();
                case (int)OtherSprites.BloodSpatter:
                    return new BloodSpatter();
                case (int)OtherSprites.Chest:
                    return new Chest(mapUnitPosition);
            }

            ItemStack itemStack = new();

            itemStack.PushStackableItem(CreateStackableItem(nSprite));
            return itemStack;
        }

        public static StackableItem CreateStackableItem(int nSprite)
        {
            // we know it's an actual item at this point
            List<InventoryReference> invRefs = GameReferences.InvRef.GetInventoryReferences(nSprite).ToList();

            if (invRefs.Count == 0)
            {
                throw new Ultima5ReduxException(
                    $"Got zero InventoryReferences back for {nSprite} with the NonAttackingUnitFactory.");
            }

            // at this point we know there are greater than one items in the stack
            // FOR NOW we pick a random choice amongst the returned references and add a single item to the pile
            int nInvRefs = invRefs.Count;
            int nChosenRef = Utils.Ran.Next() % nInvRefs;
            InventoryItem invItem = InventoryItemFactory.Create(invRefs[nChosenRef]);
            // default to 1 for now, unless I find a circumstance that needs more
            invItem.Quantity = 1;
            StackableItem item = new(invItem);

            return item;
        }
    }
}