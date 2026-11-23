using System;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// Status publikasi terpisah dari validasi (Fase PRA-D).
/// VALIDATED tidak otomatis berarti PUBLISHED.
/// </summary>
public enum PublicationStatus
{
    UNPUBLISHED = 0,
    PUBLISHED = 1
}

[Serializable]
public class LocalizedText
{
    public string id;
}

[Serializable]
public class DialogueLineData
{
    public string speaker;
    public string text;
}

/// <summary>Satu item pelajaran: vocabulary, dialogue, atau exercise.</summary>
[Serializable]
public class LessonItemData
{
    public string id;
    public string type;
    public ValidationStatus validationStatus;
    public PublicationStatus publicationStatus;

    // "ref" adalah keyword C#; dipetakan ke field JSON "ref".
    [JsonProperty("ref")]
    public string objectRef;

    public string skill;
    public string kind;
    public string question;
    public List<string> options = new List<string>();
    public int correctIndex;
    public string audioRef;
    public List<DialogueLineData> lines = new List<DialogueLineData>();
}

[Serializable]
public class LessonData
{
    public string id;
    public int order;
    public LocalizedText title;
    public LocalizedText objective;
    public List<string> skills = new List<string>();
    public ValidationStatus validationStatus;
    public PublicationStatus publicationStatus;
    public List<LessonItemData> items = new List<LessonItemData>();
}

[Serializable]
public class TopicData
{
    public string id;
    public int order;
    public LocalizedText title;
    public ValidationStatus validationStatus;
    public PublicationStatus publicationStatus;
    public List<LessonData> lessons = new List<LessonData>();
}

[Serializable]
public class CurriculumRoot
{
    public int schemaVersion;
    public string locale;
    public List<TopicData> topics = new List<TopicData>();
}
