# Deployment SMP — Build Universal (2026-11-24)

Status: siap dipasang. Build Release, offline, tanpa akun.
Lingkup: memakai 12 topik lama (fallback Buku); **bukan** kurikulum baru.

## 1. Artefak

| Berkas | Ukuran | SHA256 |
|---|---:|---|
| `Builds/Android/UabMeto_Universal.apk` | 96.833.093 B | `2E4D22279E7472A9E693D7BEDF9053AE3D9C1A202705A63C45425D0D7849ED65` |
| `Builds/Android/UabMeto.apk` (arm64) | 76.386.099 B | `F463C48BC86118A9AA9384B75A96EDCE78C59912B6DCEC9C912A0137D49B3E17` |
| `Builds/Android/dist/UabMeto_arm64.apk` | 76.386.099 B | sama dengan di atas (salinan) |
| `Builds/Android/dist/PANDUAN_INSTALASI.md` | — | panduan 1 halaman untuk guru |

APK universal berisi `lib/arm64-v8a` dan `lib/armeabi-v7a`; minSdk 26
(Android 8.0); target SDK otomatis (36); tidak `DEBUGGABLE`; tanpa fixture uji.

## 2. Perubahan tooling

`Assets/Editor/AndroidBuilder.cs`:

- konstanta `UniversalApkOutputPath`;
- metode baru `BuildUniversalApk` (menu 5c) yang menetapkan
  `ARMv7 | ARM64`, membangun `Builds/Android/UabMeto_Universal.apk`, lalu
  **memulihkan** arsitektur ke `ARM64` di blok `finally`.
- `ConfigureAndroidPlayerSettings` tidak berubah (tetap ARM64).

Verifikasi: `ProjectSettings.asset` setelah build kembali ke hash
`4B305A32A66B8E2EBF1E9BB474214F0BED9ECC1B9E04E487FE9473EEE75FB2D9`
dan `AndroidTargetArchitectures: 2`.

Tidak ada perubahan pada konten, scene, Save, atau script runtime.

## 3. Uji perangkat (CPH1803, Android 8.1, arm64)

| Uji | Hasil |
|---|---|
| Build universal | `BUILD UNIVERSAL RESULT: Succeeded | 687,3 MB | errors: 0` |
| Install (`install -r`) | Success |
| Launch | `Kurikulum kosong (belum ada materi PUBLISHED).` |
| Save | `Save dimuat: scene=Base_Map, hati=2, buah=0, dapur=True` |
| Menu Buku | 12 kartu tampil: 6 topik berisi (5, 6, 7, 8, 10, 12), 6 "Segera Hadir" (1, 2, 3, 4, 9, 11) |
| Detail topik 6 | 6 materi (4 utama + 2 pendukung) dengan status Dipelajari/Belum |
| Kembali / Tutup | kembali ke daftar, lalu ke peta |
| Resume (HOME → buka lagi) | proses sama (`pid` tidak berubah), layar normal |
| Cold start (force-stop → buka) | Save dimuat ulang, field v4/v5 utuh |
| `FATAL EXCEPTION` | 0 |
| `dumpsys` flags | `HAS_CODE ALLOW_CLEAR_USER_DATA ALLOW_BACKUP` (tanpa `DEBUGGABLE`) |

Save setelah uji: `saveVersion=5`, `hearts=2`, `fruit=0`, `xp=5`, `level=1`,
`streak=1`, `dapurCompleted=true`, `rumahCompleted=true`,
`kebunCompleted=false`; `learnedObjectIds` (27) dan `collectedPickupIds` (5)
identik dengan backup; array item/lesson/topic kosong.

Bukti: `C:\Users\ACER\AppData\Local\Temp\opencode\stage5b_shots\30..35`.

## 4. Batasan yang harus disampaikan saat demo

1. **Buku berisi materi lama**: hanya **6 dari 12** topik yang berisi
   (5, 6, 7, 8, 10, 12). Enam lainnya berlabel "Segera Hadir".
2. Detail materi di Buku bersifat **baca-saja** (nama + status), tanpa tombol
   audio/kuis. Aktivitas berjalan lewat Dapur/Rumah/Kebun.
3. APK universal memuat library 32-bit, tetapi **belum diuji pada perangkat
   32-bit sungguhan** (perangkat uji hanya arm64).
4. Layar uji hanya 1520×720 (landscape); perangkat dengan rasio lain belum
   diuji.
5. Audio tidak diuji ulang pada build ini; jalur audio tidak berubah sejak
   Stage 5A/5B (`.m4a` terverifikasi, `.wav` terverifikasi pada build debug).
6. APK ditandatangani sertifikat testing, bukan untuk Play Store.

## 5. Langkah besok

1. Pasang `UabMeto_Universal.apk` di perangkat yang akan dipakai (uji sekali
   sebelum acara, termasuk mode pesawat).
2. Jika "Aplikasi tidak terpasang": uninstall versi lama lalu ulang; atau
   gunakan `UabMeto_arm64.apk` bila perangkat 64-bit.
3. Ikuti `dist/PANDUAN_INSTALASI.md`.
