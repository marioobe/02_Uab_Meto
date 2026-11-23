using System;
using System.Collections.Generic;
using UnityEngine;

public enum ValidationStatus
{
    DRAFT = 0,
    UNVALIDATED = 1,
    VALIDATED = 2
}

[Serializable]
public class QuizQuestion
{
    public string question;
    public string[] options = new string[0];
    public int correctIndex;
    public string explanation;
    [Tooltip("Jika true, soal hanya aktif ketika nama Dawan sudah VALIDATED.")]
    public bool usesDawan;
}

/// <summary>
/// Satu objek pembelajaran. Nama Dawan sengaja boleh kosong; UI menampilkan
/// badge DRAF dan kuis berbahasa Dawan tidak aktif sampai divalidasi.
/// </summary>
[CreateAssetMenu(menuName = "UAB Meto/Learning Object", fileName = "LearningObject")]
public class LearningObjectDefinition : ScriptableObject
{
    public string id;
    public string sceneKey;
    public string category = "Benda";
    public string namaIndonesia;
    public string namaDawan;
    [Tooltip("Cara baca/dialek yang dicatat validator, mis. \"hau outu\".")]
    public string caraBaca;
    public ValidationStatus validationStatus = ValidationStatus.DRAFT;
    public string validator;
    public string validationDate;
    [TextArea(2, 5)] public string validationNotes;
    [TextArea(3, 8)] public string explanation;
    public Sprite image;
    public AudioClip pronunciation;
    public List<QuizQuestion> questions = new List<QuizQuestion>();

    [Header("Kurikulum (metadata Fase A)")]
    [Tooltip("Topik kurikulum tempat objek ini menjadi materi.")]
    public LearningTopic topicId = LearningTopic.None;

    [Tooltip("Utama = materi inti topik; Pendukung = konteks visual/pengulangan.")]
    public LearningRole role = LearningRole.Utama;

    [Tooltip("Kode topik lain yang memakai objek ini sebagai materi pendukung.")]
    public List<string> crossTopics = new List<string>();

    [Tooltip("Urutan tampil di dalam topik.")]
    public int order;

    public bool IsValidated => validationStatus == ValidationStatus.VALIDATED &&
                               !string.IsNullOrWhiteSpace(namaDawan);

    public string DisplayDawan
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(namaDawan))
            {
                return IsValidated ? namaDawan : $"{namaDawan} (DRAF)";
            }

            return "—";
        }
    }
}
