using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Jembatan baca antara model kurikulum JSON dan sistem lama (LearningTopic,
/// LearningObjectDefinition, Save v4). Tidak menulis atau memigrasikan Save.
/// </summary>
public static class CurriculumMapping
{
    /// <summary>Memetakan id/order topik JSON ke enum LearningTopic.</summary>
    public static LearningTopic ResolveTopic(string curriculumTopicId)
    {
        if (string.IsNullOrWhiteSpace(curriculumTopicId))
        {
            return LearningTopic.None;
        }

        LearningTopic byCode = LearningTopicCatalog.FromCode(curriculumTopicId);
        if (byCode != LearningTopic.None)
        {
            return byCode;
        }

        int numeric;
        if (int.TryParse(curriculumTopicId, out numeric) && numeric >= 1 && numeric <= 12)
        {
            return (LearningTopic)numeric;
        }

        return LearningTopic.None;
    }

    public static LearningTopic ResolveTopic(TopicData topic)
    {
        if (topic == null)
        {
            return LearningTopic.None;
        }

        LearningTopic byCode = ResolveTopic(topic.id);
        if (byCode != LearningTopic.None)
        {
            return byCode;
        }

        if (topic.order >= 1 && topic.order <= 12)
        {
            return (LearningTopic)topic.order;
        }

        return LearningTopic.None;
    }

    public static string TopicDisplayName(TopicData topic)
    {
        if (topic == null)
        {
            return "Topik";
        }

        if (topic.title != null && !string.IsNullOrWhiteSpace(topic.title.id))
        {
            return topic.title.id;
        }

        LearningTopic mapped = ResolveTopic(topic);
        if (mapped != LearningTopic.None)
        {
            return LearningTopicCatalog.NamaOf(mapped);
        }

        return string.IsNullOrWhiteSpace(topic.id) ? "Topik" : topic.id;
    }

    public static string LessonDisplayName(LessonData lesson)
    {
        if (lesson == null)
        {
            return "Pelajaran";
        }

        if (lesson.title != null && !string.IsNullOrWhiteSpace(lesson.title.id))
        {
            return lesson.title.id;
        }

        return string.IsNullOrWhiteSpace(lesson.id) ? "Pelajaran" : lesson.id;
    }

    public static bool IsVocabulary(LessonItemData item)
    {
        return item != null && item.type == "vocabulary";
    }

    public static bool IsVocabularyLearned(string objectRef)
    {
        if (string.IsNullOrWhiteSpace(objectRef))
        {
            return false;
        }

        return GameState.Ensure().IsLearned(objectRef);
    }

    public static bool IsVocabularyLearned(LessonItemData item)
    {
        return item != null && item.type == "vocabulary" && IsVocabularyLearned(item.objectRef);
    }

    public static int LearnedVocabularyCount(LessonData lesson)
    {
        if (lesson == null || lesson.items == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < lesson.items.Count; i++)
        {
            if (IsVocabularyLearned(lesson.items[i]))
            {
                count++;
            }
        }

        return count;
    }

    public static int VocabularyCount(LessonData lesson)
    {
        if (lesson == null || lesson.items == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < lesson.items.Count; i++)
        {
            if (IsVocabulary(lesson.items[i]) && !string.IsNullOrWhiteSpace(lesson.items[i].objectRef))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Lesson dianggap selesai bila semua vocabulary-nya sudah dipelajari.
    /// Tidak menulis completedTopicIds / Save v4.
    /// </summary>
    public static bool IsLessonComplete(LessonData lesson)
    {
        int vocabulary = VocabularyCount(lesson);
        return vocabulary > 0 && LearnedVocabularyCount(lesson) == vocabulary;
    }

    public static string ItemLabel(LessonItemData item)
    {
        if (item == null)
        {
            return "Materi";
        }

        if (item.type == "vocabulary")
        {
            return string.IsNullOrWhiteSpace(item.objectRef) ? "Kosakata" : item.objectRef;
        }

        if (item.type == "exercise")
        {
            return string.IsNullOrWhiteSpace(item.question) ? "Latihan" : item.question;
        }

        if (item.type == "dialogue")
        {
            if (item.lines != null && item.lines.Count > 0 && item.lines[0] != null &&
                !string.IsNullOrWhiteSpace(item.lines[0].text))
            {
                return item.lines[0].text;
            }

            return "Dialog";
        }

        return string.IsNullOrWhiteSpace(item.type) ? "Materi" : item.type;
    }

    public static string ExerciseText(LessonItemData item)
    {
        if (item == null)
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder();
        builder.Append(string.IsNullOrWhiteSpace(item.question) ? "Latihan" : item.question);
        if (item.options != null && item.options.Count > 0)
        {
            builder.Append("\n\n");
            for (int i = 0; i < item.options.Count; i++)
            {
                builder.Append(i + 1).Append(". ").Append(item.options[i] ?? string.Empty);
                if (i < item.options.Count - 1)
                {
                    builder.Append('\n');
                }
            }
        }

        return builder.ToString();
    }

    public static string DialogueText(LessonItemData item)
    {
        if (item == null || item.lines == null)
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < item.lines.Count; i++)
        {
            DialogueLineData line = item.lines[i];
            if (line == null)
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append('\n');
            }

            if (!string.IsNullOrWhiteSpace(line.speaker))
            {
                builder.Append(line.speaker).Append(": ");
            }

            builder.Append(line.text ?? string.Empty);
        }

        return builder.ToString();
    }

    public static int CountItems(IReadOnlyList<LessonData> lessons)
    {
        if (lessons == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < lessons.Count; i++)
        {
            LessonData lesson = lessons[i];
            if (lesson != null && lesson.items != null)
            {
                count += lesson.items.Count;
            }
        }

        return count;
    }

    public static int CountItems(TopicData topic)
    {
        return topic == null ? 0 : CountItems(topic.lessons);
    }
}
