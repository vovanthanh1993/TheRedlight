using CharacterCustomizationTool.Editor.Character;
using CharacterCustomizationTool.Editor.Enums;
using CharacterCustomizationTool.Editor.State;

namespace CharacterCustomizationTool.Editor.Randomizer.Steps.Impl
{
    public class BodyTypeAndGenderStep : IRandomizerStep
    {
        public StepResult Process(CustomizableCharacter character, CharacterState state, GroupType[] groups)
        {
            const BodyType bodyType = BodyType.General;
            const Gender gender = Gender.None;

            var characterSate = CharacterState.CreateDefault(bodyType, gender, character.GetSlotsFor(bodyType, gender));

            return new StepResult(characterSate, groups);
        }
    }
}