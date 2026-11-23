# TO-DO — UAB METO (Unity 6 / Android)

Rencana kerja yang sedang dieksekusi. Diperbarui setiap fase.

## Aturan tetap
- Sumber latar: `Assets/foto/Gambar_Scene_Baru` (jangan diubah/dihapus).
- Karakter Yuuta + movement 360° dipertahankan.
- Scene lama `VillageScene` & `RoundHouseInterior` disimpan sebagai backup, tidak dibangun.
- Tidak mengarang kosakata Dawan. Konten belum valid = `—` + badge DRAF.
- Setiap fase: compile → build APK → uji di OPPO A3s (`26b4c7b0`) → laporan bukti.

## Fase
- [x] 0. Backup `Assets` + `ProjectSettings` (`Backups/PreImplementation_20261116_224103.zip`).
- [x] 1. Fondasi: impor TMP, `GameRoot`, save v1, settings, input router, D-Pad/joystick, player, kamera, Base_Map shell. **APK uji #1** (terpasang & diuji).
- [x] 2. Base_Map lengkap: collider, 3 pintu, NPC, HUD, quest, kunci Rumah. **APK uji #2** (terpasang & diuji).
- [x] 3. Interaksi + dialog TMP + panel info + toast + modal stack. **APK uji #3** (terpasang & diuji).
- [x] 4. Dapur: data belajar, crop objek, 10 objek, kuis, hati dasar, buka Rumah. **APK uji #4** (terpasang & diuji).
- [x] 5. Buah + Kebun sumber pemulihan + rescue fruit + panel 0 hati. **APK uji #5** (terpasang & diuji).
- [x] 6. Rumah + Kebun lengkap + quest. **APK uji #6** (terpasang & diuji).
- [ ] 7. Simpan penuh + migrasi + uji korupsi. **APK uji #7**
- [ ] 8. Audio + pengaturan lengkap. **APK uji #8** (BGM/SFX sintetis jalan; audio pelafalan
      Dawan ter-link tetapi output perangkat uji gagal inisialisasi — lihat catatan FMOD)
- [ ] 9. Regression Android penuh (20 kriteria Q). **APK final #9**
- [x] 10. `docs/UAB_METO_IMPLEMENTATION_REPORT.md` + `docs/DAWAN_VALIDATION_SHEET.md`
      (lembar validasi sudah terisi lengkap; laporan perlu pembaruan bagian 19–20)

## Bukti uji perangkat (OPPO A3s, 1520×720)
- Base_Map: spawn di gerbang utama, kamera mengikuti & terjepit batas peta, pintu Dapur/Kebun memberi prompt `Masuk`.
- Dapur: masuk, panel info (gambar crop + badge DRAF), 10 objek, kuis 5 soal, salah → hati 3→2 + opsi benar hijau + umpan balik, lulus 4/5 → `dapurCompleted=true`, `rumahUnlocked=true` (tersimpan).
- Kebun: masuk dari gerbang kebun, ambil buah (`kebun_buah_3`, `fruit=1`), pakai buah → hati 2→3, `fruit=0`, tersimpan.
- Rumah terkunci (save `rumahUnlocked=false`): pesan "Terkunci — Rumah warga masih terkunci…", scene tidak berubah.
- Rumah (save normal): interior tampil, objek + penanda ada, panel info "Selimut tenun" (gambar crop, DRAF, kategori Kain).
- HUD: 3 hati, hitungan buah, tombol `Buku`/`Atur` berfungsi.

## Polish batch (dikerjakan, menunggu build #7)
- [x] Hint HUD buah: `kosong` saat buah 0, `penuh` saat hati penuh, `pakai` selain itu.
- [x] Pesan pakai buah: cek "buah habis" sebelum "hati penuh".
- [x] D-Pad: tombol krem terang + glyph gelap; joystick: cincin gelap di lingkaran krem.
- [x] Kuis lulus: panel hasil "Lulus!" + statistik + tombol `Lanjut` (tidak menutup mendadak).
- [x] Penanda pintu: warna hangat, alpha 0,07–0,18 (tidak lagi tampak seperti lubang).

## Perbaikan pasca-uji perangkat (build berikutnya)
- [x] Label pintu keluar: `Exit_Dapur`, `Exit_Rumah`, `Exit_Kebun` memakai prompt **`Keluar`**
      (sebelumnya selalu `Masuk`). `DoorInteractable.Bind()` menerima parameter `prompt` opsional;
      pintu masuk dari `Base_Map` tetap `Masuk`. **Terverifikasi di perangkat 18-11-2026.**
- [x] Soal kuis hanya diambil dari objek yang **sudah dibaca** (`GameState.Data.learnedObjectIds`),
      bukan seluruh objek scene. Objek yang belum dibuka tidak menyumbang soal.
- [x] Kuis Dapur dijalankan ulang di perangkat: `Soal 1/5`…`5/5`, semua benar, panel `Lulus!`
      “Benar 5 dari 5 soal (butuh 3). Rumah warga sekarang terbuka.”, hati tetap 3/3.
- [x] **Kuis kosakata Dawan aktif** (`usesDawan = true`). Kosakata berasal dari lembar validasi
      penutur TTU (Ibu Palmerya Sau, Desa Sekon, Kec. Insana, 19/11/2026): 31 objek VALID,
      `tikar_rumah` tetap DRAF. `LearningDataBuilder` membuat 60 soal Dawan (2 arah per objek:
      Indonesia→Dawan dan Dawan→Indonesia) dengan pengecoh dari objek lain di scene yang sama.
      `QuizPanel` memprioritaskan soal Dawan, maksimal 5 soal, satu soal per objek.
      **Terverifikasi di perangkat 20-11-2026: kuis Dapur 5/5 lulus, 5 objek berbeda, hati tetap 3/3.**
- [x] Audio pelafalan: 31 rekaman penutur (Ogg Opus) dikonversi ke WAV PCM di
      `Assets/Audio/Dawan/` lewat `Tools/ConvertDawanAudio.py`; impor disetel
      `DawanAudioImporter` (mono, Vorbis build, Decompress On Load). Semua 31 referensi
      `pronunciation` ter-link.
- [x] **Akar masalah audio OPPO A3s ditemukan**: driver ColorOS hanya menyediakan profil
      output **PCM 16-bit** (`dumpsys media.audio_policy`), sedangkan Unity 6/FMOD meminta
      **PCM float** → `AudioPolicyManagerCustom: getOutput() could not find output` →
      `FMOD failed to initialize (60)`. Bukan masalah ekstensi/file; seluruh audio Unity
      (BGM, SFX, pelafalan) senyap di perangkat itu.
- [x] **Fallback MediaPlayer** untuk pelafalan: audio Dawan juga dikemas sebagai M4A/AAC
      (`Tools/ConvertDawanAudioToAac.py` → `Assets/StreamingAssets/Dawan/`, 31 file, 1,05 MB).
      `DawanAudioPlayer` menyalin ke `persistentDataPath` saat pertama diputar lalu memutar
      lewat `android.media.MediaPlayer` (`USAGE_MEDIA`, PCM 16-bit), sehingga tetap terdengar di
      perangkat yang tidak mendukung output float. Di luar Android tetap memakai `AudioSource`.
      Bila MediaPlayer gagal, klip yang sama otomatis diputar lewat `AudioSource`
      (`PlayFallback`) agar tidak pernah benar-benar senyap.
      **Terverifikasi 20-11-2026 di Redmi 24117RN76O (Android 16):** pengguna mendengar
      pelafalan; `dumpsys media.audio_flinger` menunjukkan track `PCM16 / 44100 Hz /
      USAGE_MEDIA` aktif di output `AUDIO_DEVICE_OUT_SPEAKER`, gain `-6,9 dB` (volume 0,45),
      durasi ±4,3 detik, dan `BatteryStatsService` mencatat `StartAudioToPower` → `StopAudioToPower`.
      Menunggu uji akhir OPPO A3s.
- [x] **Dua bug JNI ditemukan lewat logcat dan diperbaiki** (penyebab "suara penutur hilang"):
      `MediaPlayer.setAudioAttributes(...)` dan `setDataSource(String)` adalah method **void**,
      jadi harus dipanggil `Call(...)`, bukan `Call<AndroidJavaObject>(...)`; versi lama mencari
      signature `(...)Ljava/lang/Object;` → `NoSuchMethodError` sehingga tidak ada suara.
      Pelepasan player juga diganti dari `isPlaying` ke timer durasi (`getDuration + 0,5 s`)
      karena `isPlaying` bisa `false` tepat setelah `start()` sehingga klip terpotong.
      Modul `com.unity.modules.androidjni` + `com.unity.modules.unitywebrequest` wajib ada di
      `Packages/manifest.json` (Unity 6 tidak memuatnya default).
- [x] Panel info menampilkan `Dawan: <nama> (<cara baca>)` + badge `TERVALIDASI` (hijau) untuk
      objek VALID, dan `Dawan: Na'he (DRAF)` + badge oranye `DRAF — menunggu validasi TTU` +
      tombol `Audio belum tersedia` untuk `tikar_rumah`. **Terverifikasi di perangkat 20-11-2026.**

## Batch latar lama + objek transparan (21-11-2026, APK 76.311.135 byte)
Keputusan pemilik proyek: **Base_Map, Dapur, Rumah, Kebun semuanya memakai latar lama**
(perabot sudah tergambar), lalu sprite objek transparan baru diletakkan di sela kosong.
- [x] `GameSceneBuilder.BeginScene` selalu memakai `GameArtImporter.SceneSpritePath` (latar lama).
- [x] `AsetTransparan23Importer.Placements` (32 baris) + `TryGetPlacement`; `AddLearningObject`
      memakai posisi ternormalisasi ini, bukan crop lama.
- [x] Tinggi objek disetel ulang (Dapur 0,36–1,0 unit; Rumah 0,5–1,6; pohon Kebun 1,8; lain 0,6–1,5).
- [x] Penanda objek di belakang sprite (`sortingOrder` 45 → 4) agar hanya tampak halo di kaki.
- [x] Kebun: 12 objek; pohon (1,8 unit) berjajar di depan pagar atas agar tidak menimpa pohon seni.
- [x] Collider Dapur: ellipse (13,76; 7,90) radius 11,6 × 4,0 + koridor pintu (berhenti di dasar anyaman).
- [x] Collider Rumah: `Wall_Top` y 13,35 (berhenti di dasar batu dinding belakang) + koridor pintu.
- [x] Dialog pemandu: popup ilustrasi ketua adat; judul di plakat, isi di perkamen kanan,
      petunjuk halaman di kiri tombol panah.
- [x] Uji perangkat OPPO A3s: Base_Map, Dapur (objek + tidak bisa menaiki dinding + panel info),
      Kebun (objek di sela bedeng), Rumah (interior + objek), dialog ketua adat, panel info `Tikar`.
- [ ] Audio pelafalan di OPPO: berkas tersalin dan `MediaPlayer.start()` berjalan, tetapi
      perangkat membalas `MediaPlayerNative: error (1, -19)` (ENODEV); FMOD juga gagal `(60)`
      dengan error yang sama bahwa AudioTrack tidak mendapat output. Uji dengar ulang saat
      perangkat dingin / setelah reboot / di unit lain. Di Redmi Android 16 sudah terdengar.

## Fase A — Fondasi kurikulum (selesai 21-11-2026)
- [x] `LearningTopic.cs`: enum 12 topik + `LearningRole` (Utama/Pendukung) + katalog nama/kode.
- [x] `LearningObjectDefinition` + `LearningDatabase`: field `topicId/role/crossTopics/order` + query per topik.
- [x] `TopicMapping` + `TopicMappingApplier` (editor): backup + snapshot konten + tulis 4 field + diff + rollback otomatis. Hasil: 32 aset, 0 konten berubah, 124 soal tetap.
- [x] `TopicPanel` (UI Buku baru): 12 topik (2 kolom), hitungan utama/pendukung, progres utama, ketuk topik menampilkan daftar materi; `QuestPanel` lama tidak dihapus.
- [x] Save v2: `completedTopicIds` + migrasi v1→v2 (aditif). Terverifikasi di perangkat: data lama utuh.
- [x] APK build + uji perangkat: panel terbuka, navigasi topik/detail/Kembali/Tutup tanpa crash.
- Catatan: `BUILD RESULT: Succeeded` (76.324.671 byte, 22:23). Lisensi Unity sempat gagal (kode 198); berhasil setelah Unity Hub dijalankan.

## Fase B — Evaluasi 70/100 + gamifikasi dasar (selesai 22-11-2026)
- [x] `GamificationService` (XP 100/level, streak tanggal lokal, 9 badge, leaderboard lokal) + `ProfilePanel`.
- [x] Evaluasi: nilai 0–100, lulus ≥ 70; panel hasil menampilkan nilai + "Pelajari ulang" (maks 3 objek).
- [x] Anti-eksploit: XP baca sekali (`learnedObjectIds`), XP benar sekali per soal (kunci hash stabil, bukan indeks), bonus lulus sekali per scene (`xpPassScenes`), XP buah sekali (non-rescue), badge sekali (`earnedBadges`).
- [x] Save v3 aditif: `xp/level/streak/lastActivityDay/earnedBadges/bestScores/leaderboard/xpQuestions/xpPassScenes` + migrasi v2→v3.
- [x] HUD: pil `Lv X · n/100 XP` + tombol `Profil` (kiri `Buku`).
- [x] APK: 76.334.467 byte, 22-11-2026 23:37.
- Uji perangkat (OPPO A3s): migrasi v2→v3 (data lama identik); kuis 5/5 → 100/100 `Lulus!`; kuis 3/5 → 60/100 `Belum lulus` + review; soal duplikat benar → XP tetap (85); bonus lulus tidak berulang (`xpPassScenes=Dapur`, xp 105); naik level (100 XP → Lv 2); badge `lulus_dapur`,`kuis_sempurna`; leaderboard 2 entri; streak kemarin→2, lewat 3 hari→1; save asli dipulihkan (SHA256 sama).

## Fase C — Empat keterampilan: menulis, bicara, dialog tematik (selesai 22-11-2026)
- [x] `WritingExerciseGenerator` + `WritingPanel`: susun kata & lengkapi kalimat dari objek VALID; opsi unik; kunci stabil `w:<tipe>:<id>:<hash>`; XP +5 sekali per kunci.
- [x] `SpeechRecorder` + `SpeechPracticePanel`: rekam maks 10 dtk/44100 Hz, auto-stop, WAV PCM 16-bit sementara, putar ulang dari AudioClip memori, bersihkan saat panel ditutup dan saat startup; izin RECORD_AUDIO ditangani (ditolak/dicabut → pesan).
- [x] `AndroidPermissionInjector`: menyuntik RECORD_AUDIO ke manifest Gradle (idempotent, tanpa mengganti manifest utama); terverifikasi di APK via aapt.
- [x] `DialogueLibrary` + `NpcInteractable`: halaman dialog berubah sesuai `guideTalked`/`dapurCompleted`/`rumahCompleted`; hanya teks Indonesia, tanpa frasa Dawan baru.
- [x] Save v4 aditif (`xpWritings`), migrasi v3→v4; `GameState`/`SaveService`/`GamificationService` diperbarui.
- [x] `InfoPanel` dua baris tombol: Dengarkan/Kuis/Tutup lalu Latihan/Bicara.
- [x] APK Fase C: 76.363.787 byte, 22-11-2026 02:24 (`UabMeto_FaseC.apk`); checkpoint Fase B tidak berubah (SHA256 `B1243D…`).
- Uji perangkat: migrasi fixture v3→v4 (data identik, `xpWritings=[]`); Writing benar → +5 XP dan salah tanpa XP; pengulangan tanpa XP; SusunKata & Lengkapi berfungsi; izin mikrofon muncul & diberikan; `Microphone.devices` kosong di OPPO (keterbatasan perangkat) sehingga rekam/putar tidak dapat diuji di unit ini; WAV divalidasi lewat self-test (header valid, 176.444 byte, 88.200 sampel, PCM non-nol); cleanup WAV saat restart terbukti; dialog tematik tampil sesuai progres; kuis/HUD/hati tetap berjalan; hash 33 aset & 124 soal tidak berubah.

## Sisa pekerjaan
- [ ] Uji ulang build #7: hint buah, tombol kontrol baru, panel hasil kuis, penanda pintu.
- [ ] Verifikasi ulang spawn pintu Rumah (setelah masuk dari Base_Map, cek posisi pemain).
- [ ] Fase 7: uji simpan/migrasi/korupsi (backup `.bak`, karantina `.corrupt-*`).
- [ ] Fase 8: audio BGM/SFX + pengaturan volume/skema kontrol lengkap.
- [x] Lembar validasi Dawan TTU (`docs/DAWAN_VALIDATION_SHEET.md`) — terisi 32 baris,
      validator Ibu Palmerya Sau, 19/11/2026.
- [ ] Laporan akhir `docs/UAB_METO_IMPLEMENTATION_REPORT.md` (20 bagian) — bagian 19–20
      perlu pembaruan bukti Dawan/audio.

## Bukti uji kosakata Dawan (20-11-2026, APK 44.987.010 byte)
- Dataset: `LearningDataBuilder selesai: 32 objek, 32 spesifikasi`; 31 audio ter-link,
  1 null (`tikar_rumah`); 60 soal `usesDawan: 1`.
- Panel `Jagung` (Dapur): `Dawan: Pena`, badge hijau `TERVALIDASI`, tombol `Dengarkan`
  aktif, `Kuis` aktif.
- Panel `Tikar` (Rumah): `Dawan: Na'he (DRAF)`, badge oranye
  `DRAF — menunggu validasi TTU`, tombol `Audio belum tersedia` nonaktif,
  `Kuis (baca semua)` nonaktif.
- Panel `Bangku` (Rumah): `Dawan: toko`, `TERVALIDASI`.
- Panel `Lemari` (Rumah): `Dawan: lemari`, `TERVALIDASI` (objek ini tidak dibuatkan soal
  kata karena kata Dawan sama dengan bahasa Indonesia; hanya soal fungsi).
- Kuis Dapur dijalankan penuh 5 soal, semuanya soal Dawan:
  1/5 "Apa bahasa Dawan untuk Jagung?" → Pena
  2/5 "Bakul?" → Oko
  3/5 arti "Na'i Hana" → Alat masak
  4/5 arti "Na'i naijan" → Tempayan
  5/5 arti "Na'i" → Periuk
  Hasil `Lulus 5/5`, hati tetap 3/3, `dapurCompleted=true`, `rumahUnlocked=true`.
- Kendala perangkat: `FMOD failed to initialize the output device (60)` muncul saat start
  dan setelah restart. File audio + tombol + referensi data sudah benar; suara belum dapat
  dipastikan keluar dari speaker perangkat uji.

## Catatan teknis
- Latar: 2752×1536 → 27,52×15,36 unit, PPU 100, max 4096 (agar ukuran dunia tetap), Bilinear, ASTC 6×6 Android, mipmap off, Read/Write off.
- Player/ikon: Point, uncompressed.
- Save: `save.json` + `save.json.bak` + karantina `.corrupt-*`, versi + migrasi.
- Satu EventSystem persisten; satu modal aktif; tidak ada `LoadScene` di luar `SceneRouter`.
- Scene: `Base_Map` (index 0), `Dapur`, `Rumah`, `Kebun`.
