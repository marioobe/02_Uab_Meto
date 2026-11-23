# Stage 4 — Interactive Curriculum Learning

Status: 4A, 4B, 4C, 4D, 4E, 4F implemented.
Date: 2026-11-22.

## 1. Scope

Interactive learning from `LessonItemData` without changing Save v4:

- exercise pilihan ganda;
- dialogue berhalaman + audio;
- fallback referensi vocabulary invalid;
- scroll daftar topik/lesson/item;
- editor tests;
- Android build/device smoke.

Tidak ada perubahan format Save, aset, soal, scene, GUID, atau checkpoint APK.

## 2. Fitur

### 4A CurriculumExercisePanel
`Assets/Scripts/UI/CurriculumExercisePanel.cs`

- membaca `question`, `options`, `correctIndex`;
- koreksi benar/salah + feedback;
- menonaktifkan opsi setelah menjawab;
- tidak memakai hati/XP global dan tidak menulis Save;
- invalid `options < 2` atau `correctIndex` di luar batas → toast, tidak crash.

### 4B CurriculumDialoguePanel
`Assets/Scripts/UI/CurriculumDialoguePanel.cs`

- menampilkan `LessonItemData.lines` per halaman;
- tombol Lanjut/Tutup;
- tombol audio bila `audioRef` diisi;
- `DawanAudioPlayer.TryPlayPath` (overload baru) memutar `.m4a` dari `StreamingAssets/Dawan`;
- path absolut/traversal/ekstensi non-m4a ditolak;
- tidak menulis Save.

### 4C Fallback & scroll
- `CurriculumService.CanRender` = loaded + has content (tidak menuntut semua referensi valid).
- `TopicPanel` memakai kurikulum bila `CanRender`; vocabulary yang `objectRef`-nya hilang ditampilkan “tidak tersedia” dan tidak bisa dibuka.
- `TopicPanel` memakai `ScrollRect` + `RectMask2D` untuk daftar topik dan detail lesson/item.
- `IsUsable` tetap tersedia untuk semantik validasi penuh.

## 3. Alur

```
TopicPanel (curriculum mode)
  ├─ topic list (scroll)
  └─ lesson detail (scroll)
       ├─ vocabulary → InfoPanel lama → learnedObjectIds
       ├─ exercise  → CurriculumExercisePanel (sesi, tanpa Save)
       └─ dialogue  → CurriculumDialoguePanel (lines + audioRef)
```

## 4. Save v4

- `SaveData` dan `SaveService` **tidak diubah**.
- Tidak ada field baru, tidak ada migrasi.
- Progres vocabulary tetap `learnedObjectIds`.
- Exercise/dialogue belum menulis progres persisten.

### 4D Save v5
Progres lesson/item kini persisten secara aditif:

- `SaveData.completedLessonIds`
- `SaveData.completedItemIds`
- `completedTopicIds` (field v2) dipakai untuk topik kurikulum.
- `CurrentVersion = 5`.
- `Migrate()` menambah `v4→v5`; field lama tidak diubah.
- `TryLoad` mengisi default v5.
- `GameState.Apply` mengisi default v5.

Aturan:
- vocabulary → `learnedObjectIds`;
- exercise/dialogue → `completedItemIds`;
- lesson → `completedLessonIds` bila semua item selesai;
- topic → `completedTopicIds` bila semua lesson selesai;
- tidak ada XP/hati baru untuk exercise kurikulum.

## 5. Pengujian

Full regression: **247/247 PASS**, 0 failure.

Tambahan Stage 4 (di `CurriculumIntegrationTests`):

- exercise option + koreksi benar;
- dialogue halaman pertama/kedua;
- ref invalid tetap `CanRender` dan UI tetap tampil;
- scroll/daftar UI dibangun tanpa exception;
- Save v4 JSON → `TryLoad`/`Migrate` v5, field lama utuh;
- progres item/lesson/topic tersimpan di memori;
- fixture self-cleaning.

## 6. Android

- Build: `BUILD RESULT: Succeeded | 623,6 MB | errors: 0`.
- APK: `Builds/Android/UabMeto.apk`, size **76.385.883 byte**, SHA256
  `2BEB70C914E00D9E985CDC37DAF0E44D472A45B554E81FDA12F780C095C94AD5`.
- Device CPH1803: install/launch OK.
- Log: `[CurriculumService] Kurikulum kosong (belum ada materi PUBLISHED).`
- Save device dimuat: `Save dimuat: scene=Base_Map, hati=2, buah=0, dapur=True`
  (progres lama tetap ada; Save v4 dimigrasikan saat runtime).
- `FATAL EXCEPTION`: 0; tap “Buku” legacy tidak crash.

## 7. Batasan

1. UI curriculum-present dengan konten nyata belum diuji di perangkat (tidak ada konten produksi).
2. Exercise/dialogue kini menyimpan completedItemIds; belum ada XP/hati khusus kurikulum.
3. ~~Audio dialogue hanya `.m4a`~~ — ditutup Stage 5B: `.m4a` dan `.wav` didukung
   lewat `DawanAudioFormat`.
4. Completion lesson/topic memakai aturan “semua item selesai”; belum ada aturan bobot/opsional.
5. ~~Scroll diuji secara struktur/editor~~ — ditutup Stage 5B: gestur scroll pada
   daftar 24 item diuji di perangkat (lihat `docs/Stage5B_ScrollAudio.md`).

## 8. File

Baru:

- `Assets/Scripts/UI/CurriculumExercisePanel.cs`
- `Assets/Scripts/UI/CurriculumDialoguePanel.cs`
- `Assets/Scripts/Curriculum/CurriculumProgress.cs`

Diubah:

- `Assets/Scripts/UI/GameUi.cs`
- `Assets/Scripts/UI/TopicPanel.cs`
- `Assets/Scripts/Audio/DawanAudioPlayer.cs`
- `Assets/Scripts/Curriculum/CurriculumService.cs`
- `Assets/Scripts/Core/SaveData.cs`
- `Assets/Scripts/Core/SaveService.cs`
- `Assets/Scripts/Core/GameState.cs`
- `Assets/Editor/CurriculumIntegrationTests.cs`
- `Assets/Editor/CurriculumImporter.cs` (full regression hook)
