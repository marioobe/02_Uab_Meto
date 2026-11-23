using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Loader kurikulum runtime (Stage 2). Hanya membaca TextAsset dari
/// Resources/Curriculum/curriculum, memakai pipeline JSON bersama,
/// dan menyaring konten yang belum PUBLISHED. Tidak pernah melempar
/// exception ke UI: konten kosong/rusak menghasilkan kurikulum kosong.
/// </summary>
public static class CurriculumLoader
{
    public const string ResourcePath = "Curriculum/curriculum";

    private static CurriculumRoot cached;
    private static bool attempted;

    /// <summary>Muat sekali dan cache. Aman dipanggil kapan saja.</summary>
    public static CurriculumRoot Load()
    {
        if (attempted)
        {
            return cached ?? CreateEmpty();
        }

        attempted = true;
        TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
        if (asset == null)
        {
            cached = CreateEmpty();
            return cached;
        }

        string error;
        CurriculumRoot root = Parse(asset.bytes, out error);
        if (root == null)
        {
            Debug.LogError("[CurriculumLoader] " + error);
            cached = CreateEmpty();
            return cached;
        }

        cached = root;
        return cached;
    }

    /// <summary>Muat + pastikan semua vocabulary ref ada di LearningDatabase.</summary>
    public static CurriculumRoot Load(LearningDatabase database)
    {
        CurriculumRoot root = Load();
        if (database == null)
        {
            return root;
        }

        List<string> errors;
        if (!ValidateReferences(root, database, out errors))
        {
            for (int i = 0; i < errors.Count; i++)
            {
                Debug.LogError("[CurriculumLoader] " + errors[i]);
            }

            return CreateEmpty();
        }

        return root;
    }

    /// <summary>Pipeline bersama: UTF-8 strict, enum string, duplicate key ditolak, lalu filter PUBLISHED.</summary>
    public static CurriculumRoot Parse(byte[] bytes, out string error)
    {
        CurriculumRoot parsed;
        if (!CurriculumJson.TryDeserialize(bytes, out parsed, out error))
        {
            return null;
        }

        if (parsed.schemaVersion <= 0 || parsed.schemaVersion > CurriculumJson.SchemaVersion)
        {
            error = "schemaVersion tidak didukung: " + parsed.schemaVersion;
            return null;
        }

        return FilterPublished(parsed);
    }

    /// <summary>Hanya topic/lesson/item PUBLISHED yang diekspos ke runtime.</summary>
    public static CurriculumRoot FilterPublished(CurriculumRoot source)
    {
        CurriculumRoot result = CreateEmpty();
        if (source == null)
        {
            return result;
        }

        result.schemaVersion = source.schemaVersion;
        result.locale = source.locale;
        result.topics = new List<TopicData>();

        if (source.topics == null)
        {
            return result;
        }

        for (int t = 0; t < source.topics.Count; t++)
        {
            TopicData topic = source.topics[t];
            if (topic == null || topic.publicationStatus != PublicationStatus.PUBLISHED)
            {
                continue;
            }

            TopicData topicCopy = new TopicData
            {
                id = topic.id,
                order = topic.order,
                title = CopyText(topic.title),
                validationStatus = topic.validationStatus,
                publicationStatus = topic.publicationStatus,
                lessons = new List<LessonData>()
            };

            if (topic.lessons != null)
            {
                for (int l = 0; l < topic.lessons.Count; l++)
                {
                    LessonData lesson = topic.lessons[l];
                    if (lesson == null || lesson.publicationStatus != PublicationStatus.PUBLISHED)
                    {
                        continue;
                    }

                    LessonData lessonCopy = new LessonData
                    {
                        id = lesson.id,
                        order = lesson.order,
                        title = CopyText(lesson.title),
                        objective = CopyText(lesson.objective),
                        skills = lesson.skills == null ? new List<string>() : new List<string>(lesson.skills),
                        validationStatus = lesson.validationStatus,
                        publicationStatus = lesson.publicationStatus,
                        items = new List<LessonItemData>()
                    };

                    if (lesson.items != null)
                    {
                        for (int i = 0; i < lesson.items.Count; i++)
                        {
                            LessonItemData item = lesson.items[i];
                            if (item == null || item.publicationStatus != PublicationStatus.PUBLISHED)
                            {
                                continue;
                            }

                            lessonCopy.items.Add(CopyItem(item));
                        }
                    }

                    topicCopy.lessons.Add(lessonCopy);
                }
            }

            result.topics.Add(topicCopy);
        }

        return result;
    }

    public static TopicData FindTopic(string topicId)
    {
        CurriculumRoot root = Load();
        if (root == null || root.topics == null || string.IsNullOrEmpty(topicId))
        {
            return null;
        }

        for (int i = 0; i < root.topics.Count; i++)
        {
            if (root.topics[i] != null && root.topics[i].id == topicId)
            {
                return root.topics[i];
            }
        }

        return null;
    }

    public static LessonData FindLesson(string topicId, string lessonId)
    {
        TopicData topic = FindTopic(topicId);
        if (topic == null || topic.lessons == null || string.IsNullOrEmpty(lessonId))
        {
            return null;
        }

        for (int i = 0; i < topic.lessons.Count; i++)
        {
            if (topic.lessons[i] != null && topic.lessons[i].id == lessonId)
            {
                return topic.lessons[i];
            }
        }

        return null;
    }

    /// <summary>Resolusi vocabulary ke aset lama tanpa mengubah LearningDatabase.</summary>
    public static LearningObjectDefinition ResolveObject(string objectRef, LearningDatabase database)
    {
        if (database == null || string.IsNullOrEmpty(objectRef))
        {
            return null;
        }

        return database.Find(objectRef);
    }

    public static bool ValidateReferences(CurriculumRoot root, LearningDatabase database,
        out List<string> errors)
    {
        errors = new List<string>();
        if (root == null || root.topics == null)
        {
            return true;
        }

        if (database == null)
        {
            errors.Add("LearningDatabase null; referensi vocabulary tidak dapat divalidasi.");
            return false;
        }

        for (int t = 0; t < root.topics.Count; t++)
        {
            TopicData topic = root.topics[t];
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
                    if (item == null || item.type != "vocabulary" || string.IsNullOrEmpty(item.objectRef))
                    {
                        continue;
                    }

                    if (database.Find(item.objectRef) == null)
                    {
                        errors.Add($"Vocabulary {item.id} ref yatim: {item.objectRef}");
                    }
                }
            }
        }

        return errors.Count == 0;
    }

    public static CurriculumRoot CreateEmpty()
    {
        return new CurriculumRoot
        {
            schemaVersion = CurriculumJson.SchemaVersion,
            locale = "id",
            topics = new List<TopicData>()
        };
    }

    /// <summary>Hanya untuk test/relog; produksi tidak perlu memanggil ini.</summary>
    public static void ClearCache()
    {
        cached = null;
        attempted = false;
    }

    private static LocalizedText CopyText(LocalizedText source)
    {
        return source == null ? null : new LocalizedText { id = source.id };
    }

    private static LessonItemData CopyItem(LessonItemData source)
    {
        return new LessonItemData
        {
            id = source.id,
            type = source.type,
            validationStatus = source.validationStatus,
            publicationStatus = source.publicationStatus,
            objectRef = source.objectRef,
            skill = source.skill,
            kind = source.kind,
            question = source.question,
            options = source.options == null ? new List<string>() : new List<string>(source.options),
            correctIndex = source.correctIndex,
            audioRef = source.audioRef,
            lines = CopyLines(source.lines)
        };
    }

    private static List<DialogueLineData> CopyLines(List<DialogueLineData> source)
    {
        List<DialogueLineData> result = new List<DialogueLineData>();
        if (source == null)
        {
            return result;
        }

        for (int i = 0; i < source.Count; i++)
        {
            DialogueLineData line = source[i];
            result.Add(line == null
                ? null
                : new DialogueLineData { speaker = line.speaker, text = line.text });
        }

        return result;
    }
}
