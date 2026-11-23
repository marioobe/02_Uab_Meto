# PROJECT GAP ANALYSIS — UAB METO

> Audit kesenjangan antara kondisi proyek saat ini dan `UAB_METO_MASTER_CONTEXT.md`.
> Dibuat: 21-11-2026. Sifat: hasil audit + rekomendasi urutan pengerjaan.
> Dokumen ini **tidak** mengubah kode; perubahan hanya dimulai setelah persetujuan pemilik.

---

## 1. Ringkasan

Inti game petualangan 2D (karakter 360°, kamera follow, 4 scene, interaksi objek, NPC,
dialog, kuis, hati, save lokal, audio pelafalan) **sudah berjalan** dan baru dipoles.
Yang belum ada adalah lapisan **kurikulum 12 topik**, **empat keterampilan** (khususnya
Speaking & Writing), **gamifikasi** (XP/level/streak/badge/leaderboard), **backend/AI**,
**admin dashboard**, dan **dokumentasi PRD** (file `PRD.md` belum pernah dibuat).

---

## 2. Kondisi saat ini (fakta + bukti)

| Aspek | Isi | Bukti |
|-------|-----|-------|
| Scene aktif | `Base_Map`, `Dapur`, `Rumah`, `Kebun` (index 0–3) | `ProjectSettings/EditorBuildSettings.asset` |
| Scene cadangan | `VillageScene`, `RoundHouseInterior` (tidak dibangun) | tidak ada di Build Settings |
| Konten | 32 objek belajar: Dapur 10, Rumah 10, Kebun 12 + 1 database | `Assets/Data/Learning/**` (33 `.asset`) |
| Audio | 31 rekaman penutur (1 null: `tikar_rumah`); M4A di StreamingAssets | `docs/rekaman penutur/*.ogg`, `Assets/StreamingAssets/Dawan/*.m4a` |
| Kuis | 5 soal/scene, lulus **60%** (3/5), soal Dawan dua arah | `Assets/Scripts/UI/QuizPanel.cs` |
| Progres | `hearts`, `fruit`, `learnedObjectIds`, `collectedPickupIds`, flag scene | `Assets/Scripts/Core/SaveData.cs` (v1) |
| Navigasi | Tanpa splash/menu/profil; boot langsung `Base_Map` | `GameBootstrap` + Build Settings |
| NPC & misi | 1 NPC pemandu + dialog + panel "Buku" status bab per lokasi | `NpcInteractable`, `QuestPanel` |
| Backend | Tidak ada; save lokal `save.json` + `.bak` + karantina `.corrupt-*` | `SaveService.cs` |
| Izin Android | Hanya `INTERNET`; **tanpa `RECORD_AUDIO`** | manifest hasil build |
| Skrip legacy | 8 skrip hanya dipakai 2 scene cadangan; `Interactable.cs`, `SceneTransition.cs` tidak dipakai scene mana pun | pelacakan GUID |

Kesimpulan: **fondasi teknis kuat, tetapi arsitektur konten masih berbasis scene, bukan
topik; belum ada data-driven curriculum, evaluasi 70/100, gamifikasi, speaking/writing,
maupun backend.**

---

## 3. Tabel kesenjangan fitur (Master Context vs aktual)

| # | Kebutuhan | Status | Bukti/Catatan | Prioritas |
|---|-----------|--------|---------------|-----------|
| 1 | Kurikulum 12 topik | Belum | `LearningObjectDefinition` hanya punya `sceneKey`, tanpa `topicId` | P0 |
| 2 | Learning system dinamis per topik | Sebagian | konten terikat scene; belum ada pemilih topik | P0 |
| 3 | Listening | Ada (Sebagian) | audio pelafalan per objek; perangkat OPPO bermasalah (lihat §7) | P0 (uji) |
| 4 | Reading | Ada | panel info + dialog | — |
| 5 | Speaking | Belum | tanpa izin mikrofon, tanpa rekam/putar-ulang, tanpa penilaian | P1 |
| 6 | Writing | Belum | hanya pilihan ganda; belum susun kata/melengkapi kalimat | P1 |
| 7 | Evaluasi ≥ 70/100 + simpan hasil + saran ulang | Berbeda | sekarang 60% (3/5), tanpa skor 0–100 & rekomendasi | P0 |
| 8 | XP, level, streak, badge, leaderboard | Belum | tanpa field & UI | P1 |
| 9 | Sistem misi (bukan sekadar status bab) | Sebagian | `QuestPanel` hanya status lokasi | P1 |
| 10 | Splash → Menu Utama → Profil → Mulai/Lanjut | Belum | boot langsung `Base_Map` | P1 |
| 11 | Firebase/Supabase/backend | Belum | hanya `INTERNET`, tanpa SDK/backend | P2 |
| 12 | Fitur AI + kuota (10/3/10) | Belum | tanpa kode AI/backend proxy | P2 |
| 13 | Admin Dashboard | Belum | di luar repo; tanpa skema API | P2 |
| 14 | `Rumah_Adat` (budaya & adat) | Belum | hanya 4 scene | P2 |
| 15 | NPC warga beragam + dialog tematik | Sebagian | 1 NPC pemandu | P1 |
| 16 | Pembukaan lokasi berdasar penguasaan | Sebagian | Rumah terbuka setelah Dapur selesai; belum berbasis topik | P0 |
| 17 | Hearts/nyawa | Ada | 3 hati + buah pemulihan | — |
| 18 | Karakter 360°, kamera follow, kontrol virtual | Ada | `PlayerMovement2D`, `CameraFollow2D`, D-Pad/joystick | — |
| 19 | Dokumentasi PRD/desain | Kurang | `PRD.md` belum ada | P0 (dok) |
| 20 | Hindari sistem duplikat | Risiko | 2 skrip tak terpakai + 8 skrip legacy cadangan | P2 |

---

## 4. Konflik yang perlu diputuskan

1. **Berbasis scene → berbasis topik.** Konten kini 100% terikat scene. Menambah `topicId`
   menyentuh 32 aset + 60 soal + `LearningDatabase` + UI "Buku" (daftar bab → daftar topik).
2. **Ambang kelulusan** 60% → **70/100** (perlu skema skor 0–100, bukan sekadar benar/salah).
3. **Speaking** butuh izin `RECORD_AUDIO` + perekaman + pemutaran ulang; penilaian AI
   menunggu backend (keputusan #5).
4. **Audio OPPO A3s** — jalur MediaPlayer sampai `start()` berjalan, tetapi perangkat
   menolak output audio (`MediaPlayerNative: error (1, -19)` ENODEV; FMOD juga `60`),
   meski speaker terdeteksi dan volume 16/16. Tidak boleh dianggap berhasil sampai
   terdengar nyata (keputusan #9).
5. **Gamifikasi** ditunda sampai fondasi kurikulum + evaluasi stabil (keputusan #6).

---

## 5. Pemetaan 32 objek ke 12 topik (DISETUJUI)

> Status final (disetujui 21-11-2026). Hanya memakai kosakata yang **sudah divalidasi**
> (31 VALID + 1 DRAF). Topik tanpa materi **tidak diisi kosakata karangan**.
> Peran `Utama` = materi inti topik; `Pendukung` = konteks visual/pengulangan/duplikat.

| # | id | Scene | Topik | Status | Peran | Alasan |
|---|----|-------|-------|--------|-------|--------|
| 1 | periuk | Dapur | 5. Aktivitas sehari-hari | VALID | Utama | Alat memasak inti |
| 2 | tungku | Dapur | 5. Aktivitas sehari-hari | VALID | Utama | Perapian memasak tradisional |
| 3 | kayu_bakar | Dapur | 5. Aktivitas sehari-hari | VALID | Utama | Bahan bakar memasak harian |
| 4 | alat_masak | Dapur | 5. Aktivitas sehari-hari | VALID | Utama | Nama umum peralatan memasak |
| 5 | cobek | Dapur | 5. Aktivitas sehari-hari | VALID | Utama | Mengolah bumbu harian *(lintas: Budaya)* |
| 6 | jagung | Dapur | 6. Makanan & minuman | VALID | Utama | Makanan pokok |
| 7 | tomat | Kebun | 6. Makanan & minuman | VALID | Utama | Bahan pangan |
| 8 | cabai | Kebun | 6. Makanan & minuman | VALID | Utama | Bahan pangan/bumbu |
| 9 | terong | Kebun | 6. Makanan & minuman | VALID | Utama | Bahan pangan |
| 10 | daun_pisang | Kebun | 6. Makanan & minuman | VALID | Pendukung | Pembungkus makanan *(lintas: Budaya, Aktivitas)* |
| 11 | bakul_hasil | Kebun | 6. Makanan & minuman | VALID | Pendukung | Hasil panen yang akan diolah *(lintas: Pekerjaan)* |
| 12 | tempat_tidur | Rumah | 7. Rumah & benda | VALID | Utama | Perabot tidur |
| 13 | lemari | Rumah | 7. Rumah & benda | VALID | Utama | Perabot penyimpanan |
| 14 | peti_kayu | Rumah | 7. Rumah & benda | VALID | Utama | Wadah penyimpanan |
| 15 | rak_piring | Rumah | 7. Rumah & benda | VALID | Utama | Perabot dapur-rumah |
| 16 | meja_makan | Rumah | 7. Rumah & benda | VALID | Utama | Perabot makan |
| 17 | bangku_rumah | Rumah | 7. Rumah & benda | VALID | Utama | Tempat duduk |
| 18 | bangku | Dapur | 7. Rumah & benda | VALID | Utama | Tempat duduk |
| 19 | tempayan | Dapur | 7. Rumah & benda | VALID | Utama | Wadah air |
| 20 | tempayan_rumah | Rumah | 7. Rumah & benda | VALID | Pendukung | Duplikat kata `tempayan`; pengulangan |
| 21 | keranjang | Rumah | 7. Rumah & benda | VALID | Utama | Wadah anyaman *(lintas: Belanja)* |
| 22 | bakul | Dapur | 7. Rumah & benda | VALID | Utama | Wadah anyaman *(lintas: Belanja)* |
| 23 | tikar | Dapur | 7. Rumah & benda | VALID | Utama | Perabot lantai *(lintas: Budaya)* |
| 24 | tikar_rumah | Rumah | 7. Rumah & benda | DRAF | Pendukung | Duplikat `tikar` + belum valid; tidak jadi soal |
| 25 | sumur | Kebun | 8. Tempat & arah | VALID | Utama | Sumber air / fasilitas tempat |
| 26 | selimut_tenun | Rumah | 10. Budaya & adat | VALID | Utama | Kain tenun ikat (kerajinan budaya) |
| 27 | pohon_jeruk | Kebun | 12. Pekerjaan & sosial | VALID | Utama | Bertani/berkebun |
| 28 | pohon_kelapa | Kebun | 12. Pekerjaan & sosial | VALID | Utama | Bertani/berkebun |
| 29 | pohon_mangga | Kebun | 12. Pekerjaan & sosial | VALID | Utama | Bertani/berkebun |
| 30 | kebun_sayur | Kebun | 12. Pekerjaan & sosial | VALID | Utama | Lahan bertani *(lintas: Tempat)* |
| 31 | bibit | Kebun | 12. Pekerjaan & sosial | VALID | Utama | Proses bertani |
| 32 | alat_kebun | Kebun | 12. Pekerjaan & sosial | VALID | Utama | Peralatan bertani |

**Ringkasan:** 28 `Utama`, 4 `Pendukung` (daun_pisang, bakul_hasil, tempayan_rumah,
tikar_rumah).

**Rekap per topik:**

| Topik | Objek | Jml |
|-------|-------|-----|
| 1 Salam & sapaan | — | 0 |
| 2 Perkenalan diri | — | 0 |
| 3 Keluarga | — | 0 |
| 4 Angka, waktu, hari | — | 0 |
| 5 Aktivitas sehari-hari | periuk, tungku, kayu_bakar, alat_masak, cobek | 5 |
| 6 Makanan & minuman | jagung, tomat, cabai, terong, daun_pisang, bakul_hasil | 6 |
| 7 Rumah & benda | tempat_tidur, lemari, peti_kayu, rak_piring, meja_makan, bangku_rumah, bangku, tempayan, tempayan_rumah, keranjang, bakul, tikar, tikar_rumah | 13 |
| 8 Tempat & arah | sumur | 1 |
| 9 Belanja & tawar-menawar | — (bakul/keranjang dipakai lintas topik sebagai penopang) | 0 |
| 10 Budaya & adat Dawan | selimut_tenun | 1 |
| 11 Kesehatan & darurat | — | 0 |
| 12 Sekolah/pekerjaan/sosial | pohon_jeruk, pohon_kelapa, pohon_mangga, kebun_sayur, bibit, alat_kebun | 6 |

Total: 5 + 6 + 13 + 1 + 1 + 6 = **32** ✓.
Topik "Segera Hadir": 1, 2, 3, 4, 9, 11 (6 topik).

### Aturan penanganan objek duplikat & lintas topik

1. **Objek dengan kosakata sama tidak dihitung sebagai kosakata baru.** Contoh:
   `tempayan` vs `tempayan_rumah` (Na'i naijan), `tikar` vs `tikar_rumah` (Na'he).
2. **Satu materi utama per kosakata.** Objek duplikat (`tempayan_rumah`, `tikar_rumah`)
   hanya menjadi **konteks visual / pengulangan**, tidak membuat soal baru.
3. **Objek lintas topik** (`bakul`, `keranjang`, `tikar`, `cobek`, `daun_pisang`,
   `kebun_sayur`) muncul di topik kedua **sebagai materi pendukung**, bukan materi utama
   kedua.
4. **`tikar_rumah` tetap DRAF** — tanpa audio, tanpa soal Dawan, tidak menjadi materi utama
   sampai divalidasi penutur.
5. Tidak ada aset/ID/data objek lama yang dihapus; pemetaan hanya menambah metadata
   (`topicId`/`role`/`crossTopics`), tidak memindahkan file.

---

## 6. Strategi migrasi Save v1 → v2

Kebijakan: **tidak ada progres pengguna yang hilang.**

- `SaveService` sudah punya mekanisme `CurrentVersion` + `Migrate(...)` (v0 → v1 saat ini).
- Fase A menaikkan `CurrentVersion` ke 2 dan menambah `Migrate` untuk v1 → v2:
  - Pertahankan semua field v1 (`hearts`, `fruit`, `learnedObjectIds`,
    `collectedPickupIds`, flag `dapurCompleted/rumahUnlocked/...`, volume, `controlScheme`).
  - Tambah field baru **dengan nilai default aman** (mis. `xp = 0`, `level = 1`,
    `streak = 0`, `bestScores` per topik kosong, `topicProgress` kosong).
  - `Migrate` hanya mengisi nilai default; **tidak menimpa** data lama.
- Validasi saat memuat tetap ada (clamp `hearts`, `fruit ≥ 0`, list non-null).
- Simpan lama tetap terbaca; `save.json.bak` + karantina `.corrupt-*` dipertahankan.
- Uji migrasi: save v1 asli dimuat → `saveVersion` jadi 2 → semua data v1 utuh + field
  baru bernilai default → simpan ulang → muat lagi (idempoten).

---

## 7. Pemeriksaan ulang audio OPPO A3s

Fakta terbaru (21-11-2026):
- Berkas audio tersalin benar (`Android/data/com.uabmeto.village/files/Dawan/tikar.m4a`).
- `MediaPlayer.start()` dipanggil, lalu `MediaPlayerNative: error (1, -19)` (ENODEV).
- Bersamaan, seluruh audio Unity gagal: `AudioTrack: Could not get audio output ...
  initCheck() = -19` → `FMOD failed (60)`.
- Speaker terdeteksi (`dumpsys audio` → `Devices: speaker`), volume media 16/16, tidak mute.

Kesimpulan: kegagalan ada di **driver/HAL perangkat uji**, bukan di kode aplikasi, tetapi
**belum terbukti berbunyi**. Tindak lanjut wajib (belum tuntas):
1. Uji dengar ulang setelah perangkat dingin / reboot bersih / cabut charger.
2. Bandingkan dengan perangkat lain (Redmi Android 16 — sudah terdengar 20-11-2026).
3. Jika tetap ENODEV, periksa apakah jack headset/USB/Bluetooth sedang "aktif semu".
4. Jangan menutup item ini sebelum ada **bukti dengar nyata** (bukan sekadar "tanpa error").

---

## 8. Roadmap fase: kriteria selesai, pengujian, laporan

Aturan umum (keputusan #8): tiap fase punya kriteria selesai + pengujian + laporan, dan
**tidak lanjut tanpa persetujuan pemilik**.

### Fase A — Fondasi kurikulum (P0)
- **Lingkup**: `topicId`/`role`/`crossTopics`/`order` di `LearningObjectDefinition`;
  pemetaan 32 objek (**disetujui**, §5); `LearningDatabase` filter per topik; UI "Buku" →
  daftar 12 topik; Save v2 + migrasi.
- **Kriteria selesai**: 32 objek punya `topicId` benar sesuai §5; pemilih topik menampilkan
  12 topik (6 "Segera Hadir"); save v1 termuat → migrasi v2 → data utuh; objek duplikat &
  lintas topik mengikuti aturan §5.
- **Pengujian**: migrasi save v1 asli di perangkat; navigasi topik; tidak ada objek hilang;
  kuis Dapur/Kebun/Rumah yang sudah ada tetap lulus.
- **Laporan**: `docs/` + baris bukti uji di `TODO_UAB_METO.md`.

### Fase B — Evaluasi 70/100 + gamifikasi dasar (P1)
- **Lingkup**: skor 0–100, ambang 70, simpan hasil + saran materi ulang; XP/level/streak/
  badge/leaderboard **lokal**.
- **Kriteria selesai**: ujian akhir menilai 0–100; kelulusan 70; hasil tersimpan &
  dapat dilihat; XP/level/streak/badge tampil; leaderboard lokal berisi skor lokal.
- **Pengujian**: ujian lulus (<70 gagal, ≥70 lulus); progres tampil setelah restart.
- **Laporan**: hasil ujian + tangkapan layar.

### Fase C — Empat keterampilan (P1)
- **Lingkup**: latihan menulis (susun kata / melengkapi kalimat); speaking bertahap
  (izin mikrofon → rekam sementara → putar ulang); dialog NPC tematik.
- **Kriteria selesai**: latihan menulis berfungsi + dinilai; rekam & putar ulang suara
  sendiri berfungsi; tanpa menyimpan audio ke server.
- **Pengujian**: izin mikrofon, rekam/putar ulang, latihan menulis di perangkat.
- **Laporan**: bukti + catatan keterbatasan.

### Persiapan validasi konten (6 topik kosong)
- **Lingkup**: menyusun daftar materi untuk 6 topik "Segera Hadir" (1, 2, 3, 4, 9, 11),
  diajukan ke penutur TTU untuk validasi; tidak ada kosakata yang dikarang.
- **Kriteria selesai**: draft materi per topik + lembar validasi terisi → status VALID/DRAF.
- **Pengujian**: pengecekan silang oleh validator; tidak ada kosakata tanpa sumber.
- **Laporan**: `docs/DAWAN_VALIDATION_SHEET.md` diperbarui + bukti rekaman.

### Fase D — Backend & AI (P2)
- **Lingkup**: rancang skema data & kebutuhan integrasi dulu (keputusan #7); Firebase auth/
  progres, Supabase audio, proxy AI + kuota (10/3/10), sinkronisasi local-first.
- **Kriteria selesai**: auth + sinkronisasi progres; audio bisa di-update tanpa APK baru;
  proxy AI aman (tanpa API key di Unity) + kuota.
- **Pengujian**: login, sinkronisasi, ganti audio server, kuota AI.
- **Laporan**: arsitektur + hasil uji.

### Fase E — Admin Dashboard (P2)
- **Lingkup**: web terpisah: 12 topik, kosakata, dialog, audio, validasi, publikasi,
  pengguna, progres, statistik.
- **Kriteria selesai**: CRUD topik/kosakata/audio; alur validasi → publikasi.
- **Pengujian**: unggah audio, validasi, publikasi, lihat statistik.
- **Laporan**: panduan + bukti.

### QA akhir (P2)
- **Lingkup**: `Rumah_Adat`, uji 2 perangkat (OPPO + Redmi), performa, regression penuh,
  bukti audio terdengar.
- **Kriteria selesai**: semua topik tervalidasi & terpublikasi; regression lolos; audio
  terdengar nyata di kedua perangkat.
- **Pengujian**: regression penuh (lihat `TODO_UAB_METO.md` kriteria Q) + audio dengar.
- **Laporan**: lembar validasi + bukti uji lintas perangkat.

---

## 9. Risiko utama

- **Migrasi data konten**: menambah `topicId` menyentuh 32 aset & 60 soal; perlu skrip
  builder yang reproducible (bukan edit manual).
- **Skema skor 70/100**: mengubah `QuizPanel` dari benar/salah menjadi skor, tanpa merusak
  kuis Dawan yang sudah lulus.
- **Audio perangkat**: OPPO A3s belum terbukti berbunyi (ENODEV); jangan tunda QA audio.
- **Speaking**: izin & perekaman Android memerlukan penanganan penolakan izin + latensi.
- **Duplikasi sistem**: skrip legacy cadangan perlu dikelola (biarkan, tapi jangan dipanggil
  dari scene aktif) agar tidak membingungkan pengembangan.
- **Performa**: perangkat kelas bawah + tekstur besar; perlu pengukuran nyata RAM/FPS.

---

## 10. Keputusan yang sudah disetujui pemilik (21-11-2026)

1. Pemetaan 32 objek → 12 topik (§5) **disetujui**.
2. `selimut_tenun` dipindahkan ke topik 10 (Budaya & adat) **disetujui**.
3. Enam topik kosong (1, 2, 3, 4, 9, 11) berstatus **"Segera Hadir"**.
4. Objek lintas topik dipakai sebagai **materi pendukung** (disetujui).
5. `tikar_rumah` tetap **DRAF**, tidak menjadi materi utama/soal sebelum divalidasi.
6. Urutan roadmap: **A → B → C → Persiapan validasi konten → D → E → QA akhir**.
7. Aturan tambahan: tidak menghapus aset/ID/data lama; kosakata sama tidak dihitung baru;
   satu materi utama per kosakata; pertahankan Save v1 + migrasi teruji; materi belum
   valid tidak dipublikasikan; tidak mengarang kosakata.

**Menunggu persetujuan:** rencana teknis rinci **Fase A** (lihat §8) — tidak ada perubahan
kode sebelum disetujui.
