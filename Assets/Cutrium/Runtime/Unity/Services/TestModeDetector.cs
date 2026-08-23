using System;
using System.Linq;

namespace Cutrium.Unity.Services
{
    /// This repo's tests are exclusively run via `-batchmode -runTests`
    /// (see CLAUDE.md's Common Commands) -- checking for that command line
    /// flag lets cloud-service code stay fully offline during an automated
    /// test run instead of making real network calls or writing to the
    /// developer's real local PlayerPrefs, without needing any Editor-only
    /// API.
    internal static class TestModeDetector
    {
        public static readonly bool IsRunningTests =
            Environment.GetCommandLineArgs().Contains("-runTests");
    }
}
