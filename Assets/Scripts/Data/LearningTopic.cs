using System;

/// <summary>
/// 12 topik kurikulum resmi (lihat docs/UAB_METO_MASTER_CONTEXT.md §6).
/// Nilai enum sengaja tetap 1..12 agar mudah dibaca di Inspector dan save.
/// </summary>
public enum LearningTopic
{
    None = 0,
    SalamSapaan = 1,
    PerkenalanDiri = 2,
    Keluarga = 3,
    AngkaWaktuHari = 4,
    AktivitasSehariHari = 5,
    MakananMinuman = 6,
    RumahBenda = 7,
    TempatArah = 8,
    BelanjaTawarMenawar = 9,
    BudayaAdat = 10,
    KesehatanDarurat = 11,
    SekolahPekerjaanSosial = 12
}

/// <summary>
/// Peran objek di dalam topik. Pendukung tidak dihitung sebagai kosakata inti baru.
/// </summary>
public enum LearningRole
{
    Utama = 0,
    Pendukung = 1
}

/// <summary>
/// Katalog topik: nama tampilan + kode stabil (dipakai crossTopics di aset).
/// </summary>
public static class LearningTopicCatalog
{
    public sealed class Entry
    {
        public LearningTopic Topic;
        public string Code;
        public string Nama;
    }

    public static readonly Entry[] All =
    {
        new Entry { Topic = LearningTopic.SalamSapaan, Code = "salam", Nama = "Salam dan sapaan" },
        new Entry { Topic = LearningTopic.PerkenalanDiri, Code = "perkenalan", Nama = "Perkenalan diri" },
        new Entry { Topic = LearningTopic.Keluarga, Code = "keluarga", Nama = "Keluarga" },
        new Entry { Topic = LearningTopic.AngkaWaktuHari, Code = "angka", Nama = "Angka, waktu dan hari" },
        new Entry { Topic = LearningTopic.AktivitasSehariHari, Code = "aktivitas", Nama = "Aktivitas sehari-hari" },
        new Entry { Topic = LearningTopic.MakananMinuman, Code = "makanan", Nama = "Makanan dan minuman" },
        new Entry { Topic = LearningTopic.RumahBenda, Code = "rumah", Nama = "Rumah dan benda" },
        new Entry { Topic = LearningTopic.TempatArah, Code = "tempat", Nama = "Tempat dan arah" },
        new Entry { Topic = LearningTopic.BelanjaTawarMenawar, Code = "belanja", Nama = "Belanja dan tawar-menawar" },
        new Entry { Topic = LearningTopic.BudayaAdat, Code = "budaya", Nama = "Budaya dan adat Dawan" },
        new Entry { Topic = LearningTopic.KesehatanDarurat, Code = "kesehatan", Nama = "Kesehatan dan keadaan darurat" },
        new Entry { Topic = LearningTopic.SekolahPekerjaanSosial, Code = "pekerjaan", Nama = "Sekolah, pekerjaan dan kehidupan sosial" }
    };

    public static Entry Get(LearningTopic topic)
    {
        for (int i = 0; i < All.Length; i++)
        {
            if (All[i].Topic == topic)
            {
                return All[i];
            }
        }

        return null;
    }

    public static string NamaOf(LearningTopic topic)
    {
        Entry entry = Get(topic);
        return entry != null ? entry.Nama : "Topik belum dipetakan";
    }

    public static string CodeOf(LearningTopic topic)
    {
        Entry entry = Get(topic);
        return entry != null ? entry.Code : string.Empty;
    }

    public static LearningTopic FromCode(string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            return LearningTopic.None;
        }

        for (int i = 0; i < All.Length; i++)
        {
            if (string.Equals(All[i].Code, code, StringComparison.Ordinal))
            {
                return All[i].Topic;
            }
        }

        return LearningTopic.None;
    }
}
