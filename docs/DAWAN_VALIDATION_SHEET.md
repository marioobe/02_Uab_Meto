# Lembar Validasi Kosakata Dawan (Dialek TTU)

Lembar ini sudah **diisi dan divalidasi** oleh penutur asli. Data di bawah menjadi sumber
`LearningDataBuilder` untuk nama Dawan, status validasi, audio pelafalan, dan soal kuis
kosakata Dawan.

- **Validator:** Ibu Palmerya Sau
- **Asal:** Desa Sekon, Kec. Insana, Timor Tengah Utara (TTU)
- **Tanggal validasi:** 19/11/2026
- **Hasil:** 31 objek `VALID`, 1 objek `DRAF` (`tikar_rumah`)
- **Audio:** 31 rekaman penutur di `docs/rekaman penutur/` (nama berkas = id objek)

> Catatan validator: teks **di luar** tanda kurung adalah cara tulis; teks **di dalam**
> tanda kurung adalah cara baca/dialek. `tikar_rumah` sengaja tetap `DRAF` karena sama
> dengan `tikar`.

## Daftar materi (32 objek)

### Dapur Bulat (10)
| No | id | Kategori | Nama Indonesia | Nama Dawan | Cara baca | Status | Audio |
|----|----|----------|----------------|------------|-----------|--------|-------|
| 1 | periuk | Alat masak | Periuk | Na'i | — | VALID | ada |
| 2 | tungku | Alat masak | Tungku batu | Tunaf | — | VALID | ada |
| 3 | kayu_bakar | Bahan bakar | Kayu bakar | Hau otu | hau outu | VALID | ada |
| 4 | jagung | Hasil kebun | Jagung | Pena | — | VALID | ada |
| 5 | tikar | Perabot | Tikar | Na'he | — | VALID | ada |
| 6 | tempayan | Wadah | Tempayan | Na'i naijan | nai najan | VALID | ada |
| 7 | bakul | Wadah | Bakul | Oko | — | VALID | ada |
| 8 | alat_masak | Alat | Alat masak | Na'i Hana | nai hana | VALID | ada |
| 9 | cobek | Alat | Cobek | Fatu bena | faot bena | VALID | ada |
| 10 | bangku | Perabot | Bangku kayu | Bena tuka | ben tuka | VALID | ada |

### Rumah Warga (10)
| No | id | Kategori | Nama Indonesia | Nama Dawan | Cara baca | Status | Audio |
|----|----|----------|----------------|------------|-----------|--------|-------|
| 11 | tempat_tidur | Perabot | Tempat tidur | hala | — | VALID | ada |
| 12 | selimut_tenun | Kain | Selimut tenun | Beti/Tais | Betei/tais | VALID | ada |
| 13 | lemari | Perabot | Lemari | lemari | — | VALID | ada |
| 14 | peti_kayu | Perabot | Peti kayu | Peti | Pet'tei | VALID | ada |
| 15 | rak_piring | Perabot | Rak piring | Pika Bale | piak bale | VALID | ada |
| 16 | meja_makan | Perabot | Meja makan | Mei/mej | — | VALID | ada |
| 17 | tikar_rumah | Perabot | Tikar | Na'he | — | DRAF | tidak |
| 18 | tempayan_rumah | Wadah | Tempayan | Na'i najan | nai najan | VALID | ada |
| 19 | keranjang | Wadah | Keranjang | Oko suli | — | VALID | ada |
| 20 | bangku_rumah | Perabot | Bangku | toko | — | VALID | ada |

### Kebun (12)
| No | id | Kategori | Nama Indonesia | Nama Dawan | Cara baca | Status | Audio |
|----|----|----------|----------------|------------|-----------|--------|-------|
| 21 | pohon_jeruk | Tanaman | Pohon jeruk | Hau lelo | — | VALID | ada |
| 22 | pohon_kelapa | Tanaman | Pohon kelapa | Hau noah | — | VALID | ada |
| 23 | pohon_mangga | Tanaman | Pohon mangga | Hau upun | — | VALID | ada |
| 24 | kebun_sayur | Tanaman | Kebun sayur | Lele utan | lel utan | VALID | ada |
| 25 | tomat | Tanaman | Tomat | Kaliman | — | VALID | ada |
| 26 | cabai | Tanaman | Cabai | Unus | — | VALID | ada |
| 27 | terong | Tanaman | Terong | Kaut | — | VALID | ada |
| 28 | bakul_hasil | Alat | Bakul hasil panen | Oko | — | VALID | ada |
| 29 | sumur | Fasilitas | Sumur | Oe mata | oe mat'a | VALID | ada |
| 30 | alat_kebun | Alat | Alat kebun | Besi tofa | bese tofa | VALID | ada |
| 31 | bibit | Tanaman | Bibit tanaman | fini | — | VALID | ada |
| 32 | daun_pisang | Tanaman | Daun pisang | Uki no'o | uik no'o | VALID | ada |

## Cara data dipakai aplikasi

1. `LearningDataBuilder` mengisi `namaDawan`, `caraBaca`, `validationStatus`, `validator`,
   `validationDate`, dan `pronunciation` pada tiap aset `Assets/Data/Learning/<Scene>/<id>.asset`.
2. Rekaman penutur berformat Ogg Opus dikonversi ke WAV PCM di `Assets/Audio/Dawan/`
   memakai `Tools/ConvertDawanAudio.py`. Berkas asli di `docs/rekaman penutur/` tidak diubah.
3. Soal kosakata Dawan dibuat otomatis **dua arah** untuk objek `VALID`:
   - “Apa bahasa Dawan untuk <Indonesia>?” (opsi kata Dawan)
   - “Apa arti kata Dawan "<Dawan>"?” (opsi nama Indonesia)
   Pengecoh diambil dari objek lain di scene yang sama. Objek dengan kata Dawan sama
   dengan bahasa Indonesia (`lemari`) tidak dibuatkan soal kata.
4. `tikar_rumah` tetap `DRAF`: nama Dawan tampil dengan penanda DRAF, tanpa audio, dan
   tanpa soal Dawan.

> Aturan proyek: **tidak mengarang kosakata Dawan**. Perubahan kosakata harus berasal
> dari penutur/validator TTU lalu dibangun ulang lewat menu Unity.
