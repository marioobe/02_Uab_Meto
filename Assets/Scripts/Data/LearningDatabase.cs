using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "UAB Meto/Learning Database", fileName = "LearningDatabase")]
public class LearningDatabase : ScriptableObject
{
    public List<LearningObjectDefinition> objects = new List<LearningObjectDefinition>();

    public LearningObjectDefinition Find(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        for (int i = 0; i < objects.Count; i++)
        {
            if (objects[i] != null && objects[i].id == id)
            {
                return objects[i];
            }
        }

        return null;
    }

    public List<LearningObjectDefinition> ForScene(string sceneKey)
    {
        List<LearningObjectDefinition> result = new List<LearningObjectDefinition>();
        for (int i = 0; i < objects.Count; i++)
        {
            LearningObjectDefinition item = objects[i];
            if (item != null && item.sceneKey == sceneKey)
            {
                result.Add(item);
            }
        }

        return result;
    }

    // ---------------------------------------------------------------- topik

    public List<LearningObjectDefinition> ForTopic(LearningTopic topic, bool includeSupporting = true)
    {
        List<LearningObjectDefinition> result = new List<LearningObjectDefinition>();
        for (int i = 0; i < objects.Count; i++)
        {
            LearningObjectDefinition item = objects[i];
            if (item == null || item.topicId != topic)
            {
                continue;
            }

            if (!includeSupporting && item.role != LearningRole.Utama)
            {
                continue;
            }

            result.Add(item);
        }

        result.Sort((a, b) =>
        {
            int orderCompare = a.order.CompareTo(b.order);
            return orderCompare != 0 ? orderCompare : string.CompareOrdinal(a.id, b.id);
        });
        return result;
    }

    public int PrimaryCount(LearningTopic topic)
    {
        return ForTopic(topic, false).Count;
    }

    public int SupportingCount(LearningTopic topic)
    {
        int total = ForTopic(topic, true).Count;
        return total - ForTopic(topic, false).Count;
    }

    public bool HasContent(LearningTopic topic)
    {
        return PrimaryCount(topic) + SupportingCount(topic) > 0;
    }
}
