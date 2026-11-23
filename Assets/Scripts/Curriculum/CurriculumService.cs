using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Service runtime kurikulum. Memuat CurriculumLoader sekali saat startup,
/// menyediakan accessor untuk UI, dan mengikat LearningDatabase dari scene aktif
/// untuk resolusi vocabulary. Tidak pernah melempar exception ke UI.
/// </summary>
public class CurriculumService : MonoBehaviour
{
    public static CurriculumService Instance { get; private set; }

    public CurriculumRoot Root { get; private set; }
    public LearningDatabase Database { get; private set; }
    public bool Loaded { get; private set; }
    public bool ReferencesValid { get; private set; } = true;

    public bool HasContent => Root != null && Root.topics != null && Root.topics.Count > 0;
    public bool IsUsable => Loaded && HasContent && ReferencesValid;
    public bool CanRender => Loaded && HasContent;

    public IReadOnlyList<TopicData> Topics =>
        Root != null && Root.topics != null ? Root.topics : Array.Empty<TopicData>();

    private bool loggedStatus;
    private bool loggedReferenceError;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject root = new GameObject("CurriculumService");
        UnityEngine.Object.DontDestroyOnLoad(root);
        root.AddComponent<CurriculumService>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        SceneManager.sceneLoaded += OnSceneLoaded;
        LoadOnce();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>Muat sekali; aman dipanggil berulang.</summary>
    public CurriculumRoot LoadOnce()
    {
        if (Loaded)
        {
            return Root;
        }

        Root = CurriculumLoader.Load();
        Loaded = true;

        if (!loggedStatus)
        {
            loggedStatus = true;
            if (HasContent)
            {
                Debug.Log($"[CurriculumService] Kurikulum dimuat: {Root.topics.Count} topik PUBLISHED.");
            }
            else
            {
                Debug.Log("[CurriculumService] Kurikulum kosong (belum ada materi PUBLISHED).");
            }
        }

        if (HasContent)
        {
            ValidateReferences();
        }

        return Root;
    }

    /// <summary>Reload hanya untuk test/debug; produksi tidak memanggil ini.</summary>
    public void Reload()
    {
        CurriculumLoader.ClearCache();
        Root = null;
        Loaded = false;
        loggedStatus = false;
        loggedReferenceError = false;
        ReferencesValid = true;
        LoadOnce();
    }

    /// <summary>Ikat database scene aktif untuk resolusi vocabulary.</summary>
    public void BindDatabase(LearningDatabase database)
    {
        if (Database == database)
        {
            return;
        }

        Database = database;
        if (Loaded)
        {
            ValidateReferences();
        }
    }

    public bool ValidateReferences(out List<string> errors)
    {
        errors = new List<string>();
        if (Root == null || Root.topics == null || Root.topics.Count == 0)
        {
            ReferencesValid = true;
            return true;
        }

        int vocabularyCount = 0;
        for (int t = 0; t < Root.topics.Count; t++)
        {
            TopicData topic = Root.topics[t];
            if (topic == null || topic.lessons == null)
            {
                continue;
            }

            for (int l = 0; l < topic.lessons.Count; l++)
            {
                LessonData lesson = topic.lessons[l];
                if (lesson == null || lesson.items == null)
                {
                    continue;
                }

                for (int i = 0; i < lesson.items.Count; i++)
                {
                    LessonItemData item = lesson.items[i];
                    if (item != null && item.type == "vocabulary" &&
                        !string.IsNullOrWhiteSpace(item.objectRef))
                    {
                        vocabularyCount++;
                    }
                }
            }
        }

        if (vocabularyCount == 0)
        {
            ReferencesValid = true;
            return true;
        }

        if (Database == null)
        {
            // Belum terikat ke scene; bukan error permanen. UI memakai fallback
            // sampai database tersedia.
            errors.Add("LearningDatabase belum terikat; referensi vocabulary belum divalidasi.");
            ReferencesValid = false;
            return false;
        }

        bool valid = CurriculumLoader.ValidateReferences(Root, Database, out errors);
        ReferencesValid = valid;
        if (!valid)
        {
            LogReferenceError(errors);
        }

        return valid;
    }

    private void ValidateReferences()
    {
        ValidateReferences(out _);
    }

    private void LogReferenceError(List<string> errors)
    {
        if (loggedReferenceError)
        {
            return;
        }

        loggedReferenceError = true;
        for (int i = 0; i < errors.Count; i++)
        {
            Debug.LogError("[CurriculumService] " + errors[i]);
        }
    }

    public LearningTopic ResolveTopic(string curriculumTopicId)
    {
        return CurriculumMapping.ResolveTopic(curriculumTopicId);
    }

    public TopicData FindTopic(string topicId)
    {
        if (Root == null || Root.topics == null || string.IsNullOrEmpty(topicId))
        {
            return null;
        }

        for (int i = 0; i < Root.topics.Count; i++)
        {
            TopicData topic = Root.topics[i];
            if (topic != null && string.Equals(topic.id, topicId, StringComparison.Ordinal))
            {
                return topic;
            }
        }

        return null;
    }

    public LessonData FindLesson(string topicId, string lessonId)
    {
        TopicData topic = FindTopic(topicId);
        if (topic == null || topic.lessons == null || string.IsNullOrEmpty(lessonId))
        {
            return null;
        }

        for (int i = 0; i < topic.lessons.Count; i++)
        {
            LessonData lesson = topic.lessons[i];
            if (lesson != null && string.Equals(lesson.id, lessonId, StringComparison.Ordinal))
            {
                return lesson;
            }
        }

        return null;
    }

    public LearningObjectDefinition ResolveVocabulary(string objectRef)
    {
        if (Database == null || string.IsNullOrEmpty(objectRef))
        {
            return null;
        }

        return CurriculumLoader.ResolveObject(objectRef, Database);
    }

    public bool TryResolveVocabulary(LessonItemData item, out LearningObjectDefinition definition)
    {
        definition = null;
        if (item == null || item.type != "vocabulary" || string.IsNullOrEmpty(item.objectRef))
        {
            return false;
        }

        definition = ResolveVocabulary(item.objectRef);
        return definition != null;
    }

    public bool IsVocabularyLearned(string objectRef)
    {
        return CurriculumMapping.IsVocabularyLearned(objectRef);
    }

    /// <summary>Catat exercise/dialogue selesai dan refresh lesson/topic (Save v5).</summary>
    public bool MarkItemCompleted(LessonItemData item)
    {
        bool changed = CurriculumProgress.MarkItemCompleted(item);
        if (CurriculumProgress.RefreshCompletion(Root))
        {
            changed = true;
        }

        return changed;
    }

    /// <summary>Refresh lesson/topic dari vocab yang sudah dibaca (mis. saat panel buku dibuka).</summary>
    public void RefreshCompletion()
    {
        CurriculumProgress.RefreshCompletion(Root);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneController controller = FindFirstObjectByType<SceneController>();
        if (controller != null)
        {
            BindDatabase(controller.database);
        }
    }
}
