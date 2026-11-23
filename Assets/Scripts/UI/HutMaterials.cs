using UnityEngine;

/// <summary>
/// Materi belajar tiap gubuk buku. Ditulis di satu tempat supaya teks di desa
/// dan di dalam gubuk selalu sama; ubah di sini tanpa membangun ulang scene.
/// </summary>
public readonly struct HutMaterial
{
    public readonly string Title;
    public readonly string Body;

    public HutMaterial(string title, string body)
    {
        Title = title;
        Body = body;
    }
}

public static class HutMaterials
{
    // Tiap paragraf dipisah baris kosong = satu halaman dialog.
    public static readonly HutMaterial[] All =
    {
        new HutMaterial(
            "Ume Kbubu",
            "Ume kbubu adalah rumah bulat warga desa. Dindingnya dari batu dan tanah liat, atapnya dari alang-alang yang tebal.\n\n" +
            "Rumah ini hampir tanpa jendela. Asap tungku di tengah ruangan menghangatkan badan dan membuat atap lebih awet.\n\n" +
            "Di dalamnya keluarga berkumpul, menyimpan jagung, dan menerima tamu. Pintunya rendah agar setiap orang masuk dengan menunduk."),

        new HutMaterial(
            "Lopo",
            "Lopo adalah lumbung padi yang berdiri di atas tiang kayu. Lantainya tinggi supaya hasil panen tetap kering dan aman dari air.\n\n" +
            "Jagung dan padi disimpan di dalam lopo. Tiang yang bulat dan licin membuat tikus sulit memanjat.\n\n" +
            "Banyaknya lopo menunjukkan hasil kebun sebuah keluarga. Lopo juga dipakai menyimpan benih untuk musim tanam berikutnya."),

        new HutMaterial(
            "Kebun & Pangan",
            "Warga menanam jagung, ubi kayu, kacang, dan sayur di kebun dekat rumah. Ada juga pisang dan buah-buahan di pekarangan.\n\n" +
            "Tanaman ditanam berselang-seling supaya tanah tidak cepat habis dan serangga pengganggu berkurang.\n\n" +
            "Hasil kebun dimakan sendiri, dijual ke pasar, atau disimpan untuk musim kering. Sisa daun dan batang dijadikan pakan ternak."),

        new HutMaterial(
            "Musyawarah Adat",
            "Keputusan penting desa diambil lewat musyawarah. Tetua adat dan warga duduk bersama membicarakan masalah yang ada.\n\n" +
            "Semua orang boleh berbicara. Keputusan diambil dengan hati yang sama supaya tidak ada yang merasa ditinggalkan.\n\n" +
            "Rasa hormat kepada tetua dan tamu ditunjukkan dengan sapaan \"Tabe\" sambil sedikit menundukkan kepala."),

        new HutMaterial(
            "Mata Air & Hutan",
            "Hutan di sekitar desa menjaga mata air tetap mengalir. Akar pohon menahan air hujan masuk ke dalam tanah.\n\n" +
            "Warga tidak menebang pohon di sekitar mata air. Airnya dipakai untuk minum, memasak, dan mengairi kebun.\n\n" +
            "Kalau hutan rusak, mata air bisa kering dan panen gagal. Karena itu hutan dijaga bersama-sama."),
    };

    public static HutMaterial Get(int index)
    {
        if (All.Length == 0)
        {
            return new HutMaterial("Gubuk Buku", "Materi belum diisi.");
        }

        int safe = Mathf.Clamp(index, 0, All.Length - 1);
        return All[safe];
    }
}
