using System;
using System.Collections.Generic;
using System.Text;

public enum WritingExerciseType
{
    SusunKata = 0,
    LengkapiKalimat = 1
}

/// <summary>Satu latihan menulis yang dihasilkan dari kosakata Dawan tervalidasi.</summary>
public sealed class WritingExercise
{
    public string Key;
    public WritingExerciseType Type;
    public string ObjectId;
    public string Instruction;
    public string Question;
    public string Correct;
    public List<string> Tiles = new List<string>();
    public List<string> Options = new List<string>();
    public string Explanation;
}

/// <summary>
/// Membuat latihan menulis dari objek yang SUDAH divalidasi (namaDawan tersedia).
/// Tidak mengubah aset/soal dan tidak menambah kosakata Dawan baru.
///
/// Dua tipe:
///  - SusunKata: hanya untuk namaDawan ≥ 2 kata (kata diacak).
///  - LengkapiKalimat: bank 4 kata, pengecoh unik setelah normalisasi.
/// </summary>
public static class WritingExerciseGenerator
{
    public static WritingExercise BuildFor(LearningObjectDefinition definition, LearningDatabase database)
    {
        if (!IsUsable(definition))
        {
            return null;
        }

        List<string> tokens = Tokens(definition.namaDawan);
        if (tokens.Count >= 2)
        {
            return BuildSusunKata(definition, tokens);
        }

        return BuildLengkapiKalimat(definition, database);
    }

    /// <summary>Semua latihan yang bisa dibuat (untuk kebutuhan masa depan / audit).</summary>
    public static List<WritingExercise> BuildAll(LearningDatabase database)
    {
        List<WritingExercise> result = new List<WritingExercise>();
        if (database == null)
        {
            return result;
        }

        for (int i = 0; i < database.objects.Count; i++)
        {
            WritingExercise exercise = BuildFor(database.objects[i], database);
            if (exercise != null)
            {
                result.Add(exercise);
            }
        }

        return result;
    }

    public static bool IsUsable(LearningObjectDefinition definition)
    {
        return definition != null &&
               definition.IsValidated &&
               !string.IsNullOrWhiteSpace(definition.namaDawan);
    }

    /// <summary>Normalisasi untuk anti-ambigu: huruf/angka kecil saja.</summary>
    public static string Normalize(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder(value.Length);
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (char.IsLetterOrDigit(c))
            {
                builder.Append(char.ToLowerInvariant(c));
            }
        }

        return builder.ToString();
    }

    public static List<string> Tokens(string dawan)
    {
        List<string> tokens = new List<string>();
        if (string.IsNullOrWhiteSpace(dawan))
        {
            return tokens;
        }

        string[] parts = dawan.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            tokens.Add(parts[i]);
        }

        return tokens;
    }

    // ---------------------------------------------------------------- tipe

    private static WritingExercise BuildSusunKata(LearningObjectDefinition definition, List<string> tokens)
    {
        WritingExercise exercise = new WritingExercise
        {
            Type = WritingExerciseType.SusunKata,
            ObjectId = definition.id,
            Instruction = "Ketuk kata sesuai urutan yang benar.",
            Question = $"Susun kata Dawan untuk: {definition.namaIndonesia}",
            Correct = definition.namaDawan,
            Explanation = definition.explanation
        };
        exercise.Key = BuildKey(exercise.Type, definition);

        System.Random random = new System.Random(Environment.TickCount);
        do
        {
            exercise.Tiles.Clear();
            exercise.Tiles.AddRange(tokens);
            Shuffle(exercise.Tiles, random);
        }
        while (exercise.Tiles.Count > 1 && SameOrder(exercise.Tiles, tokens));

        return exercise;
    }

    private static WritingExercise BuildLengkapiKalimat(LearningObjectDefinition definition,
        LearningDatabase database)
    {
        WritingExercise exercise = new WritingExercise
        {
            Type = WritingExerciseType.LengkapiKalimat,
            ObjectId = definition.id,
            Instruction = "Pilih kata Dawan yang tepat untuk melengkapi kalimat.",
            Question = $"Bahasa Dawan untuk {definition.namaIndonesia} adalah ___",
            Correct = definition.namaDawan,
            Explanation = definition.explanation
        };
        exercise.Key = BuildKey(exercise.Type, definition);

        string correctNorm = Normalize(definition.namaDawan);
        HashSet<string> used = new HashSet<string> { correctNorm };
        System.Random random = new System.Random(Environment.TickCount);

        // Pengecoh: objek valid lain, unik setelah normalisasi, utamakan satu scene.
        List<string> sameScene = CollectDistractors(database, definition, used, true);
        List<string> otherScene = CollectDistractors(database, definition, used, false);
        sameScene.AddRange(otherScene);
        Shuffle(sameScene, random);

        exercise.Options.Add(definition.namaDawan);
        for (int i = 0; i < sameScene.Count && exercise.Options.Count < 4; i++)
        {
            exercise.Options.Add(sameScene[i]);
        }

        Shuffle(exercise.Options, random);
        return exercise;
    }

    private static List<string> CollectDistractors(LearningDatabase database,
        LearningObjectDefinition definition, HashSet<string> used, bool sameSceneOnly)
    {
        List<string> result = new List<string>();
        if (database == null || database.objects == null)
        {
            return result;
        }

        for (int i = 0; i < database.objects.Count; i++)
        {
            LearningObjectDefinition candidate = database.objects[i];
            if (candidate == null || candidate == definition || !IsUsable(candidate))
            {
                continue;
            }

            if (sameSceneOnly && candidate.sceneKey != definition.sceneKey)
            {
                continue;
            }

            if (!sameSceneOnly && candidate.sceneKey == definition.sceneKey)
            {
                continue;
            }

            string normalized = Normalize(candidate.namaDawan);
            if (string.IsNullOrEmpty(normalized) || !used.Add(normalized))
            {
                continue;
            }

            result.Add(candidate.namaDawan);
        }

        return result;
    }

    private static string BuildKey(WritingExerciseType type, LearningObjectDefinition definition)
    {
        return $"w:{(int)type}:{definition.id}:{GamificationService.Hash64(definition.namaDawan)}";
    }

    private static bool SameOrder(List<string> a, List<string> b)
    {
        if (a.Count != b.Count)
        {
            return false;
        }

        for (int i = 0; i < a.Count; i++)
        {
            if (a[i] != b[i])
            {
                return false;
            }
        }

        return true;
    }

    private static void Shuffle<T>(List<T> list, System.Random random)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}
