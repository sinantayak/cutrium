# English/Turkish UI Localization and Shared Settings Entry Points

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This plan follows `.agent/PLANS.md` and the repository instructions in `AGENTS.md`.

## Purpose / Big Picture

After this change, every player-facing UI label in the vertical-slice scene can be rendered in English or Turkish. The language button inside the existing Settings popup switches the language immediately and remembers the choice between sessions. The Home page gains a top-right settings gear, the existing gameplay gear becomes slightly larger, and both identical-sized gears open the same Settings popup.

The result can be observed by entering Play Mode, opening Settings from either Home or gameplay, and pressing the language button. Static navigation labels and runtime-authored gameplay messages change without restarting the scene. The logical gameplay systems remain independent of translated strings and presentation assets.

## Progress

- [x] (2026-08-22) Read the relevant product, technical, planning, UI setup, Settings presenter, and runtime text-writing code.
- [x] (2026-08-22) Inventory the scene's TMP/legacy UI labels and the presenters that overwrite labels at runtime.
- [x] (2026-08-22) Confirm that the Unity Localization package is not installed and that adding a production dependency requires explicit approval.
- [x] (2026-08-22) Add the repository-native localization table, service, and centralized label presenter.
- [x] (2026-08-22) Populate English/Turkish UI translations and dynamic format handling.
- [x] (2026-08-22) Connect the Settings language button and persist the selected language.
- [x] (2026-08-22) Add the Home settings gear, resize the gameplay gear, and connect both to the same popup.
- [x] (2026-08-22) Extend the idempotent Editor setup so it creates/updates all required serialized references without hand-editing Unity YAML.
- [x] (2026-08-22) Add Play Mode coverage for translations, dynamic labels, language switching, persisted state boundaries, and shared Settings entry points.
- [ ] Run available automated validation and document manual Unity checks for tall phone, common phone, and 4:3 tablet views.
- [x] (2026-08-22) Record the localization architecture decision as ADR-042 in `Docs/DECISIONS.md`.
- [x] (2026-08-23) Add explicit English/Turkish title, description, and sector content for all 24 active Earth landmarks and bind the completion presenter to the selected language.
- [ ] (2026-08-23) Regenerate the 24 landmark assets and scene localization reference with the idempotent Unity setup command.
- [ ] Complete this plan's retrospective after the scene setup and visual checks run.

## Surprises & Discoveries

- Observation: The project does not currently depend on `com.unity.localization`.
  Evidence: `Packages/manifest.json` contains no Unity Localization package entry.

- Observation: UI text is split between authored scene labels and labels overwritten at runtime by several presenters.
  Evidence: `CaptureHudPresenter`, `PreLevelIntroPresenter`, `FeedbackPresenter`, `GameplayIdentityHudPresenter`, `SandBowlPresenter`, and `FrontEndPresenter` all assign text directly.

- Observation: The existing Settings presenter owns only the gameplay open button, while the popup itself is already reusable.
  Evidence: `SettingsPanelPresenter` serializes one `_openButton`; `SettingsPanelSceneSetup` resolves the gameplay HUD button and builds one popup.

- Observation: The active Earth catalog contains 24 landmark assets whose title, description, and sector were authored only in Turkish; the three exact landmark pairs in the UI table cover legacy/demo assets, not the active catalog.
  Evidence: The `Earth/Chapter01` and `Earth/Chapter02` assets are created from `FirstTwelveLandmarkContent` and `ChapterTwoLandmarkContent`, and their serialized records had only `_displayTitle`, `_shortDescription`, and `_sector` fields.

- Observation: The current LapsusPro TMP atlas does not contain the Turkish Latin-extension glyphs required by the new copy.
  Evidence: The serialized font character table contains none of Unicode 199, 214, 220, 231, 246, 252, 286, 287, 304, 305, 350, or 351. The localization setup now links the source OTF, enables dynamic multi-atlas population, and preloads those characters.

- Observation: The open main Unity instance compiled the first implementation pass successfully, but remained in Play Mode; an isolated validation-project attempt then failed before import because the headless license channel repeatedly timed out.
  Evidence: `Cutrium.Presentation.dll`, `Cutrium.Editor.dll`, and `Cutrium.PlayModeTests.dll` were regenerated at 23:45 with Tundra build success and no C# errors. The isolated `setup.log` repeatedly reported licensing channel timeouts and never reached the setup method, so that temporary process and directory were removed.

## Decision Log

- Decision: Use a small repository-native `ScriptableObject` localization table plus serialized runtime service instead of adding the Unity Localization package.
  Rationale: It satisfies the current two-language UI scope without an unapproved third-party/production dependency and keeps the implementation replaceable.
  Date/Author: 2026-08-22 / Codex

- Decision: Keep English as the default language and persist explicit language changes under a namespaced PlayerPrefs key.
  Rationale: This preserves current behavior and existing authored English labels while making Settings changes survive restarts.
  Date/Author: 2026-08-22 / Codex

- Decision: Use one centralized `LocalizationPresenter` with explicit serialized bindings to scene labels.
  Rationale: The scene contains both static labels and text rewritten by existing presenters. A late presentation pass can translate changed values without coupling gameplay/domain presenters to localization, while one bounded loop avoids dozens of per-label update components.
  Date/Author: 2026-08-22 / Codex

- Decision: Use exact source translations for authored labels and deterministic pattern translation for number-bearing runtime strings.
  Rationale: Numeric labels such as `LEVEL 4`, `TARGET 75%`, `CUT: 1/3`, and rich completion summaries should preserve live values without hard-coded per-level variants.
  Date/Author: 2026-08-22 / Codex

- Decision: Make both settings gears 48 by 48 UI units and bind both to the existing popup presenter.
  Rationale: This is a modest increase from the current 34-unit gameplay icon and gives Home/gameplay consistent touch presentation.
  Date/Author: 2026-08-22 / Codex

- Decision: Reconfigure the existing TMP font asset for dynamic multi-atlas population and preload required Turkish characters from the repository's source OTF.
  Rationale: A correct translation table is not sufficient if the active TMP atlas cannot render Turkish. Reusing the existing typeface preserves the art direction and avoids introducing another font dependency.
  Date/Author: 2026-08-22 / Codex

- Decision: Store active landmark English and Turkish copy directly in each `LandmarkDefinition`, selected by `LandmarkRevealPresenter` through the shared localization service.
  Rationale: Descriptions are long content records and exact-string lookup would duplicate fragile punctuation-sensitive source text in the general UI table. Explicit localized content keeps artwork/progression language-neutral, gives missing Turkish data a safe English fallback, and refreshes an open completion card immediately when the language changes.
  Date/Author: 2026-08-23 / Codex

## Outcomes & Retrospective

Runtime, setup tooling, translations, shared Settings entry points, tests, and
ADR-042 are implemented. Production-scene serialization and automated/manual
Unity validation remain pending because the open main Editor is in Play Mode
and the separate headless validation instance could not acquire a license.

## Context and Orientation

The runtime Settings behavior is in `Assets/Cutrium/Runtime/Presentation/Settings/SettingsPanelPresenter.cs`. The idempotent scene construction utility is `Assets/Cutrium/Editor/Setup/SettingsPanelSceneSetup.cs`. The Home, Shop, and Challenge pages are managed by `Assets/Cutrium/Runtime/Presentation/FrontEnd/FrontEndPresenter.cs`, and the Home page hierarchy is authored by `Assets/Cutrium/Editor/Setup/FrontEndSceneSetup.cs`.

The gameplay settings gear is created/configured by `Assets/Cutrium/Editor/Setup/LandmarkRevealPresentationSetup.cs` under `Canvas/SafeAreaRoot/TopHUD/GameplayHudRow/SettingsSlot/SettingsButton`. The Home page is already inside the safe-area-aware frontend hierarchy, so its new gear can use a top-right anchored slot without changing gameplay geometry.

Runtime text is displayed by both `UnityEngine.UI.Text` and `TMPro.TMP_Text`. Some labels are static; others are assigned every frame or on state changes. Localization therefore belongs in the presentation assembly and must not change level data, capture rules, collision geometry, or gameplay state.

The localization content asset will live at `Assets/Cutrium/Content/Localization/MainLocalizationTable.asset`. New runtime types will live under `Assets/Cutrium/Runtime/Presentation/Localization/`. Editor setup code will create/update the asset and serialized scene references; `.unity`, `.prefab`, and `.asset` YAML will not be edited manually.

## Plan of Work

First, add a `SupportedLanguage` enum, serializable localization entries, a `LocalizationTable` ScriptableObject, and a `LocalizationService`. The service will expose the active language, emit a change event, translate exact source strings, translate known dynamic formats, and optionally persist the selection. English returns the authored source text unchanged.

Second, add a centralized `LocalizationPresenter`. The Editor setup records every player-facing TMP and legacy Text label as an explicit serialized binding with its authored source. The presenter renders all bindings when language changes and detects values subsequently replaced by existing runtime presenters, treating those replacements as new English source strings before translating them. This keeps runtime object searches out of the dependency strategy and avoids modifying gameplay logic to know about locale state.

Third, extend `SettingsPanelPresenter` to receive the localization service and a second Home open button. Both open buttons call the same popup-opening method. The language action toggles English/Turkish and updates its own visible language label through the same localization binding. Audio, haptic, close, home, and exit behavior remain unchanged.

Fourth, extend `SettingsPanelSceneSetup` to create/reuse the localization asset and services, create a top-right Home settings gear using the same visual sprite as gameplay, size both gear slots to 48 by 48 UI units, and serialize all label and button references. Running `Cutrium/Setup/Apply Settings Panel` remains the single idempotent setup operation for this feature.

Fifth, add automated tests for exact translations, dynamic formats, presenter refresh behavior, Settings language toggling, complete scene label binding, and the two equal-sized buttons opening the same popup. Existing tests that assert English copy will explicitly select English without persisting it so a developer's local language preference cannot make tests nondeterministic.

Finally, run compilation/tests available from the command line or Unity Editor, inspect relevant Console output, and manually verify the Home/gameplay gears and popup at tall-phone, common-phone, and 4:3-tablet aspect ratios.

## Concrete Steps

1. From `S:\Tayacknity\Cutrium`, add the localization runtime files under `Assets/Cutrium/Runtime/Presentation/Localization/` with their Unity `.meta` files.
2. Extend `SettingsPanelPresenter` with the second open button and `LocalizationService` reference.
3. Add/update Editor setup code under `Assets/Cutrium/Editor/Setup/` and update the gear-size constant in `LandmarkRevealPresentationSetup`.
4. Run the idempotent Unity menu command `Cutrium/Setup/Apply Settings Panel` to create/update the content asset and scene serialization.
5. Add Play Mode tests under `Assets/Cutrium/Tests/PlayMode/` and, if useful for pure table behavior, Edit Mode tests under the existing test assembly.
6. Run relevant Unity tests and inspect the scene at the required representative aspect ratios.

## Validation and Acceptance

Acceptance requires all of the following:

- The app begins in English when no preference exists.
- Pressing the Settings language control changes all bound UI labels to Turkish immediately; pressing it again restores English.
- Dynamic labels retain their live numbers and rich-text markup while translating their words.
- The chosen language is restored after recreating the service/session boundary.
- The Home and gameplay settings gears are both 48 by 48 UI units, use the same visual treatment, and open the same Settings panel.
- The Settings popup can still be closed and its existing audio/music/haptic, Home, and Exit actions retain their prior behavior.
- Every scene TMP/legacy Text label intended for players has an explicit localization binding; debug-only labels may be excluded and documented.
- No gameplay logic depends on translated text.
- Relevant Edit Mode and Play Mode tests pass, with no new Console errors or relevant warnings.
- Layout is manually checked in a tall phone, common phone, and 4:3 tablet Game view.

## Idempotence and Recovery

The setup command must find or create named objects, reuse the localization table asset, replace table entries deterministically, and refresh serialized label bindings. Re-running it must not duplicate services, Home buttons, popup controls, or assets. If setup is interrupted, run `Cutrium/Setup/Apply Settings Panel` again after fixing any reported missing asset. Existing GUIDs and `.meta` files must be preserved.

Language persistence uses one namespaced PlayerPrefs key. Automated tests that exercise persistence will restore/delete their test value so they do not alter the developer's chosen language.

## Artifacts and Notes

Expected primary artifacts:

- `Assets/Cutrium/Runtime/Presentation/Localization/SupportedLanguage.cs`
- `Assets/Cutrium/Runtime/Presentation/Localization/LocalizationTable.cs`
- `Assets/Cutrium/Runtime/Presentation/Localization/LocalizationService.cs`
- `Assets/Cutrium/Runtime/Presentation/Localization/LocalizationPresenter.cs`
- `Assets/Cutrium/Editor/Setup/LocalizationSceneSetup.cs`
- `Assets/Cutrium/Content/Localization/MainLocalizationTable.asset`
- Updated `SettingsPanelPresenter`, Settings setup, gear setup, tests, scene, and decision log.

## Interfaces and Dependencies

The runtime API should settle around these interfaces:

    public enum SupportedLanguage
    {
        English,
        Turkish
    }

    public sealed class LocalizationService : MonoBehaviour
    {
        public SupportedLanguage CurrentLanguage { get; }
        public event Action<SupportedLanguage> LanguageChanged;
        public string Localize(string source);
        public void SetLanguage(SupportedLanguage language, bool savePreference = true);
        public void ToggleLanguage();
    }

    public sealed class LocalizationPresenter : MonoBehaviour
    {
        public int LabelCount { get; }
        public void RefreshNow();
    }

`SettingsPanelPresenter.ConfigureForSetup(...)` will accept the Home open button and localization service in addition to its existing references. There are no new package dependencies.

Revision note (2026-08-22): Initial plan created after repository and scene-text inventory. It records the dependency-free localization architecture, dynamic text strategy, and shared Settings entry-point acceptance criteria before production edits.

Revision note (2026-08-23): Extended the localization scope to all 24 active Earth landmark titles, descriptions, and sectors. Landmark content now owns its bilingual records while the shared service remains the language authority.
