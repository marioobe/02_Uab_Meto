# UAB METO — MASTER PROJECT CONTEXT

> Dokumen acuan utama pengembangan aplikasi Uab Meto.
> Dibuat: 21-11-2026. Sumber: arahan pemilik proyek + audit implementasi yang ada
> (`docs/PROJECT_GAP_ANALYSIS.md`). Bila ada perbedaan, dokumen ini yang berlaku.

---

## 1. Tujuan

Uab Meto adalah **game edukasi petualangan 2D** untuk memperkenalkan dan mengajarkan
**bahasa Dawan (Uab Meto)**, khususnya variasi yang digunakan masyarakat TTU di sekitar
Kefamenanu, Nusa Tenggara Timur.

- Target pengguna: masyarakat umum **usia 16 tahun ke atas**, terutama pemula.
- Teknologi: **Unity 6 (6000.6.0f1)**, target **Android**.

---

## 2. Konsep utama

Uab Meto **bukan** sekadar kuis atau kamus. Ia adalah **satu pengalaman game petualangan
dan pembelajaran bahasa Dawan yang terintegrasi**, bukan sekumpulan fitur berdiri sendiri.

Pemain mengendalikan karakter utama, menjelajahi kampung tradisional Dawan, bertemu NPC,
berinteraksi dengan lingkungan, menjalankan misi, dan belajar bahasa Dawan lewat aktivitas
sehari-hari. Pengalaman harus menyenangkan, interaktif, mudah dipahami, dan beridentitas
budaya Dawan.

---

## 3. Alur utama pengguna

1. Splash Screen.
2. Menu Utama.
3. Profil / autentikasi (bila diperlukan).
4. Memulai / melanjutkan petualangan.
5. Masuk ke Base Map.
6. Menggerakkan karakter dengan kontrol virtual.
7. Menjelajahi lokasi kampung.
8. Bertemu dan berinteraksi dengan NPC.
9. Menjalankan misi pembelajaran.
10. Mempelajari kosakata dan materi.
11. Mendengarkan audio penutur lokal.
12. Berlatih berbicara.
13. Membaca dialog.
14. Mengerjakan latihan menulis.
15. Mengikuti evaluasi.
16. Mendapatkan XP, nilai, dan progres.
17. Membuka materi / lokasi berikutnya.
18. Kembali menjelajahi kampung.

---

## 4. Lokasi dan scene

Scene yang tersedia (tidak boleh diganti/dihapus tanpa audit dan persetujuan):
`Base_Map`, `Rumah`, `Dapur`, `Kebun`.

| Lokasi | Fungsi |
|--------|--------|
| `Base_Map` | pusat navigasi dan eksplorasi kampung |
| `Rumah` | salam, perkenalan, keluarga |
| `Dapur` | makanan dan minuman |
| `Kebun` | tanaman, hewan, aktivitas |
| `Rumah_Adat` | budaya dan adat Dawan (direncanakan) |

Lokasi lain dapat dikembangkan sesuai kebutuhan. **Gunakan sistem scene yang efisien**:
jangan membuat scene baru per materi/kosakata. Gunakan **satu sistem Learning dinamis**
yang menampilkan materi berdasarkan topik yang dipilih.

---

## 5. Empat keterampilan pembelajaran

Semua materi mendukung empat keterampilan:

1. **Listening** — mendengar pengucapan bahasa Dawan.
2. **Speaking** — melatih pengucapan lewat mikrofon dan AI.
3. **Reading** — membaca kosakata, kalimat, dialog.
4. **Writing** — menyusun kata, melengkapi kalimat, menulis jawaban.

Keempat keterampilan **tidak** harus menjadi empat menu terpisah; integrasikan ke dalam
satu rangkaian pembelajaran. Setiap pembelajaran idealnya memuat: pengenalan, kosakata,
gambar/ilustrasi, audio pengucapan, contoh kalimat, dialog, latihan interaktif, evaluasi,
dan hasil.

---

## 6. Kurikulum resmi (12 topik)

1. Salam dan sapaan
2. Perkenalan diri
3. Keluarga
4. Angka, waktu dan hari
5. Aktivitas sehari-hari
6. Makanan dan minuman
7. Rumah dan benda
8. Tempat dan arah
9. Belanja dan tawar-menawar
10. Budaya dan adat Dawan
11. Kesehatan dan keadaan darurat
12. Sekolah, pekerjaan dan kehidupan sosial

Ketentuan:
- Jangan mengubah/menghilangkan topik tanpa persetujuan.
- Materi bahasa **wajib divalidasi penutur lokal** yang memahami variasi Dawan TTU.
- **Jangan mengarang kosakata** atau menganggap satu dialek mewakili seluruh variasi.
- **Pemetaan 32 objek** ke 12 topik **sudah disetujui** (lihat `PROJECT_GAP_ANALYSIS.md` §5).
- Enam topik tanpa materi (1, 2, 3, 4, 9, 11) ditampilkan berstatus **"Segera Hadir"**
  sampai materi barunya divalidasi penutur.
- **Objek dengan kosakata sama** (`tempayan`/`tempayan_rumah`, `tikar`/`tikar_rumah`) **tidak
  dihitung sebagai kosakata baru**: satu materi utama per kosakata, objek duplikatnya hanya
  konteks visual / pengulangan.
- **Objek lintas topik** dipakai sebagai **materi pendukung** (contoh/pengulangan), bukan
  materi utama kedua.
- Materi **DRAF** (mis. `tikar_rumah`) tidak boleh menjadi materi utama atau soal sebelum
  divalidasi.

---

## 7. Sistem game

- Karakter utama, pergerakan 360°.
- Kontrol virtual empat arah.
- Kamera mengikuti karakter.
- NPC interaktif + dialog NPC.
- Sistem misi.
- Sistem pembukaan lokasi.
- XP dan level.
- Streak.
- Badge.
- Leaderboard.
- Hearts/nyawa.

XP adalah penghargaan atas aktivitas belajar. **Pembukaan materi didasarkan pada
penyelesaian pembelajaran dan penguasaan, bukan hanya jumlah XP.**

---

## 8. Fitur AI

- Tutor AI: maksimal **10 pertanyaan/hari**.
- Percakapan AI: maksimal **3 sesi/hari**.
- Latihan pengucapan: maksimal **10 kali/hari**.
- Koreksi jawaban + penjelasan.

Ketentuan:
- AI terhubung lewat **backend aman**; **API key tidak boleh disimpan di dalam Unity**.
- AI adalah **fasilitas tambahan**; pembelajaran utama tetap berjalan tanpa AI.

---

## 9. Backend dan penyimpanan

- Unity 6: aplikasi Android.
- Firebase: autentikasi, database, progres, backend.
- Supabase Storage Free: penyimpanan audio pembelajaran.
- Cloud Functions / backend aman: integrasi AI, validasi, kuota.
- Admin Dashboard Web: pengelolaan konten.

Ketentuan:
- **Local-first** untuk materi dan progres bila memungkinkan.
- Audio pembelajaran dapat diperbarui admin tanpa menerbitkan APK baru.
- Rekaman suara pengguna **tidak** disimpan sebagai riwayat audio; hanya hasil penilaian
  dan saran yang disimpan.

---

## 10. Admin Dashboard

Admin dapat: mengelola 12 topik, kosakata, dialog, unggah/ganti audio, membuat latihan &
ujian, memvalidasi materi, menerbitkan materi, mengelola pengguna, melihat progres & hasil
ujian, serta statistik. **Materi belum divalidasi tidak boleh dipublikasikan.**

---

## 11. Sistem evaluasi

- Setiap topik punya latihan dan evaluasi.
- Hasil evaluasi disimpan; progres diperbarui.
- **Ambang kelulusan ujian akhir: 70/100**.
- Pengguna dapat melihat hasil dan materi yang perlu dipelajari kembali.

---

## 12. Aturan pengembangan

1. Audit proyek sebelum mengubah kode.
2. Jangan menghapus aset/scene/script/konfigurasi yang masih digunakan.
3. Pertahankan kompatibilitas Unity 6.
4. Hindari sistem duplikat.
5. Arsitektur modular dan mudah dipelihara.
6. Pisahkan data materi dari kode UI.
7. Data-driven architecture untuk kurikulum.
8. Perhatikan performa Android.
9. Perhatikan keamanan Firebase dan API AI.
10. Dokumentasikan setiap perubahan penting.
11. Jangan mengarang data bahasa Dawan.
12. Jangan implementasi fitur besar tanpa memeriksa dependensi & dampaknya.

---

## 13. Keputusan yang disetujui pemilik proyek (21-11-2026)

1. Pertahankan scene, aset, script, dan sistem lama yang masih digunakan. Tidak ada
   penghapusan/pembangunan ulang tanpa alasan teknis jelas.
2. **Pertahankan Save v1** dan buat **migrasi aman ke Save v2**; progres pengguna tidak
   boleh hilang.
3. Sistem pembelajaran beralih **bertahap** dari berbasis scene ke berbasis **12 topik**.
   Lakukan **pemetaan 32 objek** sebelum migrasi. Jangan mengarang materi Dawan.
4. **Ambang kelulusan evaluasi = 70/100.**
5. **Speaking bertahap**: mulai dari mikrofon → perekaman sementara → pemutaran ulang;
   penilaian AI menyusul setelah backend siap.
6. **Gamifikasi** dikerjakan setelah fondasi kurikulum + evaluasi stabil. Leaderboard
   cukup **lokal** dulu.
7. **Firebase/Supabase/AI** hanya diimplementasikan setelah struktur data dan kebutuhan
   integrasi dirancang jelas.
8. Setiap fase wajib punya **kriteria selesai, pengujian, dan laporan**. Tidak lanjut ke
   fase berikutnya tanpa persetujuan.
9. **Audio OPPO A3s diperiksa ulang**; keberhasilan audio tidak boleh disimpulkan hanya
   dari "tidak ada error di kode".
10. **Pemetaan 32 objek ke 12 topik disetujui** (lihat `PROJECT_GAP_ANALYSIS.md` §5).
11. `selimut_tenun` **dipindahkan ke topik 10 (Budaya & adat)**.
12. Enam topik kosong ditampilkan **"Segera Hadir"**; tidak ada kosakata baru yang dikarang.
13. **Objek duplikat & lintas topik** ditangani sesuai aturan di §6.
14. **`tikar_rumah` tetap DRAF** dan tidak menjadi materi utama/soal sebelum divalidasi.
15. Urutan roadmap final: **A → B → C → Persiapan validasi konten → D → E → QA akhir**.

---

## 14. Roadmap (disetujui)

| Urutan | Fase | Fokus | Prioritas |
|--------|------|-------|-----------|
| 1 | A | Fondasi kurikulum: `topicId` + migrasi data 32 objek + pemilih topik + Save v2 | P0 |
| 2 | B | Evaluasi (70/100) + gamifikasi dasar (XP, level, streak, badge, leaderboard lokal) | P1 |
| 3 | C | Empat keterampilan: latihan menulis, speaking (mikrofon/rekam/putar-ulang), dialog tematik | P1 |
| 4 | Persiapan validasi konten | 6 topik kosong disiapkan + divalidasi penutur TTU | P0/P1 |
| 5 | D | Backend & AI (Firebase, Supabase, proxy AI + kuota, sinkronisasi) | P2 |
| 6 | E | Admin Dashboard (web terpisah) | P2 |
| 7 | QA akhir | `Rumah_Adat`, uji 2 perangkat, performa, regression penuh | P2 |

Aturan eksekusi: **satu fase selesai + dilaporkan + disetujui dulu, baru lanjut fase
berikutnya.** Detail kriteria selesai, pengujian, dan laporan per fase ada di
`docs/PROJECT_GAP_ANALYSIS.md`.

---

## 15. Prinsip utama

> **Bangun Uab Meto sebagai satu pengalaman game petualangan dan pembelajaran bahasa
> Dawan yang terintegrasi, bukan sekumpulan fitur yang berdiri sendiri.**
