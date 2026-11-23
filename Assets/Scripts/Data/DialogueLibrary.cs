/// <summary>
/// Kumpulan halaman dialog NPC berdasarkan progres pemain (Fase C).
///
/// Hanya memakai teks Indonesia yang sudah ada; tidak menambah frasa Dawan
/// baru. Materi dialog berbahasa Dawan menunggu validasi penutur (fase
/// "Persiapan validasi konten").
/// </summary>
public static class DialogueLibrary
{
    /// <summary>Halaman dialog pemandu sesuai progres. Null = pakai halaman lama.</summary>
    public static string[] PagesFor(ProgressService progress)
    {
        if (progress == null)
        {
            return null;
        }

        if (!progress.GuideTalked)
        {
            return new[]
            {
                "Selamat datang di kampung Uab Meto.",
                "Di sini kamu bisa belajar nama benda dalam bahasa Dawan.",
                "Mulai dari Dapur Bulat: baca semua benda, lalu jawab kuisnya.",
                "Rumah warga akan terbuka setelah kamu menyelesaikan Dapur.",
                "Kebun selalu terbuka. Buah di sana bisa memulihkan hati."
            };
        }

        if (!progress.DapurCompleted)
        {
            return new[]
            {
                "Dapur Bulat masih menunggu, pemuda.",
                "Dekati benda di dalam dapur, buka panelnya untuk membaca nama Dawan.",
                "Setelah semua benda dibaca, tombol Kuis akan aktif.",
                "Nilai 70 ke atas diperlukan untuk lulus. Semangat!"
            };
        }

        if (!progress.RumahCompleted)
        {
            return new[]
            {
                "Kabar baik: Rumah warga sekarang sudah terbuka.",
                "Di sana ada perabot rumah tangga untuk dipelajari.",
                "Jangan lupa membaca semua benda sebelum mengerjakan kuis.",
                "Kebun tetap terbuka bila kamu butuh buah untuk memulihkan hati."
            };
        }

        if (!progress.KebunCompleted)
        {
            return new[]
            {
                "Dapur dan Rumah sudah kamu selesaikan. Hebat!",
                "Sekarang jelajahi Kebun: ada tanaman dan alat kebun di sana.",
                "Lima buah di kebun bisa dipakai saat hati berkurang.",
                "Kumpulkan nilai terbaikmu lewat kuis di setiap lokasi."
            };
        }

        return new[]
        {
            "Semua lokasi sudah kamu jelajahi.",
            "Kamu bisa mengulang kuis untuk memperbaiki nilai.",
            "Buka panel Profil untuk melihat level, badge, dan papan skor.",
            "Teruslah berlatih menulis dan berbicara dalam bahasa Dawan."
        };
    }
}
