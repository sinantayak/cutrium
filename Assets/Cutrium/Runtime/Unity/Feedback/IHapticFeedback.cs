namespace Cutrium.Unity.Feedback
{
    public enum HapticFeedbackCue
    {
        BarrierLock = 0,
        BarrierBreak = 1,
        LargeCapture = 2,
        NearMiss = 3,
        LevelComplete = 4,
        Ui = 5,
    }

    public interface IHapticFeedback
    {
        void Play(HapticFeedbackCue cue);
    }

    public sealed class NoOpHapticFeedback : IHapticFeedback
    {
        public void Play(HapticFeedbackCue cue)
        {
        }
    }
}
