using System;
using System.Linq;
using System.Reflection;
using Cutrium.Gameplay.Barriers;
using Cutrium.Gameplay.Geometry;
using Cutrium.Gameplay.Session;
using Cutrium.Presentation.Theme;
using NUnit.Framework;
using UnityEngine;

namespace Cutrium.Gameplay.EditModeTests
{
    public sealed class ThemeDefinitionTests
    {
        private Texture2D _textureA;
        private Texture2D _textureB;
        private Sprite _spriteA;
        private Sprite _spriteB;

        [SetUp]
        public void SetUp()
        {
            _textureA = new Texture2D(2, 2);
            _textureB = new Texture2D(4, 1);
            _spriteA = Sprite.Create(
                _textureA,
                new Rect(0f, 0f, 2f, 2f),
                new Vector2(0.5f, 0.5f));
            _spriteB = Sprite.Create(
                _textureB,
                new Rect(0f, 0f, 4f, 1f),
                new Vector2(0.5f, 0.5f));
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_spriteA);
            UnityEngine.Object.DestroyImmediate(_spriteB);
            UnityEngine.Object.DestroyImmediate(_textureA);
            UnityEngine.Object.DestroyImmediate(_textureB);
        }

        [Test]
        public void Resolver_UsesSelectedThenFallbackThenFlatDefault()
        {
            ThemeDefinition selected = CreateTheme(
                "selected",
                background: _spriteA,
                threat: null);
            ThemeDefinition fallback = CreateTheme(
                "fallback",
                background: _spriteB,
                threat: _spriteB);
            try
            {
                ResolvedTheme resolved = ThemeResolver.Resolve(
                    selected,
                    fallback);
                Assert.That(resolved.BackgroundSprite, Is.SameAs(_spriteA));
                Assert.That(resolved.Threat.Sprite, Is.SameAs(_spriteB));
                Assert.That(resolved.StableId, Is.EqualTo("selected"));

                ResolvedTheme flat = ThemeResolver.Resolve(null, null);
                Assert.That(flat.BackgroundSprite, Is.Null);
                Assert.That(flat.BackgroundColor,
                    Is.EqualTo(new Color(0.09f, 0.05f, 0.035f, 1f)));
                Assert.That(flat.Threat.Sprite, Is.Null);
                Assert.That(flat.Threat.Scale, Is.EqualTo(Vector2.one));
                Assert.That(flat.StableId, Is.EqualTo("component-flat"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(selected);
                UnityEngine.Object.DestroyImmediate(fallback);
            }
        }

        [Test]
        public void ResolveSandBowl_UsesSelectedThenFallbackThenNullDefault()
        {
            ThemeDefinition selected = CreateTheme("selected");
            ThemeDefinition fallback = CreateTheme("fallback");
            selected.ConfigureSandBowlForSetup(_spriteA, null, null);
            fallback.ConfigureSandBowlForSetup(_spriteB, _spriteB, _spriteB);
            try
            {
                SandBowlVisualStyle resolved =
                    ThemeResolver.ResolveSandBowl(selected, fallback);
                // Selected wins for the field it sets (sand texture); falls
                // through to fallback for the fields it leaves null (bowl
                // outline/mask).
                Assert.That(resolved.SandTexture, Is.SameAs(_spriteA));
                Assert.That(resolved.BowlOutlineSprite, Is.SameAs(_spriteB));
                Assert.That(resolved.BowlInteriorMaskSprite, Is.SameAs(_spriteB));

                SandBowlVisualStyle none =
                    ThemeResolver.ResolveSandBowl(null, null);
                Assert.That(none.SandTexture, Is.Null);
                Assert.That(none.BowlOutlineSprite, Is.Null);
                Assert.That(none.BowlInteriorMaskSprite, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(selected);
                UnityEngine.Object.DestroyImmediate(fallback);
            }
        }

        [Test]
        public void Resolver_PreservesConfiguredScaleOffsetAndColors()
        {
            Color threatColor = new Color(0.2f, 0.3f, 0.4f, 0.5f);
            ThemeDefinition theme = CreateTheme(
                "scaled",
                threat: _spriteA,
                scale: new Vector2(1.5f, 0.75f),
                offset: new Vector2(0.12f, -0.08f),
                threatColor: threatColor);
            try
            {
                ResolvedTheme resolved = ThemeResolver.Resolve(theme, null);
                Assert.That(resolved.Threat.Scale,
                    Is.EqualTo(new Vector2(1.5f, 0.75f)));
                Assert.That(resolved.Threat.Offset,
                    Is.EqualTo(new Vector2(0.12f, -0.08f)));
                Assert.That(resolved.Threat.Color, Is.EqualTo(threatColor));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(theme);
            }
        }

        [TestCase(0f, 1f)]
        [TestCase(1f, 0f)]
        [TestCase(-1f, 1f)]
        [TestCase(float.NaN, 1f)]
        public void Definition_RejectsInvalidVisualScale(float x, float y)
        {
            ThemeDefinition theme = ScriptableObject.CreateInstance<ThemeDefinition>();
            try
            {
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    Configure(theme, "invalid", null, null,
                        new Vector2(x, y), Vector2.zero, Color.white));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(theme);
            }
        }

        [Test]
        public void OptionalSpritesAndMaterialMayAllBeNull()
        {
            ThemeDefinition theme = CreateTheme("minimal");
            try
            {
                ResolvedTheme resolved = ThemeResolver.Resolve(theme, null);
                Assert.That(resolved.BackgroundSprite, Is.Null);
                Assert.That(resolved.BoardSprite, Is.Null);
                Assert.That(resolved.FrameSprite, Is.Null);
                Assert.That(resolved.Threat.Sprite, Is.Null);
                Assert.That(resolved.Threat.ShadowSprite, Is.Null);
                Assert.That(resolved.Threat.TrailSprite, Is.Null);
                Assert.That(resolved.Barrier.BodySprite, Is.Null);
                Assert.That(resolved.Barrier.CapSprite, Is.Null);
                Assert.That(resolved.Capture.Sprite, Is.Null);
                Assert.That(resolved.Capture.Material, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(theme);
            }
        }

        [Test]
        public void GameplayAssemblyHasNoArtOrUnityPresentationTypes()
        {
            Assembly gameplay = typeof(ThreatMotionSession).Assembly;
            Type[] forbidden =
            {
                typeof(Sprite),
                typeof(Material),
                typeof(GameObject),
                typeof(AudioClip),
                typeof(ParticleSystem),
                typeof(Transform),
            };
            foreach (Type type in gameplay.GetTypes())
            {
                Type[] exposed = type.GetFields(
                        BindingFlags.Public
                        | BindingFlags.NonPublic
                        | BindingFlags.Instance
                        | BindingFlags.Static)
                    .Select(field => field.FieldType)
                    .Concat(type.GetProperties().Select(property =>
                        property.PropertyType))
                    .ToArray();
                foreach (Type forbiddenType in forbidden)
                {
                    CollectionAssert.DoesNotContain(
                        exposed,
                        forbiddenType,
                        $"{type.FullName} exposes {forbiddenType.Name}.");
                }
            }
        }

        [Test]
        public void ThemeResolutionCannotChangeDeterministicReplay()
        {
            ThreatMotionSession themed = CreateSession();
            ThreatMotionSession flat = CreateSession();
            ThemeDefinition theme = CreateTheme(
                "visual-only",
                threat: _spriteB,
                scale: new Vector2(2f, 0.5f),
                offset: new Vector2(0.2f, -0.1f));
            try
            {
                for (int tick = 0; tick < 60; tick++)
                {
                    _ = ThemeResolver.Resolve(theme, null);
                    themed.Tick(1f / 60f);
                    flat.Tick(1f / 60f);
                }

                var intent = new BarrierIntent(
                    new LogicalPoint(5f, 4f),
                    BarrierOrientation.Horizontal);
                themed.TryStartBarrier(intent);
                flat.TryStartBarrier(intent);
                for (int tick = 0; tick < 120; tick++)
                {
                    _ = ThemeResolver.Resolve(theme, null);
                    themed.Tick(1f / 60f);
                    flat.Tick(1f / 60f);
                }

                Assert.That(themed.Threats, Is.EqualTo(flat.Threats));
                Assert.That(themed.CapturedFraction,
                    Is.EqualTo(flat.CapturedFraction));
                Assert.That(themed.ActiveBarrier,
                    Is.EqualTo(flat.ActiveBarrier));
                Assert.That(themed.LastBarrierEvent,
                    Is.EqualTo(flat.LastBarrierEvent));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(theme);
            }
        }

        private static ThemeDefinition CreateTheme(
            string id,
            Sprite background = null,
            Sprite threat = null,
            Vector2? scale = null,
            Vector2? offset = null,
            Color? threatColor = null)
        {
            ThemeDefinition theme = ScriptableObject.CreateInstance<ThemeDefinition>();
            Configure(
                theme,
                id,
                background,
                threat,
                scale ?? Vector2.one,
                offset ?? Vector2.zero,
                threatColor ?? Color.white);
            return theme;
        }

        private static void Configure(
            ThemeDefinition theme,
            string id,
            Sprite background,
            Sprite threat,
            Vector2 scale,
            Vector2 offset,
            Color threatColor)
        {
            theme.ConfigureForSetup(
                id,
                background,
                Color.black,
                null,
                Color.gray,
                null,
                Color.cyan,
                threat,
                threatColor,
                scale,
                offset,
                null,
                Color.black,
                null,
                Color.clear,
                null,
                null,
                null,
                Color.cyan,
                Color.cyan,
                Color.green,
                Color.red,
                null,
                null,
                Color.green,
                Color.black,
                Color.cyan,
                Color.white);
        }

        private static ThreatMotionSession CreateSession()
        {
            var tolerance = new GeometryTolerancePolicy(
                0.0001f,
                0.00001f,
                0.0001f,
                0.001f);
            var motion = new ThreatMotionConfiguration(
                new LogicalRect(0f, 0f, 10f, 16f),
                new LogicalPoint(5f, 8f),
                new LogicalVector(0.8f, 0.6f),
                1.6f,
                0.35f,
                8);
            return new ThreatMotionSession(
                motion,
                new BarrierConfiguration(3f, 0.08f, 0f, 16),
                new CaptureLevelConfiguration(1f),
                tolerance);
        }
    }
}
