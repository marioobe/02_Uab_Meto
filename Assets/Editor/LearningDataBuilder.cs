using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Membuat aset LearningObjectDefinition + LearningDatabase.
/// Kosakata Dawan berasal dari lembar validasi TTU (validator: Ibu Palmerya Sau,
/// Desa Sekon, Kec. Insana, 19/11/2026). Soal kosakata Dawan dibuat otomatis
/// dari data tersebut dengan pengecoh objek lain di scene yang sama.
/// </summary>
public static class LearningDataBuilder
{
    private const string DataFolder = "Assets/Data/Learning";
    private const string DatabasePath = DataFolder + "/LearningDatabase.asset";
    private const string ValidatorName = "Ibu Palmerya Sau";
    private const string ValidationDate = "19/11/2026";
    private const string ValidatorNotes =
        "Divalidasi penutur asli dialek TTU (Desa Sekon, Kec. Insana). " +
        "Teks di luar kurung adalah cara tulis; di dalam kurung adalah cara baca/dialek.";
    private const string DraftNotes =
        "Menunggu validasi penutur asli dialek TTU. Jangan dipakai sebagai materi final.";

    private sealed class DawanSpec
    {
        public string Name;
        public string Reading;
        public ValidationStatus Status = ValidationStatus.VALIDATED;
    }

    private sealed class QuestionSpec
    {
        public string Question;
        public string[] Options;
        public int CorrectIndex;
        public string Explanation;
    }

    private sealed class ObjectSpec
    {
        public string Id;
        public string Scene;
        public string Category;
        public string Indonesia;
        public string Explanation;
        public QuestionSpec[] Questions;
    }

    [MenuItem("UAB Meto/2 - Build Learning Data", priority = 2)]
    public static LearningDatabase BuildAll()
    {
        Directory.CreateDirectory(DataFolder);
        DawanAudioImporter.EnsureImported();

        LearningDatabase database = AssetDatabase.LoadAssetAtPath<LearningDatabase>(DatabasePath);
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<LearningDatabase>();
            AssetDatabase.CreateAsset(database, DatabasePath);
        }

        database.objects.Clear();
        ObjectSpec[] specs = CreateSpecs();

        for (int i = 0; i < specs.Length; i++)
        {
            ObjectSpec spec = specs[i];
            string folder = $"{DataFolder}/{spec.Scene}";
            Directory.CreateDirectory(folder);
            string path = $"{folder}/{spec.Id}.asset";

            LearningObjectDefinition definition =
                AssetDatabase.LoadAssetAtPath<LearningObjectDefinition>(path);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<LearningObjectDefinition>();
                AssetDatabase.CreateAsset(definition, path);
            }

            DawanSpec dawan;
            if (!DawanWords.TryGetValue(spec.Id, out dawan))
            {
                dawan = new DawanSpec { Name = string.Empty, Status = ValidationStatus.DRAFT };
            }

            bool validated = dawan.Status == ValidationStatus.VALIDATED &&
                             !string.IsNullOrWhiteSpace(dawan.Name);

            definition.id = spec.Id;
            definition.sceneKey = spec.Scene;
            definition.category = spec.Category;
            definition.namaIndonesia = spec.Indonesia;
            definition.namaDawan = dawan.Name ?? string.Empty;
            definition.caraBaca = dawan.Reading ?? string.Empty;
            definition.validationStatus = validated ? ValidationStatus.VALIDATED : ValidationStatus.DRAFT;
            definition.validator = validated ? ValidatorName : string.Empty;
            definition.validationDate = validated ? ValidationDate : string.Empty;
            definition.validationNotes = validated ? ValidatorNotes : DraftNotes;
            definition.explanation = spec.Explanation;
            // Gambar objek memakai aset transparan baru bila tersedia; jika belum,
            // jatuh ke potongan latar lama agar panel info tidak kosong.
            definition.image = AssetDatabase.LoadAssetAtPath<Sprite>(
                AsetTransparan23Importer.ObjectSpritePath(spec.Scene, spec.Id));
            if (definition.image == null)
            {
                definition.image = AssetDatabase.LoadAssetAtPath<Sprite>(
                    GameArtImporter.CropSpritePath(spec.Scene, spec.Id));
            }
            definition.pronunciation = DawanAudioImporter.LoadClip(spec.Id);

            definition.questions.Clear();
            for (int q = 0; q < spec.Questions.Length; q++)
            {
                QuestionSpec question = spec.Questions[q];
                definition.questions.Add(new QuizQuestion
                {
                    question = question.Question,
                    options = question.Options,
                    correctIndex = question.CorrectIndex,
                    explanation = question.Explanation,
                    usesDawan = false
                });
            }

            AddDawanQuestions(definition, spec, specs, dawan);

            EditorUtility.SetDirty(definition);
            database.objects.Add(definition);
        }

        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"LearningDataBuilder selesai: {database.objects.Count} objek, {specs.Length} spesifikasi.");
        return database;
    }

    private static QuestionSpec Q(string question, string correct, string[] wrong, string explanation)
    {
        string[] options = new string[wrong.Length + 1];
        int correctIndex = (StableHash(question + correct) & 0x7FFFFFFF) % options.Length;
        options[correctIndex] = correct;

        int w = 0;
        for (int i = 0; i < options.Length; i++)
        {
            if (i == correctIndex)
            {
                continue;
            }

            options[i] = wrong[w];
            w++;
        }

        return new QuestionSpec
        {
            Question = question,
            Options = options,
            CorrectIndex = correctIndex,
            Explanation = explanation
        };
    }

    /// <summary>Kosakata hasil validasi penutur asli, 19/11/2026 (dialek TTU).</summary>
    private static readonly Dictionary<string, DawanSpec> DawanWords =
        new Dictionary<string, DawanSpec>
        {
            // Dapur Bulat
            { "periuk", Word("Na'i") },
            { "tungku", Word("Tunaf") },
            { "kayu_bakar", Word("Hau otu", "hau outu") },
            { "jagung", Word("Pena") },
            { "tikar", Word("Na'he") },
            { "tempayan", Word("Na'i naijan", "nai najan") },
            { "bakul", Word("Oko") },
            { "alat_masak", Word("Na'i Hana", "nai hana") },
            { "cobek", Word("Fatu bena", "faot bena") },
            { "bangku", Word("Bena tuka", "ben tuka") },

            // Rumah Warga
            { "tempat_tidur", Word("hala") },
            { "selimut_tenun", Word("Beti/Tais", "Betei/tais") },
            { "lemari", Word("lemari") },
            { "peti_kayu", Word("Peti", "Pet'tei") },
            { "rak_piring", Word("Pika Bale", "piak bale") },
            { "meja_makan", Word("Mei/mej") },
            { "tikar_rumah", new DawanSpec
                {
                    Name = "Na'he",
                    Reading = string.Empty,
                    Status = ValidationStatus.DRAFT
                }
            },
            { "tempayan_rumah", Word("Na'i najan", "nai najan") },
            { "keranjang", Word("Oko suli") },
            { "bangku_rumah", Word("toko") },

            // Kebun
            { "pohon_jeruk", Word("Hau lelo") },
            { "pohon_kelapa", Word("Hau noah") },
            { "pohon_mangga", Word("Hau upun") },
            { "kebun_sayur", Word("Lele utan", "lel utan") },
            { "tomat", Word("Kaliman") },
            { "cabai", Word("Unus") },
            { "terong", Word("Kaut") },
            { "bakul_hasil", Word("Oko") },
            { "sumur", Word("Oe mata", "oe mat'a") },
            { "alat_kebun", Word("Besi tofa", "bese tofa") },
            { "bibit", Word("fini") },
            { "daun_pisang", Word("Uki no'o", "uik no'o") }
        };

    private static DawanSpec Word(string name, string reading = null)
    {
        return new DawanSpec
        {
            Name = name,
            Reading = reading ?? string.Empty,
            Status = ValidationStatus.VALIDATED
        };
    }

    /// <summary>
    /// Membuat soal kosakata Dawan dua arah untuk objek yang sudah valid:
    /// Indonesia -> Dawan dan Dawan -> Indonesia. Pengecoh diambil dari objek lain
    /// di scene yang sama agar tetap berkaitan dengan materi yang dipelajari.
    /// </summary>
    private static void AddDawanQuestions(LearningObjectDefinition definition, ObjectSpec spec,
        ObjectSpec[] all, DawanSpec own)
    {
        if (own == null || own.Status != ValidationStatus.VALIDATED ||
            string.IsNullOrWhiteSpace(own.Name))
        {
            return;
        }

        // Kata yang sama dengan bahasa Indonesia (mis. "lemari") tidak dibuat soal
        // karena jawabannya terlalu jelas; soal fungsi objek tetap tersedia.
        if (string.Equals(own.Name.Trim(), spec.Indonesia.Trim(),
                System.StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        List<string> dawanPool = new List<string>();
        List<string> indonesiaPool = new List<string>();

        for (int i = 0; i < all.Length; i++)
        {
            ObjectSpec other = all[i];
            if (ReferenceEquals(other, spec) || other.Scene != spec.Scene)
            {
                continue;
            }

            DawanSpec otherDawan;
            if (!DawanWords.TryGetValue(other.Id, out otherDawan) ||
                otherDawan.Status != ValidationStatus.VALIDATED ||
                string.IsNullOrWhiteSpace(otherDawan.Name))
            {
                continue;
            }

            if (string.Equals(otherDawan.Name.Trim(), own.Name.Trim(),
                    System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            dawanPool.Add(otherDawan.Name.Trim());
            indonesiaPool.Add(other.Indonesia);
        }

        string reading = string.IsNullOrWhiteSpace(own.Reading)
            ? string.Empty
            : $" (dibaca: {own.Reading.Trim()})";
        string dawanName = own.Name.Trim();

        string[] dawanDistractors = PickDistractors(dawanPool, dawanName, spec.Id + "d", 2);
        if (dawanDistractors != null)
        {
            QuestionSpec question = Q(
                $"Apa bahasa Dawan untuk {spec.Indonesia}?",
                dawanName,
                dawanDistractors,
                $"Dalam bahasa Dawan, {spec.Indonesia.ToLowerInvariant()} disebut {dawanName}{reading}.");
            definition.questions.Add(ToQuizQuestion(question, true));
        }

        string[] indonesiaDistractors = PickDistractors(indonesiaPool, spec.Indonesia, spec.Id + "i", 2);
        if (indonesiaDistractors != null)
        {
            QuestionSpec meaning = Q(
                $"Apa arti kata Dawan \"{dawanName}\"?",
                spec.Indonesia,
                indonesiaDistractors,
                $"Kata Dawan {dawanName} berarti {spec.Indonesia.ToLowerInvariant()}{reading}.");
            definition.questions.Add(ToQuizQuestion(meaning, true));
        }
    }

    private static QuizQuestion ToQuizQuestion(QuestionSpec spec, bool usesDawan)
    {
        return new QuizQuestion
        {
            question = spec.Question,
            options = spec.Options,
            correctIndex = spec.CorrectIndex,
            explanation = spec.Explanation,
            usesDawan = usesDawan
        };
    }

    private static string[] PickDistractors(List<string> pool, string correct, string seed, int count)
    {
        List<string> unique = new List<string>();
        for (int i = 0; i < pool.Count; i++)
        {
            string candidate = pool[i].Trim();
            if (candidate.Length == 0 ||
                string.Equals(candidate, correct.Trim(), System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            bool exists = false;
            for (int u = 0; u < unique.Count; u++)
            {
                if (string.Equals(unique[u], candidate, System.StringComparison.OrdinalIgnoreCase))
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                unique.Add(candidate);
            }
        }

        if (unique.Count < count)
        {
            return null;
        }

        unique.Sort((a, b) => StableHash(seed + a).CompareTo(StableHash(seed + b)));
        return unique.GetRange(0, count).ToArray();
    }

    private static int StableHash(string value)
    {
        unchecked
        {
            int hash = 17;
            for (int i = 0; i < value.Length; i++)
            {
                hash = hash * 31 + value[i];
            }

            return hash;
        }
    }

    private static ObjectSpec[] CreateSpecs()
    {
        return new[]
        {
            // ------------------------------------------------------------ DAPUR
            new ObjectSpec
            {
                Id = "periuk", Scene = "Dapur", Category = "Alat masak",
                Indonesia = "Periuk",
                Explanation = "Periuk adalah wadah logam untuk merebus air, nasi, atau sayur di atas tungku.",
                Questions = new[]
                {
                    Q("Periuk dipakai untuk apa?", "Merebus makanan",
                        new[] { "Memotong kayu", "Menyimpan pakaian" },
                        "Periuk dipakai untuk merebus makanan di atas tungku."),
                    Q("Periuk diletakkan di atas ...", "tungku",
                        new[] { "tempat tidur", "lemari" },
                        "Tungku menjadi tempat api untuk memanaskan periuk.")
                }
            },
            new ObjectSpec
            {
                Id = "tungku", Scene = "Dapur", Category = "Alat masak",
                Indonesia = "Tungku batu",
                Explanation = "Tungku adalah tempat menyalakan api untuk memasak. Di Dapur Bulat, tungku dibuat dari batu.",
                Questions = new[]
                {
                    Q("Apa bahan tungku di Dapur Bulat?", "Batu",
                        new[] { "Kain", "Kaca" },
                        "Tungku di Dapur Bulat tersusun dari batu."),
                    Q("Api di tungku digunakan untuk ...", "memasak",
                        new[] { "menjahit", "menulis" },
                        "Api tungku dipakai untuk memasak makanan.")
                }
            },
            new ObjectSpec
            {
                Id = "kayu_bakar", Scene = "Dapur", Category = "Bahan bakar",
                Indonesia = "Kayu bakar",
                Explanation = "Kayu bakar adalah ranting dan batang kering yang dibakar di tungku.",
                Questions = new[]
                {
                    Q("Kayu bakar dipakai untuk ...", "menyalakan api",
                        new[] { "menyeduh teh", "menyiram tanaman" },
                        "Kayu bakar menjadi bahan bakar api tungku."),
                    Q("Kayu bakar sebaiknya disimpan ...", "di tempat kering",
                        new[] { "di dalam air", "di atas kompor menyala" },
                        "Kayu kering lebih mudah menyala dan aman disimpan.")
                }
            },
            new ObjectSpec
            {
                Id = "jagung", Scene = "Dapur", Category = "Hasil kebun",
                Indonesia = "Jagung",
                Explanation = "Jagung digantung di atas tungku agar kering dan tahan lama.",
                Questions = new[]
                {
                    Q("Mengapa jagung digantung di atas tungku?", "Agar kering dan tahan lama",
                        new[] { "Agar berwarna biru", "Agar bisa terbang" },
                        "Asap dan panas tungku membantu jagung mengering."),
                    Q("Jagung adalah contoh ...", "hasil kebun",
                        new[] { "alat masak", "perabot rumah" },
                        "Jagung dipanen dari kebun.")
                }
            },
            new ObjectSpec
            {
                Id = "tikar", Scene = "Dapur", Category = "Perabot",
                Indonesia = "Tikar",
                Explanation = "Tikar adalah anyaman daun atau serat yang dipakai untuk duduk dan tidur.",
                Questions = new[]
                {
                    Q("Tikar terbuat dari ...", "anyaman serat",
                        new[] { "besi", "kaca" },
                        "Tikar dibuat dari daun atau serat yang dianyam."),
                    Q("Tikar biasanya dipakai untuk ...", "duduk",
                        new[] { "memotong kayu", "menyimpan air" },
                        "Tikar menjadi alas duduk dan tidur.")
                }
            },
            new ObjectSpec
            {
                Id = "tempayan", Scene = "Dapur", Category = "Wadah",
                Indonesia = "Tempayan",
                Explanation = "Tempayan adalah wadah tanah liat besar untuk menyimpan air atau hasil panen.",
                Questions = new[]
                {
                    Q("Tempayan biasanya menyimpan ...", "air atau hasil panen",
                        new[] { "pakaian basah", "alat elektronik" },
                        "Tempayan besar dipakai menyimpan air atau hasil panen."),
                    Q("Tempayan terbuat dari ...", "tanah liat",
                        new[] { "logam tipis", "plastik bening" },
                        "Tempayan adalah wadah tanah liat.")
                }
            },
            new ObjectSpec
            {
                Id = "bakul", Scene = "Dapur", Category = "Wadah",
                Indonesia = "Bakul",
                Explanation = "Bakul adalah keranjang anyaman untuk membawa hasil kebun.",
                Questions = new[]
                {
                    Q("Bakul dipakai untuk ...", "membawa hasil kebun",
                        new[] { "memasak nasi", "menyalakan api" },
                        "Bakul anyaman memudahkan membawa hasil kebun."),
                    Q("Bakul dibuat dengan cara ...", "dianyam",
                        new[] { "ditempa", "dicetak" },
                        "Bakul dibuat dari anyaman daun atau bambu.")
                }
            },
            new ObjectSpec
            {
                Id = "alat_masak", Scene = "Dapur", Category = "Alat",
                Indonesia = "Alat masak",
                Explanation = "Alat masak seperti sendok, sodet, dan panci digantung di rak dapur.",
                Questions = new[]
                {
                    Q("Alat masak disimpan di ...", "rak dapur",
                        new[] { "kolong tempat tidur", "atas pagar" },
                        "Rak dapur membuat alat masak rapi dan mudah diambil."),
                    Q("Contoh alat masak adalah ...", "sodet",
                        new[] { "cangkul", "sapu lidi" },
                        "Sodet dipakai mengaduk masakan.")
                }
            },
            new ObjectSpec
            {
                Id = "cobek", Scene = "Dapur", Category = "Alat",
                Indonesia = "Cobek",
                Explanation = "Cobek adalah batu cekung untuk menumbuk atau menghaluskan bumbu.",
                Questions = new[]
                {
                    Q("Cobek dipakai untuk ...", "menghaluskan bumbu",
                        new[] { "memotong kayu", "menyimpan air" },
                        "Cobek dan ulekan dipakai menghaluskan bumbu."),
                    Q("Cobek terbuat dari ...", "batu",
                        new[] { "kain", "kertas" },
                        "Cobek dibuat dari batu cekung.")
                }
            },
            new ObjectSpec
            {
                Id = "bangku", Scene = "Dapur", Category = "Perabot",
                Indonesia = "Bangku kayu",
                Explanation = "Bangku kayu adalah tempat duduk rendah saat memasak atau berkumpul.",
                Questions = new[]
                {
                    Q("Bangku kayu dipakai untuk ...", "duduk",
                        new[] { "memasak", "menyimpan air" },
                        "Bangku kayu menjadi tempat duduk."),
                    Q("Bangku di dapur terbuat dari ...", "kayu",
                        new[] { "kaca", "besi" },
                        "Bangku dapur dibuat dari kayu.")
                }
            },

            // ------------------------------------------------------------ RUMAH
            new ObjectSpec
            {
                Id = "tempat_tidur", Scene = "Rumah", Category = "Perabot",
                Indonesia = "Tempat tidur",
                Explanation = "Tempat tidur adalah tempat beristirahat di dalam rumah.",
                Questions = new[]
                {
                    Q("Tempat tidur dipakai untuk ...", "beristirahat",
                        new[] { "memasak", "menyimpan alat kebun" },
                        "Tempat tidur dipakai untuk tidur dan beristirahat."),
                    Q("Tempat tidur biasanya diletakkan di ...", "kamar",
                        new[] { "sumur", "kebun" },
                        "Kamar adalah ruang untuk tempat tidur.")
                }
            },
            new ObjectSpec
            {
                Id = "selimut_tenun", Scene = "Rumah", Category = "Kain",
                Indonesia = "Selimut tenun",
                Explanation = "Selimut tenun dibuat dari kain bermotif dan dipakai saat tidur agar hangat.",
                Questions = new[]
                {
                    Q("Selimut tenun dipakai agar ...", "hangat",
                        new[] { "basah", "terbang" },
                        "Selimut menjaga tubuh tetap hangat."),
                    Q("Selimut tenun memiliki ...", "motif",
                        new[] { "mesin", "layar" },
                        "Kain tenun dihiasi motif khas.")
                }
            },
            new ObjectSpec
            {
                Id = "lemari", Scene = "Rumah", Category = "Perabot",
                Indonesia = "Lemari",
                Explanation = "Lemari adalah tempat menyimpan pakaian dan barang rumah tangga.",
                Questions = new[]
                {
                    Q("Lemari dipakai untuk ...", "menyimpan pakaian",
                        new[] { "memasak air", "menanam sayur" },
                        "Lemari menyimpan pakaian dan barang agar rapi."),
                    Q("Lemari di rumah biasanya memiliki ...", "pintu",
                        new[] { "roda", "layar" },
                        "Lemari memiliki pintu untuk membuka dan menutup.")
                }
            },
            new ObjectSpec
            {
                Id = "peti_kayu", Scene = "Rumah", Category = "Perabot",
                Indonesia = "Peti kayu",
                Explanation = "Peti kayu adalah kotak penyimpanan barang berharga.",
                Questions = new[]
                {
                    Q("Peti kayu dipakai untuk ...", "menyimpan barang",
                        new[] { "menumbuk bumbu", "menggoreng" },
                        "Peti kayu menyimpan barang berharga."),
                    Q("Peti kayu terbuat dari ...", "kayu",
                        new[] { "kaca", "tanah liat" },
                        "Peti dibuat dari papan kayu.")
                }
            },
            new ObjectSpec
            {
                Id = "rak_piring", Scene = "Rumah", Category = "Perabot",
                Indonesia = "Rak piring",
                Explanation = "Rak piring menyimpan piring, mangkuk, dan cangkir agar rapi.",
                Questions = new[]
                {
                    Q("Rak piring menyimpan ...", "piring dan mangkuk",
                        new[] { "kayu bakar", "alas kaki" },
                        "Rak piring menampung peralatan makan."),
                    Q("Rak piring biasanya ditempatkan di ...", "dapur atau ruang makan",
                        new[] { "sumur", "jalan kampung" },
                        "Rak piring dekat tempat makan.")
                }
            },
            new ObjectSpec
            {
                Id = "meja_makan", Scene = "Rumah", Category = "Perabot",
                Indonesia = "Meja makan",
                Explanation = "Meja makan adalah meja panjang tempat keluarga makan bersama.",
                Questions = new[]
                {
                    Q("Meja makan dipakai untuk ...", "makan bersama",
                        new[] { "mencuci pakaian", "menyimpan air" },
                        "Keluarga berkumpul di meja makan."),
                    Q("Meja makan berbentuk ...", "panjang",
                        new[] { "bulat sempurna", "seperti bola" },
                        "Meja makan di rumah ini berbentuk panjang.")
                }
            },
            new ObjectSpec
            {
                Id = "tikar_rumah", Scene = "Rumah", Category = "Perabot",
                Indonesia = "Tikar",
                Explanation = "Tikar di rumah dipakai untuk duduk dan berbincang.",
                Questions = new[]
                {
                    Q("Tikar di rumah dipakai untuk ...", "duduk",
                        new[] { "memotong", "mencangkul" },
                        "Tikar menjadi alas duduk saat berkumpul."),
                    Q("Tikar dibuat dari bahan ...", "anyaman",
                        new[] { "logam", "kaca" },
                        "Tikar berasal dari anyaman serat.")
                }
            },
            new ObjectSpec
            {
                Id = "tempayan_rumah", Scene = "Rumah", Category = "Wadah",
                Indonesia = "Tempayan",
                Explanation = "Tempayan besar menyimpan air minum di rumah.",
                Questions = new[]
                {
                    Q("Tempayan di rumah menyimpan ...", "air minum",
                        new[] { "alat tulis", "batu" },
                        "Tempayan menjadi wadah air minum."),
                    Q("Tempayan ditutup agar ...", "air tetap bersih",
                        new[] { "air menjadi panas", "tempayan terbang" },
                        "Tutup melindungi air dari debu.")
                }
            },
            new ObjectSpec
            {
                Id = "keranjang", Scene = "Rumah", Category = "Wadah",
                Indonesia = "Keranjang",
                Explanation = "Keranjang anyaman dipakai membawa dan menyimpan hasil kebun.",
                Questions = new[]
                {
                    Q("Keranjang dipakai membawa ...", "hasil kebun",
                        new[] { "api", "air panas" },
                        "Keranjang anyaman membawa hasil kebun."),
                    Q("Keranjang dibuat dengan cara ...", "dianyam",
                        new[] { "dilas", "ditempa" },
                        "Keranjang berasal dari anyaman.")
                }
            },
            new ObjectSpec
            {
                Id = "bangku_rumah", Scene = "Rumah", Category = "Perabot",
                Indonesia = "Bangku",
                Explanation = "Bangku kayu tempat duduk saat berkumpul di rumah.",
                Questions = new[]
                {
                    Q("Bangku di rumah dipakai untuk ...", "duduk",
                        new[] { "memasak", "menyimpan piring" },
                        "Bangku menjadi tempat duduk."),
                    Q("Bangku di rumah terbuat dari ...", "kayu",
                        new[] { "kain", "kaca" },
                        "Bangku dibuat dari kayu.")
                }
            },

            // ------------------------------------------------------------ KEBUN
            new ObjectSpec
            {
                Id = "pohon_jeruk", Scene = "Kebun", Category = "Tanaman",
                Indonesia = "Pohon jeruk",
                Explanation = "Pohon jeruk menghasilkan buah jeruk. Buah ini bisa dimakan dan menambah hati.",
                Questions = new[]
                {
                    Q("Pohon jeruk menghasilkan ...", "buah jeruk",
                        new[] { "padi", "kayu bakar" },
                        "Jeruk tumbuh di pohon jeruk."),
                    Q("Buah jeruk bisa dipakai untuk ...", "menambah hati",
                        new[] { "menyalakan api", "membuat pagar" },
                        "Buah di kebun memulihkan satu hati.")
                }
            },
            new ObjectSpec
            {
                Id = "pohon_kelapa", Scene = "Kebun", Category = "Tanaman",
                Indonesia = "Pohon kelapa",
                Explanation = "Pohon kelapa tumbuh tinggi dan berdaun panjang. Buahnya disebut kelapa.",
                Questions = new[]
                {
                    Q("Daun pohon kelapa berbentuk ...", "panjang",
                        new[] { "jarum kecil", "bulat kecil" },
                        "Daun kelapa panjang dan menyirip."),
                    Q("Pohon kelapa tumbuh ...", "tinggi",
                        new[] { "di dalam air", "di bawah batu" },
                        "Batang kelapa menjulang tinggi.")
                }
            },
            new ObjectSpec
            {
                Id = "pohon_mangga", Scene = "Kebun", Category = "Tanaman",
                Indonesia = "Pohon mangga",
                Explanation = "Pohon mangga berbuah manis dan rindang.",
                Questions = new[]
                {
                    Q("Buah pohon mangga adalah ...", "mangga",
                        new[] { "jeruk", "tomat" },
                        "Pohon mangga menghasilkan mangga."),
                    Q("Pohon mangga memberi ...", "keteduhan",
                        new[] { "api", "batu" },
                        "Daunnya rindang sehingga memberi keteduhan.")
                }
            },
            new ObjectSpec
            {
                Id = "kebun_sayur", Scene = "Kebun", Category = "Tanaman",
                Indonesia = "Kebun sayur",
                Explanation = "Kebun sayur adalah petak tanah tempat menanam sayuran.",
                Questions = new[]
                {
                    Q("Kebun sayur ditanami ...", "sayuran",
                        new[] { "batu", "pakaian" },
                        "Petak tanah ditanami sayur."),
                    Q("Sebelum menanam, tanah perlu ...", "digemburkan",
                        new[] { "dicat", "dibekukan" },
                        "Tanah gembur memudahkan akar tumbuh.")
                }
            },
            new ObjectSpec
            {
                Id = "tomat", Scene = "Kebun", Category = "Tanaman",
                Indonesia = "Tomat",
                Explanation = "Tomat berbuah merah dan dipakai untuk masakan.",
                Questions = new[]
                {
                    Q("Warna buah tomat yang matang adalah ...", "merah",
                        new[] { "biru", "hitam" },
                        "Tomat matang berwarna merah."),
                    Q("Tomat biasanya dipakai untuk ...", "masakan",
                        new[] { "bahan bakar", "pakaian" },
                        "Tomat menjadi bahan masakan.")
                }
            },
            new ObjectSpec
            {
                Id = "cabai", Scene = "Kebun", Category = "Tanaman",
                Indonesia = "Cabai",
                Explanation = "Cabai adalah tanaman buah kecil yang terasa pedas.",
                Questions = new[]
                {
                    Q("Rasa cabai adalah ...", "pedas",
                        new[] { "manis", "asin" },
                        "Cabai memberi rasa pedas."),
                    Q("Cabai yang matang berwarna ...", "merah",
                        new[] { "hitam", "biru" },
                        "Cabai matang umumnya merah.")
                }
            },
            new ObjectSpec
            {
                Id = "terong", Scene = "Kebun", Category = "Tanaman",
                Indonesia = "Terong",
                Explanation = "Terong adalah sayuran berwarna ungu atau hijau.",
                Questions = new[]
                {
                    Q("Warna terong yang umum adalah ...", "ungu",
                        new[] { "merah menyala", "biru" },
                        "Terong sering berwarna ungu."),
                    Q("Terong termasuk ...", "sayuran",
                        new[] { "alat masak", "perabot" },
                        "Terong dimasak sebagai sayuran.")
                }
            },
            new ObjectSpec
            {
                Id = "bakul_hasil", Scene = "Kebun", Category = "Alat",
                Indonesia = "Bakul hasil panen",
                Explanation = "Bakul dipakai mengumpulkan hasil panen dari kebun.",
                Questions = new[]
                {
                    Q("Bakul hasil panen dipakai untuk ...", "mengumpulkan hasil kebun",
                        new[] { "memasak", "menanam bibit" },
                        "Bakul menampung hasil panen."),
                    Q("Bakul hasil panen diletakkan di ...", "kebun",
                        new[] { "sumur", "atap rumah" },
                        "Bakul dipakai saat memanen di kebun.")
                }
            },
            new ObjectSpec
            {
                Id = "sumur", Scene = "Kebun", Category = "Fasilitas",
                Indonesia = "Sumur",
                Explanation = "Sumur adalah sumber air untuk menyiram tanaman dan kebutuhan rumah.",
                Questions = new[]
                {
                    Q("Sumur menghasilkan ...", "air",
                        new[] { "api", "tanah kering" },
                        "Sumur menjadi sumber air."),
                    Q("Air sumur dipakai untuk ...", "menyiram tanaman",
                        new[] { "menyalakan lampu", "membuat kain" },
                        "Air sumur menyiram tanaman dan kebutuhan rumah.")
                }
            },
            new ObjectSpec
            {
                Id = "alat_kebun", Scene = "Kebun", Category = "Alat",
                Indonesia = "Alat kebun",
                Explanation = "Cangkul, sekop, dan sabit dipakai merawat kebun.",
                Questions = new[]
                {
                    Q("Cangkul dipakai untuk ...", "menggemburkan tanah",
                        new[] { "memasak", "menjahit" },
                        "Cangkul menggemburkan tanah."),
                    Q("Alat kebun disimpan di ...", "gudang alat",
                        new[] { "tempat tidur", "meja makan" },
                        "Gudang alat menjaga alat tetap aman.")
                }
            },
            new ObjectSpec
            {
                Id = "bibit", Scene = "Kebun", Category = "Tanaman",
                Indonesia = "Bibit tanaman",
                Explanation = "Bibit adalah tanaman muda yang disiapkan untuk ditanam di kebun.",
                Questions = new[]
                {
                    Q("Bibit ditanam agar menjadi ...", "tanaman dewasa",
                        new[] { "batu", "pagar" },
                        "Bibit tumbuh menjadi tanaman dewasa."),
                    Q("Bibit sebaiknya disiram ...", "secara teratur",
                        new[] { "dengan api", "sekali saja seumur hidup" },
                        "Bibit perlu air yang cukup dan teratur.")
                }
            },
            new ObjectSpec
            {
                Id = "daun_pisang", Scene = "Kebun", Category = "Tanaman",
                Indonesia = "Daun pisang",
                Explanation = "Daun pisang lebar dan panjang, sering dipakai sebagai pembungkus makanan.",
                Questions = new[]
                {
                    Q("Daun pisang berbentuk ...", "lebar dan panjang",
                        new[] { "kecil dan keras", "berduri" },
                        "Daun pisang lebar dan panjang."),
                    Q("Daun pisang sering dipakai untuk ...", "membungkus makanan",
                        new[] { "menyalakan api", "membuat kaca" },
                        "Daun pisang menjadi pembungkus alami.")
                }
            }
        };
    }
}
