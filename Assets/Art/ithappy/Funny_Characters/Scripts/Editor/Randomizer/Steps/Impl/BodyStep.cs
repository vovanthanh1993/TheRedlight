using CharacterCustomizationTool.Editor.Character;
using CharacterCustomizationTool.Editor.Enums;
using CharacterCustomizationTool.Editor.State;
using UnityEngine;

namespace CharacterCustomizationTool.Editor.Randomizer.Steps.Impl
{
    public class BodyStep : StepBase
    {
        protected override GroupType GroupType => GroupType.Body;

        public override StepResult Process(CustomizableCharacter character, CharacterState state, GroupType[] groups)
        {
            var skinColorIndex = Random.Range(0, character.GetVariantsCountInGroup(state, GroupType));
            var slotState = new SlotState(SlotType.SkinColor, GroupType.Body, true, skinColorIndex);

            return new StepResult(state.Update(slotState), RemoveSelf(groups));
        }
    }
}