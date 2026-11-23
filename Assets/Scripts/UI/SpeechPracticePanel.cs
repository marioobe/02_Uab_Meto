using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel latihan bicara: rekam (maks 10 detik) → berhenti → putar ulang → hapus.
/// Rekaman hanya sementara (speech_tmp.wav), tanpa upload/riwayat/penilaian AI.
/// </summary>
public class SpeechPracticePanel : MonoBehaviour
{
    private static readonly Color Ink = new Color(0.26f, 0.19f, 0.11f, 1f);
    private static readonly Color Muted = new Color(0.42f, 0.33f, 0.22f, 0.75f);
    private static readonly Color Okay = new Color(0.16f, 0.45f, 0.22f, 1f);
    private static readonly Color Warn = new Color(0.62f, 0.22f, 0.16f, 1f);

    private GameObject root;
    private TextMeshProUGUI targetLabel;
    private TextMeshProUGUI hintLabel;
    private TextMeshProUGUI statusLabel;
    private TextMeshProUGUI feedbackLabel;
    private Button recordButton;
    private Button stopButton;
    private Button playButton;
    private Button clearButton;

    private SpeechRecorder recorder;
    private bool wasRecording;

    public void Build(GameObject panelRoot, RectTransform content, Sprite panelSprite)
    {
        root = panelRoot;
        recorder = gameObject.AddComponent<SpeechRecorder>();

        UiFactory.CreateLabel(content, "Title", "Latihan Bicara", 40f, Ink,
            TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(40f, -28f), new Vector2(800f, 50f), true);

        targetLabel = UiFactory.CreateLabel(content, "Target", string.Empty, 40f, Ink,
            TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(40f, -92f), new Vector2(1100f, 52f), true);

        hintLabel = UiFactory.CreateLabel(content, "Hint", string.Empty, 24f, Muted,
            TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(40f, -142f), new Vector2(1100f, 34f));

        UiFactory.CreateLabel(content, "Instructions",
            "Rekam maksimal 10 detik, lalu putar ulang. Rekaman hanya sementara dan tidak diunggah.",
            22f, Muted, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(40f, -190f), new Vector2(1100f, 60f));

        statusLabel = UiFactory.CreateLabel(content, "Status", "Siap merekam (maks. 10 detik).", 26f, Ink,
            TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(40f, -262f), new Vector2(1100f, 60f), true);

        feedbackLabel = UiFactory.CreateLabel(content, "Feedback", string.Empty, 22f, Muted,
            TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(40f, -330f), new Vector2(1100f, 60f));

        recordButton = UiFactory.CreateButton(content, "RecordButton", "Rekam", null,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(40f, 150f), new Vector2(240f, 80f),
            new Color(0.22f, 0.45f, 0.24f, 0.95f), new Color(1f, 0.98f, 0.9f, 1f), 30f);
        recordButton.onClick.AddListener(OnRecord);

        stopButton = UiFactory.CreateButton(content, "StopButton", "Berhenti", null,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(300f, 150f), new Vector2(240f, 80f),
            new Color(0.62f, 0.28f, 0.16f, 0.95f), new Color(1f, 0.97f, 0.9f, 1f), 30f);
        stopButton.onClick.AddListener(OnStop);

        playButton = UiFactory.CreateButton(content, "PlayButton", "Putar Ulang", null,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(560f, 150f), new Vector2(240f, 80f),
            new Color(0.35f, 0.25f, 0.14f, 0.9f), new Color(1f, 0.96f, 0.86f, 1f), 27f);
        playButton.onClick.AddListener(OnPlay);

        clearButton = UiFactory.CreateButton(content, "ClearButton", "Hapus", null,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(820f, 150f), new Vector2(180f, 80f),
            new Color(0.42f, 0.32f, 0.2f, 0.9f), new Color(1f, 0.96f, 0.86f, 1f), 28f);
        clearButton.onClick.AddListener(OnClear);

        Button close = UiFactory.CreateButton(content, "CloseButton", "Tutup", null,
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-40f, 42f), new Vector2(220f, 80f),
            new Color(0.3f, 0.2f, 0.12f, 0.9f), new Color(1f, 0.96f, 0.86f, 1f), 30f);
        close.onClick.AddListener(() =>
        {
            AudioManager.Instance?.PlaySfx(SfxId.Click);
            Close();
        });

        root.SetActive(false);
    }

    public void Show(LearningObjectDefinition definition)
    {
        if (root == null || definition == null)
        {
            return;
        }

        targetLabel.text = string.IsNullOrWhiteSpace(definition.namaDawan)
            ? definition.namaIndonesia
            : definition.namaDawan;
        hintLabel.text = string.IsNullOrWhiteSpace(definition.caraBaca)
            ? $"Latih pengucapan: {definition.namaIndonesia}"
            : $"Cara baca: {definition.caraBaca}";

        recorder.Clear();
        wasRecording = false;
        statusLabel.color = Ink;
        statusLabel.text = SpeechRecorder.HasPermission()
            ? "Siap merekam (maks. 10 detik)."
            : "Izin mikrofon belum diberikan.";
        feedbackLabel.text = string.Empty;

        RefreshButtons();
        root.SetActive(true);
        UiModalState.Push(this);
    }

    public void Close()
    {
        if (root == null || !root.activeSelf)
        {
            return;
        }

        if (recorder != null)
        {
            recorder.Clear();
        }

        root.SetActive(false);
        UiModalState.Pop(this);
    }

    private void OnDestroy()
    {
        UiModalState.Pop(this);
    }

    private void Update()
    {
        if (recorder == null)
        {
            return;
        }

        if (wasRecording && !recorder.IsRecording)
        {
            wasRecording = false;
            OnStopped();
        }

        wasRecording = recorder.IsRecording;
    }

    // ---------------------------------------------------------------- aksi

    private void OnRecord()
    {
        if (recorder == null || recorder.IsRecording)
        {
            return;
        }

        statusLabel.color = Ink;
        statusLabel.text = "Meminta izin mikrofon...";
        recorder.EnsurePermission(granted =>
        {
            if (!granted)
            {
                statusLabel.color = Warn;
                statusLabel.text = "Izin mikrofon ditolak. Aktifkan izin mikrofon di Pengaturan Android.";
                feedbackLabel.text = "Tanpa izin, latihan bicara tidak dapat merekam.";
                RefreshButtons();
                return;
            }

            StartRecord();
        });
    }

    private void StartRecord()
    {
        string error;
        if (!recorder.StartRecording(out error))
        {
            statusLabel.color = Warn;
            statusLabel.text = error;
            feedbackLabel.text = "Pastikan mikrofon tersedia dan izin sudah diberikan.";
            RefreshButtons();
            return;
        }

        wasRecording = true;
        statusLabel.color = Ink;
        statusLabel.text = "Merekam... (maks. 10 detik)";
        feedbackLabel.text = "Ucapkan kata target dengan jelas.";
        RefreshButtons();
    }

    private void OnStop()
    {
        if (recorder == null)
        {
            return;
        }

        recorder.StopRecording();
        wasRecording = false;
        OnStopped();
    }

    private void OnStopped()
    {
        if (recorder == null || !recorder.HasRecording)
        {
            statusLabel.color = WarningColor();
            statusLabel.text = "Tidak ada rekaman. Coba rekam lagi.";
            RefreshButtons();
            return;
        }

        statusLabel.color = Okay;
        statusLabel.text = $"Rekaman selesai: {recorder.RecordedSamples} sampel · " +
                           $"{recorder.DurationSeconds:0.0} detik.";
        feedbackLabel.text = "Tekan 'Putar Ulang' untuk mendengarkan hasil rekaman.";
        RefreshButtons();
    }

    private void OnPlay()
    {
        if (recorder == null || !recorder.HasRecording)
        {
            return;
        }

        recorder.Play();
        statusLabel.color = Ink;
        statusLabel.text = "Memutar rekaman...";
    }

    private void OnClear()
    {
        if (recorder == null)
        {
            return;
        }

        recorder.Clear();
        statusLabel.color = Ink;
        statusLabel.text = "Rekaman dihapus.";
        feedbackLabel.text = string.Empty;
        RefreshButtons();
    }

    private void RefreshButtons()
    {
        if (recorder == null)
        {
            return;
        }

        bool recording = recorder.IsRecording;
        bool has = recorder.HasRecording;

        recordButton.interactable = !recording;
        stopButton.interactable = recording;
        playButton.interactable = has && !recording;
        clearButton.interactable = has && !recording;
    }

    private static Color WarningColor()
    {
        return new Color(0.62f, 0.22f, 0.16f, 1f);
    }
}
