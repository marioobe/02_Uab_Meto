# Prompt NotebookLM — Slide Presentasi Uab Meto

Paket untuk membuat deck presentasi peluncuran **Uab Meto** di SMP Putri,
24 November 2026.

## Isi paket

| Berkas | Fungsi |
|---|---|
| `Alur_Aplikasi_UabMeto.md` | **Sumber utama.** Upload ke NotebookLM sebagai source. |
| `Sumber_Gambar_PPT.pdf` | Kumpulan screenshot asli untuk dipakai di slide. |
| `Prompt_NotebookLM.md` | Dokumen ini: prompt + langkah pakai. |

---

## 1. Langkah pakai (10 menit)

1. Buka <https://notebooklm.google.com>, buat notebook baru:
   **"Uab Meto — Presentasi SMP Putri"**.
2. Klik **Add source** → upload **`Alur_Aplikasi_UabMeto.md`**.
3. Upload juga **`Sumber_Gambar_PPT.pdf`** (agar slide bisa memakai screenshot asli).
4. Tunggu sampai NotebookLM selesai membaca kedua sumber.
5. Buka panel **Studio** → pilih **Slide deck** (atau **Presentasi**).
6. Jika ada kolom **Sesuaikan / Customize / Instruksi tambahan**, tempel
   **Prompt Pendek** di bawah. Jika kolomnya pendek, tempel **Prompt Sedang**.
7. Jika tidak ada kolom kustomisasi sama sekali, tempel **Prompt Panjang** ke
   chat NotebookLM sebelum menekan tombol buat slide.
8. Setelah jadi: periksa satu per satu (lihat checklist §6), lalu unduh
   (PDF) atau presentasikan langsung dari NotebookLM.
9. Bila hasil kurang rapi, jangan ulang dari nol — minta perbaikan per slide,
   misalnya: *"Perbaiki slide 5: ringkas menjadi 4 poin, hapus kalimat panjang."*

> Catatan: NotebookLM hanya memakai isi sumber. Kalau ada angka yang salah di
> slide, hampir selalu karena angka itu tidak ada di dokumen sumber.

---

## 2. Prompt Pendek (untuk kolom kustomisasi)

```
Buat 14 slide presentasi Bahasa Indonesia untuk peluncuran game "Uab Meto" di
SMP Putri, 24 November 2026. Audiens: siswa SMP dan guru. Gunakan hanya fakta
dari sumber; jangan menambah angka atau klaim baru.

Gaya: bersih dan hangat (krem, cokelat tanah, hijau), font sans-serif besar,
maksimal 5 poin per slide, maksimal 8 kata per poin, banyak ruang kosong.

Tiap slide: judul maksimal 6 kata, satu kalimat pembuka, poin singkat, dan
catatan pembicara 30-40 detik. Jangan mengarang gambar; pakai screenshot dari
sumber bila ada. Jangan sebut "siap Play Store", "sudah diuji semua HP", atau
istilah teknis (APK, IL2CPP, hash).

Urutan: (1) judul, (2) masalah, (3) solusi, (4) apa itu Uab Meto, (5) alur
pemain 6 langkah, (6) peta dan 3 lokasi, (7) cara belajar di dalam lokasi,
(8) empat keterampilan, (9) kuis dan nilai, (10) gamifikasi, (11) Buku dan 12
topik, (12) teknologi dan validasi bahasa, (13) hasil uji, (14) penutup + tim.
```

---

## 3. Prompt Sedang (rekomendasi)

```
Buat deck 14 slide dalam Bahasa Indonesia untuk acara peluncuran aplikasi
"Uab Meto" di SMP Putri pada 24 November 2026.

Audiens: siswa SMP (13-15 tahun) dan guru. Nada ramah dan bangga, bukan laporan
teknis. Tujuan: audiens paham apa itu Uab Meto, bagaimana alurnya, dan mau
mencobanya.

Aturan isi:
- Pakai hanya fakta dari sumber. Jangan menambah statistik, kutipan, atau nama.
- Angka yang boleh disebut: 32 objek belajar, 124 soal, 12 topik (6 berisi,
  6 "Segera Hadir"), 3 lokasi + peta kampung, kuis 5 soal, lulus 70, hati 3,
  4 keterampilan.
- Sebut validasi kosakata oleh penutur asli TTU (Ibu Palmerya Sau).
- Jangan menyebut "siap Play Store", "sudah diuji semua perangkat", atau
  "audio sempurna". Sebut "versi demo", "siap dipasang offline".
- Hindari istilah teknis (APK, IL2CPP, hash, arm64). Paling banyak satu baris
  di slide teknologi.

Aturan tampilan:
- Palet krem, cokelat tanah, hijau daun; latar terang; font sans-serif besar.
- Satu ide per slide; maksimal 5 poin; maksimal 8 kata per poin.
- Jangan menaruh paragraf panjang atau tabel padat.
- Pakai screenshot dari sumber bila tersedia; jangan mengarang tangkapan layar.

Setiap slide wajib punya judul maksimal 6 kata, satu kalimat pembuka, poin
singkat, dan catatan pembicara 30-40 detik dalam bahasa lisan.

Susunan wajib:
1. Judul + subjudul acara.
2. Masalah: bahasa daerah makin jarang dipakai; belajar dari buku terasa berat.
3. Solusi: game petualangan 2D belajar kosakata Dawan sambil bermain.
4. Apa itu Uab Meto: jelajah, belajar, uji.
5. Alur pemain 6 langkah: buka aplikasi, muncul di peta, jalan ke lokasi,
   dekat objek lalu Baca, dengarkan dan baca arti, selesaikan kuis.
6. Peta dan lokasi: peta kampung, Dapur, Rumah, Kebun; Rumah terbuka setelah
   kuis Dapur lulus.
7. Cara belajar di dalam lokasi: halo, panel info, nama Indonesia + Dawan,
   cara baca, audio pelafalan.
8. Empat keterampilan: mendengar, membaca, menulis, berbicara.
9. Kuis dan nilai: 5 soal acak, nilai 0-100, lulus 70, salah mengurangi hati,
   ada "pelajari ulang".
10. Gamifikasi: hati 3, buah, XP, level, streak, badge, papan peringkat lokal.
11. Menu Buku: 12 topik, 6 berisi, 6 "Segera Hadir".
12. Teknologi dan validasi: Unity 6, Android, offline, tanpa akun, validasi
    penutur asli TTU.
13. Hasil uji: berjalan di perangkat nyata, tanpa crash, karakter baru rapi.
14. Penutup: ajakan mencoba dan melestarikan bahasa Dawan, tim Dady_Home_Stay,
    terima kasih.
```

---

## 4. Prompt Panjang (cadangan, untuk chat)

```
Kamu membantu menyiapkan presentasi peluncuran aplikasi untuk sekolah, bukan
laporan proyek. Audiens utama siswa SMP dan guru. Bahasa Indonesia, sederhana,
kalimat pendek.

Tugas: buat tepat 14 slide, urutan sesuai daftar di bagian akhir. Jangan
menambah slide lain.

Sumber kebenaran: dokumen "Alur Aplikasi Uab Meto" dan PDF screenshot yang
sudah diunggah. Dilarang mengarang angka, nama, kutipan, atau tangkapan layar.
Kalau sebuah data tidak ada di sumber, jangan tampilkan.

Larangan klaim:
- jangan menulis "siap Play Store" atau "aplikasi resmi";
- jangan menulis "sudah diuji di semua perangkat";
- jangan menulis "audio selalu berjalan sempurna";
- jangan memakai istilah APK, IL2CPP, hash, arm64, commit, build log;
- jangan menampilkan nama file atau tautan.

Gaya visual:
- palet krem, cokelat tanah, hijau daun;
- latar terang, teks gelap, kontras tinggi;
- font sans-serif besar (judul minimal 32 pt pada slide 16:9);
- satu ide per slide, maksimal 5 poin, maksimal 8 kata per poin;
- banyak ruang kosong, tanpa bayangan berat atau gradien ramai;
- gunakan screenshot asli dari sumber pada slide 6, 7, dan 13.

Setiap slide berisi:
- judul maksimal 6 kata;
- satu kalimat pembuka;
- poin-poin singkat;
- catatan pembicara 30-40 detik, bahasa lisan, siap dibaca.

Daftar slide:
1. "Uab Meto: Belajar Bahasa Dawan Lewat Game" + subjudul "Peluncuran di SMP
   Putri, 24 November 2026".
2. Masalah: bahasa daerah makin jarang dipakai anak muda; media belajar yang
   ada terasa membosankan.
3. Solusi: game petualangan 2D yang mengajarkan kosakata Dawan sambil bermain.
4. Apa itu Uab Meto: tiga pilar jelajah, belajar, uji.
5. Alur pemain: 6 langkah ringkas dari membuka aplikasi sampai lulus kuis.
6. Peta dan lokasi: peta kampung, Dapur, Rumah, Kebun; Rumah terbuka setelah
   Dapur lulus.
7. Cara belajar di dalam lokasi: mendekat, tombol Baca, panel info, audio.
8. Empat keterampilan: mendengar, membaca, menulis, berbicara.
9. Kuis dan nilai: 5 soal acak, nilai 0-100, lulus 70, hati, pelajari ulang.
10. Gamifikasi: hati 3, buah, XP, level, streak, badge, papan peringkat lokal.
11. Menu Buku: 12 topik, 6 berisi, 6 "Segera Hadir".
12. Teknologi dan validasi: Unity 6, Android, offline, tanpa akun, validasi
    penutur asli TTU.
13. Hasil uji: berjalan di perangkat nyata, tanpa crash, karakter baru rapi.
14. Penutup: ajakan mencoba, tim Dady_Home_Stay, terima kasih.
```

---

## 5. Prompt tambahan (pakai setelah deck jadi)

- **Merapikan slide tertentu:**
  `Perbaiki slide 7: hapus kalimat pembuka, sisakan 4 poin, setiap poin maksimal 7 kata.`
- **Menambah catatan pembicara:**
  `Tulis ulang catatan pembicara slide 5 supaya cukup 30 detik dan memakai bahasa sehari-hari.`
- **Mengganti slide yang terlalu penuh:**
  `Slide 10 terlalu padat. Pecah menjadi dua slide: hati dan buah; lalu XP, level, badge, peringkat.`
- **Konsistensi istilah:**
  `Ganti semua "nyawa" menjadi "hati" dan semua "level" tetap "level".`

---

## 6. Checklist sebelum dipakai besok

- [ ] Semua angka di slide ada di `Alur_Aplikasi_UabMeto.md`.
- [ ] Tidak ada kata "Play Store", "semua perangkat", atau "audio sempurna".
- [ ] Slide 11 menyebut **6 dari 12** topik berisi, bukan 12 semuanya.
- [ ] Slide 5 (alur) bisa dipahami tanpa penjelasan tambahan.
- [ ] Screenshot di slide 6, 7, 13 berasal dari aplikasi terbaru.
- [ ] Setiap slide punya catatan pembicara.
- [ ] Teks tidak terpotong saat dipresentasikan (cek mode presentasi).
- [ ] Total durasi presentasi 10-12 menit.
- [ ] Slide 14 memuat nama tim Dady_Home_Stay dengan ejaan benar.

---

## 7. Catatan gambar

`Sumber_Gambar_PPT.pdf` berisi 7 tangkapan layar:

| Urutan | Gambar | Dipakai untuk |
|---|---|---|
| 1 | Peta kampung (Base_Map) | Slide 6 |
| 2 | Dapur | Slide 6, 7 |
| 3 | Rumah | Slide 6 |
| 4 | Panel info bangku | Slide 7 |
| 5 | Panel info alat masak | Slide 7 |
| 6 | Karakter diam di gerbang | Slide 13 |
| 7 | Karakter berjalan | Slide 13 |

Panel info pada PDF memakai build sebelum karakter baru. Bila waktu masih ada,
ganti dengan screenshot dari APK terbaru agar seragam dengan slide lain.
