# Kunci, Rubrik Skor, dan Cara Analisis

Dokumen ini **tidak diberikan kepada siswa**. Dipakai panitia untuk menilai dan
mengolah hasil pre-test/post-test.

## 1. Kunci Bagian B (tes kosakata Dawan)

| No | Pertanyaan | Kunci | Kata Dawan | Skor |
|---|---|---|---|---|
| 1 | Bahasa Dawan untuk "jagung" | **C** | Pena | 1 |
| 2 | Bahasa Dawan untuk "sumur" | **A** | Oe mata | 1 |
| 3 | Bahasa Dawan untuk "tikar" | **B** | Na'he | 1 |

- Jawaban **D. Belum tahu** = 0 poin (bukan jawaban salah, tetapi belum menguasai).
- Skor maksimal Bagian B = **3**.

## 2. Cara menghitung hasil tes kosakata

Untuk setiap siswa:

```
Pre_total  = jumlah benar Bagian B pre-test   (0–3)
Post_total = jumlah benar Bagian B post-test  (0–3)
Selisih    = Post_total − Pre_total
```

Untuk satu kelas:

```
Rata-rata pre  = jumlah Pre_total  / jumlah siswa
Rata-rata post = jumlah Post_total / jumlah siswa
% benar pre    = (Rata-rata pre  / 3) × 100
% benar post   = (Rata-rata post / 3) × 100
```

**N-gain (Hake)** untuk melihat besar peningkatan:

```
N-gain = (% benar post − % benar pre) / (100 − % benar pre)
```

| N-gain | Kategori |
|---|---|
| ≥ 0,70 | Tinggi |
| 0,30 – 0,69 | Sedang |
| < 0,30 | Rendah |

Bila `% benar pre = 100`, N-gain tidak terdefinisi — laporkan "sudah maksimal".

Selain itu hitung juga:

- jumlah siswa yang **naik**, **tetap**, dan **turun**;
- persentase siswa yang menjawab benar **per soal** (soal 1, 2, 3).

## 3. Rubrik Bagian A (kuesioner pengalaman)

### 3.1 Skor Pengalaman (post-test soal 1, 2, 3, 4, 7)

Kelima soal ini berskala positif. Beri nilai:

| Pilihan | Nilai |
|---|---|
| A | 4 |
| B | 3 |
| C | 2 |
| D | 1 |

```
Rata-rata pengalaman = (A1 + A2 + A3 + A4 + A7) / 5      (skala 1–4)
Skor pengalaman (0–100) = ((Rata-rata − 1) / 3) × 100
```

Contoh interpretasi (deskriptif, bukan patokan baku):

| Skor | Interpretasi |
|---|---|
| 81–100 | Sangat baik |
| 61–80 | Baik |
| 41–60 | Cukup |
| 21–40 | Kurang |
| 0–20 | Sangat kurang |

### 3.2 Soal yang dilaporkan terpisah

Jangan campur semua soal menjadi satu angka. Laporkan per aspek:

| Aspek | Soal | Cara lapor |
|---|---|---|
| Tingkat kesulitan | Post 5 | % memilih **A. Pas** (ideal) + distribusi |
| Kelancaran/bug | Post 6 | % memilih **A. Semua lancar** + distribusi |
| Persepsi belajar | Post 8 | % memilih **A + B** ("ya, banyak/sedikit") |
| Keinginan main lagi | Post 9 | % memilih **A + B** |
| Rekomendasi | Post 10 | % memilih **A + B** |
| Nilai keseluruhan | Post 11 | Rata-rata skala 1–10 |

### 3.3 Perbandingan ekspektasi vs realita

| Yang dibandingkan | Pre-test | Post-test |
|---|---|---|
| Tampilan | Soal A5 (harapan) | Soal A1 (penilaian) |
| Kepuasan | Soal A10 (keyakinan) | Soal A7 (kepuasan) |
| Minat bermain | Soal A9 (tertarik mencoba) | Soal A9 (main lagi) |

Hitung rata-rata skala 1–4 (A=4 … D=1) untuk pre dan post, lalu laporkan
selisihnya. Positif = pengalaman melebihi harapan; negatif = di bawah harapan.

## 4. Contoh tabel rekap (Google Sheets)

| Kode | Kelas | Pre B1 | Pre B2 | Pre B3 | Pre Total | Post B1 | Post B2 | Post B3 | Post Total | Selisih | N-gain | Rata A (post) | Nilai 1–10 | Catatan |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| AN-07 | VIII A | 1 | 0 | 1 | 2 | 1 | 1 | 1 | 3 | +1 | 1,00 | 3,6 | 9 | - |

Template kosong tersedia di `Rekap_Skor_Template.csv`.

## 5. Contoh kalimat untuk laporan

- "Rata-rata skor tes kosakata Dawan meningkat dari **1,2/3 (40%)** pada pre-test
  menjadi **2,4/3 (80%)** pada post-test."
- "N-gain sebesar **0,67** termasuk kategori **sedang**."
- "Sebanyak **18 dari 25 siswa (72%)** mengalami peningkatan skor."
- "Sebanyak **84%** siswa menyatakan puas atau sangat puas setelah bermain."
- "Aspek yang paling menonjol adalah tampilan (rata-rata 3,5/4), sedangkan
  kontrol memperoleh rata-rata 3,1/4."

## 6. Catatan metodologi (penting)

1. Pre-test diberikan **sebelum** siswa melihat atau memainkan game.
2. Post-test diberikan **segera setelah** siswa mencoba game.
3. Jangan memperlihatkan kunci sebelum post-test selesai.
4. Jangan menjumlahkan Bagian A dan Bagian B menjadi satu skor; keduanya
   mengukur hal berbeda (pengalaman vs pengetahuan).
5. Sampel kecil (satu sekolah) → laporkan secara **deskriptif**. Hindari klaim
   "meningkatkan prestasi secara umum" tanpa kelompok pembanding.
6. Kode siswa harus sama pada pre dan post agar bisa dipasangkan (paired).
