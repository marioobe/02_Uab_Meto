# Uab Meto — Alur Aplikasi (Dokumen Sumber Presentasi)

> Dokumen ini dibuat sebagai **bahan sumber untuk NotebookLM** agar dapat menyusun
> slide presentasi peluncuran aplikasi **Uab Meto** di SMP Putri, 24 November 2026.
> Semua angka dan nama di bawah ini sudah sesuai kondisi aplikasi saat ini.
> Gunakan dokumen ini apa adanya; jangan menambah data di luar dokumen.

---

## 1. Ringkasan

**Uab Meto** adalah game petualangan edukatif 2D untuk belajar **bahasa Dawan**
(bahasa daerah Timor, variasi Timor Tengah Utara). Pemain mengendalikan seorang
karakter, berjalan menjelajahi kampung, masuk ke rumah warga, lalu mempelajari
nama benda dalam bahasa Indonesia dan bahasa Dawan.

Pembelajaran tidak disajikan seperti buku teks, tetapi lewat kegiatan bermain:
mendekati benda, membaca namanya, mendengarkan pelafalan, berlatih menulis dan
berbicara, lalu mengerjakan kuis untuk membuka lokasi berikutnya.

Aplikasi dibuat dengan **Unity 6**, berjalan di **Android**, dapat dipakai
**tanpa internet**, dan menyimpan progres pemain secara otomatis.

---

## 2. Fakta kunci (angka yang boleh disebut)

| Aspek | Nilai |
|---|---|
| Jenis aplikasi | Game petualangan edukatif 2D |
| Bahasa yang dipelajari | Bahasa Dawan (Uab Meto), variasi TTU |
| Platform | Android |
| Teknologi | Unity 6 (6000.6.0f1) |
| Lokasi bermain | 1 peta kampung + 3 lokasi (Dapur, Rumah, Kebun) |
| Objek belajar | **32 objek** (Dapur 10, Rumah 10, Kebun 12) |
| Soal kuis | **124 soal** |
| Topik kurikulum | **12 topik**; 6 topik sudah berisi, 6 topik "Segera Hadir" |
| Kuis per sesi | 5 soal acak |
| Nilai kelulusan | **70 dari 100** |
| Hati (nyawa) | Maksimal 3 |
| Keterampilan | Mendengar, membaca, menulis, berbicara |
| Penyimpanan progres | Otomatis, di perangkat (offline) |
| Akses | Tanpa akun, tanpa login |
| Ukuran pemasangan | Sekitar 97 MB (versi universal) |

Catatan angka: 32 objek berasal dari 32 aset data belajar; bila menyebut jumlah
berkas data, totalnya 33 aset (32 objek + 1 database). Untuk slide, cukup
sebut **32 objek belajar**.

---

## 3. Masalah yang ingin dijawab

1. Bahasa daerah makin jarang dipakai oleh anak muda.
2. Belajar dari daftar kata terasa membosankan dan cepat dilupakan.
3. Media belajar bahasa Dawan yang interaktif masih sedikit.
4. Anak lebih tertarik pada game daripada buku pelajaran.

**Jawaban Uab Meto:** belajar bahasa Dawan lewat pengalaman bermain yang
menyenangkan, dengan konteks benda sehari-hari yang benar-benar ada di rumah
dan kebun warga Timor.

---

## 4. Apa itu Uab Meto

Uab Meto dibangun di atas tiga kegiatan yang saling terhubung:

| Pilar | Isi |
|---|---|
| **Jelajah** | Pemain berjalan di kampung, masuk ke Dapur, Rumah, dan Kebun. |
| **Belajar** | Mendekati benda, membaca nama Indonesia + Dawan, mendengar pelafalan. |
| **Uji** | Mengerjakan kuis, mendapat nilai, membuka lokasi berikutnya. |

Ditambah dua pendukung: **latihan** (menyusun kata dan melengkapi kalimat) dan
**bicara** (merekam suara lalu memutar ulang).

---

## 5. Alur pemain (langkah demi langkah)

1. **Buka aplikasi.** Muncul layar pembuka, lalu pemain berdiri di gerbang
   kampung (peta utama).
2. **Kenali kontrol.** Pemain menggerakkan karakter dengan tombol arah (D-Pad)
   atau joystick di layar. Kamera mengikuti karakter.
3. **Pilih tujuan.** Dari peta kampung ada tiga tujuan: **Dapur**, **Rumah**,
   dan **Kebun**. Pemain berjalan ke pintu, lalu menekan tombol **Masuk**.
4. **Dekati benda.** Di dalam lokasi ada banyak benda (periuk, tikar, jagung,
   pohon kelapa, dan seterusnya). Saat pemain mendekat, muncul lingkaran cahaya
   dan tombol **Baca**.
5. **Baca dan dengarkan.** Panel info terbuka dan menampilkan: gambar benda,
   nama Indonesia, nama Dawan, cara baca, penjelasan singkat, dan tombol audio
   untuk mendengar pelafalan.
6. **Tandai dipelajari.** Setiap benda yang dibaca tercatat sebagai "dipelajari".
7. **Berlatih.** Dari panel yang sama pemain bisa membuka **Latihan** (susun kata
   atau lengkapi kalimat) dan **Bicara** (rekam suara, dengarkan ulang, hapus).
8. **Kerjakan kuis.** Setelah benda di lokasi dipelajari, pemain membuka kuis:
   5 soal acak tentang benda yang sudah dibaca.
9. **Dapatkan nilai.** Nilai dihitung 0-100. Lulus bila mencapai **70**.
   Jawaban salah mengurangi satu hati. Bila hati habis, muncul panel pemulihan.
10. **Buka lokasi berikutnya.** Setelah lulus kuis Dapur, **Rumah** terbuka.
    Pemain kembali ke peta kampung dan melanjutkan petualangan.
11. **Kumpulkan buah.** Di Kebun ada buah yang bisa diambil untuk memulihkan hati.
12. **Lihat progres.** Menu **Profil** menampilkan level, XP, streak, badge, dan
    papan peringkat lokal.
13. **Baca Buku.** Menu **Buku** menampilkan 12 topik kurikulum beserta status
    materi yang sudah dipelajari.
14. **Keluar dengan aman.** Progres tersimpan otomatis, jadi pemain bisa
    melanjutkan lain waktu.

---

## 6. Alur scene (peta perjalanan)

```
             Base_Map (Kampung)
                    |
      +-------------+-------------+
      |             |             |
   Dapur          Kebun         Rumah
  (terbuka)     (terbuka)   (terkunci di awal)
      |
      |  lulus kuis Dapur (nilai >= 70)
      v
   Rumah terbuka
```

| Lokasi | Isi | Kapan terbuka |
|---|---|---|
| **Base_Map** | Kampung, jalan, pintu menuju 3 lokasi | Sejak awal |
| **Dapur** | 10 objek (alat masak, periuk, tikar, jagung, dll.) | Sejak awal |
| **Kebun** | 12 objek (pohon, sayur, sumur, alat kebun) + buah | Sejak awal |
| **Rumah** | 10 objek (meja, lemari, tempat tidur, selimut tenun) | Setelah lulus kuis Dapur |

Alasan Rumah dikunci: pemain belajar lebih dulu di Dapur, lalu menerapkan
kosakata yang sudah dikenal pada benda di dalam rumah. Kebun sengaja dibuka
sejak awal agar pemain selalu punya sumber buah untuk memulihkan hati.

---

## 7. Isi lokasi dan contoh kosakata

### Dapur (10 objek)
Alat masak, bakul, bangku kayu, cobek, jagung, kayu bakar, periuk, tempayan,
tikar, tungku batu.

Contoh: **jagung = Pena**, **tikar = Na'he**, **alat masak = Na'i Hana**.

### Rumah (10 objek)
Bangku, keranjang, lemari, meja makan, peti kayu, rak piring, selimut tenun,
tempat tidur, tempayan, tikar.

Contoh: **selimut tenun = Beti/Tais**, **rak piring = Pika Bale**,
**tempat tidur = hala**.

### Kebun (12 objek)
Alat kebun, bakul hasil panen, bibit tanaman, cabai, daun pisang, kebun sayur,
pohon jeruk, pohon kelapa, pohon mangga, sumur, terong, tomat.

Contoh: **cabai = Unus**, **tomat = Kaliman**, **sumur = Oe mata**,
**pohon kelapa = Hau noah**.

---

## 8. Layar dan fitur

| Layar / fitur | Fungsi |
|---|---|
| **HUD** | Menampilkan hati (maksimal 3), jumlah buah, level dan XP, serta tombol Profil, Buku, dan Atur. |
| **Kontrol sentuh** | D-Pad atau joystick (bisa dipilih di Pengaturan); mendukung gerak sambil menekan tombol aksi. |
| **Panel info** | Gambar benda, nama Indonesia, nama Dawan, cara baca, status validasi, penjelasan, tombol Dengarkan, Kuis, Latihan, Bicara, dan Tutup. |
| **Panel kuis** | 5 soal pilihan ganda, umpan balik jawaban, nilai 0-100, status Lulus/Belum lulus, dan daftar "pelajari ulang". |
| **Panel latihan** | Menyusun kata dan melengkapi kalimat. |
| **Panel bicara** | Merekam suara maksimal 10 detik, memutar ulang, menghapus; rekaman tidak diunggah. |
| **Panel pemulihan** | Muncul saat hati habis; mengarahkan pemain memakai buah. |
| **Buku** | 12 topik kurikulum; 6 topik berisi materi, 6 topik bertanda "Segera Hadir". |
| **Profil** | Level, XP menuju level berikut, streak, 9 badge, dan papan peringkat lokal (10 teratas). |
| **Pengaturan** | Pilih D-Pad atau joystick, atur suara, dan lainnya. |
| **Dialog pemandu** | NPC di kampung memberi sapaan yang berubah sesuai progres pemain. |

---

## 9. Kuis, nilai, dan hati

- Setiap sesi kuis berisi **5 soal acak**.
- Soal diambil dari objek yang sudah dibaca, bukan seluruh isi lokasi.
- Soal mengutamakan kosakata Dawan, dua arah: Indonesia ke Dawan dan Dawan ke Indonesia.
- Pengecoh (pilihan salah) diambil dari benda lain di lokasi yang sama, sehingga
  pilihan tetap masuk akal.
- Nilai = jumlah benar / jumlah soal x 100.
- **Lulus jika nilai 70 atau lebih.**
- Jawaban salah mengurangi **1 hati**. Hati maksimal **3**.
- Bila hati habis, kuis diblokir sementara dan pemain diarahkan memakai buah.
- Setelah lulus, muncul panel "Lulus!" dengan statistik, lalu progres disimpan.

---

## 10. Gamifikasi

| Elemen | Penjelasan |
|---|---|
| **Hati** | Maksimal 3; berkurang saat jawaban salah. |
| **Buah** | Diambil di Kebun; dipakai untuk menambah hati. |
| **XP** | Didapat dari aktivitas belajar; ditampilkan di HUD. |
| **Level** | Naik setelah XP mencapai batas tertentu. |
| **Streak** | Mencatat kebiasaan bermain berturut-turut. |
| **Badge** | 9 badge yang terbuka sesuai pencapaian. |
| **Papan peringkat** | Lokal di perangkat; 10 teratas ditampilkan. |

Pembukaan lokasi ditentukan oleh **kelulusan kuis**, bukan hanya jumlah XP.

---

## 11. Empat keterampilan belajar

| Keterampilan | Bentuk di aplikasi |
|---|---|
| **Mendengar** | Tombol audio memutar pelafalan kata Dawan. |
| **Membaca** | Nama Indonesia + nama Dawan + cara baca + penjelasan benda. |
| **Menulis** | Latihan menyusun kata dan melengkapi kalimat. |
| **Berbicara** | Rekam suara maksimal 10 detik, lalu putar ulang dan bandingkan. |

Keempatnya menyatu dalam satu alur, bukan empat menu terpisah.

---

## 12. Teknologi dan penyimpanan

- Dibuat dengan **Unity 6**, dibangun untuk **Android**.
- Mendukung perangkat 64-bit dan 32-bit melalui satu APK universal.
- Berjalan **offline**: materi, audio, dan kuis sudah ada di dalam aplikasi.
- **Tanpa akun**: pemain langsung bisa bermain.
- Progres disimpan otomatis dalam berkas `save.json` di perangkat, dengan
  salinan cadangan dan pemulihan bila berkas rusak.
- Materi belajar disimpan sebagai data terpisah dari tampilan, sehingga mudah
  diperbarui tanpa mengubah kode game.

---

## 13. Validasi bahasa

- Kosakata Dawan **tidak dikarang**.
- Validasi dilakukan oleh penutur asli: **Ibu Palmerya Sau**, Desa Sekon,
  Kecamatan Insana, Timor Tengah Utara, pada **19 November 2026**.
- Hasil validasi: **31 objek berstatus VALID**, **1 objek berstatus DRAF**
  (tikar di Rumah, karena bentuk dan katanya sama dengan tikar di Dapur).
- Objek DRAF tetap bisa dilihat, tetapi audio dan soal kata barunya dinonaktifkan
  sampai divalidasi ulang.
- Aplikasi menampilkan badge **TERVALIDASI** (hijau) atau
  **DRAF — menunggu validasi TTU** (oranye) pada panel info.

Catatan cara baca: teks di luar tanda kurung adalah cara menulis, teks di dalam
kurung adalah cara membaca. Contoh: **Hau otu (hau outu)**.

---

## 14. Hasil uji dan kesiapan demo

| Uji | Hasil |
|---|---|
| Build aplikasi | Berhasil, tanpa error |
| Pemasangan di perangkat Android | Berhasil |
| Berjalan di perangkat nyata | Berhasil, tanpa crash (0 fatal error) |
| Karakter baru | Sudah terpasang dan diuji: diam, jalan depan, samping kanan, samping kiri, belakang |
| Tepi karakter | Sudah dibersihkan; tidak ada garis putih tersisa |
| Progres tersimpan | Terbukti bertahan setelah aplikasi ditutup dan dibuka lagi |
| Mode offline | Berjalan tanpa internet |
| Perangkat 32-bit | Didukung melalui APK universal |

Perangkat yang dipakai untuk uji: OPPO A3s (Android 8.1). APK universal juga
disiapkan untuk perangkat 32-bit seperti Infinix Smart 9.

---

## 15. Batasan yang harus disampaikan dengan jujur

1. **Buku baru berisi 6 dari 12 topik.** Enam topik lain bertanda "Segera Hadir"
   karena materinya belum divalidasi penutur.
2. **Halaman Buku bersifat baca-saja** (nama materi dan status), belum ada tombol
   audio atau kuis di dalam Buku. Aktivitas belajar berjalan di Dapur, Rumah,
   dan Kebun.
3. **Audio belum diuji menyeluruh di semua merek perangkat.** Pada satu perangkat
   uji, pemutaran musik latar gagal karena keterbatasan perangkat, bukan karena
   kesalahan aplikasi.
4. **Ini versi demo**, bukan aplikasi Play Store. APK ditandatangani untuk
   keperluan uji dan demonstrasi.
5. **Satu sekolah, satu hari.** Penilaian di SMP Putri bersifat deskriptif,
   bukan penelitian skala besar.

---

## 16. Tim dan lisensi

**Tim Dady_Home_Stay:**

- Mario Batista Spanyola Obe
- Basilo Agung de Fretis
- Benediktus Umardani Neonbeni
- Chriscento Novaldry Nokas
- Fabriozio Yakobus Sasi

Musik latar "Feel Good Island Loop" (musik: Brandon Morris; loop: AntumDeluge)
berlisensi **CC0**.

---

## 17. Susunan slide yang diminta (14 slide)

1. **Judul** — "Uab Meto: Belajar Bahasa Dawan Lewat Game", subjudul acara.
2. **Masalah** — bahasa daerah makin jarang dipakai; belajar dari buku terasa berat.
3. **Solusi** — game petualangan 2D untuk belajar kosakata Dawan sambil bermain.
4. **Apa itu Uab Meto** — tiga pilar: jelajah, belajar, uji.
5. **Alur pemain** — 6 langkah ringkas dari membuka aplikasi sampai lulus kuis.
6. **Peta dan lokasi** — peta kampung, Dapur, Rumah, Kebun; Rumah terbuka setelah Dapur.
7. **Cara belajar di dalam lokasi** — mendekat, muncul tombol Baca, panel info, audio.
8. **Empat keterampilan** — mendengar, membaca, menulis, berbicara.
9. **Kuis dan nilai** — 5 soal, nilai 0-100, lulus 70, hati, pelajari ulang.
10. **Gamifikasi** — hati, buah, XP, level, streak, badge, papan peringkat lokal.
11. **Menu Buku dan 12 topik** — 6 topik berisi, 6 "Segera Hadir".
12. **Teknologi dan validasi bahasa** — Unity 6, Android, offline, validasi penutur asli.
13. **Hasil uji dan kesiapan demo** — berjalan di perangkat nyata, tanpa crash.
14. **Penutup** — ajakan mencoba, tim Dady_Home_Stay, terima kasih.

---

## 18. Istilah agar tidak salah tafsir

| Istilah | Arti dalam konteks ini |
|---|---|
| **Uab Meto** | Nama bahasa Dawan; juga nama aplikasi. |
| **Dawan** | Sebutan lain bahasa Uab Meto. |
| **TTU** | Timor Tengah Utara, daerah validasi kosakata. |
| **Objek belajar** | Benda di dalam game yang punya nama Indonesia + Dawan. |
| **Hati** | Nyawa pemain; berkurang saat jawaban kuis salah. |
| **Streak** | Jumlah hari berturut-turut bermain. |
| **Badge** | Lencana pencapaian. |
| **DRAF** | Status materi yang belum divalidasi penutur. |
| **HUD** | Tampilan informasi di layar (hati, buah, level). |
