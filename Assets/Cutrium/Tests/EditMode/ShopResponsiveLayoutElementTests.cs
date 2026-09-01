using Cutrium.Presentation.Shop;
using NUnit.Framework;
using UnityEngine;

namespace Cutrium.Gameplay.EditModeTests
{
    public sealed class ShopResponsiveLayoutElementTests
    {
        private GameObject _root;
        private ShopResponsiveLayoutElement _layout;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject(
                "ShopResponsiveLayoutTest",
                typeof(RectTransform));
            _layout = _root.AddComponent<ShopResponsiveLayoutElement>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        [Test]
        public void SingleCard_PreservesSourceTextureAspect()
        {
            const float sourceAspect = 512f / 102f;
            _layout.ConfigureForSetup(sourceAspect);

            float height = _layout.CalculatePreferredHeight(1016f);

            Assert.That(1016f / height, Is.EqualTo(sourceAspect).Within(0.001f));
            Assert.That(_layout.ColumnCount, Is.EqualTo(1));
        }

        [Test]
        public void ThreeColumnRow_AccountsForGapsAndKeepsSquareItems()
        {
            _layout.ConfigureForSetup(1f, 3, 16f);

            float height = _layout.CalculatePreferredHeight(1016f);

            Assert.That(height, Is.EqualTo(328f).Within(0.001f));
            Assert.That(_layout.ColumnCount, Is.EqualTo(3));
            Assert.That(_layout.ColumnSpacing, Is.EqualTo(16f));
        }

        [Test]
        public void PaddedThreeColumnRow_KeepsSquareChildArea()
        {
            _layout.ConfigureForSetup(1f, 3, 16f, 0f, 16f);

            float height = _layout.CalculatePreferredHeight(1016f);

            Assert.That(height, Is.EqualTo(344f).Within(0.001f));
            Assert.That(height - _layout.VerticalPadding, Is.EqualTo(328f));
        }

        [Test]
        public void InsetCard_UsesVisualWidthForItsAspect()
        {
            const float sourceAspect = 512f / 178f;
            _layout.ConfigureForSetup(sourceAspect, 1, 0f, 36f);

            float height = _layout.CalculatePreferredHeight(1016f);

            Assert.That(
                height,
                Is.EqualTo((1016f - 36f) / sourceAspect).Within(0.001f));
            Assert.That(_layout.HorizontalPadding, Is.EqualTo(36f));
        }

        [Test]
        public void FramedCard_AddsVerticalPaddingAroundVisualAspect()
        {
            const float sourceAspect = 512f / 102f;
            _layout.ConfigureForSetup(sourceAspect, 1, 0f, 16f, 16f);

            float height = _layout.CalculatePreferredHeight(1016f);

            Assert.That(
                height,
                Is.EqualTo((1016f - 16f) / sourceAspect + 16f)
                    .Within(0.001f));
            Assert.That(_layout.VerticalPadding, Is.EqualTo(16f));
        }

        [Test]
        public void InvalidSetupValues_AreClampedToSafeLayoutValues()
        {
            _layout.ConfigureForSetup(0f, 0, -20f, -12f, -8f);

            Assert.That(_layout.ItemAspectRatio, Is.GreaterThan(0f));
            Assert.That(_layout.ColumnCount, Is.EqualTo(1));
            Assert.That(_layout.ColumnSpacing, Is.Zero);
            Assert.That(_layout.HorizontalPadding, Is.Zero);
            Assert.That(_layout.VerticalPadding, Is.Zero);
            Assert.That(
                _layout.CalculatePreferredHeight(0f),
                Is.Zero);
        }
    }
}
