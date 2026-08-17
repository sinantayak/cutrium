using System.Collections.Generic;
using Cutrium.Gameplay.Session;
using Cutrium.Unity.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Cutrium.Presentation.HUD
{
    /// Renders the level's burn limit (see ThreatMotionSession.HasBurnLimit/
    /// BarrierBreaksRemaining) as a row of heart icons in the TopHUD Health
    /// region -- no number, just hearts side by side that dim in place as
    /// the player breaks barriers against threats. The row is rebuilt
    /// whenever a level's own life count differs from the previous one, so
    /// each level can size its own burn budget independently.
    [DisallowMultipleComponent]
    public sealed class HealthHudPresenter : MonoBehaviour
    {
        [SerializeField] private FirstPlayableController _controller;
        [SerializeField] private RectTransform _heartRow;
        [SerializeField] private Sprite _heartSprite;
        [SerializeField] private float _heartSize = 40f;
        [SerializeField] private float _burntAlpha = 0.28f;

        private readonly List<Image> _hearts = new List<Image>();
        private ThreatMotionSession _observedSession;
        private int _observedMaxLives = -1;

        public IReadOnlyList<Image> Hearts => _hearts;

        public void Configure(
            FirstPlayableController controller,
            RectTransform heartRow,
            Sprite heartSprite)
        {
            ConfigureForSetup(controller, heartRow, heartSprite);
        }

        public void ConfigureForSetup(
            FirstPlayableController controller,
            RectTransform heartRow,
            Sprite heartSprite)
        {
            _controller = controller;
            _heartRow = heartRow;
            _heartSprite = heartSprite;
            _observedSession = null;
            _observedMaxLives = -1;
            RefreshNow();
        }

        public void RefreshNow()
        {
            if (_controller == null
                || _controller.Session == null
                || _heartRow == null)
            {
                return;
            }

            ThreatMotionSession session = _controller.Session;
            int maxLives = session.HasBurnLimit
                ? session.MaximumAcceptedBarrierBreaks
                : 0;
            bool sessionChanged = !ReferenceEquals(_observedSession, session);
            if (sessionChanged || maxLives != _observedMaxLives)
            {
                _observedSession = session;
                _observedMaxLives = maxLives;
                RebuildHearts(maxLives);
            }

            int alive = session.HasBurnLimit
                ? session.BarrierBreaksRemaining
                : 0;
            for (int index = 0; index < _hearts.Count; index++)
            {
                Image heart = _hearts[index];
                Color color = heart.color;
                color.a = index < alive ? 1f : _burntAlpha;
                heart.color = color;
            }
        }

        private void LateUpdate()
        {
            RefreshNow();
        }

        private void RebuildHearts(int count)
        {
            for (int index = _hearts.Count - 1; index >= 0; index--)
            {
                Destroy(_hearts[index].gameObject);
            }

            _hearts.Clear();
            for (int index = 0; index < count; index++)
            {
                var heartObject = new GameObject(
                    $"Heart_{index}",
                    typeof(RectTransform),
                    typeof(Image));
                heartObject.transform.SetParent(_heartRow, false);
                var rect = (RectTransform)heartObject.transform;
                rect.sizeDelta = new Vector2(_heartSize, _heartSize);
                Image image = heartObject.GetComponent<Image>();
                image.sprite = _heartSprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                image.raycastTarget = false;
                image.color = Color.white;
                _hearts.Add(image);
            }
        }
    }
}
