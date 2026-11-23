# Uab Meto

Aplikasi pembelajaran bahasa **Dawan** (Timor) untuk Android, dibuat dengan
**Unity 6** (6000.6.0f1). Pemain menjelajahi desa Timor dan mempelajari
kosakata Dawan lewat 3 lokasi, kuis, latihan, dan pelafalan audio.

> **Catatan repo ini:** berisi **kode, alur, dan dokumen** saja (repo ringan).
> Aset gambar/audio (`Assets/Art`, `Assets/Audio`, `Assets/StreamingAssets`,
> `Assets/foto`) **tidak disertakan**, sehingga proyek tidak bisa langsung
> di-build/dijalankan tanpa memulihkan aset tersebut. Bagian "Peta isi" di bawah
> menjelaskan di mana aset seharusnya berada.

## Ringkasan

| Aspek | Nilai |
|---|---|
| Platform | Android (arm64, minSdk 26) |
| Engine | Unity 6, build IL2CPP |
| Konten | 3 lokasi (Dapur, Rumah, Kebun), 33 objek belajar, 124 soal |
| Fitur | eksplorasi, panel info, kuis, latihan, bicara (rekam), pelafalan Dawan, save progres |
| Save | `save.json` di penyimpanan aplikasi, versi 5 + migrasi |
| Bahasa UI | Indonesia; materi dalam bahasa Dawan |

## Peta isi

```
Assets/
  Scripts/                  seluruh logika runtime
    Core/                   bootstrap, save, state, movement, camera
    Audio/                  AudioManager, DawanAudioPlayer, format audio
    Characters/             animator & gerak karakter
    Interaction/            trigger objek, pintu, NPC, buah
    Curriculum/             pipeline kurikulum (JSON -> service)
    Speech/                 perekaman suara (latihan bicara)
    UI/                     panel menu, info, kuis, pengaturan, dll
  Editor/                   alat pipeline (editor-only)
    GameSceneBuilder.cs     membangun scene, collider, pintu, posisi objek
    AsetTransparan23Importer.cs   tabel posisi objek (Placements)
    GameArtImporter.cs      impor latar 2752x1536
    YuutaSheet4x4Importer.cs      impor frame karakter
    CurriculumImporter.cs   impor & self-test kurikulum
    CurriculumValidator.cs  validasi kurikulum
    AndroidBuilder.cs       build APK (arm64 / universal)
  Data/Learning/            33 LearningObjectDefinition + LearningDatabase.asset
  Scenes/                   6 scene (Base_Map, Dapur, Rumah, Kebun, ...)
  Art/                      [TIDAK DISERTAKAN] latar & sprite (Scenes, Learning2, Characters, UI)
  Audio/ + StreamingAssets/ [TIDAK DISERTAKAN] rekaman pelafalan Dawan (.wav / .m4a)
ProjectSettings/            pengaturan proyek & versi Unity
Packages/manifest.json      daftar paket Unity
docs/                       laporan tahap, test matrix, prompt aset
Builds/Android/dist/Bukti_Spotlight/  screenshot bukti uji (disertakan)
```

## Alur runtime

1. `GameBootstrap.Init()` (`[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`)
   membuat `GameRoot` (persisten) dan menambahkan service:
   `SaveService`, `SettingsService`, `HealthService`, `InventoryService`,
   `ProgressService`, `AudioManager`, `SceneRouter`, `ScreenFader`, `ToastView`.
2. `SaveService.Load()` membaca `save.json` (versi 5) dan menjalankan migrasi
   bila versi lama.
3. `CurriculumService` memuat kurikulum lewat `CurriculumLoader` saat startup.
4. `SceneRouter` membuka scene pertama: `Base_Map` (Kampung).
5. Scene dibangun saat *build* oleh `GameSceneBuilder` (editor), yang membuat
   collider, pintu, titik interaksi objek, NPC, dan pemain.

## Alur scene (permainan)

```
Base_Map (Kampung)
  ├─ Dapur Bulat   (pintu 32,5%/31,6%)   -> terbuka
  ├─ Rumah Warga   (pintu 73,4%/28,4%)   -> terkunci sampai materi+kuis Dapur selesai
  └─ Kebun         (gerbang 81%/73%)     -> terbuka
```

Setiap lokasi berisi objek belajar; mendekat memunculkan halo + tombol "Baca",
yang membuka panel info (nama, arti, audio, kuis, latihan).

## Alur data

```
Curriculum JSON  ->  CurriculumValidator  ->  CurriculumImporter  ->  CurriculumLoader
   (isi materi)        (cek aturan)            (simpan ke asset)       (baca saat runtime)
```

- `LearningDatabase` (ScriptableObject) menyimpan `LearningObjectDefinition`
  per objek (id, nama Dawan, arti, audio, kuis).
- Save menyimpan `completedLessonIds`, `completedItemIds`, `completedTopicIds`.

## Audio

- `AudioManager`: SFX prosedural (klik, benar, salah, pintu, hati) + musik latar
  (`Resources/Music/bgm_village`, loop, ducking saat pelafalan).
- `DawanAudioPlayer`: memutar pelafalan lewat `android.media.MediaPlayer`
  (PCM 16-bit) untuk perangkat yang menolak output FMOD; fallback AudioSource.
- `DawanAudioFormat`: allowlist ekstensi `.m4a` / `.wav`.

## Build Android

```powershell
Unity.exe -batchmode -quit -disableAssetImportWorkerProcess `
  -projectPath <path> -executeMethod AndroidBuilder.BuildApk -logFile build.log
```

Menu editor tersedia: `Tools/UAB METO/…` (build data, build scene, build APK).

## Dokumen penting (`docs/`)

| Dokumen | Isi |
|---|---|
| `UAB_METO_IMPLEMENTATION_REPORT.md` | laporan implementasi utama |
| `Stage2_Remediation_TestMatrix.md` | 264 assertion & matriks uji |
| `Stage5B_ScrollAudio.md` | uji scroll & audio `.wav`/`.m4a` |
| `Stage5D_SpotlightBGM.md` | perbaikan titik Dapur & musik latar |
| `Stage5E_KarakterBaru.md` | penggantian spritesheet karakter & pembersihan tepi |
| `instrumen_smp_putri/` | pre-test, post-test, kunci & rubrik, panduan Google Form |
| `Deployment_SMP_2026-11-24.md` | artefak, hasil uji, batasan deploy |
| `prompt_asset.md` | prompt generator aset (base map, interior, UI) |

## Lisensi & Kepemilikan

Proyek **Uab Meto** adalah milik kelompok **Dady_Home_Stay**:

- Mario Batista Spanyola Obe
- Basilo Agung de Fretis
- Benediktus Umardani Neonbeni
- Chriscento Novaldry Nokas
- Fabriozio Yakobus Sasi

Musik latar "Feel Good Island Loop" (musik: Brandon Morris; loop: AntumDeluge)
berlisensi CC0.
