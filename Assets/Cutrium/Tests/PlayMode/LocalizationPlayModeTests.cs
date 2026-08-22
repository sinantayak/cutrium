using Cutrium.Presentation.Localization;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cutrium.PlayModeTests
{
    public sealed class LocalizationPlayModeTests
    {
        [Test]
        public void Service_LocalizesExactDynamicAndRichGameplayCopy()
        {
            using var rig = new LocalizationRig();

            Assert.That(rig.Service.CurrentLanguage,
                Is.EqualTo(SupportedLanguage.English));
            Assert.That(rig.Service.Localize("PLAY"), Is.EqualTo("PLAY"));

            rig.Service.SetLanguage(SupportedLanguage.Turkish, false);

            Assert.That(rig.Service.Localize("PLAY"), Is.EqualTo("OYNA"));
            Assert.That(
                rig.Service.Localize("PLAY LEVEL 12"),
                Is.EqualTo("SEVİYE 12 OYNA"));
            Assert.That(
                rig.Service.Localize("TARGET 75%"),
                Is.EqualTo("HEDEF %75"));
            Assert.That(
                rig.Service.Localize("CUT: 2/7"),
                Is.EqualTo("KESİM: 2/7"));
            Assert.That(
                rig.Service.Localize("9 CUTS"),
                Is.EqualTo("9 KESİM"));

            string rich =
                "<color=#FFAA00>LEVEL 4 COMPLETE</color>\n" +
                "CAPTURED 82%  •  3 CUTS\n" +
                "TIME 12.5s  •  BROKEN 1";
            string translated = rig.Service.Localize(rich);
            Assert.That(translated,
                Does.Contain("<color=#FFAA00>SEVİYE 4 TAMAMLANDI</color>"));
            Assert.That(translated, Does.Contain("KAPLANAN %82"));
            Assert.That(translated, Does.Contain("3 KESİM"));
            Assert.That(translated, Does.Contain("SÜRE 12.5sn"));
            Assert.That(translated, Does.Contain("KIRILAN 1"));
        }

        [Test]
        public void Presenter_UpdatesStaticAndNewRuntimeTextImmediately()
        {
            using var rig = new LocalizationRig();
            Text legacy = rig.CreateLegacyLabel("Legacy", "HOME");
            TMP_Text tmp = rig.CreateTmpLabel("Tmp", "PLAY");
            LocalizationPresenter presenter = rig.Root.AddComponent<
                LocalizationPresenter>();
            presenter.ConfigureForSetup(
                rig.Service,
                new[] { legacy },
                new[] { tmp });

            rig.Service.SetLanguage(SupportedLanguage.Turkish, false);
            presenter.RefreshNow();
            Assert.That(legacy.text, Is.EqualTo("ANA SAYFA"));
            Assert.That(tmp.text, Is.EqualTo("OYNA"));

            tmp.text = "PLAY LEVEL 7";
            presenter.RefreshNow();
            Assert.That(tmp.text, Is.EqualTo("SEVİYE 7 OYNA"));

            rig.Service.SetLanguage(SupportedLanguage.English, false);
            presenter.RefreshNow();
            Assert.That(legacy.text, Is.EqualTo("HOME"));
            Assert.That(tmp.text, Is.EqualTo("PLAY LEVEL 7"));
        }

        [Test]
        public void Service_AcceptsEitherAuthoredLanguageAsTableSource()
        {
            using var rig = new LocalizationRig();

            Assert.That(
                rig.Service.Localize("Galata Kulesi"),
                Is.EqualTo("Galata Tower"));
            rig.Service.SetLanguage(SupportedLanguage.Turkish, false);
            Assert.That(
                rig.Service.Localize("Galata Tower"),
                Is.EqualTo("Galata Kulesi"));
        }

        [Test]
        public void Service_RestoresThePersistedLanguageChoice()
        {
            bool hadPreference = PlayerPrefs.HasKey(
                LocalizationService.LanguagePreferenceKey);
            int previousPreference = PlayerPrefs.GetInt(
                LocalizationService.LanguagePreferenceKey,
                (int)SupportedLanguage.English);
            PlayerPrefs.DeleteKey(LocalizationService.LanguagePreferenceKey);

            LocalizationTable table = ScriptableObject.CreateInstance<
                LocalizationTable>();
            table.ConfigureForSetup(new[]
            {
                new LocalizationEntry("PLAY", "OYNA"),
            });
            var firstObject = new GameObject("FirstLocalizationService");
            var secondObject = new GameObject("SecondLocalizationService");
            try
            {
                LocalizationService first = firstObject.AddComponent<
                    LocalizationService>();
                first.ConfigureForSetup(
                    table,
                    true,
                    SupportedLanguage.English);
                first.SetLanguage(SupportedLanguage.Turkish);

                LocalizationService second = secondObject.AddComponent<
                    LocalizationService>();
                second.ConfigureForSetup(
                    table,
                    true,
                    SupportedLanguage.English);
                Assert.That(second.CurrentLanguage,
                    Is.EqualTo(SupportedLanguage.Turkish));
            }
            finally
            {
                Object.DestroyImmediate(firstObject);
                Object.DestroyImmediate(secondObject);
                Object.DestroyImmediate(table);
                if (hadPreference)
                {
                    PlayerPrefs.SetInt(
                        LocalizationService.LanguagePreferenceKey,
                        previousPreference);
                }
                else
                {
                    PlayerPrefs.DeleteKey(
                        LocalizationService.LanguagePreferenceKey);
                }

                PlayerPrefs.Save();
            }
        }

        private sealed class LocalizationRig : System.IDisposable
        {
            public LocalizationRig()
            {
                Root = new GameObject("LocalizationTestRoot");
                Table = ScriptableObject.CreateInstance<LocalizationTable>();
                Table.ConfigureForSetup(new[]
                {
                    new LocalizationEntry("PLAY", "OYNA"),
                    new LocalizationEntry("HOME", "ANA SAYFA"),
                    new LocalizationEntry("Galata Tower", "Galata Kulesi"),
                });
                Service = Root.AddComponent<LocalizationService>();
                Service.ConfigureForSetup(
                    Table,
                    false,
                    SupportedLanguage.English);
            }

            public GameObject Root { get; }
            public LocalizationTable Table { get; }
            public LocalizationService Service { get; }

            public Text CreateLegacyLabel(string name, string value)
            {
                var child = new GameObject(name, typeof(RectTransform));
                child.transform.SetParent(Root.transform, false);
                Text label = child.AddComponent<Text>();
                label.text = value;
                return label;
            }

            public TMP_Text CreateTmpLabel(string name, string value)
            {
                var child = new GameObject(name, typeof(RectTransform));
                child.transform.SetParent(Root.transform, false);
                TMP_Text label = child.AddComponent<TextMeshProUGUI>();
                label.text = value;
                return label;
            }

            public void Dispose()
            {
                Object.DestroyImmediate(Root);
                Object.DestroyImmediate(Table);
            }
        }
    }
}
