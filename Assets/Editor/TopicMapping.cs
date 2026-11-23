/// <summary>
/// Satu-satunya sumber kebenaran pemetaan 32 objek ke 12 topik (Fase A).
/// Sesuai docs/PROJECT_GAP_ANALYSIS.md §5. Hanya metadata yang ditulis:
/// topicId, role, crossTopics, order. Konten pembelajaran tidak disentuh.
/// </summary>
public static class TopicMapping
{
    public sealed class Entry
    {
        public string Id;
        public LearningTopic Topic;
        public LearningRole Role;
        public string[] CrossTopicCodes;
        public int Order;
    }

    private static Entry E(string id, LearningTopic topic, LearningRole role, int order,
        params string[] crossTopics)
    {
        return new Entry
        {
            Id = id,
            Topic = topic,
            Role = role,
            CrossTopicCodes = crossTopics ?? new string[0],
            Order = order
        };
    }

    public static readonly Entry[] Entries =
    {
        // 5. Aktivitas sehari-hari (5 utama)
        E("periuk", LearningTopic.AktivitasSehariHari, LearningRole.Utama, 1),
        E("tungku", LearningTopic.AktivitasSehariHari, LearningRole.Utama, 2),
        E("kayu_bakar", LearningTopic.AktivitasSehariHari, LearningRole.Utama, 3),
        E("alat_masak", LearningTopic.AktivitasSehariHari, LearningRole.Utama, 4),
        E("cobek", LearningTopic.AktivitasSehariHari, LearningRole.Utama, 5, "budaya"),

        // 6. Makanan dan minuman (4 utama, 2 pendukung)
        E("jagung", LearningTopic.MakananMinuman, LearningRole.Utama, 1),
        E("tomat", LearningTopic.MakananMinuman, LearningRole.Utama, 2),
        E("cabai", LearningTopic.MakananMinuman, LearningRole.Utama, 3),
        E("terong", LearningTopic.MakananMinuman, LearningRole.Utama, 4),
        E("daun_pisang", LearningTopic.MakananMinuman, LearningRole.Pendukung, 5, "budaya", "aktivitas"),
        E("bakul_hasil", LearningTopic.MakananMinuman, LearningRole.Pendukung, 6, "pekerjaan"),

        // 7. Rumah dan benda (11 utama, 2 pendukung)
        E("tempat_tidur", LearningTopic.RumahBenda, LearningRole.Utama, 1),
        E("lemari", LearningTopic.RumahBenda, LearningRole.Utama, 2),
        E("peti_kayu", LearningTopic.RumahBenda, LearningRole.Utama, 3),
        E("rak_piring", LearningTopic.RumahBenda, LearningRole.Utama, 4),
        E("meja_makan", LearningTopic.RumahBenda, LearningRole.Utama, 5),
        E("bangku_rumah", LearningTopic.RumahBenda, LearningRole.Utama, 6),
        E("bangku", LearningTopic.RumahBenda, LearningRole.Utama, 7),
        E("tempayan", LearningTopic.RumahBenda, LearningRole.Utama, 8),
        E("tempayan_rumah", LearningTopic.RumahBenda, LearningRole.Pendukung, 9),
        E("keranjang", LearningTopic.RumahBenda, LearningRole.Utama, 10, "belanja"),
        E("bakul", LearningTopic.RumahBenda, LearningRole.Utama, 11, "belanja"),
        E("tikar", LearningTopic.RumahBenda, LearningRole.Utama, 12, "budaya"),
        E("tikar_rumah", LearningTopic.RumahBenda, LearningRole.Pendukung, 13),

        // 8. Tempat dan arah (1 utama)
        E("sumur", LearningTopic.TempatArah, LearningRole.Utama, 1),

        // 10. Budaya dan adat Dawan (1 utama)
        E("selimut_tenun", LearningTopic.BudayaAdat, LearningRole.Utama, 1),

        // 12. Sekolah, pekerjaan dan kehidupan sosial (6 utama)
        E("pohon_jeruk", LearningTopic.SekolahPekerjaanSosial, LearningRole.Utama, 1),
        E("pohon_kelapa", LearningTopic.SekolahPekerjaanSosial, LearningRole.Utama, 2),
        E("pohon_mangga", LearningTopic.SekolahPekerjaanSosial, LearningRole.Utama, 3),
        E("kebun_sayur", LearningTopic.SekolahPekerjaanSosial, LearningRole.Utama, 4, "tempat"),
        E("bibit", LearningTopic.SekolahPekerjaanSosial, LearningRole.Utama, 5),
        E("alat_kebun", LearningTopic.SekolahPekerjaanSosial, LearningRole.Utama, 6)
    };

    public static bool TryGet(string id, out Entry entry)
    {
        for (int i = 0; i < Entries.Length; i++)
        {
            if (Entries[i].Id == id)
            {
                entry = Entries[i];
                return true;
            }
        }

        entry = null;
        return false;
    }
}
