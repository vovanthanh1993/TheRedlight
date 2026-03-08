using System.Collections.Generic;
using CharacterCustomizationTool.Editor.Enums;

namespace CharacterCustomizationTool.Editor.Randomizer
{
    public static class GeneratorSettings
    {
        public static List<BodyType> BodyTypes { get; private set; } = new() { BodyType.General, };
        public static List<Gender> Genders { get; private set; } = new() { Gender.None, };
        public static bool StandardFace { get; set; } = true;
        public static bool IsEva { get; set; } = true;
        public static bool IsBase { get; set; } = true;

        public static void ToDefault()
        {
            BodyTypes = new List<BodyType> { BodyType.General, };
            Genders = new List<Gender> { Gender.None, };

            StandardFace = true;
            IsEva = true;
            IsBase = true;
        }

        public static bool IsToggled(BodyType bodyType)
        {
            return BodyTypes.Contains(bodyType);
        }

        public static bool IsToggled(Gender gender)
        {
            return Genders.Contains(gender);
        }
    }
}