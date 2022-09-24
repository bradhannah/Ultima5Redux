using System;
using System.Collections.Generic;
using System.Linq;
using Ultima5Redux.PlayerCharacters.Inventory;
using Ultima5Redux.References;
using Ultima5Redux.References.Maps;
using Ultima5Redux.References.PlayerCharacters.Inventory;

namespace Ultima5Redux.MapUnits.CombatMapUnits
{
    public static class NonAttackingUnitFactory
    {
        // public enum DropSprites { Nothing = 0, Chest = 257, DeadBody = 286, BloodSpatter = 287 }

        // 257 = Chest
        // 271 = ItemFood
        public static NonAttackingUnit Create(int nSprite, MapUnitPosition mapUnitPosition)
        {
            if (mapUnitPosition == null) throw new ArgumentNullException(nameof(mapUnitPosition));

            switch (nSprite)
            {
                case (int)TileReference.SpriteIndex.Nothing: return null;
                case >= (int)TileReference.SpriteIndex.Whirlpool_KeyIndex
                    and <= (int)TileReference.SpriteIndex.Whirlpool_KeyIndex + 4:
                    return new Whirlpool(mapUnitPosition);
                case (int)TileReference.SpriteIndex.PoisonField or (int)TileReference.SpriteIndex.MagicField
                    or (int)TileReference.SpriteIndex.FireField or (int)TileReference.SpriteIndex.ElectricField:
                    return new ElementalField((ElementalField.FieldType)nSprite, mapUnitPosition);
                case (int)TileReference.SpriteIndex.DeadBody:
                    return new DeadBody(mapUnitPosition);
                case (int)TileReference.SpriteIndex.BloodSpatter:
                    return new BloodSpatter(mapUnitPosition);
                case (int)TileReference.SpriteIndex.Chest:
                    return new Chest(mapUnitPosition);
            }

            ItemStack itemStack = new(mapUnitPosition);

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

        public static StackableItem CreateStackableItem(int nSprite, int nQuality)
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
            //int nInvRefs = invRefs.Count;
            //int nChosenRef = Utils.Ran.Next() % nInvRefs;

            ///// NOTE: it is looking for a high weapon index BECAUSE it wants the Equipment #, not the index of the weapon within
            /// it's own array. I will need to do some sort of Equipment lookup for weapons, armour
            /// I think these represent all the tile types that should index into Equipment #
            InventoryItem invItem;
            switch ((TileReference.SpriteIndex)nSprite)
            {
                case TileReference.SpriteIndex.ItemPotion
                    or TileReference.SpriteIndex.ItemScroll or TileReference.SpriteIndex.ItemWeapon
                    or TileReference.SpriteIndex.ItemShield or TileReference.SpriteIndex.ItemHelm
                    or TileReference.SpriteIndex.ItemRing or TileReference.SpriteIndex.ItemArmour
                    or TileReference.SpriteIndex.ItemAnkh:
                {
                    if ((TileReference.SpriteIndex)nSprite == TileReference.SpriteIndex.ItemScroll
                        && nQuality == 255)
                    {
                        InventoryReference inventoryReference =
                            GameReferences.InvRef.GetInventoryReference(InventoryReferences.InventoryReferenceType.Item,
                                "HMSCape");
                        invItem = InventoryItemFactory.Create(inventoryReference);
                        // default to 1 for now, unless I find a circumstance that needs more
                        invItem.Quantity = 1;
                    }
                    else
                    {
                        InventoryReference inventoryReference =
                            GameReferences.InvRef.GetInventoryReference((DataOvlReference.Equipment)nQuality);
                        invItem = InventoryItemFactory.Create(inventoryReference);
                        // default to 1 for now, unless I find a circumstance that needs more
                        invItem.Quantity = 1;
                    }

                    break;
                }
                case TileReference.SpriteIndex.ItemKey or
                    TileReference.SpriteIndex.ItemGem or
                    TileReference.SpriteIndex.ItemTorch or
                    TileReference.SpriteIndex.ItemFood or
                    TileReference.SpriteIndex.ItemMoney:
                    invItem = InventoryItemFactory.Create(invRefs[0]);
                    invItem.Quantity = 5;
                    break;
                default:
                    // everything else uses the "quality" attribute as a quantity
                    invItem = InventoryItemFactory.Create(invRefs[nQuality]);
                    invItem.Quantity = nQuality;
                    break;
            }

            StackableItem item = new(invItem);

            return item;
        }
    }
}