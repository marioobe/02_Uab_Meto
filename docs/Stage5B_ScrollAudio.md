# Stage 5B — Uji Scroll Daftar Panjang & Audio WAV

Status: selesai.
Tanggal: 2026-11-23.

## 1. Tujuan

Menutup dua batasan Stage 4 yang masih terbuka:

1. perilaku sentuh/scroll pada daftar panjang di perangkat;
2. dukungan `.wav` pada dialogue kurikulum (validator sudah menerimanya sejak
   Stage 1, pemutar runtime belum).

Konten uji bersifat sementara dan bukan konten produksi.

## 2. Perubahan kode

### 2.1 `Assets/Scripts/Audio/DawanAudioFormat.cs` (baru)

Satu sumber kebenaran format audio:

- `SupportedExtensions = { ".m4a", ".wav" }`, `PrimaryExtension = ".m4a"`;
- `TryNormalizePath` menolak path absolut, drive letter, traversal (`..`),
  nama kosong, dan ekstensi di luar daftar; subfolder tetap diizinkan
  (sama seperti aturan validator).

### 2.2 `Assets/Scripts/Audio/DawanAudioPlayer.cs`

- `TryPlayPath` memakai `DawanAudioFormat.TryNormalizePath` (sebelumnya
  menolak semua selain `.m4a` secara hardcode);
- pipeline cache/StreamingAssets membawa **nama file utuh**, sehingga `.wav`
  ikut disalin ke penyimpanan aplikasi dan diputar lewat MediaPlayer;
- `TryPlay(id, clip, volume)` (kosakata lama) tetap memakai `.m4a`.

### 2.3 `Assets/Editor/CurriculumValidator.cs`

- daftar `SupportedAudioExtensions` privat dihapus; validator memakai
  `DawanAudioFormat.IsSupportedExtension`, sehingga aturan runtime dan editor
  tidak dapat melenceng lagi. Aturan header RIFF/ftyp dan pesan error tidak
  berubah.

### 2.4 `Assets/Editor/CurriculumIntegrationTests.cs`

`AudioFormatSelfTest()` menambah **17 assertion**: m4a/wav/huruf besar/subfolder/
backslash diterima; mp3, ogg, traversal, traversal tersembunyi, absolut Unix,
absolut Windows, drive letter, tanpa ekstensi, hanya ekstensi, kosong, dan null
ditolak; allowlist runtime = m4a + wav.

`CurriculumDialoguePanel` hanya berubah komentar (m4a/wav).

## 3. Fixture sementara

- `Assets/Resources/Curriculum/curriculum.json` (dihapus setelah uji):
  - 12 topik, judul berakhiran `(UJI)`;
  - topik 1: 3 lesson, 24 item (7 kosakata, 6 latihan, 3 dialog: `.m4a`,
    `.wav`, dan `audioRef` yang filenya tidak ada);
  - topik 2: 2 item yang dapat diselesaikan penuh;
  - topik 3–12: 2 item masing-masing.
- `Assets/StreamingAssets/Dawan/uji_bunyi.wav` (dihapus setelah uji):
  1,5 detik, PCM 16-bit mono 22050 Hz, dibuat dengan Python `wave`.

Keduanya ikut terpaket di APK debug (diverifikasi: entri
`assets/Dawan/uji_bunyi.wav` ada dan penanda `Topik UJI 01` ditemukan di data
Resources).

## 4. Hasil perangkat (CPH1803, Android 8.1)

| Item | Hasil |
|---|---|
| Build debug | `BUILD DEBUG RESULT: Succeeded | 1017,6 MB | errors: 0` |
| Install debug | Success |
| `CurriculumService` | `Kurikulum dimuat: 12 topik PUBLISHED.` |
| Daftar topik | 12 kartu tampil dalam 2 kolom × 6 baris; topik 12 terlihat |
| Detail topik 1 | `3 pelajaran · 24 materi` |
| Scroll | Gestur swipe (dimulai di atas baris) menggeser daftar; kartu teratas keluar layar |
| Item terakhir | `Dialog UJI M4A-2` tercapai setelah scroll dan dapat diketuk |
| Dialogue | Panel modal tampil di atas TopicPanel; label `1/2 · lanjut` (navigasi halaman sudah diverifikasi Stage 5A; kode panel tidak berubah) |
| Audio `.m4a` | `DawanAudioPlayer: memutar .../bakul.m4a (3263 ms)` |
| Audio `.wav` | `DawanAudioPlayer: memutar .../uji_bunyi.wav (1500 ms)` |
| Audio hilang | warning `HTTP/1.1 404 Not Found`; panel tetap stabil, tanpa crash |
| Exercise | panel pilihan, koreksi `Benar.` dan opsi benar disorot hijau |
| Save v5 item | `completedItemIds: uji-t01-l1-i12, uji-t02-l1-i02` |
| Save v5 lesson/topic | `completedLessonIds: uji-t02-l1`; `completedTopicIds: uji-t02` |
| Field v4 | `hearts`, `fruit`, `dapurCompleted`, `rumahCompleted`, `kebunCompleted`, `xp`, `level`, `streak`, `learnedObjectIds` (27), `collectedPickupIds` (5), `xpWritings` — semuanya identik dengan backup |
| `FATAL EXCEPTION` aplikasi | 0 |
| Save restore | hash kembali sama dengan backup (`B53B8A44…`) |

Catatan lingkungan: perangkat sempat tertidur saat build produksi berjalan;
screenshot menjadi hitam dan activity berstatus STOP. Setelah layar dibangunkan,
aplikasi produksi berjalan normal. `FATAL EXCEPTION IN SYSTEM PROCESS` yang
muncul berasal dari perintah `svc power stayon` (SecurityException shell), bukan
dari aplikasi.

## 5. Temuan

1. **Batas 12 topik bersifat keras.** Grid 2 kolom × 6 baris × 98 px = 588 px,
   sedangkan viewport 600 px; daftar topik karena itu tidak pernah membutuhkan
   scroll vertikal. Topik ke-13 dan seterusnya akan berada di kolom ketiga
   (x≈1488) di luar layar karena horizontal scroll dimatikan. Sesuai desain
   "Buku — 12 Topik", tetapi perlu redesign bila kurikulum tumbuh.
2. **Audio hilang tidak memberi umpan balik visual.** `TryPlayPath` mengembalikan
   true karena ekstraksi berjalan asinkron; kegagalan 404 hanya menjadi warning
   di log, tanpa toast. Panel tidak crash. Kandidat perbaikan: toast dari
   `ExtractThenPlay` saat `fallbackClip == null`.
3. **Scroll realistis ada di daftar detail**, bukan daftar topik: 24 item
   menghasilkan tinggi konten ±1.668 px pada viewport 560 px.
4. Tap pada label kartu/baris tetap bekerja karena label memakai
   `raycastTarget = false` dan `Bg` menjadi target tombol; tidak ada bug input
   yang ditemukan.

## 6. Regression & build

- Full regression final (fixture bersih): **264/264 PASS**, 0 failure,
  0 NOT RUN.
  - Log: `C:\Users\ACER\AppData\Local\Temp\opencode\stage5b_full_final.log`.
- Build produksi: `BUILD RESULT: Succeeded | 623,7 MB | errors: 0`.
- APK produksi final: `Builds/Android/UabMeto.apk`
  - size **76.386.099 byte**
  - SHA256 `F463C48BC86118A9AA9384B75A96EDCE78C59912B6DCEC9C912A0137D49B3E17`
- APK produksi diverifikasi:
  - tidak ada entri `uji_bunyi.wav`;
  - tidak ada penanda `Topik UJI` / `uji_bunyi` di data Resources;
  - aplikasi tidak `DEBUGGABLE`.
- Perangkat dengan build produksi:
  - `[CurriculumService] Kurikulum kosong (belum ada materi PUBLISHED).`
  - `Save dimuat: scene=Base_Map, hati=2, buah=0, dapur=True`
  - Save v5 dengan array item/lesson/topic kosong; field v4 utuh.

## 7. Cleanup

- `Assets/Resources/Curriculum` + `.meta`: dihapus.
- `Assets/StreamingAssets/Dawan/uji_bunyi.wav` + `.meta`: dihapus
  (StreamingAssets kembali 31 `.m4a`, 0 `.wav`).
- Cache wav di perangkat (`files/Dawan/uji_bunyi.wav`): dihapus.
- `Builds/Android/UabMeto_Debug.apk`: dihapus.
- Save perangkat: direstorasi dari backup sebelum pengujian.
- Tidak ada file dengan nama mengandung `uji` di `Assets`.

## 8. Integritas data terlindungi

| Item | Hasil |
|---|---|
| 33 aset pembelajaran | **MATCH** `F75E12D2…` |
| 124 soal | **MATCH** `A4E5EF2B…` |
| 74 file data manifest | **0 mismatch** |
| Save v4 fixture | **MATCH** `2B4B6F70…` |
| APK checkpoint Fase B | **TIDAK BERUBAH** `B1243DB8…` |
| `Assets/Resources/Curriculum` | tidak ada |
| `Library/UabMetoStage2` | tidak ada |
| Source curriculum produksi | tidak ada |

## 9. Batasan

1. Topik ke-13+ belum didukung tata letak (lihat temuan 1).
2. Audio hilang hanya memberi warning log, tanpa toast (lihat temuan 2).
3. `.wav` diuji dengan file 1,5 detik mono 16-bit; variasi codec/bitrate lain
   belum diuji (validator tetap memeriksa header RIFF).
4. Konten produksi belum ada; pengujian perangkat berikutnya memerlukan fixture
   yang disetujui.
5. XP/hati untuk exercise kurikulum tetap tidak berubah (keputusan terpisah).

## 10. Rekomendasi

1. Putuskan desain bila jumlah topik dapat melebihi 12 (paginasi atau satu
   kolom dengan scroll).
2. Tambahkan toast kegagalan audio asinkron bila umpan balik dinilai perlu.
3. Lanjut ke Stage 5C (konten produksi) setelah materi disetujui TTU.
