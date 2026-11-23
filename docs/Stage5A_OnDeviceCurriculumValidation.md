# Stage 5A — Android On-Device Curriculum Validation

Status: completed.
Date: 2026-11-23.

## 1. Tujuan

Memvalidasi UI curriculum-present dan Save v5 pada perangkat fisik menggunakan
konten uji internal dan build debuggable, tanpa menerbitkan konten produksi.

## 2. Metode

- Build debug: `BuildOptions.Development | BuildOptions.AllowDebugging`,
  output `Builds/Android/UabMeto_Debug.apk` (sementara).
- Fixture `Assets/Resources/Curriculum/curriculum.json` (sementara, bukan produksi):
  topik `salam`, lesson `salam-1`, vocabulary `periuk`, exercise,
  dialogue dengan `audioRef=periuk.m4a`.
- Save perangkat dibackup ke host sebelum uji:
  `stage5a_save_backup/save_before.json` dan `.bak`.
- Setelah uji: fixture dihapus, Save perangkat direstorasi, debug APK dihapus,
  APK produksi dibangun ulang tanpa fixture.

## 3. Hasil perangkat (CPH1803, Android 8.1)

| Item | Hasil |
|---|---|
| Debug APK install | Success |
| `CurriculumService` | `Kurikulum dimuat: 1 topik PUBLISHED` |
| Buku | Menampilkan topik `Salam dan sapaan (UJI)`, 1 pelajaran, 3 materi |
| Detail lesson | Vocabulary `Periuk — Na'i` (dipelajari), Latihan, Dialog |
| Exercise | Panel muncul, pilihan, koreksi benar |
| Dialogue | Halaman 1/2 dan 2/2, tombol Lanjut/Tutup |
| Audio dialog | `DawanAudioPlayer: memutar .../periuk.m4a (3343 ms)` |
| Save v5 | `completedItemIds: salam-1-ex, salam-1-dlg`; `completedLessonIds: salam-1`; `completedTopicIds: salam` |
| Save lama | `hearts=2`, `dapurCompleted=true`, `rumahCompleted=true`, `xp=5`, learned/pickup tetap |
| FATAL EXCEPTION | 0 |
| Save restore | Hash file kembali sama dengan backup sebelum uji |

## 4. Bug ditemukan & diperbaiki

Panel exercise/dialogue terbuka **di belakang** `TopicPanel` karena urutan sibling
`ModalCanvas`. Diperbaiki dengan `transform.SetAsLastSibling()` pada:

- `CurriculumExercisePanel.Show`
- `CurriculumDialoguePanel.Show`
- `InfoPanel.Show`
- `DialoguePanel.Show`

Setelah rebuild debug, panel tampil dan dapat dioperasikan.

## 5. Cleanup produksi

- `Assets/Resources/Curriculum` dihapus (file + `.meta`).
- `Builds/Android/UabMeto_Debug.apk` dihapus (mengandung fixture).
- APK produksi final: `Builds/Android/UabMeto.apk`
  - size **76.387.411 byte**
  - SHA256 `490D3132E35EEE53F03B7B83A350365CC6FE02204BF733C27157DD6DDF2A3BD4`
- Produksi di perangkat: `[CurriculumService] Kurikulum kosong...`; tidak ada fixture.
- Save perangkat: v5, tanpa item/lesson/topic uji.

## 6. Pengujian

- Full regression: **247/247 PASS**, 0 failure (setelah Stage 5B: **264/264**).
- Data: 33 aset + 124 soal + 74 file data manifest, 0 mismatch.
- APK checkpoint Fase B tidak berubah.

## 7. Batasan

1. Scroll dengan daftar sangat panjang belum diuji gestur visual.
2. Audio hanya `.m4a`; `.wav` belum.
3. Konten uji internal sudah dihapus; pengujian perangkat berikutnya memerlukan
   fixture yang disetujui.
4. `adb backup` tidak dapat diselesaikan (dialog nonaktif); backup dilakukan
   dengan pull file Save setelah build debuggable.
