# Stage 5E - Karakter Baru (Karakter_baru.png)

Status: terpasang & dikonfirmasi pengguna (uji otomatis perangkat terbatas - lihat 6.2).
Tanggal: 2026-11-23.

## 1. Tujuan

Mengganti karakter pemain lama (Yuuta2, spritesheet 4x4) dengan aset baru
`C:\Users\ACER\Downloads\Karakter_baru.png`, tanpa sisa putih/halo di tepi
sprite dan tanpa mengubah data pembelajaran, Save, atau pengaturan proyek.

## 2. Sumber aset

| Item | Nilai |
|---|---|
| Berkas | `Karakter_baru.png` (1242x1266 px, PNG 32-bit transparan) |
| SHA256 | `AF2B1A1B8BF824A46F625ED66314AEDC7AA70386E245B719A71DB8C511B16CCA` |
| Grid | 4x4 (16 sel), sel 310x316 px |
| Isi per frame | 111-140 x 258-272 px |
| Salinan di proyek | `Assets/foto/aset_transparan_23/aset_karakter_transparan/baru/karakter_baru_4x4.png` |

### 2.1 Pemetaan baris

| Baris sheet | Isi | Dipakai sebagai |
|---|---|---|
| 1 | jalan menghadap bawah (depan) | `walk_down`; frame 1 = `idle_down` |
| 2 | jalan menghadap kiri (profil) | tidak dipakai |
| 3 | jalan menghadap kanan (profil) | `walk_side` |
| 4 | jalan membelakangi kamera | `walk_up`; frame 1 = `idle_up` |

Baris 2 dilewati karena runtime hanya punya satu animasi samping dan
mencerminkannya (`SpriteRenderer.flipX`) saat pemain bergerak ke kiri.
Baris 1 terbukti siklus jalan (kaki bergerak di paruh bawah), bukan idle,
sehingga idle memakai frame pertamanya (kedua kaki rapat).

## 3. Perubahan berkas

| Berkas | Perubahan |
|---|---|
| `Assets/Editor/YuutaSheet4x4Importer.cs` | sumber baru; potong per frame; defringe; pusatkan; sejajarkan kaki; PPU 180; hapus frame `idle_down` lama |
| `Assets/Editor/GameSceneBuilder.cs` | `idleDown` diambil dari frame pertama `walk_down` |
| `Assets/Art/Characters/Yuuta2/*.png` | 12 frame baru (4 `walk_down`, 4 `walk_side`, 4 `walk_up`); 4 `idle_down` lama dihapus |
| `Assets/Scenes/{Base_Map,Dapur,Rumah,Kebun}.unity` | dibangun ulang lewat `GameSceneBuilder.BuildAll` |

Tidak ada perubahan pada `Assets/Data`, `Assets/Scripts`, `ProjectSettings`,
`Packages`, audio, atau aset art lain.

## 4. Cara pengolahan frame

1. **Ukur tepi alpha** tiap sel (ambang alpha > 8) agar margin transparan
   tidak ikut terbawa.
2. **Defringe**: piksel semi-transparan (alpha <= 160) yang nyaris putih
   (R,G,B >= 232) dijadikan transparan. Piksel opaque tidak disentuh, jadi
   putih pada mata/hiasan kain tetap utuh.
3. **Pusatkan** tiap frame berdasarkan kotak isinya.
4. **Sejajarkan kaki**: baris terbawah isi ditempatkan di baris terbawah
   kanvas, sehingga karakter tidak naik-turun saat animasi berganti.
5. **Kanvas seragam** 148x276 px, pivot (0,5; 0), `FullRect`, filter Point,
   tanpa kompresi.
6. **PPU 180**: tinggi isi 272 px / 180 = 1,51 unit, mendekati karakter lama
   (frame 473 px / PPU 300 = 1,58 unit). Collider pemain tidak diubah
   (radius 0,26; offset +0,30).

## 5. Verifikasi impor

| Uji | Hasil |
|---|---|
| Jumlah frame | 12 (3 arah x 4) |
| Ukuran semua frame | 148x276 px |
| Pusat horizontal (bbox) | 73-73,5 dari pusat kanvas 74 |
| Dasar kaki (bbox bawah) | 275 pada semua frame (kanvas 0-275) |
| Piksel putih samar di tepi | **0** pada 12 frame |
| Piksel putih opaque | 0-24 (mata/hiasan, sah) |
| Sisa berkas `idle_down` | 0 |
| Meta | `spritePixelsToUnits: 180`, pivot (0,5; 0) |
| GUID frame `walk_*` | dipertahankan (nama berkas sama) |
| Scene merujuk GUID `idle_down` lama | 0 dari 6 scene |

Bukti: `Builds/Android/dist/Bukti_KarakterBaru/00_preview_frame_import.png`
dan `01_perbandingan_lama_baru.png`.

## 6. Bangun, pasang, uji

### 6.1 Bangun

| Item | Hasil |
|---|---|
| Rebuild scene | `GameSceneBuilder selesai: Base_Map, Dapur, Rumah, Kebun.` |
| Jumlah GameObject scene | 47 / 76 / 39 / 54 - sama dengan sebelum perubahan |
| Build APK | `BUILD RESULT: Succeeded \| errors: 0` |
| APK | 74.716.490 B (71,26 MB) |
| SHA256 | `856D00010B7C0E0F6A764EAC6F775B7C5DFFE4B20C0DC2D9243C0AE3F5F7E5E0` |
| Salinan distribusi | `Builds/Android/dist/UabMeto_UAS_2026-11-23.apk` |
| Install | `Success` pada CPH1803; data aplikasi dipertahankan |

APK lebih kecil dari versi sebelumnya (76.679.996 B) karena tekstur karakter
turun dari 262x473 menjadi 148x276 per frame dan jumlah frame 16 -> 12.

### 6.2 Uji perangkat

| Uji | Hasil |
|---|---|
| Aplikasi terpasang & dibuka | PASS (install `Success`, Unity berjalan tanpa fatal exception) |
| Sisa putih/halo pada karakter | **PASS - dikonfirmasi pengguna**: "tidak ada garis putih lagi" |
| Screenshot otomatis build ini | **TIDAK DIAMBIL** - interface ADB hilang setelah layar perangkat tidur; Windows hanya mendeteksi MTP/USB storage (VID_22D9 PID_2773 tanpa interface ADB) |
| Animasi jalan depan/samping/belakang | **BELUM DIUJI OTOMATIS** - menunggu ADB aktif kembali |

Screenshot `01_setelah_install.png` dan `02_tunggu.png` berwarna hitam karena
layar perangkat mati (`Display Power: state=OFF`), bukan karena crash.

## 7. Rollback

| Item | Lokasi |
|---|---|
| Frame & scene sebelum perubahan | `Builds/Backup/karakter_baru_pre_20261123_1145.zip` |
| APK sebelum karakter baru | `Builds/Backup/UabMeto_prev_karakter.apk` |
| Save perangkat sebelum install | `Builds/Backup/save_pre_karakter/save.json` (SHA256 `48C84F4605684AD9995110966C979A1D0A5A74601185DFBA420D14FAA1437862`) |

Rollback cepat: timpa `Assets/Art/Characters/Yuuta2` + `Assets/Scenes` dari zip,
kembalikan dua skrip editor, lalu rebuild scene & APK.

## 8. Risiko tersisa

1. Animasi jalan belum diverifikasi otomatis di perangkat untuk build ini;
   konfirmasi pengguna baru mencakup tidak adanya garis putih.
2. Baris profil kiri pada sheet tidak dipakai; sisi kiri memakai pencerminan
   baris kanan. Bila pencerminan terlihat janggal, baris 2 bisa diaktifkan
   dengan menambah animasi kiri/kanan terpisah di `PlayerAnimator2D`.
3. Tinggi karakter turun ~4% (1,51 vs 1,58 unit). Collider tidak diubah, jadi
   interaksi tetap sama; ubah `PixelsPerUnit` ke ~172 bila ingin persis lama.
4. Save di perangkat dipertahankan; `lastScene` masih `Dapur` dengan 10 objek
   sudah dibaca.
