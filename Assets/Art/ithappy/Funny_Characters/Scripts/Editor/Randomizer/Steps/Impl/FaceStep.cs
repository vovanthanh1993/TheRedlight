using CharacterCustomizationTool.Editor.Character;
using CharacterCustomizationTool.Editor.Enums;
using CharacterCustomizationTool.Editor.Extensions;
using CharacterCustomizationTool.Editor.State;
using UnityEngine;

namespace CharacterCustomizationTool.Editor.Randomizer.Steps.Impl
{
    public class FaceStep : StepBase
    {
        protected override GroupType GroupType => GroupType.Face;

        public override StepResult Process(CustomizableCharacter character, CharacterState state, GroupType[] groups)
        {
            int faceIndex;

            if (!GeneratorSettings.StandardFace || !character.TryGetVariantsByName(SlotType.Face, "usual", out var faceIndexes))
            {
                var facesCount = character.GetVariantsCount(SlotType.Face);
                faceIndex = Random.Range(0, facesCount);
            }
            else
            {
                faceIndex = faceIndexes.Random();
            }

            return new StepResult(state.Update(new SlotState(SlotType.Face, GroupType.Face, true, faceIndex)), RemoveSelf(groups));
        }
    }
}