using System;
using System.Linq;
using System.Reflection;
using Cutrium.Gameplay.Geometry;
using NUnit.Framework;

namespace Cutrium.Gameplay.EditModeTests
{
    public sealed class GameplayAssemblyBoundaryTests
    {
        [Test]
        public void GameplayAssembly_DoesNotReferenceUnityEngine()
        {
            var gameplayAssembly = typeof(LogicalPoint).Assembly;
            var engineReferences = gameplayAssembly
                .GetReferencedAssemblies()
                .Where(reference => reference.Name.StartsWith("UnityEngine", StringComparison.Ordinal))
                .Select(reference => reference.FullName)
                .ToArray();

            Assert.That(gameplayAssembly.GetName().Name, Is.EqualTo("Cutrium.Gameplay"));
            Assert.That(engineReferences, Is.Empty);
        }

        [TestCase(typeof(LogicalPoint))]
        [TestCase(typeof(LogicalVector))]
        [TestCase(typeof(LogicalRect))]
        [TestCase(typeof(GeometryTolerancePolicy))]
        public void GeometryState_IsImmutableAndFloatBacked(Type geometryType)
        {
            var fields = geometryType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            Assert.That(geometryType.IsValueType, Is.True);
            Assert.That(fields, Is.Not.Empty);
            Assert.That(fields.All(field => field.FieldType == typeof(float)), Is.True);
            Assert.That(fields.All(field => field.IsInitOnly), Is.True);
        }
    }
}
