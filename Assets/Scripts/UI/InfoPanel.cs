using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Panel informasi objek belajar: gambar, nama ID, nama Dawan + badge, penjelasan.</summary>
public class InfoPanel : MonoBehaviour
{
    private GameObject root;
    private Image objectImage;
    private TextMeshProUGUI titleLabel;
    private TextMeshProUGUI categoryLabel;
    private TextMeshProUGUI indonesiaLabel;
    private TextMeshProUGUI dawanLabel;
    private TextMeshProUGUI badgeLabel;
    private Image badgeBackground;
    private TextMeshProUGUI explanationLabel;
    private Button audioButton;
    private Button quizButton;

    private LearningObjectDefinition current;
    private Action onClose;

    public void Build(GameObject panelRoot, Image image, TextMeshProUGUI title, TextMeshProUGUI category,
        TextMeshProUGUI indonesia, TextMeshProUGUI dawan, TextMeshProUGUI badge, Image badgeBg,
        TextMeshProUGUI explanation, Button audio, Button quiz)
    {
        root = panelRoot;
        objectImage = image;
        titleLabel = title;
        categoryLabel = category;
        indonesiaLabel = indonesia;
        dawanLabel = dawan;
        badgeLabel = badge;
        badgeBackground = badgeBg;
        explanationLabel = explanation;
        audioButton = audio;
        quizButton = quiz;

        audioButton.onClick.AddListener(PlayPronunciation);
        quizButton.onClick.AddListener(OpenQuiz);
        root.SetActive(false);
    }

    public void Show(LearningObjectDefinition definition, Action closed = null)
    {
        if (definition == null || root == null)
        {
            return;
        }

        current = definition;
        onClose = closed;

        // Membuka panel = materi sudah dibaca; objek dihitung selesai.
        GameState state = GameState.Ensure();
        bool firstRead = state.MarkLearned(definition.id);
        SaveService.Instance?.RequestSave();
        if (firstRead)
        {
            // XP baca hanya sekali (learnedObjectIds menjadi gerbangnya).
            GamificationService.OnObjectRead(definition);
        }

        titleLabel.text = string.IsNullOrEmpty(definition.namaIndonesia)
            ? definition.name
            : definition.namaIndonesia;
        categoryLabel.text = string.IsNullOrEmpty(definition.category) ? "Objek" : definition.category;
        indonesiaLabel.text = $"Indonesia: {definition.namaIndonesia}";
        string dawanText = definition.DisplayDawan;
        if (definition.IsValidated && !string.IsNullOrWhiteSpace(definition.caraBaca))
        {
            dawanText += $" ({definition.caraBaca})";
        }

        dawanLabel.text = $"Dawan: {dawanText}";
        explanationLabel.text = definition.explanation;

        bool validated = definition.IsValidated;
        badgeLabel.text = validated ? "TERVALIDASI" : "DRAF — menunggu validasi TTU";
        badgeBackground.color = validated
            ? new Color(0.18f, 0.55f, 0.28f, 0.95f)
            : new Color(0.72f, 0.42f, 0.12f, 0.95f);

        if (objectImage != null)
        {
            objectImage.sprite = definition.image;
            objectImage.enabled = definition.image != null;
            objectImage.preserveAspect = true;
        }

        if (audioButton != null)
        {
            bool hasAudio = definition.pronunciation != null;
            audioButton.interactable = hasAudio;
            TextMeshProUGUI label = audioButton.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = hasAudio ? "Dengarkan" : "Audio belum tersedia";
            }
        }

        if (quizButton != null)
        {
            bool allLearned = GameUi.Instance != null && GameUi.Instance.Scene != null &&
                              GameUi.Instance.Scene.AllObjectsLearned();
            quizButton.interactable = allLearned;
            TextMeshProUGUI label = quizButton.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = allLearned ? "Kuis" : "Kuis (baca semua)";
            }
        }

        root.SetActive(true);
        root.transform.SetAsLastSibling();
        UiModalState.Push(this);
    }

    public void Close()
    {
        if (root == null || !root.activeSelf)
        {
            return;
        }

        root.SetActive(false);
        UiModalState.Pop(this);
        Action callback = onClose;
        onClose = null;
        current = null;
        callback?.Invoke();
    }

    public void MarkLearnedAndClose()
    {
        if (current != null)
        {
            GameState state = GameState.Ensure();
            if (state.MarkLearned(current.id))
            {
                AudioManager.Instance?.PlaySfx(SfxId.Correct);
                ToastView.Instance?.Show($"{current.namaIndonesia} dipelajari.");
            }

            SaveService.Instance?.RequestSave();
        }

        Close();
    }

    private void PlayPronunciation()
    {
        if (current == null || current.pronunciation == null)
        {
            return;
        }

        float volume = GameState.Ensure().Data.sfxVolume;

        // Di Android, pelafalan diputar lewat MediaPlayer (PCM 16-bit) agar tetap
        // terdengar di perangkat yang tidak mendukung output float Unity/FMOD.
        if (DawanAudioPlayer.TryPlay(current.id, current.pronunciation, volume))
        {
            return;
        }

        AudioSource.PlayClipAtPoint(current.pronunciation,
            Camera.main != null ? Camera.main.transform.position : Vector3.zero, volume);
    }

    private void OpenQuiz()
    {
        Close();
        GameUi.Instance?.ShowQuiz();
    }

    /// <summary>Dipakai tombol "Latihan" di InfoPanel (Fase C).</summary>
    public void OpenWriting()
    {
        if (current == null)
        {
            return;
        }

        LearningObjectDefinition definition = current;
        Close();
        GameUi.Instance?.ShowWriting(definition);
    }

    /// <summary>Dipakai tombol "Bicara" di InfoPanel (Fase C).</summary>
    public void OpenSpeaking()
    {
        if (current == null)
        {
            return;
        }

        LearningObjectDefinition definition = current;
        Close();
        GameUi.Instance?.ShowSpeaking(definition);
    }

    private void OnDestroy()
    {
        UiModalState.Pop(this);
    }
}
