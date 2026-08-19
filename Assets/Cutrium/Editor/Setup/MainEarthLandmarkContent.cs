using System;
using Cutrium.Presentation.Landmark;

namespace Cutrium.Editor.Setup
{
    public static class MainEarthLandmarkContent
    {
        public static LandmarkDefinition[] CreateOrUpdateAssets()
        {
            LandmarkDefinition[] chapterOne =
                FirstTwelveLandmarkContent.CreateOrUpdateAssets();
            LandmarkDefinition[] chapterTwo =
                ChapterTwoLandmarkContent.CreateOrUpdateAssets();
            var definitions = new LandmarkDefinition[
                chapterOne.Length + chapterTwo.Length];
            Array.Copy(chapterOne, definitions, chapterOne.Length);
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
