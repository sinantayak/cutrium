using System;
using Cutrium.Presentation.Landmark;
using Cutrium.Presentation.Localization;
using NUnit.Framework;
using UnityEngine;

namespace Cutrium.Gameplay.EditModeTests
{
    public sealed class LandmarkDefinitionTests
    {
        private LandmarkDefinition _landmark;
        private Texture2D _texture;
        private Sprite _artwork;

        [SetUp]
        public void SetUp()
        {
            _landmark = ScriptableObject.CreateInstance<LandmarkDefinition>();
            _texture = new Texture2D(2, 2);
            _artwork = Sprite.Create(
                _texture,
                new Rect(0f, 0f, 2f, 2f),
                new Vector2(0.5f, 0.5f));
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_landmark);
            UnityEngine.Object.DestroyImmediate(_artwork);
            UnityEngine.Object.DestroyImmediate(_texture);
        }

        [Test]
        public void ConfigureForSetup_StoresAllFieldsAndAllowsNullArtwork()
        {
            _landmark.ConfigureForSetup(
                "alpine-overlook",
                "Alpine Overlook",
                "A quiet ridge above the clouds.",
                "Europe",
                _artwork);

            Assert.That(_landmark.LandmarkId, Is.EqualTo("alpine-overlook"));
            Assert.That(_landmark.DisplayTitle, Is.EqualTo("Alpine Overlook"));
            Assert.That(_landmark.ShortDescription,
                Is.EqualTo("A quiet ridge above the clouds."));
            Assert.That(_landmark.Sector, Is.EqualTo("Europe"));
            Assert.That(_landmark.Artwork, Is.SameAs(_artwork));

            _landmark.ConfigureForSetup(
                "no-art",
                "No Art Yet",
                null,
                null,
                null);

            Assert.That(_landmark.Artwork, Is.Null);
            Assert.That(_landmark.ShortDescription, Is.Empty);
            Assert.That(_landmark.Sector, Is.Empty);
        }

        [Test]
        public void ConfigureForSetup_RejectsMissingIdOrTitle()
        {
            Assert.Throws<ArgumentException>(() =>
                _landmark.ConfigureForSetup(
                    " ",
                    "Title",
                    string.Empty,
                    string.Empty,
                    null));
            Assert.Throws<ArgumentException>(() =>
                _landmark.ConfigureForSetup(
                    "id",
                    " ",
                    string.Empty,
                    string.Empty,
                    null));
        }

        [Test]
        public void ConfigureLocalizedForSetup_ReturnsCopyForSelectedLanguage()
        {
            _landmark.ConfigureLocalizedForSetup(
                "galata-tower",
                "Galata Tower",
                "An English description.",
                "Istanbul / TURKEY",
                "Galata Kulesi",
                "Türkçe bir açıklama.",
                "İstanbul / TÜRKİYE",
                _artwork);

            Assert.That(
                _landmark.GetDisplayTitle(SupportedLanguage.English),
                Is.EqualTo("Galata Tower"));
            Assert.That(
                _landmark.GetShortDescription(SupportedLanguage.English),
                Is.EqualTo("An English description."));
            Assert.That(
                _landmark.GetSector(SupportedLanguage.English),
                Is.EqualTo("Istanbul / TURKEY"));
            Assert.That(
                _landmark.GetDisplayTitle(SupportedLanguage.Turkish),
                Is.EqualTo("Galata Kulesi"));
            Assert.That(
                _landmark.GetShortDescription(SupportedLanguage.Turkish),
                Is.EqualTo("Türkçe bir açıklama."));
            Assert.That(
                _landmark.GetSector(SupportedLanguage.Turkish),
                Is.EqualTo("İstanbul / TÜRKİYE"));
        }

        [Test]
        public void LegacyConfiguration_FallsBackToEnglishForTurkish()
        {
            _landmark.ConfigureForSetup(
                "legacy",
                "Legacy Title",
                "Legacy description.",
                "Legacy sector",
                null);

            Assert.That(
                _landmark.GetDisplayTitle(SupportedLanguage.Turkish),
                Is.EqualTo("Legacy Title"));
            Assert.That(
                _landmark.GetShortDescription(SupportedLanguage.Turkish),
                Is.EqualTo("Legacy description."));
            Assert.That(
                _landmark.GetSector(SupportedLanguage.Turkish),
                Is.EqualTo("Legacy sector"));
        }

        [Test]
        public void Defaults_AreInertBeforeConfiguration()
        {
            var fresh = ScriptableObject.CreateInstance<LandmarkDefinition>();
            try
            {
                Assert.That(fresh.LandmarkId, Is.Not.Null.And.Not.Empty);
                Assert.That(fresh.DisplayTitle, Is.Not.Null.And.Not.Empty);
                Assert.That(fresh.ShortDescription, Is.Not.Null);
                Assert.That(fresh.Sector, Is.Not.Null);
                Assert.That(fresh.Artwork, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(fresh);
            }
        }
    }
}
