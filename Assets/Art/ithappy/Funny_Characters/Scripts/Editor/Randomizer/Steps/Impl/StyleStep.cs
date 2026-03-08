using System.Collections.Generic;
using System.Linq;
using CharacterCustomizationTool.Editor.Character;
using CharacterCustomizationTool.Editor.Enums;
using CharacterCustomizationTool.Editor.State;

namespace CharacterCustomizationTool.Editor.Randomizer.Steps.Impl
{
    public class StyleStep : IRandomizerStep
    {
        public static IEnumerable<GroupType> EvaGroups => new[]
        {
            GroupType.SuitHelmet, GroupType.SuitTop, GroupType.SuitBottom,
            GroupType.SuitGloves, GroupType.SuitShoes, GroupType.SuitBackpack
        };

        public static IEnumerable<GroupType> BaseGroups => new[] { GroupType.Outerwear, GroupType.Pants, GroupType.Shoes, GroupType.Bodysuit, };

        public StepResult Process(CustomizableCharacter character, CharacterState state, GroupType[] groups)
        {
            var processedGroups = groups;

            if (!GeneratorSettings.IsEva) processedGroups = groups.Except(EvaGroups).ToArray();
            if (!GeneratorSettings.IsBase) processedGroups = groups.Except(BaseGroups).ToArray();


            return new StepResult(state, processedGroups);
        }
    }
}