using System.Collections.Generic;
using System.Linq;
using CharacterCustomizationTool.Editor.Enums;

namespace CharacterCustomizationTool.Editor.Character
{
    public static class SlotSorter
    {
        private static readonly List<SlotType> SlotTypesInOrder = new()
        {
            SlotType.SkinColor,
            SlotType.Face,
            SlotType.Hair,
            SlotType.FacialHair,
            SlotType.Hat,
            SlotType.Outerwear,
            SlotType.Pants,
            SlotType.Shoes,
            SlotType.Gloves,
            SlotType.Backpack,
        };

        public static IEnumerable<Slot> Sort(IEnumerable<Slot> slots)
        {
            var sortedSlots = SlotTypesInOrder
                .Select(type => slots.FirstOrDefault(p => p.IsOfType(type)))
                .Where(part => part != null)
                .ToList();

            return sortedSlots;
        }
    }
}