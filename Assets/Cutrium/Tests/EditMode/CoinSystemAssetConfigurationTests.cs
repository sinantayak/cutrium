using Cutrium.Presentation.Feedback;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Cutrium.Gameplay.EditModeTests
{
    public sealed class CoinSystemAssetConfigurationTests
    {
        private const string GuiFolder = "Assets/Cutrium/Content/Gui/";
        private const string SoundsFolder = "Assets/Cutrium/Content/Sounds/";

        [Test]
        public void RoadmapCoinVisualsAndSounds_AreImportable()
        {
            Assert.That(
                AssetDatabase.LoadAssetAtPath<Sprite>(
                    GuiFolder + "CoinStackL1.png"),
                Is.Not.Null);
            Assert.That(
                AssetDatabase.LoadAssetAtPath<Sprite>(
                    GuiFolder + "Coin_HUD.png"),
                Is.Not.Null);
            Assert.That(
                AssetDatabase.LoadAssetAtPath<AudioClip>(
                    SoundsFolder + "SFX_CoinEarn.wav"),
                Is.Not.Null);
            Assert.That(
                AssetDatabase.LoadAssetAtPath<AudioClip>(
                    SoundsFolder + "SFX_CoinSpend.wav"),
                Is.Not.Null);
        }

        [Test]
        public void FeedbackAudioPresenter_ExposesCoinCuesExplicitly()
        {
            AudioClip earn = AssetDatabase.LoadAssetAtPath<AudioClip>(
                SoundsFolder + "SFX_CoinEarn.wav");
            AudioClip spend = AssetDatabase.LoadAssetAtPath<AudioClip>(
                SoundsFolder + "SFX_CoinSpend.wav");
            var root = new GameObject(
                "CoinAudioConfigurationTest",
                typeof(AudioSource),
                typeof(FeedbackAudioPresenter));

            try
            {
                FeedbackAudioPresenter presenter =
                    root.GetComponent<FeedbackAudioPresenter>();
                presenter.ConfigureClips(new FeedbackAudioClipSet
                {
                    CoinEarnClip = earn,
                    CoinSpendClip = spend,
                });

                Assert.That(presenter.CoinEarnClip, Is.SameAs(earn));
                Assert.That(presenter.CoinSpendClip, Is.SameAs(spend));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
