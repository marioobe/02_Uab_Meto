# Stage 2 — Traceability & Remediation Test Matrix

Status: remediation Stage 2, read/write implementation.
Date: 2026-11-22

## 1. Keputusan penomoran

Penomoran lama **T1–T124 tidak dipertahankan sebagai matriks 1:1**. Audit menemukan:

- 67 nomor T muncul pada nama assertion sebelum remediasi.
- 6 nomor hanya menjadi komentar pengelompokan.
- 1 nomor (T81) berstatus NOT RUN sebelum remediasi.
- 50 nomor tidak memiliki jejak implementasi.
- Nomor T118–T124 dipakai ulang oleh beberapa suite untuk skenario berbeda.

Karena itu, sumber kebenaran adalah **assertion executable** yang benar-benar dijalankan. Nomor T yang tidak ada tidak diisi dengan test duplikat.

## 2. Matriks suite (final)

| Suite | Assertion | Lokasi | Bukti |
|---|---:|---|---|
| Stage 1 `EditorSelfTest` | 23 | `Assets/Editor/CurriculumValidator.cs` | `[Stage1SelfTest] RESULT` |
| Parser `ParserSelfTest` | 8 | `Assets/Editor/CurriculumValidator.cs` | `[ParserSelfTest] RESULT` |
| Path guard | 11 | `CurriculumImporter.PathGuardSelfTest` | `[PathGuardSelfTest] RESULT` |
| T81 access denied | 2 | `CurriculumImporter.PathGuardAccessDeniedSelfTest` | `[PathGuardAccessDeniedSelfTest] RESULT` |
| Journal | 10 | `CurriculumImporter.JournalSelfTest` | `[JournalSelfTest] RESULT` |
| Backup | 17 | `CurriculumImporter.BackupSelfTest` | `[BackupSelfTest] RESULT` |
| Retention guard C2 | 18 | `CurriculumImporter.RetentionGuardSelfTest` | `[RetentionGuardSelfTest] RESULT` |
| Promotion | 17 | `CurriculumImporter.PromotionSelfTest` | `[PromotionSelfTest] RESULT` |
| Recovery | 44 | `CurriculumImporter.RecoverySelfTest` | `[RecoverySelfTest] RESULT` |
| Locking | 14 | `CurriculumImporter.LockSelfTest` | `[LockSelfTest] RESULT` |
| Importer end-to-end | 25 | `CurriculumImporter.ImporterSelfTest` | `[ImporterSelfTest] RESULT` |
| Loader unit | 8 | `CurriculumImporter.LoaderSelfTest` | `[LoaderSelfTest] RESULT` |
| Loader runtime fixture | 8 | `CurriculumImporter.LoaderRuntimeSelfTest` | `[LoaderRuntimeSelfTest] RESULT` |
| **Total** | **205** | | `FullRegressionSelfTest` |

Catatan: angka final mengikuti log `FullRegressionSelfTest`; jika berbeda, log adalah otoritas.
Stage 3 menambahkan 23 assertion integrasi; lihat `docs/Stage3_Integration.md`.
Stage 4 menambahkan 19 assertion; Stage 5B menambahkan 17 assertion audio
(`AudioFormatSelfTest`). Total saat ini **264 assertion** (log
`stage5b_full_final.log`: 264 PASS, 0 gagal).

## 3. Status nomor T

### VERIFIED (runtime + log)
T8, T30, T32, T33, T36–T42, T43, T44, T45, T46, T47, T48, T49, T50, T51, T52, T53, T54, T55, T56, T57, T59, T60, T61–T64, T66–T70, T72, T73, T81, T82–T85, T88–T93, T103, T104, T106, T109–T124.

### PARTIAL / komentar pengelompokan
- T34 — skenario dicakup T32/T33.
- T58, T71, T77, T79, T80 — skenario reparse dicakup T30/T59/T70/T73.

### NOT FOUND / OBSOLETE
T1–T7, T9–T29, T31, T35, T65, T74–T76, T78, T86, T87, T94–T102, T105, T107, T108.

Nomor-nomor ini tidak dijalankan dan tidak boleh diklaim sebagai PASS.

## 4. Pemetaan ke requirement keamanan

| Requirement | Assertion utama |
|---|---|
| R1 parser bersama | Parser 8/8, Stage1 23/23 |
| R2 schema + enum | Parser, validator, loader |
| R3 path guard | PathGuard 11/11, T81 2/2 |
| R4 journal atomik | Journal 10/10 |
| R5 backup + retention | Backup 17/17, Retention 18/18 |
| R6 promotion + recovery | Promotion 17/17, Recovery 44/44 |
| R7 importer + loader | Importer 25/25, Loader 8/8, LoaderRuntime 8/8 |

## 5. Aturan pelaporan

- FAIL hanya jika assertion gagal.
- NOT RUN harus disertai alasan.
- PASS tidak boleh disimpulkan dari inspeksi kode.
- Tidak ada test yang ditambahkan hanya untuk menaikkan angka.
