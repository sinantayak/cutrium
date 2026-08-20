using System.IO;
using System.Linq;
using Cutrium.Editor.Setup;
using Cutrium.Gameplay.Geometry;
using Cutrium.Gameplay.Session;
using Cutrium.Gameplay.Threats;
using Cutrium.Presentation.HUD;
using Cutrium.Unity.Input;
using Cutrium.Unity.Simulation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Cutrium.Gameplay.EditModeTests
{
    public sealed class ChapterTwoGameplayProgressionTests
    {
        [Test]
        public void MainProgression_CombinesTwoContiguousTwelveLevelChapters()
        {
            CoreFunLevelDefinition[] definitions =
                MainGameplayProgression.CreateDefinitions();

            Assert.That(definitions, Has.Length.EqualTo(24));
            Assert.That(definitions.Select(level => level.DisplayNumber),
                Is.EqualTo(Enumerable.Range(1, 24)));
            Assert.That(definitions.Select(level => level.StableId)
                    .Distinct().Count(),
                Is.EqualTo(24));
            Assert.That(definitions[11].StableId,
                Is.EqualTo("first-twelve-mastery"));
            Assert.That(definitions[12].StableId,
                Is.EqualTo("motion-reset"));
            Assert.That(definitions[23].StableId,
                Is.EqualTo("motion-and-gravity-mastery"));
        }

        [Test]
        public void ChapterTwo_PreservesAuthoredTargetsPacingAndThreatCounts()
        {
            CoreFunLevelConfiguration[] levels =
                ChapterTwoGameplayProgression.CreateDefinitions()
                    .Select(level => level.ToRuntimeConfiguration())
                    .ToArray();

            Assert.That(levels.Select(level => level.DisplayNumber),
                Is.EqualTo(Enumerable.Range(13, 12)));
            Assert.That(levels.Select(level =>
                    level.Capture.TargetCapturedFraction),
                Is.EqualTo(new[]
                {
                    0.76f, 0.78f, 0.8f, 0.78f,
                    0.8f, 0.82f, 0.82f, 0.8f,
                    0.83f, 0.84f, 0.82f, 0.86f,
                }));
            Assert.That(levels.Select(level => level.Barrier.GrowthSpeed),
                Is.EqualTo(new[]
                {
                    3.1f, 2.9f, 2.65f, 3f,
                    2.8f, 2.7f, 2.6f, 2.8f,
                    2.55f, 2.55f, 2.9f, 2.55f,
                }));
            Assert.That(levels.Select(level => level.ThreatMotions.Count),
                Is.EqualTo(new[] { 1, 2, 2, 1, 1, 2, 2, 2, 2, 2, 4, 3 }));
            Assert.That(levels[10].ThreatMotions.Count, Is.EqualTo(4));
            Assert.That(levels[11].Capture.MaximumAcceptedCuts, Is.EqualTo(10));
        }

        [Test]
        public void CometAndHeavy_AreDistinctNormalMotionProfiles()
        {
            CoreFunLevelConfiguration comet =
                ChapterTwoGameplayProgression.CreateDefinitions()[3]
                    .ToRuntimeConfiguration();
            CoreFunLevelConfiguration heavy =
                ChapterTwoGameplayProgression.CreateDefinitions()[4]
                    .ToRuntimeConfiguration();

            Assert.That(comet.ThreatMotion.Behavior.Kind,
                Is.EqualTo(ThreatBehaviorKind.Normal));
            Assert.That(heavy.ThreatMotion.Behavior.Kind,
                Is.EqualTo(ThreatBehaviorKind.Normal));
            Assert.That(comet.ThreatMotion.Speed,
                Is.GreaterThan(heavy.ThreatMotion.Speed));
            Assert.That(comet.ThreatMotion.Radius,
                Is.LessThan(heavy.ThreatMotion.Radius));
            Assert.That(comet.ThreatMotion.Radius, Is.EqualTo(0.29f));
            Assert.That(heavy.ThreatMotion.Radius, Is.EqualTo(0.52f));
        }

        [Test]
        public void GravityLevels_HaveOneChargeAndMasteryCombinesInstant()
        {
            CoreFunLevelConfiguration[] levels =
                ChapterTwoGameplayProgression.CreateDefinitions()
                    .Select(level => level.ToRuntimeConfiguration())
                    .ToArray();

            foreach (int index in new[] { 7, 8, 10, 11 })
            {
                Assert.That(levels[index].Power.GravityWellCharges,
                    Is.EqualTo(1));
                Assert.That(levels[index].Power.GravityWellDurationSeconds,
                    Is.GreaterThan(4f));
                Assert.That(levels[index].Power.GravityWellRadius,
                    Is.EqualTo(4.5f));
            }

            Assert.That(levels[11].Power.InstantBarrierCharges, Is.EqualTo(1));
            Assert.That(levels[9].Power.FreezePulseCharges, Is.EqualTo(1));
        }

        [Test]
        public void GravityWell_TurnsWithinRadiusWithoutChangingSpeed()
        {
            ThreatMotionSession session = GravitySession(
                new LogicalPoint(5f, 8f),
                new LogicalVector(1f, 0f));

            Assert.That(session.TryActivateGravityWell(
                new LogicalPoint(5f, 10f)), Is.True);
            session.Tick(0.1f);

            float expectedX = 2f * Mathf.Cos(10f * Mathf.Deg2Rad);
            float expectedY = 2f * Mathf.Sin(10f * Mathf.Deg2Rad);
            Assert.That(session.Threat.Velocity.X,
                Is.EqualTo(expectedX).Within(0.002f));
            Assert.That(session.Threat.Velocity.Y,
                Is.EqualTo(expectedY).Within(0.002f));
            Assert.That(session.Threat.Speed, Is.EqualTo(2f).Within(0.001f));
            Assert.That(session.GravityWellChargesRemaining, Is.Zero);
        }

        [Test]
        public void GravityWell_InvalidPlacementDoesNotConsumeACharge()
        {
            ThreatMotionSession session = GravitySession(
                new LogicalPoint(5f, 8f),
                new LogicalVector(1f, 0f));

            Assert.That(session.TryActivateGravityWell(
                new LogicalPoint(12f, 8f)), Is.False);
            Assert.That(session.GravityWellChargesRemaining, Is.EqualTo(1));
            Assert.That(session.GravityWellActive, Is.False);
        }

        [Test]
        public void GravityWell_EmptyPlacementDoesNotConsumeACharge()
        {
            ThreatMotionSession session = GravitySession(
                new LogicalPoint(2f, 2f),
                new LogicalVector(1f, 0f));

            Assert.That(session.TryActivateGravityWell(
                new LogicalPoint(8f, 14f)), Is.False);
            Assert.That(session.GravityWellChargesRemaining, Is.EqualTo(1));
            Assert.That(session.GravityWellActive, Is.False);
        }

        [Test]
        public void GravityWell_DoesNotInfluenceAcrossCompletedBarrier()
        {
            ThreatMotionSession session = GravitySession(
                new LogicalPoint(2f, 8f),
                new LogicalVector(1f, 0f));
            Assert.That(session.TryActivateGravityWell(
                new LogicalPoint(4f, 8f)), Is.True);
            var splitBarrier = new Cutrium.Gameplay.Barriers.BarrierState(
                new Cutrium.Gameplay.Barriers.BarrierId(1),
                new Cutrium.Gameplay.Board.RoomId(1),
                new LogicalPoint(3f, 8f),
                Cutrium.Gameplay.Barriers.BarrierOrientation.Vertical,
                8f, 8f, 8f, 8f, 8f, 0.08f,
                Cutrium.Gameplay.Barriers.BarrierLifecycle.Locked);
            Assert.That(
                session.Board.TryApplyLockedBarrier(splitBarrier).Applied,
                Is.True);
            LogicalVector before = session.Threat.Velocity;

            session.Tick(0.1f);

            Assert.That(session.Threat.Velocity.X,
                Is.EqualTo(before.X).Within(0.001f));
            Assert.That(session.Threat.Velocity.Y,
                Is.EqualTo(before.Y).Within(0.001f));
        }

        [Test]
        public void GravityPointTargeting_CommitsPointInsteadOfBarrierIntent()
        {
            var gameObject = new GameObject("GravityGestureTest");
            BarrierGestureAdapter gesture =
                gameObject.AddComponent<BarrierGestureAdapter>();
            int barrierCount = 0;
            LogicalPoint? selectedPoint = null;
            gesture.IntentCommitted += _ => barrierCount++;
            gesture.PointCommitted += point => selectedPoint = point;
            gesture.SetPointTargeting(true);
            gesture.ProcessSample(Sample(
                PointerSamplePhase.Started,
                new LogicalPoint(4f, 7f)));
            gesture.ProcessSample(Sample(
                PointerSamplePhase.Released,
                new LogicalPoint(4.5f, 7.5f)));

            Assert.That(selectedPoint, Is.EqualTo(new LogicalPoint(4.5f, 7.5f)));
            Assert.That(barrierCount, Is.Zero);
            Assert.That(gesture.CommittedIntentCount, Is.Zero);
            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void GravityHudHighlight_FollowsPointTargetingState()
        {
            var root = new GameObject("GravityHudHighlightTest");
            FirstPlayableController controller =
                root.AddComponent<FirstPlayableController>();
            BarrierGestureAdapter gesture =
                root.AddComponent<BarrierGestureAdapter>();
            controller.ConfigureBarrierForSetup(
                gesture,
                3f,
                0.08f,
                0.6f,
                16);
            controller.ConfigureLevelsForSetup(new[]
            {
                ChapterTwoGameplayProgression.CreateDefinitions()[7],
            });
            controller.AdvanceSimulation(0f);

            var buttonObject = new GameObject(
                "GravityWellButton",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(Outline));
            buttonObject.transform.SetParent(root.transform, false);
            Image image = buttonObject.GetComponent<Image>();
            Button button = buttonObject.GetComponent<Button>();
            Outline highlight = buttonObject.GetComponent<Outline>();
            button.targetGraphic = image;
            highlight.enabled = false;

            var chargesObject = new GameObject(
                "Charges",
                typeof(RectTransform),
                typeof(Text));
            chargesObject.transform.SetParent(buttonObject.transform, false);
            Text charges = chargesObject.GetComponent<Text>();
            PowerHudPresenter presenter =
                root.AddComponent<PowerHudPresenter>();
            presenter.Configure(
                controller,
                null,
                null,
                null,
                null,
                null,
                null,
                buttonObject,
                button,
                charges,
                highlight);

            try
            {
                Assert.That(controller.GravityWellTargeting, Is.False);
                Assert.That(highlight.enabled, Is.False);
                Assert.That(image.color, Is.EqualTo(Color.white));

                Assert.That(controller.ToggleGravityWellTargeting(), Is.True);
                presenter.RefreshNow();
                Assert.That(highlight.enabled, Is.True);
                Assert.That(image.color,
                    Is.EqualTo(new Color(1f, 0.93f, 0.78f, 1f)));

                controller.CancelGravityWellTargeting();
                presenter.RefreshNow();
                Assert.That(highlight.enabled, Is.False);
                Assert.That(image.color, Is.EqualTo(Color.white));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ChapterTwoEarthLandmarks_MatchMarkdownAndArtwork()
        {
            FirstTwelveLandmarkContent.Entry[] entries =
                ChapterTwoLandmarkContent.Entries;
            string markdown = File.ReadAllText("earth-landmarks.md");
            string plainMarkdown = markdown.Replace("*", string.Empty);

            Assert.That(entries, Has.Length.EqualTo(12));
            Assert.That(entries[0].Title, Is.EqualTo("Çin Seddi"));
            Assert.That(entries[11].Title, Is.EqualTo("Ayasofya"));
            foreach (FirstTwelveLandmarkContent.Entry entry in entries)
            {
                Assert.That(plainMarkdown, Does.Contain(entry.Description));
                Assert.That(entry.Description.Length, Is.InRange(290, 430));
                Assert.That(
                    AssetDatabase.LoadAssetAtPath<Sprite>(entry.ArtworkPath),
                    Is.Not.Null,
                    entry.ArtworkPath);
            }
        }

        private static ThreatMotionSession GravitySession(
            LogicalPoint position,
            LogicalVector direction)
        {
            var motion = new ThreatMotionConfiguration(
                CoreFunLevelConfiguration.FixedBoardBounds,
                position,
                direction,
                2f,
                0.35f,
                8);
            var powers = new PowerConfiguration(
                0, 1f, 0.1f, 0, 1f,
                1, 4f, 3f, 100f);
            return new ThreatMotionSession(
                new[] { motion },
                new Cutrium.Gameplay.Barriers.BarrierConfiguration(
                    3f, 0.08f, 1f, 16),
                new CaptureLevelConfiguration(0.8f),
                Cutrium.Gameplay.Feedback.FeedbackTuningConfiguration.Default,
                powers,
                new GeometryTolerancePolicy(
                    0.0001f, 0.00001f, 0.0001f, 0.001f));
        }

        private static PointerSample Sample(
            PointerSamplePhase phase,
            LogicalPoint point) =>
            new PointerSample(
                phase,
                Vector2.zero,
                1,
                true,
                true,
                false,
                point);
    }
}
