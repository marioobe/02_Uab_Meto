# Stage 5D - Perbaikan Titik Dapur & Musik Latar

Status: selesai (audio tidak dapat didengar di perangkat uji - lihat 5.3).
Tanggal: 2026-11-23.

## 1. Tujuan

1. Memperbaiki satu objek Dapur (`alat_masak`) yang tidak pernah memunculkan
   prompt "Baca".
2. Menambahkan musik latar yang sebelumnya belum ada sama sekali
   (`AudioManager.PlayBgm` tidak pernah dipanggil).

## 2. Perubahan

| Berkas | Perubahan |
|---|---|
| `Assets/Editor/AsetTransparan23Importer.cs` | `alat_masak` 0.350/0.170 -> 0.350/0.275 |
| `Assets/Scripts/Audio/AudioManager.cs` | BGM dari `Resources/Music/bgm_village`, loop, fade-in 1,5 s, `SetBgmDucked` |
| `Assets/Scripts/Audio/DawanAudioPlayer.cs` | BGM diturunkan ke 35% selama pelafalan, naik lagi saat selesai |
| `Assets/Resources/Music/bgm_village.ogg` | baru, 356.652 B |
| `Assets/Scenes/{Base_Map,Dapur,Rumah,Kebun}.unity` | dibangun ulang lewat pipeline resmi |

### 2.1 Penyebab bug Dapur

Titik lama berada di y dunia 12,749. Collider pemain beradius 0,26 dengan
offset (0; +0,30), sehingga kaki hanya bisa mencapai y = 10,89 di dekat dinding
anyaman. Jarak 1,96 melebihi batas deteksi 1,80 (1,25 detectRadius + 0,55
trigger objek) - prompt tidak pernah muncul. Titik baru (11,136) berjarak 0,66
dari posisi kaki terdekat.

## 3. Aset audio

| Item | Nilai |
|---|---|
| Judul | Feel Good Island Loop |
| Asal | OpenGameArt, `feel_good_island_loop.ogg` |
| Lisensi | CC0 (juga tersedia OGA-BY 3.0); kredit: Brandon Morris (musik asli), AntumDeluge (suntingan loop) |
| Durasi | 51,7 detik (loop) |
| SHA256 | `694E109421E0A1D8FA88463659DE00ACB84A8A83DC9C899215BFD5DA5DC9AE9E` |

## 4. Bangun & pasang

| Item | Hasil |
|---|---|
| Rebuild scene | `GameSceneBuilder selesai: Base_Map, Dapur, Rumah, Kebun.` |
| Build APK | `BUILD RESULT: Succeeded \| errors: 0` |
| APK | 76.679.996 B, SHA256 `ECF48260A5FA6B36B29F376CC9128CE21860924BC67B0FE7BCA4027E3C37DC6D` |
| Salinan | `Builds/Android/dist/UabMeto_UAS_2026-11-23.apk` |
| Install | `Success` (data aplikasi dipertahankan) |

## 5. Hasil uji perangkat (CPH1803, Android 8.1)

### 5.1 Bug Dapur - PASS

| Uji | Hasil |
|---|---|
| Jalan ke rak alat masak | pemain berhenti di depan rak, halo tampil |
| Prompt | `Baca` muncul di layar |
| Panel | `Alat masak` / Dawan `Na'i Hana (nai hana)` / status `TERVALIDASI` |
| Tombol panel | Dengarkan, Kuis, Latihan, Bicara, Tutup tampil |
| Crash | 0 `FATAL EXCEPTION`, 0 exception Unity |

Bukti: `Builds/Android/dist/Bukti_Spotlight/06_Dapur_alat_masak_Baca.jpg` dan
`07_Panel_Alat_Masak.jpg`.

### 5.2 Musik latar - termuat, tidak terdengar di perangkat ini

| Uji | Hasil |
|---|---|
| Klip ditemukan | `AudioManager: BGM 'bgm_village' diputar (51,7 detik, loop).` |
| Tidak restart saat pindah scene | tidak ada log BGM kedua setelah masuk Dapur |
| Terdengar | **TIDAK** - lihat 5.3 |

### 5.3 Batasan perangkat

Log perangkat saat aplikasi berjalan:

```
AudioTrack: Could not get audio output ... initCheck() = -19
Unity: FMOD failed to initialize the output device (60)
MediaPlayerNative: error (1, -19)
```

Perangkat tidak menyediakan output audio untuk aplikasi apa pun (speaker
terdeteksi, volume media 10/16). Ini bukan cacat aplikasi: jalur Unity
(AudioSource/BGM) dan jalur `android.media.MediaPlayer` (pelafalan Dawan)
sama-sama gagal. Kondisi yang sama sudah tercatat pada 21-11-2026 dan tetap
setelah reboot.

## 6. Data terlindungi

| Item | Hasil |
|---|---|
| Berkas di `Assets/Data`, `Assets/Art`, `ProjectSettings` yang berubah 3 jam terakhir | tidak ada |
| `LearningObjectDefinition` | 33 (tidak berubah) |
| Sprite objek | 32 (tidak berubah) |
| Latar scene | 4 (tidak berubah) |
| Sprite karakter | 42 (tidak berubah) |
| Jumlah objek kunci per scene (lama -> baru) | Base_Map 13->13, Dapur 61->61, Rumah 22->22, Kebun 31->31 |
| Posisi objek Dapur selain `alat_masak` | identik dengan sebelumnya |

Scene ditulis ulang oleh Unity sehingga urutan `fileID` di dalam YAML berubah
(byte berbeda), tetapi jumlah dan isi objeknya sama.

## 7. Rollback

| Item | Lokasi |
|---|---|
| Sumber sebelum perubahan | `Builds/Backup/pre_bgm_20261123_1021.zip` |
| APK sebelum perubahan | `Builds/Backup/UabMeto_prev_spotlight.apk` |

## 8. Risiko tersisa

1. Audio tidak dapat diverifikasi di CPH1803 karena kerusakan output perangkat.
   Perlu diuji di perangkat lain (Redmi Android 16 pernah berhasil) atau dengan
   speaker Bluetooth.
2. Musik 51,7 detik akan terasa berulang pada sesi panjang; dapat diganti tanpa
   perubahan kode (nama berkas sama).
3. `detectRadius` pemain tetap 1,25; titik lain sudah dihitung aman (jarak
   0,00-0,57 dari posisi kaki terdekat).
