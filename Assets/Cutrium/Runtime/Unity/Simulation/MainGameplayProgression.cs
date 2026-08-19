using System;

namespace Cutrium.Unity.Simulation
{
    public static class MainGameplayProgression
    {
        public const int LevelCount =
            FirstTwelveGameplayProgression.LevelCount
            + ChapterTwoGameplayProgression.LevelCount;

        public static CoreFunLevelDefinition[] CreateDefinitions()
        {
            CoreFunLevelDefinition[] chapterOne =
                FirstTwelveGameplayProgression.CreateDefinitions();
            CoreFunLevelDefinition[] chapterTwo =
                ChapterTwoGameplayProgression.CreateDefinitions();
            var definitions = new CoreFunLevelDefinition[LevelCount];
            Array.Copy(chapterOne, 0, definitions, 0, chapterOne.Length);
            Array.Copy(
                chapterTwo,
                0,
                definitions,
                chapterOne.Length,
                chapterTwo.Length);
            return definitions;
        }
    }
}
