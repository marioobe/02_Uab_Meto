# Panduan Membuat Google Form (Pre-Test & Post-Test)

Buat **dua form terpisah**:

1. `Pre-Test Uab Meto — SMP Putri`
2. `Post-Test Uab Meto — SMP Putri`

Jangan gabung dalam satu form, supaya siswa tidak bisa membuka post-test lebih awal.

## 1. Struktur Form Pre-Test

| Bagian | Isi | Tipe pertanyaan |
|---|---|---|
| Judul | Pre-Test Uab Meto — SMP Putri | - |
| Deskripsi | "Kami sedang mengembangkan game belajar bahasa Dawan. Isi sesuai pendapatmu. Tidak ada jawaban benar/salah. Kode siswa dipakai untuk mencocokkan dengan post-test." | - |
| Bagian 1 | Identitas: **Kode Siswa**, **Kelas**, **Tanggal** | Jawaban singkat / dropdown / tanggal |
| Bagian 2 | Bagian A — 10 soal (soal 1–10) | Pilihan ganda |
| Bagian 3 | Bagian B — 3 soal kosakata (soal 1–3) | Pilihan ganda |

## 2. Struktur Form Post-Test

| Bagian | Isi | Tipe pertanyaan |
|---|---|---|
| Judul | Post-Test Uab Meto — SMP Putri | - |
| Deskripsi | "Isi setelah kamu mencoba game. Kode siswa harus sama dengan pre-test." | - |
| Bagian 1 | Identitas: **Kode Siswa**, **Kelas**, **Tanggal** | Jawaban singkat / dropdown / tanggal |
| Bagian 2 | Bagian A — 10 soal (soal 1–10) | Pilihan ganda |
| Bagian 3 | Bagian B — 3 soal kosakata (soal 1–3) | Pilihan ganda |
| Bagian 4 | Soal 11 nilai 1–10; soal 12–13 saran (paragraf); soal 14 opsional | Skala linier + paragraf |

## 3. Setelan penting (Settings)

Buka **Settings** pada setiap form:

| Setelan | Nilai | Alasan |
|---|---|---|
| Collect email addresses | **Off** | menjaga anonimitas; identitas cukup kode siswa |
| Limit to 1 response | **Off** | tidak memaksa login akun sekolah |
| Shuffle question order | **Off** | memudahkan penomoran & rekap |
| Shuffle answer order | **Off** | kunci A/B/C/D tetap konsisten |
| Make questions required | **On** (kecuali soal 12–14) | mengurangi jawaban kosong |
| Quiz mode | **Off** untuk pre-test | jangan sampai kunci terlihat |
| Confirmation message | "Terima kasih! Jawabanmu membantu pengembangan game Uab Meto." | - |

Untuk post-test, **boleh** mengaktifkan Quiz mode **hanya pada Bagian B** agar
otomatis dinilai, tetapi di **Settings → Quizzes → Respondent can see** matikan:

- Missed questions
- Correct answers
- Point values

Dengan begitu siswa tidak melihat kunci, tetapi skor Bagian B otomatis terisi.

## 4. Validasi kode siswa

Pada pertanyaan **Kode Siswa**:

- Tipe: Jawaban singkat
- Wajib diisi: Ya
- Deskripsi: "2 huruf nama depan + 2 angka nomor absen, contoh: AN-07"
- Validasi respons (opsional): **Cocok dengan pola** `^[A-Za-z]{2}-[0-9]{2}$`
  (huruf besar/kecil bebas, tanda hubung wajib)

Kalau siswa lupa kode, minta mereka menuliskan kode yang sama seperti pre-test;
jangan membuat kode baru.

## 5. Cara menyebarkan

1. Klik **Send** → ikon link → centang **Shorten URL**.
2. Buat **QR code** dari link (bisa lewat situs pembuat QR gratis) dan tampilkan
   di proyektor/print.
3. Atau tempel link di grup WhatsApp kelas.
4. Siapkan juga **versi cetak** (file `.docx`) untuk siswa yang tidak membawa HP
   atau jika jaringan bermasalah.

## 6. Mengolah hasil

1. Buka tab **Responses** → **Link to Sheets**.
2. Buat dua spreadsheet: `Pre` dan `Post`.
3. Di spreadsheet rekap, buat kolom `Kode Siswa` sebagai kunci penghubung.
4. Gunakan `VLOOKUP`/`XLOOKUP` untuk menarik nilai post berdasarkan kode siswa.
5. Hitung skor dengan rumus di `03_Kunci_dan_Rubrik.md`.

Contoh rumus cepat di Google Sheets (Bagian B, pre):

```
=COUNTIF(B2:D2,"Pena") + ...   ← tidak disarankan
```

Lebih aman: beri skor manual 1/0 per sel (B1, B2, B3), lalu:

```
Pre_Total  = SUM(E2:G2)
Post_Total = SUM(H2:J2)
Selisih    = Post_Total - Pre_Total
N_gain     = (Post_Total/3 - Pre_Total/3) / (1 - Pre_Total/3)
```

## 7. Checklist H-1

- [ ] Dua form sudah dibuat dan diuji sendiri (preview).
- [ ] Kode siswa wajib dan deskripsinya jelas.
- [ ] Semua soal Bagian A & B wajib diisi.
- [ ] Quiz mode pre-test **mati**.
- [ ] Link/QR sudah dicoba di HP panitia.
- [ ] File `.docx` pre/post sudah dicetak (cadangan).
- [ ] Lembar kunci hanya dipegang panitia.
- [ ] Spreadsheet rekap sudah dibuat.
