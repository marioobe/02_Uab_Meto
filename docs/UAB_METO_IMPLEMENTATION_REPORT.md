# Laporan Implementasi — UAB METO (Unity 6 / Android)

Dokumen kerja implementasi game edukasi 2D “Uab Meto”. Diperbarui bertahap setiap fase;
bagian yang belum diuji ditandai jelas.

---

## 1. Ringkasan proyek
Game petualangan edukatif 2D berlatar kampung Uab Meto. Pemain menjelajah empat scene
(`Base_Map`, `Dapur`, `Rumah`, `Kebun`), membaca nama benda dalam bahasa Indonesia + Dawan,
menjawab kuis, dan mengelola hati serta buah. Target perangkat: Android landscape
(uji: OPPO A3s, Android 8.1, 1520×720).

## 2. Tujuan dan kriteria keberhasilan
- Pemain dapat berjalan 360° di peta, masuk/keluar bangunan, dan berinteraksi dengan benda.
- Materi belajar data-driven, dapat divalidasi penutur asli TTU.
- Kuis salah mengurangi hati; hati 0 memblokir kuis sampai dipulihkan dengan buah.
- Progres tersimpan (versi + cadangan + penanganan file rusak).
- APK terpasang dan berjalan di perangkat uji nyata, bukan hanya di Editor.

## 3. Ruang lingkup
Termasuk: empat scene utama, 32 objek belajar, kuis per scene, HUD, pengaturan, simpan-muat,
audio SFX prosedural, penanda interaksi, panel dialog/info/kuis/pengaturan/buku/darurat.
Tidak termasuk (belum): BGM final, rekaman pelafalan Dawan, validasi bahasa oleh ahli.

## 4. Batasan yang dipertahankan
- `Assets/foto/Gambar_Scene_Baru` adalah sumber seni; tidak diubah/dihapus.
- Karakter utama (Yuuta) dan movement 360° dipertahankan.
- Scene lama `VillageScene` dan `RoundHouseInterior` disimpan sebagai backup, tidak dibangun.
- Tidak mengarang kosakata Dawan.

## 5. Arsitektur runtime
- `GameBootstrap` (persisten, `BeforeSceneLoad`): `SaveService`, `SettingsService`,
  `HealthService`, `InventoryService`, `ProgressService`, `AudioManager`, `SceneRouter`,
  `ScreenFader`, `ToastView`, `AppLifecycle`, satu `EventSystem`.
- `SceneController` per scene: menempatkan pemain di entry point, mengikat kamera, membangun UI.
- `GameUi`: HUD, D-Pad/joystick, prompt interaksi, dan semua panel modal (satu modal aktif
  lewat `UiModalState`).
- `SceneRouter`: satu-satunya jalur pindah scene (tidak ada `LoadScene` liar).

## 6. Scene dan tata letak
| Scene | Isi | Catatan |
|-------|-----|---------|
| `Base_Map` | kampung, 3 pintu (Dapur, Rumah, Kebun), NPC pemandu | spawn gerbang utama (13,35; 2,50) |
| `Dapur` | 10 objek belajar, kuis, pintu keluar | pintu masuk (8,95; 10,5) |
| `Rumah` | 10 objek belajar, terkunci sampai Dapur selesai | pintu (20,2; 11,0) |
| `Kebun` | 12 objek belajar, 5 buah + 1 buah darurat | gerbang (22,3; 4,15) |

Posisi collider, pintu, dan titik interaksi dihitung dari kotak potong seni (grid 10%),
bukan diperkirakan. Verifikasi editor: menu **UAB Meto/4 - Verify Scene Layout**.

**Pembaruan 21-11-2026** — keputusan akhir pemilik proyek: keempat scene memakai **latar lama**
(perabot/pohon sudah tergambar di seni). Sprite objek transparan dari
`Assets/foto/aset_transparan_23` tetap dipakai, tetapi ditempatkan di **sela kosong** latar:
Dapur 10 objek, Rumah 10 objek, Kebun 12 objek. Posisi ternormalisasi (0..1 dari kiri-atas)
disimpan di `AsetTransparan23Importer.Placements`; tinggi objek diatur lewat `TargetHeight`
(PPU = tinggi hasil potong / TargetHeight). Penanda interaksi digambar **di belakang** sprite
(`sortingOrder 4`) sehingga hanya tampak sebagai halo di kaki objek. Collider Dapur memakai
ellipse (pusat 13,76; 7,90, radius 11,6 × 4,0) + koridor pintu; Rumah memakai `Wall_Top` di
y 13,35 + koridor pintu, sehingga pemain berhenti di dasar batu/lantai.

## 7. Data belajar
- 32 aset `LearningObjectDefinition` di `Assets/Data/Learning/<Scene>/<id>.asset`.
- Field: `id`, `sceneKey`, `category`, `namaIndonesia`, `namaDawan`, `caraBaca`,
  `validationStatus`, `validator`, `validationDate`, `validationNotes`, `explanation`, `image`,
  `pronunciation`, `questions[]` (pertanyaan, opsi, `correctIndex`, penjelasan, `usesDawan`).
- Dibangun lewat `LearningDataBuilder` (menu **UAB Meto/3**), jadi isi data dapat diulang
  secara reproducible.
- Gambar objek dipotong otomatis dari seni scene ke `Assets/Art/Learning/<scene>/<id>.png`.
- **Fase A (21-11-2026)**: tiap objek kini punya metadata kurikulum `topicId` (enum 12 topik),
  `role` (Utama/Pendukung), `crossTopics`, dan `order` — ditulis oleh `TopicMappingApplier`
  (hanya 4 field; konten terlindung). Pemetaan sesuai `docs/PROJECT_GAP_ANALYSIS.md` §5:
  28 Utama + 4 Pendukung (`daun_pisang`, `bakul_hasil`, `tempayan_rumah`, `tikar_rumah`).

## 8. Status validasi bahasa Dawan
- Kosakata sudah **divalidasi penutur asli**: **Ibu Palmerya Sau**, Desa Sekon, Kec. Insana,
  Timor Tengah Utara (TTU), tanggal **19/11/2026**. Sumber:
  `docs/01_Lembar_Validasi_Kosakata_Dawan_TTU.docx` dan `docs/DAWAN_VALIDATION_SHEET.md`.
- Hasil: **31 objek `VALID`**, **1 objek `DRAF`** (`tikar_rumah`, karena bentuknya sama dengan
  `tikar` di Dapur sehingga tidak diberi audio/soal kata baru).
- Aturan penulisan dari validator: teks di luar kurung = cara tulis; di dalam kurung = cara
  baca/dialek. Aplikasi menampilkan `Dawan: Hau otu (hau outu)` untuk objek yang punya cara baca.
- Badge panel: hijau **`TERVALIDASI`** untuk objek valid, oranye
  **`DRAF — menunggu validasi TTU`** untuk `tikar_rumah` (audio & kuis kata nonaktif).
- Tidak ada kosakata Dawan yang dikarang di aplikasi.

## 9. Alur interaksi dan dialog
1. Pemain mendekat → prompt (`Baca` / `Masuk` / `Keluar` / `Bicara` / `Ambil`).
   - Pintu dari `Base_Map` ke bangunan: **`Masuk`**.
   - Pintu keluar di dalam `Dapur`/`Rumah`/`Kebun`: **`Keluar`** (diperbaiki, sebelumnya selalu `Masuk`).
2. Tombol interaksi kanan bawah / tombol aksi → panel info (gambar, kategori, nama Indonesia,
   Dawan, badge status, penjelasan, tombol audio, tombol kuis).
3. Membaca objek menandainya “dipelajari”. Setelah semua objek scene dipelajari, tombol
   **Kuis** aktif (sebelumnya berlabel `Kuis (baca semua)` dan non-aktif).
4. **Fase C**: panel info memiliki dua baris tombol — **Dengarkan / Kuis / Tutup** lalu
   **Latihan / Bicara**. `Latihan` membuka `WritingPanel` (susun kata / lengkapi kalimat),
   `Bicara` membuka `SpeechPracticePanel` (rekam sementara → putar ulang, tanpa upload).

## 10. Kuis dan hati
- **Fase B (22-11-2026)**: nilai **0–100** (`benar/total × 100`), **lulus ≥ 70**. Panel hasil
  menampilkan `Nilai: X/100`, status `Lulus!`/`Belum lulus`, dan **"Pelajari ulang"** (maks 3
  objek yang salah beserta jawaban benar). Kuis 5 soal ⇒ lulus minimal 4 benar (80).
- 5 soal acak per sesi.
- Soal diambil **hanya dari objek yang sudah dibaca** (`learnedObjectIds`), bukan seluruh objek
  scene — objek yang belum dibuka tidak menyumbang soal.
- **Prioritas kosakata Dawan**: total 60 soal ber-`usesDawan = true` (2 arah per objek valid:
  Indonesia→Dawan dan Dawan→Indonesia). `QuizPanel` memilih soal Dawan lebih dulu, maksimal
  satu soal per objek, lalu mengisi sisa slot dengan soal fungsi bila perlu.
- Pengecoh diambil dari objek lain **di scene yang sama**, jadi opsi selalu berkaitan dengan
  materi yang dipelajari. Objek yang kata Dawannya sama dengan bahasa Indonesia (`lemari`)
  tidak dibuatkan soal kata, hanya soal fungsi.
- Jawaban salah: −1 hati, opsi benar disorot hijau, umpan balik penjelasan.
- Hati 0: panel darurat (recovery) muncul; kuis diblokir sampai hati dipulihkan.
- Lulus: panel hasil **“Lulus!”** dengan statistik (benar/total, ambang) dan tombol
  **Lanjut**; `CompleteCurrentScene()` menyimpan progres (Dapur membuka Rumah).

## 11. Buah dan pemulihan
- Buah di Kebun (5 titik) diambil dengan prompt `Ambil`; buah darurat muncul hanya saat
  hati 0 dan buah 0 (anti-softlock).
- HUD buah selalu dapat ditekan: +1 hati bila tidak penuh; pesan jelas bila hati penuh
  atau buah habis.
- Kebun terbuka sejak awal sehingga pemain tidak pernah kehabisan sumber pemulihan.

## 12. HUD dan UI
- Hati (maks 3) + hitungan `x/3`, pil buah + hint dinamis (`pakai` / `penuh` / `kosong`),
  pil **level/XP** (`Lv X · n/100 XP`), tombol **`Profil`**, `Buku`, dan `Atur`.
- **Fase B (22-11-2026)**: panel `Profil` menampilkan Level, XP menuju level berikut, streak,
  9 badge (terkunci/terbuka), dan **leaderboard lokal** (maks 20 tersimpan, 10 teratas tampil).
- **Fase C (22-11-2026)**: `InfoPanel` dua baris tombol (Dengarkan/Kuis/Tutup, Latihan/Bicara);
  `WritingPanel` (susun kata & lengkapi kalimat); `SpeechPracticePanel` (rekam maks 10 dtk,
  putar ulang, hapus; WAV sementara dibersihkan saat panel ditutup dan saat startup).
  Dialog pemandu berubah sesuai progres via `DialogueLibrary` (teks Indonesia, tanpa Dawan baru).
- Semua panel memakai TextMeshPro, skala referensi 1920×1080 dengan `matchWidthOrHeight = 0.5`
  untuk Android landscape.
- Bahasa UI: Indonesia.

## 13. Kontrol sentuh
- D-Pad (default) atau joystick analog, dipilih di Pengaturan; hanya satu aktif pada satu waktu
  (`InputRouter`).
- D-Pad: tombol krem terang + glyph gelap; joystick: lingkaran krem dengan cincin gelap agar
  terlihat di atas seni terang.
- Multi-touch didukung (bergerak sambil menekan tombol interaksi).

## 14. Simpan-muat
- `save.json` di `Application.persistentDataPath`, berisi `saveVersion`, progres, pengaturan.
- Penulisan atomik, salinan `save.json.bak`, file rusak dikarantina (`.corrupt-*`),
  migrasi versi bila format lama.
- Simpan otomatis saat pause/keluar dan pada perubahan penting (buah, hati, progres).

## 15. Audio
- SFX prosedural: klik, ambil buah, benar, salah, pintu, hati.
- Volume musik & efek diatur di panel Pengaturan dan tersimpan.
- **Pelafalan Dawan tersedia**: 31 rekaman penutur asli di `docs/rekaman penutur/` (format asli
  Ogg Opus, tidak diubah). Karena importer Unity tidak men-decode Opus, rekaman dikonversi ke
  WAV PCM 16-bit mono 44,1 kHz di `Assets/Audio/Dawan/<id>.wav` lewat
  `Tools/ConvertDawanAudio.py` (memakai ffmpeg dari `imageio-ffmpeg`).
- `DawanAudioImporter` menyetel impor: mono, Vorbis saat build, Decompress On Load, preload.
  Semua 31 referensi `pronunciation` ter-link ke aset data; `tikar_rumah` sengaja kosong.
- Tombol `Dengarkan` memutar klip dengan volume SFX; objek tanpa audio menampilkan
  `Audio belum tersedia` (nonaktif).
- **Kompatibilitas perangkat**: OPPO A3s (ColorOS, Android 8.1) hanya menyediakan profil output
  **PCM 16-bit**, sedangkan Unity 6/FMOD meminta **PCM float**. Log perangkat:
  `AudioPolicyManagerCustom: getOutput() could not find output … format 5` →
  `AAUDIO_ERROR_INTERNAL` → `FMOD failed to initialize the output device (60)`, sehingga seluruh
  audio Unity senyap (bukan hanya pelafalan, dan bukan masalah ekstensi berkas).
- **Fallback MediaPlayer**: audio Dawan juga dikemas sebagai M4A/AAC 64 kbps di
  `Assets/StreamingAssets/Dawan/` (`Tools/ConvertDawanAudioToAac.py`, 31 berkas, 1,05 MB).
  `DawanAudioPlayer` menyalin berkas ke `persistentDataPath/Dawan` saat pertama diputar lalu
  memutar lewat `android.media.MediaPlayer` (jalur PCM 16-bit) dengan `USAGE_MEDIA` +
  `CONTENT_TYPE_SPEECH`. Di luar Android, `InfoPanel` tetap memakai `AudioSource`. Bila jalur
  MediaPlayer gagal (berkas tidak terbaca, gagal salin, atau exception JNI), klip yang sama
  otomatis diputar lewat `AudioSource` (`PlayFallback`) sebagai jaring pengaman.
  Modul bawaan `com.unity.modules.androidjni` dan `com.unity.modules.unitywebrequest`
  diaktifkan di `Packages/manifest.json` (proyek Unity 6 memang tidak memuatnya secara default).
- **Catatan JNI** (penting saat merawat kode ini): `MediaPlayer.setAudioAttributes(...)` dan
  `setDataSource(String)` bertipe **void**. Panggil dengan `Call(...)`. Memakai
  `Call<AndroidJavaObject>(...)` membuat Unity mencari signature `(...)Ljava/lang/Object;`
  dan melempar `NoSuchMethodError`, sehingga tidak ada suara sama sekali. Player dilepas
  memakai timer `getDuration() + 0,5 s`, bukan `isPlaying`, karena `isPlaying` dapat bernilai
  `false` tepat setelah `start()` sehingga klip terpotong.
- **Bukti**: di HP Android 16 (Redmi 24117RN76O, `ib4tozizknzpxou8`) tidak ada error FMOD dan
  pelafalan terdengar (konfirmasi pengguna, 20-11-2026). Verifikasi sistem: `dumpsys
  media.audio_flinger` menampilkan track `00000001` = PCM 16-bit, 44 100 Hz, `Usg 1`
  (`USAGE_MEDIA`), gain `-6,9 dB` (= volume 0,45), pada output
  `AUDIO_DEVICE_OUT_SPEAKER`; `BatteryStatsService` mencatat `StartAudioToPower` →
  `StopAudioToPower` setelah ±4,3 detik (durasi klip). APK final: 46 408 053 byte,
  20-11-2026 11:35:59.
- **Uji OPPO A3s 21-11-2026**: jalur MediaPlayer berjalan sampai `start()` (berkas `tikar.m4a`
  tersalin ke `Android/data/com.uabmeto.village/files/Dawan/`), tetapi perangkat membalas
  `MediaPlayerNative: error (1, -19)` (ENODEV). Pada saat yang sama seluruh audio Unity juga
  gagal (`AudioTrack: Could not get audio output ... initCheck() = -19` → `FMOD failed (60)`),
  termasuk setelah reboot. Speaker aktif di `dumpsys audio` dan volume media 16/16, jadi
  kegagalan ada di driver/HAL perangkat uji, bukan di kode aplikasi. Uji dengar final perlu
  diulang saat perangkat dingin atau memakai perangkat lain.

## 16. Tekstur dan performa
- Latar 2752×1536 → dunia 27,52 × 15,36 unit; PPU 100; maksimum tekstur 4096 agar ukuran
  dunia tetap presisi; Bilinear; mipmap mati; Read/Write mati; override Android ASTC 6×6.
- Sprite karakter/ikon: Point, tanpa kompresi.
- Klaim keamanan RAM **belum** dibuat: perlu pengukuran nyata di perangkat kelas bawah.

## 17. Build
- Unity 6000.6.0f1, `minSdkVersion 26`, target Android landscape.
- Build otomatis: `AndroidBuilder.BuildApk` (batchmode) → `Builds/Android/UabMeto.apk`.
- **APK final 21-11-2026**: 76 311 135 byte, 21-11-2026 12:14:30 (memuat latar lama + objek
  transparan, penanda di belakang objek, dan dialog ketua adat versi akhir).
- **APK final 22-11-2026 (Fase B)**: 76 334 467 byte, 22-11-2026 23:37:38 (Fase A + Fase B: kurikulum,
  evaluasi 70/100, XP/level/streak/badge/leaderboard lokal, profil).
  Disimpan sebagai checkpoint: `Builds/Android/UabMeto_FaseB_checkpoint.apk`.
- **APK Fase C (22-11-2026)**: 76 363 787 byte, 22-11-2026 02:24:14
  (`Builds/Android/UabMeto_FaseC.apk`) — menulis, bicara, dialog tematik, izin RECORD_AUDIO.
- Pemasangan uji: `adb install -r -d`.

## 18. Pengujian perangkat (bukti)
Perangkat: OPPO A3s (`26b4c7b0`), Android 8.1, 1520×720 landscape.

| Uji | Hasil | Bukti |
|-----|-------|-------|
| Boot ke `Base_Map` di gerbang | Lulus | log `Scene siap: Base_Map entry=Start pos=(13.35, 2.50)` |
| Kamera mengikuti & terjepit batas peta | Lulus | tangkapan layar |
| Prompt & masuk Dapur (`Masuk`) | Lulus | log `Scene siap: Dapur` |
| Panel info objek + badge DRAF | Lulus | tangkapan “Tungku batu”, “Kayu bakar”, “Selimut tenun” |
| Kuis Dapur: salah → hati 3→2 | Lulus | log `Hati berkurang (Jawaban kuis salah). Sisa: 2` |
| Kuis lulus → `dapurCompleted`, `rumahUnlocked` | Lulus | `save.json` |
| Kebun: ambil buah | Lulus | `fruit=1`, toast “+1 buah” |
| Pakai buah → hati 2→3 | Lulus | `hearts=3`, `fruit=0` |
| Rumah terkunci menampilkan pesan, scene tetap | Lulus | tangkapan “Terkunci …” |
| Interior Rumah + objek | Lulus | tangkapan panel “Selimut tenun” |
| Label pintu masuk dari Base_Map | Lulus | prompt `Masuk` di pintu Dapur |
| Label pintu keluar (Dapur/Rumah/Kebun) | Lulus | prompt `Keluar` di pintu keluar Dapur (build 18-11-2026 09:01) |
| Tombol kuis terkunci sebelum semua objek dibaca | Lulus | label `Kuis (baca semua)` + non-aktif (save 1 objek) |
| Tombol kuis aktif setelah semua objek dibaca | Lulus | label `Kuis` di panel “Bangku kayu” |
| Kuis lulus 5/5 → panel `Lulus!` | Lulus | “Benar 5 dari 5 soal (butuh 3). Rumah warga sekarang terbuka.”, hati tetap 3/3 |
| Simpan bertahan setelah restart | Lulus | log `Save dimuat: scene=…, hati=3, buah=0` |
| Simpan rusak → karantina + pulih dari `.bak` | Lulus | log `File save rusak disimpan: …save.corrupt-*.json` lalu `Save dipulihkan dari backup.` |
| Panel darurat saat hati 0 | Lulus | “Hati Habis” + tombol `Pakai buah` (non-aktif saat buah 0), `Ke Kebun`, `Tutup` |
| Panel `Jagung` menampilkan Dawan + badge valid | Lulus | `Dawan: Pena`, badge hijau `TERVALIDASI`, `Dengarkan` & `Kuis` aktif |
| Panel `Tikar` (Rumah) tetap DRAF | Lulus | `Dawan: Na'he (DRAF)`, badge oranye `DRAF — menunggu validasi TTU`, `Audio belum tersedia` |
| Panel `Lemari`/`Bangku` menampilkan kata valid | Lulus | `Dawan: lemari`, `Dawan: toko`, badge `TERVALIDASI` |
| Kuis Dawan Dapur 5 soal, 5 objek berbeda | Lulus | soal Jagung→Pena, Bakul→Oko, arti `Na'i Hana`, `Na'i naijan`, `Na'i`; hasil `Lulus 5/5`, hati 3/3 |
| Audio pelafalan ter-link di data | Lulus | 31 aset `pronunciation` berisi, 1 null (`tikar_rumah`); 60 soal `usesDawan: 1` |
| Pemutaran pelafalan di Android 16 (Redmi 24117RN76O) | Lulus | pengguna mendengar suara; `audio_flinger`: track PCM16 44,1 kHz `USAGE_MEDIA` di speaker, gain −6,9 dB, ±4,3 dtk; `StartAudioToPower` → `StopAudioToPower` |
| Pemutaran pelafalan di OPPO A3s (ColorOS/Android 8.1) | **Gagal di perangkat uji (bukan kode)** | 21-11-2026: berkas tersalin (`Dawan/tikar.m4a`) dan `MediaPlayer.start()` berjalan, lalu `MediaPlayerNative: error (1, -19)` (ENODEV). FMOD juga gagal dengan galat yang sama (`AudioTrack: Could not get audio output ... initCheck() = -19`). AudioStream keluar (speaker) aktif dan volume media 16/16, jadi driver audio perangkat uji sedang tidak menyediakan output untuk aplikasi apa pun; perlu diuji ulang setelah perangkat dingin/reboot atau di perangkat lain |
| Latar lama dipulihkan di Dapur/Rumah/Kebun + objek transparan | Lulus | 21-11-2026, tangkapan layar: Dapur (isi rumah bulat), Kebun (kebun sayur), Rumah (interior) memakai seni lama; 10/12/10 sprite objek berdiri di sela perabot |
| Penanda objek tidak lagi menutupi sprite | Lulus | `sortingOrder` penanda 45 ��' 4; halo tampak di belakang kaki objek (`m_SortingOrder: 4` di `Dapur.unity`) |
| Dialog pemandu memakai popup ketua adat | Lulus | judul `Pemandu Desa` di plakat, isi di perkamen kanan, petunjuk di kiri tombol panah (tangkapan 21-11-2026) |
| Tembok Dapur tidak bisa dinaiki | Lulus | pemain berhenti di dasar anyaman (fence ellipse + koridor pintu) setelah 4 kali geser ke atas |
| Panel info + `Dengarkan` merespons sentuh | Lulus | `Tikar`/`Tungku batu` terbuka; tombol bekerja (uji koordinat 400,589 / 1140,589) |
| **Fase A**: metadata topik pada 32 aset | Lulus | `TopicMappingApplier`: 32 aset dipetakan, 0 konten berubah, 124 soal tetap; hanya `topicId/role/crossTopics/order` ditulis |
| **Fase A**: migrasi Save v1 → v2 | Lulus | `saveVersion` 1 → 2, `completedTopicIds: []`; `hearts/fruit/learnedObjectIds(27)/collectedPickupIds/flag scene` utuh |
| **Fase A**: tombol Buku → pemilih 12 topik | Lulus | panel terbuka; ketuk topik menampilkan daftar materi; `Kembali` dan `Tutup` bekerja tanpa crash |
| **Fase B**: evaluasi 70/100 | Lulus | kuis 5/5 → `Nilai: 100/100 · Lulus!`; kuis 3/5 → `Nilai: 60/100 · Belum lulus` + daftar "Pelajari ulang" |
| **Fase B**: anti-eksploit XP | Lulus | soal duplikat benar → XP tetap 85; bonus lulus hanya sekali (`xpPassScenes=Dapur`, xp 105); buka ulang objek → XP tetap |
| **Fase B**: level, badge, leaderboard | Lulus | 100 XP → Level 2; badge `lulus_dapur` + `kuis_sempurna` (2/9); leaderboard 2 entri 100/100, `bestScores.Dapur=100` |
| **Fase B**: streak (tanggal lokal) | Lulus | `lastActivityDay` kemarin → streak 2; lewat 3 hari → streak 1; aktivitas hari sama tidak menambah |
| **Fase B**: migrasi v2→v3 + proteksi save | Lulus | `saveVersion` 3; learned 27 & flag/volume identik; save asli dipulihkan byte-identik (SHA256 `5B4F58…`) |
| **Fase C**: migrasi fixture v3→v4 | Lulus | `saveVersion` 4; xp 125/level 2/learned 27/xpQuestions 13/badge/best/leaderboard identik; `xpWritings=[]` |
| **Fase C**: Writing — susun kata | Lulus | `Kayu bakar` → `Hau otu` → `Benar! +5 XP`, XP 130→135, kunci `w:0:kayu_bakar:…` |
| **Fase C**: Writing — lengkapi kalimat | Lulus | `Tikar` → `Na'he` → +5 XP, kunci `w:1:tikar:…`; opsi unik (Na'he/Na'i naijan/Pika Bale/Lele utan) |
| **Fase C**: Writing — salah & pengulangan | Lulus | Salah → XP tetap 125, `xpWritings=0`; ulang benar → XP tetap 130, `xpWritings` tetap 1 |
| **Fase C**: Speaking — izin | Lulus | Dialog sistem "Izinkan UAB Meto merekam audio" muncul; `IZINKAN` → `RECORD_AUDIO: granted=true` |
| **Fase C**: Speaking — rekam | **Tidak dapat diuji di OPPO** | `Microphone.devices` kosong → status "Mikrofon tidak ditemukan"; penanganan tanpa crash; keterbatasan perangkat (audio ENODEV) |
| **Fase C**: WAV writer (self-test) | Lulus | 88.200 sampel, 176.444 byte (44+data), header RIFF/WAVE/fmt/data, PCM 16-bit mono 44,1 kHz, amplitudo maks 16.383, 88.185/88.200 sampel non-nol |
| **Fase C**: cleanup WAV | Lulus | `speech_tmp.wav` dummy hilang setelah restart aplikasi |
| **Fase C**: Dialog tematik | Lulus | Progres Dapur+Rumah selesai → halaman "Dapur dan Rumah sudah kamu selesaikan. Hebat!" (1/4) |
| **Fase C**: manifest Android | Lulus | `aapt dump permissions`: `android.permission.RECORD_AUDIO` ada di APK; injector idempotent |
| **Fase C**: regresi + hash konten | Lulus | Kuis 1/5 tampil, HUD/hati normal; 33 aset & 124 soal SHA256 identik baseline |

## 19. Batasan, risiko, dan pekerjaan lanjutan
- Kosakata Dawan **sudah divalidasi** (Ibu Palmerya Sau, TTU, 19/11/2026). Satu objek
  (`tikar_rumah`) sengaja tetap `DRAF` karena bentuknya sama dengan `tikar`.
- Kuis kosakata Dawan **sudah aktif** (60 soal dua arah, pengecoh dari scene yang sama) dan
  sudah lulus 5/5 di perangkat.
- **Audio pelafalan belum dapat dibuktikan berbunyi** di OPPO A3s karena perangkat uji
  (21-11-2026) menolak semua output audio: `AudioTrack`/`MediaPlayer` mengembalikan ENODEV
  (`-19`) dan FMOD gagal inisialisasi, bahkan setelah reboot. Di Redmi Android 16 pelafalan
  sudah terdengar (20-11-2026). Uji dengar OPPO perlu diulang setelah perangkat dingin atau
  dengan unit lain.
- **Rekam/putar suara (Fase C) belum dapat diuji di OPPO**: izin `RECORD_AUDIO` muncul dan
  berhasil diberikan, tetapi `Microphone.devices` kosong (perangkat yang sama dengan masalah
  audio ENODEV), sehingga rekaman tidak bisa dimulai. Penulisan WAV divalidasi lewat
  self-test editor (header, ukuran, PCM non-nol); rekam/putar nyata perlu diuji di perangkat
  yang mikrofonnya berfungsi (mis. Redmi).
- Uji simpan rusak/migrasi, pengaturan audio, panel Buku, dan panel darurat masih perlu
  dijalankan ulang pada build final.
- Pemain dapat berdiri “menimpa” seni bangunan (seni latar datar); perlu penyesuaian titik
  interaksi bila dianggap mengganggu.
- Perlu pengukuran RAM/FPS nyata di perangkat kelas bawah sebelum klaim performa.

## 20. Cara menjalankan ulang
1. Konversi audio (sekali saja / bila rekaman berubah):
   `python Tools/ConvertDawanAudio.py` → menghasilkan `Assets/Audio/Dawan/*.wav`.
   Butuh `pip install imageio-ffmpeg` atau ffmpeg di PATH. Berkas asli di
   `docs/rekaman penutur/` tidak diubah.
2. Unity: buka proyek, jalankan menu berurutan
   **UAB Meto/1 - Impor Seni**, **2 - Bangun Scene**, **3 - Bangun Data Belajar**
   (langkah 3 juga menyetel ulang impor audio lewat `DawanAudioImporter`).
3. Verifikasi: **UAB Meto/4 - Verify Scene Layout**, **UAB Meto/5 - Verify Art & Data**.
4. Build: `AndroidBuilder.BuildApk`, lalu `adb install -r -d Builds/Android/UabMeto.apk`.
5. Uji: jalankan daftar pada bagian 18; simpan bukti log/tangkapan layar.
   Untuk build di mesin RAM 8 GB gunakan `-batchmode -nographics -disableAssetImportWorkerProcess`.
