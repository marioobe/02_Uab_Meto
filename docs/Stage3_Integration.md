# Stage 3 — Curriculum Runtime Integration

Status: implemented and tested (3A–3D).
Date: 2026-11-22.

## 1. Tujuan

Menghubungkan Curriculum JSON ke runtime Uab Meto tanpa mengganti sistem lama
(`LearningDatabase` + `LearningObjectDefinition`) dan tanpa mengubah Save v4.

Prinsip:

- satu jalur parser: `CurriculumLoader` → `CurriculumJson`;
- loader dipanggil sekali saat startup;
- konten hanya PUBLISHED;
- fallback penuh ke sistem lama bila kurikulum kosong/invalid;
- tidak ada migrasi Save, tidak ada perubahan aset/scene/GUID.

## 2. Arsitektur

```
[RuntimeInitializeOnLoadMethod BeforeSceneLoad]
  CurriculumService.Bootstrap()
    └─ GameObject "CurriculumService" (DontDestroyOnLoad)
        └─ Awake → LoadOnce()
              └─ CurriculumLoader.Load()
                    └─ Resources.Load<TextAsset>("Curriculum/curriculum")
                          ├─ null → CreateEmpty() (IsUsable=false)
                          ├─ invalid → log + CreateEmpty() (IsUsable=false)
                          └─ valid → FilterPublished()

Scene loaded
  CurriculumService.OnSceneLoaded()
    └─ FindFirstObjectByType<SceneController>()
        └─ BindDatabase(controller.database)
              └─ ValidateReferences (vocabulary objectRef)
                    ├─ valid → IsUsable=true
                    └─ invalid/missing → IsUsable=false (fallback)
```

### 2.1 CurriculumService

`Assets/Scripts/Curriculum/CurriculumService.cs`

- auto-bootstrap sebelum scene pertama;
- `LoadOnce()` idempotent + cache dari `CurriculumLoader`;
- `BindDatabase(LearningDatabase)` per scene;
- `IsUsable` = loaded + ada topik + referensi valid;
- accessor: `Topics`, `FindTopic`, `FindLesson`, `ResolveVocabulary`,
  `TryResolveVocabulary`, `IsVocabularyLearned`;
- tidak melempar exception ke UI;
- log startup: `[CurriculumService] Kurikulum kosong...` atau jumlah topik.

### 2.2 CurriculumMapping

`Assets/Scripts/Curriculum/CurriculumMapping.cs`

- id/order topik JSON → enum `LearningTopic` (`salam` → SalamSapaan, `12` → pekerjaan, dst.);
- nama tampilan topik/lesson;
- hitung vocabulary dan progres baca dari `GameState.Data.learnedObjectIds`;
- label/teks exercise & dialogue;
- **tidak menulis Save** dan tidak memakai key baru.

### 2.3 UI fallback

`Assets/Scripts/UI/TopicPanel.cs`

- `Show()` memilih mode:
  - `CurriculumService.IsUsable == true` → daftar topik/lesson/item kurikulum;
  - selain itu → perilaku lama persis (`LearningDatabase.ForTopic`).
- Item vocabulary membuka `InfoPanel` lama (progres `learnedObjectIds` tetap).
- Exercise/dialogue ditampilkan sebagai preview read-only melalui `DialoguePanel`.
- Back button kembali ke daftar mode aktif.

## 3. Perilaku kosong / rusak / referensi invalid

| Kondisi | Perilaku |
|---|---|
| Asset curriculum tidak ada | `CurriculumLoader` → empty; UI fallback legacy; tidak crash |
| JSON rusak | parse error → empty; UI fallback; log error |
| Topik PUBLISHED kosong | `IsUsable=false`; fallback legacy |
| Vocabulary ref tidak ada | `ReferencesValid=false`; fallback legacy |
| Database belum terikat | `ReferencesValid=false` sampai scene bind; fallback |

## 4. Kompatibilitas progres

- Save v4 tidak berubah.
- Progress lama (`learnedObjectIds`, `completedTopicIds`, XP) tetap.
- Progres vocabulary kurikulum dipetakan ke `learnedObjectIds` melalui `objectRef`.
- `completedTopicIds` tidak diisi otomatis; belum ada migrasi.
- Exercise/dialogue kurikulum belum menulis progres.

## 5. Pengujian

Full regression: `228/228 assertion PASS`, 0 failure.

Suite baru:

| Suite | Hasil |
|---|---|
| `CurriculumIntegrationTests` mapping | PASS |
| `CurriculumService` kosong/fixture/DRAFT/invalid ref/JSON rusak | PASS |
| `TopicPanel` curriculum-present smoke | PASS |
| Service fixture self-cleaning | PASS |

Suite lama tetap:

- Stage 1 23/23
- Parser 8/8
- Path guard 11/11 + T81 2/2
- Journal 10/10
- Backup 17/17
- Retention 18/18
- Promotion 17/17
- Recovery 44/44
- Lock 14/14
- Importer 25/25
- Loader 8/8
- Loader runtime 8/8

## 6. Android

- Build: `BUILD RESULT: Succeeded | 623,4 MB | errors: 0`.
- APK: `Builds/Android/UabMeto.apk`, SHA256 `6CB54C88535A47B5C35C6551669A72AE7A09FEA08704BC4625F759934113124B`.
- Device: CPH1803, install sukses, launch sukses.
- Log runtime:
  - `[CurriculumService] Kurikulum kosong (belum ada materi PUBLISHED).`
  - `CurriculumService:LoadOnce()`
- Tidak ada `FATAL EXCEPTION`.
- Metadata APK memuat `CurriculumService`, `CurriculumMapping`, `LoadOnce`,
  `CurriculumLoader`, `TopicPanel` (tidak di-strip).

## 7. Batasan / deferred

1. UI curriculum-present belum diuji di perangkat karena tidak ada konten produksi;
   jalur yang teruji di perangkat adalah fallback legacy.
2. Exercise/dialogue kurikulum masih preview read-only, belum menjalankan kuis
   atau pencatatan progres.
3. Daftar lesson/item belum memiliki scroll; jumlah besar perlu desain UI lanjutan.
4. Belum ada pemetaan completion topik kurikulum ke `completedTopicIds`.
5. `CurriculumService` memakai `FindFirstObjectByType<SceneController>()` saat
   scene load; scene tanpa `SceneController` tetap fallback.

## 8. File

Baru:

- `Assets/Scripts/Curriculum/CurriculumService.cs`
- `Assets/Scripts/Curriculum/CurriculumMapping.cs`
- `Assets/Editor/CurriculumIntegrationTests.cs`

Diubah:

- `Assets/Scripts/UI/TopicPanel.cs`

Tidak berubah:

- 33 learning assets, 124 questions, Save v4, 6 scene, GUID/meta lama,
  APK checkpoint Fase B, `LearningDatabase` dan sistem lama.
