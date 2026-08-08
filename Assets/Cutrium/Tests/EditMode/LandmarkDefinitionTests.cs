using System;
using Cutrium.Presentation.Landmark;
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
