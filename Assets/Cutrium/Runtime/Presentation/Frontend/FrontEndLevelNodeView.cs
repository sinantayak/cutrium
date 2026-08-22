using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cutrium.Presentation.Frontend
{
    public enum FrontEndLevelNodeState
    {
        Upcoming,
        Traversed,
        Selected,
    }

    [DisallowMultipleComponent]
    public sealed class FrontEndLevelNodeView : MonoBehaviour
    {
        [SerializeField] private int _levelNumber = 1;
        [SerializeField] private Button _button;
        [SerializeField] private Image _nodeImage;
        [SerializeField] private Image _selectionGlow;
        [SerializeField] private TMP_Text _numberLabel;
        [SerializeField] private Image _lockImage;

        private bool _subscribed;

        public event Action<FrontEndLevelNodeView> Clicked;

        public int LevelNumber => _levelNumber;
        public Button Button => _button;
        public Image NodeImage => _nodeImage;
        public Image SelectionGlow => _selectionGlow;
        public TMP_Text NumberLabel => _numberLabel;
        public Image LockImage => _lockImage;
        public FrontEndLevelNodeState State { get; private set; }

        public void ConfigureForSetup(
            int levelNumber,
            Button button,
            Image nodeImage,
            Image selectionGlow,
            TMP_Text numberLabel,
            Image lockImage = null)
        {
            if (levelNumber <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(levelNumber));
            }

            Unsubscribe();
            _levelNumber = levelNumber;
            _button = button;
            _nodeImage = nodeImage;
            _selectionGlow = selectionGlow;
            _numberLabel = numberLabel;
            _lockImage = lockImage;
            if (isActiveAndEnabled && Application.isPlaying)
            {
                Subscribe();
            }
        }

        public void ApplyState(
            FrontEndLevelNodeState state,
            Color upcomingColor,
            Color traversedColor,
            Color selectedColor,
            Color numberColor)
        {
            State = state;
            Color nodeColor = state switch
            {
                FrontEndLevelNodeState.Traversed => traversedColor,
                FrontEndLevelNodeState.Selected => selectedColor,
                _ => upcomingColor,
            };
            nodeColor.a = 1f;

            if (_nodeImage != null)
            {
                _nodeImage.color = nodeColor;
            }

            if (_selectionGlow != null)
            {
                _selectionGlow.gameObject.SetActive(
                    state == FrontEndLevelNodeState.Selected);
            }

            bool locked = state == FrontEndLevelNodeState.Upcoming;
            if (_numberLabel != null)
            {
                _numberLabel.text = _levelNumber.ToString();
                _numberLabel.color = numberColor;
                _numberLabel.gameObject.SetActive(!locked);
            }

            if (_lockImage != null)
            {
                _lockImage.gameObject.SetActive(locked);
            }

            transform.localScale = state == FrontEndLevelNodeState.Selected
                ? Vector3.one * 1.08f
                : Vector3.one;
            if (_button != null)
            {
                _button.interactable = true;
            }
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                Subscribe();
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_subscribed || _button == null)
            {
                return;
            }

            _button.onClick.AddListener(OnClicked);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            if (_button != null)
            {
                _button.onClick.RemoveListener(OnClicked);
            }

            _subscribed = false;
        }

        private void OnClicked()
        {
            Clicked?.Invoke(this);
        }
    }
}
