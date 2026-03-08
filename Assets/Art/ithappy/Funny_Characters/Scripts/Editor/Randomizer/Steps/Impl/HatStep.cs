using CharacterCustomizationTool.Editor.Enums;

namespace CharacterCustomizationTool.Editor.Randomizer.Steps.Impl
{
    public class HatStep : SlotStepBase
    {
        protected override SlotType SlotType => SlotType.Hat;
        protected override float Probability => .33f;
    }
}