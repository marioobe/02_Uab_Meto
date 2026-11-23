# Stage 2 — Known Limitations & Residual Risks

Status: remediation Stage 2.
Date: 2026-11-22

## 1. TOCTOU path guard

Validasi path dan hash dilakukan sebelum operasi tulis. Implementasi tidak memegang
handle direktori sampai `File.Replace`/`File.Move`/`File.Copy`, sehingga proses lokal
adversarial masih dapat menukar parent directory menjadi junction/symlink di antara
validasi dan operasi tulis.

- Mitigasi saat ini: validasi ulang path, cek reparse point, hash sebelum/sesudah,
  dan `File.Replace` di direktori yang sama.
- Status: **accepted risk** untuk threat model non-adversarial.
- Peningkatan lanjutan: handle-based validation dengan`FILE_FLAG_OPEN_REPARSE_POINT`
  dan handle direktori yang dipegang sampai penulisan selesai.

## 2. Atomicity & durability

- `File.Replace`/`File.Move` mengandalkan jaminan NTFS satu volume.
- File temp/journal memakai `Flush(true)`, tetapi tidak ada fsync direktori induk.
- Tidak ada uji power-loss; hasil setelah listrik mati bergantung pada filesystem/OS.
- Drive proyek produksi terverifikasi Fixed/NTFS.
- Kode belum menolak mapped network drive atau filesystem non-NTFS/FAT.

Status: **known limitation**, tidak ada klaim durability melebihi jaminan OS.

## 3. Crash consistency

Status PREPARED/PROMOTING/COMMITTED/RECOVERY_REQUIRED diuji melalui journal dan artifact
buatan. Belum ada test yang benar-benar membunuh proses pada setiap titik.

Status: **PARTIAL — diuji secara simulasi, bukan crash proses nyata.**

## 4. Loader runtime

`LoaderRuntimeSelfTest` memakai fixture sementara di `Assets/Resources/Curriculum` dan
membersihkannya di `finally`. Test ini memverifikasi `Resources.Load<TextAsset>` di editor.

Belum diverifikasi:
- perilaku loader di APK Android;
- managed stripping karena loader belum direferensikan UI;
- integrasi UI/aplikasi.

## 5. T81 ACL

`PathGuardAccessDeniedSelfTest` hanya berjalan di Windows dan memakai `icacls` pada
direktori temp. Jika ACL tidak efektif (mis. proses elevated), hasilnya NOT RUN.
Tidak ada ACL proyek yang diubah.

## 6. Orphan / unknown files

Kebijakan tetap: orphan dan unknown backup **tidak** dihapus otomatis.
Temp promosi sekarang dicatat di journal PREPARED sebelum ditulis, tetapi file yang
tidak dikenal tetap dipertahankan untuk investigasi.

## 7. Cakupan test

Jumlah assertion final adalah 205, bukan 124. Penomoran lama T1–T124 sudah obsolete;
lihat `Stage2_Remediation_TestMatrix.md`.

## 8. Produksi

`Assets/Curriculum/source/curriculum.json` belum ada. Importer produksi tetap NO-OP.
Tidak ada `Assets/Resources/Curriculum` permanen dan tidak ada `Library/UabMetoStage2`
produksi yang dibuat selama remediasi.
