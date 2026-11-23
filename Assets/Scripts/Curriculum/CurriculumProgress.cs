using System.Collections.Generic;

/// <summary>
/// Progres kurikulum persisten (Save v5, aditif). Vocabulary memakai
/// learnedObjectIds lama; exercise/dialogue memakai completedItemIds;
/// lesson/topic memakai completedLessonIds/completedTopicIds.
/// </summary>
public static class CurriculumProgress
{
    public static bool IsItemCompleted(LessonItemData item)
    {
        if (item == null)
        {
            return false;
        }

        GameState state = GameState.Ensure();
        if (item.type == "vocabulary")
        {
            return !string.IsNullOrWhiteSpace(item.objectRef) && state.IsLearned(item.objectRef);
        }

        return !string.IsNullOrWhiteSpace(item.id) && state.Data.completedItemIds.Contains(item.id);
    }

    /// <summary>Menandai exercise/dialogue selesai. Vocabulary tetap lewat learnedObjectIds.</summary>
    public static bool MarkItemCompleted(LessonItemData item)
    {
        if (item == null || item.type == "vocabulary" || string.IsNullOrWhiteSpace(item.id))
        {
            return false;
        }

        GameState state = GameState.Ensure();
        if (state.Data.completedItemIds.Contains(item.id))
        {
            return false;
        }

        state.Data.completedItemIds.Add(item.id);
        state.NotifyChanged();
        SaveService.Instance?.RequestSave();
        return true;
    }

    public static bool IsLessonCompleted(LessonData lesson)
    {
        if (lesson == null || lesson.items == null || lesson.items.Count == 0 ||
            string.IsNullOrWhiteSpace(lesson.id))
        {
            return false;
        }

        for (int i = 0; i < lesson.items.Count; i++)
        {
            if (!IsItemCompleted(lesson.items[i]))
            {
                return false;
            }
        }

        return true;
    }

    public static bool IsTopicCompleted(TopicData topic)
    {
        return topic != null && !string.IsNullOrWhiteSpace(topic.id) &&
               GameState.Ensure().Data.completedTopicIds.Contains(topic.id);
    }

    /// <summary>
    /// Refresh lesson/topic completion dari item yang sudah selesai.
    /// Mengembalikan true bila ada perubahan yang ditulis ke Save v5.
    /// </summary>
    public static bool RefreshCompletion(CurriculumRoot root)
    {
        if (root == null || root.topics == null)
        {
            return false;
        }

        bool changed = false;
        GameState state = GameState.Ensure();

        for (int t = 0; t < root.topics.Count; t++)
        {
            TopicData topic = root.topics[t];
            if (topic == null || topic.lessons == null || topic.lessons.Count == 0)
            {
                continue;
            }

            bool allLessons = true;
            for (int l = 0; l < topic.lessons.Count; l++)
            {
                LessonData lesson = topic.lessons[l];
                if (lesson == null)
                {
                    allLessons = false;
                    continue;
                }

                if (IsLessonCompleted(lesson))
                {
                    if (!string.IsNullOrWhiteSpace(lesson.id) &&
                        !state.Data.completedLessonIds.Contains(lesson.id))
                    {
                        state.Data.completedLessonIds.Add(lesson.id);
                        changed = true;
                    }
                }
                else
                {
                    allLessons = false;
                }
            }

            if (allLessons && !string.IsNullOrWhiteSpace(topic.id) &&
                !state.Data.completedTopicIds.Contains(topic.id))
            {
                state.Data.completedTopicIds.Add(topic.id);
                changed = true;
            }
        }

        if (changed)
        {
            state.NotifyChanged();
            SaveService.Instance?.RequestSave();
        }

        return changed;
    }

    public static int CompletedItemCount(TopicData topic)
    {
        if (topic == null || topic.lessons == null)
        {
            return 0;
        }

        int count = 0;
        for (int l = 0; l < topic.lessons.Count; l++)
        {
            LessonData lesson = topic.lessons[l];
            if (lesson == null || lesson.items == null)
            {
                continue;
            }

            for (int i = 0; i < lesson.items.Count; i++)
            {
                if (IsItemCompleted(lesson.items[i]))
                {
                    count++;
                }
            }
        }

        return count;
    }
}
