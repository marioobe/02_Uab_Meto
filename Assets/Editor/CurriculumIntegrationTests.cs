using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Unit test Stage 3: CurriculumService, mapping, filter PUBLISHED, fallback,
/// dan validasi referensi. Semua fixture self-cleaning; produksi tidak disentuh.
/// </summary>
public static class CurriculumIntegrationTests
{
    private static int failures;

    public static void SelfTest()
    {
        RunAll();
    }

    public static int RunAll()
    {
        failures = 0;
        SaveV5MigrationSelfTest();
        CurriculumMappingSelfTest();
        CurriculumServiceSelfTest();
        AudioFormatSelfTest();
        Debug.Log($"[CurriculumIntegration] RESULT: {(failures == 0 ? "PASS" : "FAIL")}");
        return failures;
    }

    private static void Expect(string name, bool condition)
    {
        if (condition)
        {
            Debug.Log($"[CurriculumIntegration] PASS {name}");
        }
        else
        {
            failures++;
            Debug.LogError($"[CurriculumIntegration] GAGAL {name}");
        }
    }

    private static void AudioFormatSelfTest()
    {
        string normalized;
        Expect("audio m4a diterima",
            DawanAudioFormat.TryNormalizePath("periuk.m4a", out normalized) &&
            normalized == "periuk.m4a");
        Expect("audio wav diterima",
            DawanAudioFormat.TryNormalizePath("uji_bunyi.wav", out normalized) &&
            normalized == "uji_bunyi.wav");
        Expect("audio ekstensi huruf besar diterima",
            DawanAudioFormat.TryNormalizePath("UJI.WAV", out normalized) &&
            string.Equals(normalized, "UJI.WAV", StringComparison.Ordinal));
        Expect("audio subfolder diterima",
            DawanAudioFormat.TryNormalizePath("sub/uji.wav", out normalized) &&
            normalized == "sub/uji.wav");
        Expect("audio backslash dinormalisasi",
            DawanAudioFormat.TryNormalizePath("sub\\uji.wav", out normalized) &&
            normalized == "sub/uji.wav");
        Expect("audio mp3 ditolak", !DawanAudioFormat.TryNormalizePath("lagu.mp3", out _));
        Expect("audio ogg ditolak", !DawanAudioFormat.TryNormalizePath("lagu.ogg", out _));
        Expect("audio traversal ditolak", !DawanAudioFormat.TryNormalizePath("../escape.m4a", out _));
        Expect("audio traversal tersembunyi ditolak",
            !DawanAudioFormat.TryNormalizePath("sub/../escape.m4a", out _));
        Expect("audio absolut unix ditolak",
            !DawanAudioFormat.TryNormalizePath("/sdcard/uji.m4a", out _));
        Expect("audio absolut windows ditolak",
            !DawanAudioFormat.TryNormalizePath("C:/uji.m4a", out _));
        Expect("audio drive backslash ditolak",
            !DawanAudioFormat.TryNormalizePath("C:\\uji.m4a", out _));
        Expect("audio tanpa ekstensi ditolak",
            !DawanAudioFormat.TryNormalizePath("tanpaekstensi", out _));
        Expect("audio hanya ekstensi ditolak", !DawanAudioFormat.TryNormalizePath(".m4a", out _));
        Expect("audio kosong ditolak", !DawanAudioFormat.TryNormalizePath("", out _));
        Expect("audio null ditolak", !DawanAudioFormat.TryNormalizePath(null, out _));
        Expect("allowlist runtime = m4a + wav",
            DawanAudioFormat.SupportedExtensions.Length == 2 &&
            DawanAudioFormat.IsSupportedExtension(".m4a") &&
            DawanAudioFormat.IsSupportedExtension(".wav") &&
            DawanAudioFormat.PrimaryExtension == ".m4a");
    }

    private static void SaveV5MigrationSelfTest()
    {
        Expect("SaveService versi 5", SaveService.CurrentVersion == 5);

        const string v4Json = "{\"saveVersion\":4,\"hearts\":2,\"fruit\":7,\"xp\":42," +
                              "\"learnedObjectIds\":[\"periuk\"],\"xpWritings\":[\"w:1:x:1\"]}";
        SaveData legacy = JsonConvert.DeserializeObject<SaveData>(v4Json);
        Expect("v4 JSON terbaca", legacy != null && legacy.saveVersion == 4 &&
                                  legacy.hearts == 2 && legacy.fruit == 7 && legacy.xp == 42);

        GameObject host = new GameObject("SaveMigrationTest");
        try
        {
            SaveService service = host.AddComponent<SaveService>();
            MethodInfo migrate = typeof(SaveService).GetMethod("Migrate",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Expect("metode Migrate tersedia", migrate != null);
            if (migrate != null && legacy != null)
            {
                SaveData migrated = (SaveData)migrate.Invoke(service, new object[] { legacy });
                Expect("migrasi v4->v5",
                    migrated != null && migrated.saveVersion == 5 &&
                    migrated.hearts == 2 && migrated.fruit == 7 && migrated.xp == 42 &&
                    migrated.learnedObjectIds.Contains("periuk"));
                Expect("field v5 default",
                    migrated != null && migrated.completedLessonIds != null &&
                    migrated.completedItemIds != null);
            }

            string temp = Path.Combine(Path.GetTempPath(),
                "UabMeto_SaveV5_" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                File.WriteAllText(temp, v4Json, new UTF8Encoding(false));
                MethodInfo tryLoad = typeof(SaveService).GetMethod("TryLoad",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                object[] args = { temp, null };
                SaveData loaded = tryLoad != null ? (SaveData)tryLoad.Invoke(service, args) : null;
                Expect("TryLoad v4 -> v5",
                    loaded != null && loaded.saveVersion == 5 && loaded.hearts == 2 &&
                    loaded.xp == 42 && loaded.completedLessonIds != null &&
                    loaded.completedItemIds != null);
            }
            finally
            {
                if (File.Exists(temp))
                {
                    File.Delete(temp);
                }
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    private static void CurriculumMappingSelfTest()
    {
        Expect("mapping salam", CurriculumMapping.ResolveTopic("salam") == LearningTopic.SalamSapaan);
        Expect("mapping keluarga", CurriculumMapping.ResolveTopic("keluarga") == LearningTopic.Keluarga);
        Expect("mapping numeric 12",
            CurriculumMapping.ResolveTopic("12") == LearningTopic.SekolahPekerjaanSosial);
        Expect("mapping unknown -> None", CurriculumMapping.ResolveTopic("tidak-ada") == LearningTopic.None);
        Expect("mapping order fallback", CurriculumMapping.ResolveTopic(new TopicData
        {
            id = "custom",
            order = 3
        }) == LearningTopic.Keluarga);

        TopicData titled = new TopicData
        {
            id = "salam",
            title = new LocalizedText { id = "Sapaan" }
        };
        Expect("display title", CurriculumMapping.TopicDisplayName(titled) == "Sapaan");

        LessonItemData vocabulary = new LessonItemData
        {
            id = "v1",
            type = "vocabulary",
            objectRef = "periuk"
        };
        LessonItemData exercise = new LessonItemData
        {
            id = "e1",
            type = "exercise",
            question = "Pilih?",
            options = new List<string> { "A", "B" }
        };
        LessonData lesson = new LessonData
        {
            id = "l1",
            items = new List<LessonItemData> { vocabulary, exercise }
        };
        Expect("count vocabulary", CurriculumMapping.VocabularyCount(lesson) == 1);
        Expect("lesson not complete", !CurriculumMapping.IsLessonComplete(lesson));

        string exerciseText = CurriculumMapping.ExerciseText(exercise);
        Expect("exercise text", exerciseText.Contains("Pilih?") && exerciseText.Contains("A") &&
                                  exerciseText.Contains("B"));
    }

    private static void CurriculumServiceSelfTest()
    {
        string resourcesDir = Path.Combine(Application.dataPath, "Resources");
        string curriculumDir = Path.Combine(resourcesDir, "Curriculum");
        string fixturePath = Path.Combine(curriculumDir, "curriculum.json");

        if (File.Exists(fixturePath))
        {
            Debug.Log("[CurriculumIntegration] NOT RUN service fixture (fixture produksi ada)");
            return;
        }

        LearningDatabase database = ScriptableObject.CreateInstance<LearningDatabase>();
        LearningObjectDefinition periuk = ScriptableObject.CreateInstance<LearningObjectDefinition>();
        periuk.id = "periuk";
        periuk.namaIndonesia = "Periuk";
        periuk.namaDawan = "Periuk";
        database.objects.Add(periuk);

        GameObject serviceObject = null;
        bool fixtureCreated = false;

        try
        {
            CurriculumLoader.ClearCache();
            serviceObject = new GameObject("CurriculumServiceTest");
            CurriculumService emptyService = serviceObject.AddComponent<CurriculumService>();
            emptyService.LoadOnce();
            Expect("service kosong loaded", emptyService.Loaded);
            Expect("service kosong tidak usable", !emptyService.HasContent && !emptyService.IsUsable);
            UnityEngine.Object.DestroyImmediate(serviceObject);

            Directory.CreateDirectory(curriculumDir);
            fixtureCreated = true;
            File.WriteAllBytes(fixturePath, CurriculumJson.SerializeUtf8(BuildFixture()));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            CurriculumLoader.ClearCache();

            serviceObject = new GameObject("CurriculumServiceTest");
            CurriculumService service = serviceObject.AddComponent<CurriculumService>();
            service.LoadOnce();
            Expect("service memuat fixture", service.HasContent && service.Topics.Count == 1);
            Expect("service filter DRAFT", service.FindTopic("draft") == null);
            Expect("service belum valid tanpa database", !service.IsUsable);

            service.BindDatabase(database);
            Expect("service valid dengan database", service.IsUsable && service.ReferencesValid);
            Expect("service resolve vocabulary",
                service.ResolveVocabulary("periuk") == periuk &&
                service.TryResolveVocabulary(BuildVocabularyItem(), out LearningObjectDefinition resolved) &&
                resolved == periuk);
            Expect("service resolve topic", service.ResolveTopic("salam") == LearningTopic.SalamSapaan);

            TopicData topic = service.FindTopic("salam");
            Expect("service find lesson",
                topic != null && service.FindLesson("salam", "salam-1") != null);

            RunTopicPanelSmoke(service, database);
            RunInteractivePanelsSmoke(service);
            RunProgressSmoke(service);

            LearningDatabase emptyDatabase = ScriptableObject.CreateInstance<LearningDatabase>();
            service.BindDatabase(emptyDatabase);
            Expect("service ref yatim menonaktifkan usable", !service.ReferencesValid && !service.IsUsable);
            Expect("service ref yatim tetap bisa render", service.CanRender);
            RunTopicPanelSmoke(service, database);
            UnityEngine.Object.DestroyImmediate(emptyDatabase);

            File.WriteAllText(fixturePath, "{ corrupt", new UTF8Encoding(false));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            service.Reload();
            Expect("service JSON rusak -> kosong", !service.HasContent && !service.IsUsable);
        }
        catch (Exception exception)
        {
            failures++;
            Debug.LogError("[CurriculumIntegration] exception: " + exception);
        }
        finally
        {
            CurriculumLoader.ClearCache();
            if (serviceObject != null)
            {
                UnityEngine.Object.DestroyImmediate(serviceObject);
            }

            if (fixtureCreated)
            {
                try
                {
                    AssetDatabase.DeleteAsset("Assets/Resources/Curriculum");
                }
                catch (Exception)
                {
                    // fallback
                }

                try
                {
                    if (Directory.Exists(curriculumDir))
                    {
                        Directory.Delete(curriculumDir, true);
                    }

                    string meta = curriculumDir + ".meta";
                    if (File.Exists(meta))
                    {
                        File.Delete(meta);
                    }

                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                }
                catch (Exception exception)
                {
                    Debug.LogError("[CurriculumIntegration] cleanup gagal: " + exception.Message);
                }
            }

            UnityEngine.Object.DestroyImmediate(periuk);
            UnityEngine.Object.DestroyImmediate(database);
        }

        Expect("service fixture self-cleaning",
            !File.Exists(fixturePath) && !Directory.Exists(curriculumDir));
    }

    private static void RunTopicPanelSmoke(CurriculumService service, LearningDatabase database)
    {
        Canvas canvas = null;
        GameObject panelRoot = null;
        try
        {
            canvas = UiFactory.CreateCanvas("CurriculumUiTestCanvas", 0);
            panelRoot = new GameObject("CurriculumUiTestPanel", typeof(RectTransform));
            panelRoot.transform.SetParent(canvas.transform, false);
            RectTransform rect = (RectTransform)panelRoot.transform;
            rect.sizeDelta = new Vector2(1560f, 878f);

            RectTransform content = UiFactory.CreateRect(panelRoot.transform, "Content",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            TopicPanel panel = panelRoot.AddComponent<TopicPanel>();
            panel.Build(panelRoot, content, null);
            panel.Show(database);

            Button[] buttons = panelRoot.GetComponentsInChildren<Button>(true);
            Button topicButton = null;
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].gameObject.name == "Bg")
                {
                    topicButton = buttons[i];
                    break;
                }
            }

            Expect("UI curriculum topic button", topicButton != null);
            if (topicButton != null)
            {
                topicButton.onClick.Invoke();
            }

            TextMeshProUGUI[] labels = panelRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
            bool hasDetail = false;
            for (int i = 0; i < labels.Length; i++)
            {
                if (labels[i] != null && labels[i].text != null &&
                    (labels[i].text.Contains("Salam") || labels[i].text.Contains("Sapaan")))
                {
                    hasDetail = true;
                    break;
                }
            }

            Expect("UI curriculum detail renders", hasDetail);
            panel.Close();
        }
        catch (Exception exception)
        {
            failures++;
            Debug.LogError("[CurriculumIntegration] UI exception: " + exception);
        }
        finally
        {
            if (panelRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(panelRoot);
            }

            if (canvas != null)
            {
                UnityEngine.Object.DestroyImmediate(canvas.gameObject);
            }
        }
    }

    private static void RunInteractivePanelsSmoke(CurriculumService service)
    {
        Canvas canvas = null;
        GameObject exerciseRoot = null;
        GameObject dialogueRoot = null;
        try
        {
            LessonData lesson = service.FindLesson("salam", "salam-1");
            if (lesson == null || lesson.items == null || lesson.items.Count < 3)
            {
                failures++;
                Debug.LogError("[CurriculumIntegration] interactive fixture tidak lengkap");
                return;
            }

            canvas = UiFactory.CreateCanvas("CurriculumInteractiveTestCanvas", 0);

            exerciseRoot = new GameObject("ExercisePanelTest", typeof(RectTransform));
            exerciseRoot.transform.SetParent(canvas.transform, false);
            TextMeshProUGUI exTitle = UiFactory.CreateLabel(exerciseRoot.transform, "Title", "Latihan", 30f,
                UiFactory.Ink, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(20f, -10f), new Vector2(1000f, 40f), true);
            TextMeshProUGUI exQuestion = UiFactory.CreateLabel(exerciseRoot.transform, "Question", "", 28f,
                UiFactory.Ink, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(20f, -60f), new Vector2(1000f, 60f));
            RectTransform exOptions = UiFactory.CreateRect(exerciseRoot.transform, "Options",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(20f, -130f), new Vector2(1100f, 300f));
            TextMeshProUGUI exFeedback = UiFactory.CreateLabel(exerciseRoot.transform, "Feedback", "", 24f,
                UiFactory.Ink, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(20f, -440f), new Vector2(1000f, 60f));
            Button exClose = UiFactory.CreateButton(exerciseRoot.transform, "CloseButton", "Tutup", null,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-20f, 20f), new Vector2(200f, 70f), Color.gray, Color.white);
            CurriculumExercisePanel exercise = exerciseRoot.AddComponent<CurriculumExercisePanel>();
            exercise.Build(exerciseRoot, exTitle, exQuestion, exOptions, exFeedback, exClose);
            exercise.Show(lesson.items[1]);

            Button correctOption = null;
            Button[] exButtons = exerciseRoot.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < exButtons.Length; i++)
            {
                if (exButtons[i] != null && exButtons[i].gameObject.name == "Option_0")
                {
                    correctOption = exButtons[i];
                    break;
                }
            }

            Expect("exercise option tersedia", correctOption != null);
            if (correctOption != null)
            {
                correctOption.onClick.Invoke();
            }

            Expect("exercise koreksi benar", exFeedback.text.Contains("Benar"));
            exercise.Close();

            dialogueRoot = new GameObject("DialoguePanelTest", typeof(RectTransform));
            dialogueRoot.transform.SetParent(canvas.transform, false);
            TextMeshProUGUI dlgTitle = UiFactory.CreateLabel(dialogueRoot.transform, "Title", "Dialog", 30f,
                UiFactory.Ink, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(20f, -10f), new Vector2(1000f, 40f), true);
            TextMeshProUGUI dlgBody = UiFactory.CreateLabel(dialogueRoot.transform, "Body", "", 28f,
                UiFactory.Ink, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(20f, -70f), new Vector2(1200f, 260f));
            TextMeshProUGUI dlgHint = UiFactory.CreateLabel(dialogueRoot.transform, "Hint", "", 20f,
                UiFactory.Ink, TextAlignmentOptions.MidlineRight, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 0f), new Vector2(-20f, 120f), new Vector2(300f, 30f));
            Button dlgNext = UiFactory.CreateButton(dialogueRoot.transform, "NextButton", "Lanjut", null,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-240f, 20f), new Vector2(200f, 70f), Color.green, Color.white);
            Button dlgClose = UiFactory.CreateButton(dialogueRoot.transform, "CloseButton", "Tutup", null,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-20f, 20f), new Vector2(200f, 70f), Color.gray, Color.white);
            Button dlgAudio = UiFactory.CreateButton(dialogueRoot.transform, "AudioButton", "Audio", null,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(20f, 20f), new Vector2(200f, 70f), Color.gray, Color.white);
            CurriculumDialoguePanel dialogue = dialogueRoot.AddComponent<CurriculumDialoguePanel>();
            dialogue.Build(dialogueRoot, dlgTitle, dlgBody, dlgHint, dlgNext, dlgClose, dlgAudio);
            dialogue.Show(lesson.items[2]);

            Expect("dialogue halaman pertama", dlgBody.text.Contains("Selamat pagi"));
            dialogue.Next();
            Expect("dialogue halaman kedua", dlgBody.text.Contains("juga"));
            dialogue.Close();
        }
        catch (Exception exception)
        {
            failures++;
            Debug.LogError("[CurriculumIntegration] interactive UI exception: " + exception);
        }
        finally
        {
            if (exerciseRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(exerciseRoot);
            }

            if (dialogueRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(dialogueRoot);
            }

            if (canvas != null)
            {
                UnityEngine.Object.DestroyImmediate(canvas.gameObject);
            }
        }
    }

    private static void RunProgressSmoke(CurriculumService service)
    {
        LessonData lesson = service.FindLesson("salam", "salam-1");
        if (lesson == null || lesson.items == null)
        {
            failures++;
            Debug.LogError("[CurriculumIntegration] progress fixture tidak lengkap");
            return;
        }

        GameState state = GameState.Ensure();
        List<string> learnedBefore = new List<string>(state.Data.learnedObjectIds);
        List<string> lessonsBefore = new List<string>(state.Data.completedLessonIds);
        List<string> itemsBefore = new List<string>(state.Data.completedItemIds);
        List<string> topicsBefore = new List<string>(state.Data.completedTopicIds);

        try
        {
            if (!state.Data.learnedObjectIds.Contains("periuk"))
            {
                state.Data.learnedObjectIds.Add("periuk");
            }

            LessonItemData exercise = null;
            LessonItemData dialogue = null;
            for (int i = 0; i < lesson.items.Count; i++)
            {
                if (lesson.items[i] != null && lesson.items[i].type == "exercise")
                {
                    exercise = lesson.items[i];
                }
                else if (lesson.items[i] != null && lesson.items[i].type == "dialogue")
                {
                    dialogue = lesson.items[i];
                }
            }

            Expect("progress item fixture", exercise != null && dialogue != null);
            service.MarkItemCompleted(exercise);
            service.MarkItemCompleted(dialogue);
            service.RefreshCompletion();

            Expect("progres item exercise tersimpan",
                exercise != null && state.Data.completedItemIds.Contains(exercise.id));
            Expect("progres item dialogue tersimpan",
                dialogue != null && state.Data.completedItemIds.Contains(dialogue.id));
            Expect("progres lesson tersimpan",
                state.Data.completedLessonIds.Contains(lesson.id));
            Expect("progres topic tersimpan",
                state.Data.completedTopicIds.Contains("salam"));
            Expect("lesson complete helper", CurriculumProgress.IsLessonCompleted(lesson));
        }
        finally
        {
            state.Data.learnedObjectIds.Clear();
            state.Data.learnedObjectIds.AddRange(learnedBefore);
            state.Data.completedLessonIds.Clear();
            state.Data.completedLessonIds.AddRange(lessonsBefore);
            state.Data.completedItemIds.Clear();
            state.Data.completedItemIds.AddRange(itemsBefore);
            state.Data.completedTopicIds.Clear();
            state.Data.completedTopicIds.AddRange(topicsBefore);
        }
    }

    private static LessonItemData BuildVocabularyItem()
    {
        return new LessonItemData
        {
            id = "salam-1-vocab",
            type = "vocabulary",
            objectRef = "periuk",
            validationStatus = ValidationStatus.VALIDATED,
            publicationStatus = PublicationStatus.PUBLISHED
        };
    }

    private static CurriculumRoot BuildFixture()
    {
        CurriculumRoot root = new CurriculumRoot
        {
            schemaVersion = CurriculumJson.SchemaVersion,
            locale = "id",
            topics = new List<TopicData>()
        };

        TopicData salam = new TopicData
        {
            id = "salam",
            order = 1,
            title = new LocalizedText { id = "Salam" },
            validationStatus = ValidationStatus.VALIDATED,
            publicationStatus = PublicationStatus.PUBLISHED,
            lessons = new List<LessonData>()
        };

        LessonData lesson = new LessonData
        {
            id = "salam-1",
            order = 1,
            title = new LocalizedText { id = "Sapaan dasar" },
            objective = new LocalizedText { id = "Menyapa" },
            skills = new List<string> { "membaca" },
            validationStatus = ValidationStatus.VALIDATED,
            publicationStatus = PublicationStatus.PUBLISHED,
            items = new List<LessonItemData>
            {
                BuildVocabularyItem(),
                new LessonItemData
                {
                    id = "salam-1-ex",
                    type = "exercise",
                    validationStatus = ValidationStatus.VALIDATED,
                    publicationStatus = PublicationStatus.PUBLISHED,
                    skill = "membaca",
                    question = "Pilih sapaan.",
                    options = new List<string> { "A", "B" },
                    correctIndex = 0
                },
                new LessonItemData
                {
                    id = "salam-1-dlg",
                    type = "dialogue",
                    validationStatus = ValidationStatus.VALIDATED,
                    publicationStatus = PublicationStatus.PUBLISHED,
                    lines = new List<DialogueLineData>
                    {
                        new DialogueLineData { speaker = "Pemandu", text = "Selamat pagi." },
                        new DialogueLineData { speaker = "Pemain", text = "Selamat pagi juga." }
                    }
                }
            }
        };
        salam.lessons.Add(lesson);
        root.topics.Add(salam);

        root.topics.Add(new TopicData
        {
            id = "draft",
            order = 2,
            title = new LocalizedText { id = "Draft" },
            validationStatus = ValidationStatus.UNVALIDATED,
            publicationStatus = PublicationStatus.UNPUBLISHED,
            lessons = new List<LessonData>
            {
                new LessonData
                {
                    id = "draft-1",
                    skills = new List<string> { "membaca" },
                    validationStatus = ValidationStatus.UNVALIDATED,
                    publicationStatus = PublicationStatus.UNPUBLISHED
                }
            }
        });

        return root;
    }
}
