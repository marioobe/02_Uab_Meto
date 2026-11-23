# Prompt Aset — Uab Meto (UAS Mobile)

Dokumen kerja untuk membuat ulang aset visual aplikasi **Uab Meto** memakai generator gambar AI eksternal.
Semua angka di dokumen ini **diambil dari kode proyek** (`GameSceneBuilder.cs`, `GameArtImporter.cs`, `AsetTransparan23Importer.cs`, `YuutaSheet4x4Importer.cs`, `GameUi.cs`), bukan perkiraan.

- Versi: 2.0
- Tanggal: 2026-11-23
- Proyek: `D:\02_Uab_Meto` (Unity 6, Android, landscape)
- Status: belum ada aset baru yang diintegrasikan

---

## 0. Masalah yang harus diperbaiki (dari keluhan Anda)

| Keluhan | Penyebab sebenarnya (terverifikasi) | Perbaikan di dokumen ini |
|---|---|---|
| Karakter bergerak monoton | Hanya ada 4 frame per arah; `idle` hanya 4 frame arah bawah; arah samping & atas memakai frame pertama walk sebagai idle; tidak ada napas/anggukan | §10: 6 strip (idle + walk untuk 3 arah), baseline & tinggi identik |
| Isi barang berantakan | **Latar lama sudah penuh perabot** (periuk, lemari, kasur, pohon, sumur tergambar). Sprite objek transparan ditempel di atasnya → dobel, dan posisinya tidak seimbang | §5–§8: latar **kosong total**; §6–§8 tabel penempatan baru yang seimbang |
| Bentuk tidak seimbang | PPU tiap objek berbeda tanpa acuan (mis. `lemari` PPU 370,625), ukuran ditentukan acak | §9: ukuran target dalam **unit dunia**, rumus PPU, 4 kelas ukuran |

Tambahan temuan: di Kebun ada **dua sumur** (satu tergambar di latar, satu sprite objek) dan pohon latar bertumpuk dengan sprite pohon. Ini sumber utama kesan "berantakan".

---

## 1. Fakta teknis yang mengikat (jangan diubah)

### Dunia & kamera

| Parameter | Nilai | Konsekuensi untuk aset |
|---|---|---|
| Ukuran dunia | **27,52 × 15,36 unit** | semua latar & collider memakai ruang ini |
| Latar | **2752 × 1536 px, PPU 100** (`maxTextureSize` 4096) | rasio 16:9, 1 px = 0,01 unit |
| Kamera | orthographic size **5** | tinggi layar 10 unit, lebar 17,78 unit; peta lebih tinggi dari layar → kamera mengikuti pemain |
| Pemain | canvas **262 × 473 px**, PPU **300** (tinggi 1,577 unit) | karakter baru wajib seukuran ini |
| Canvas UI | **1920 × 1080**, Scale With Screen Size, match 0.5 | semua ukuran tombol/ikon dalam ruang 1920×1080 |

**Rumus konversi koordinat (wajib dipakai):**

```text
x unit = x% × 27,52
y unit (dari bawah) = (1 − y%) × 15,36
```

Kalau generator tidak bisa 2752×1536, kirim **2048×1152** dan saya set PPU 74,42 (2048 ÷ 27,52) supaya ukuran dunia tetap sama.

### Batas lantai (objek tidak boleh lebih rendah dari ini)

| Scene | `floorY` (unit) | ymaksimum objek |
|---|---:|---:|
| Dapur | 2,6 | 83,1% |
| Rumah | 4,6 | 70,1% |
| Kebun | 3,8 | 75,3% |

Selain itu, di Dapur objek harus berada **di dalam pagar elips** (pusat 50%/48,6%, setengah sumbu 42%/26%). Di Rumah objek harus di dalam dinding (x 8–87%, y 19–75%).

### Folder aset aktif (jangan tertukar)

| Jenis | Folder aktif dipakai build |
|---|---|
| Latar | `Assets/Art/Scenes/` (2752×1536) |
| Sprite objek | `Assets/Art/Learning2/<Scene>/` |
| Karakter | `Assets/Art/Characters/Yuuta2/` |
| UI | `Assets/Art/UI2/` |

`Assets/Art/Scenes2/` (1376×768) adalah jalur lama yang tidak dipakai `GameSceneBuilder`; jangan dipakai untuk aset baru. `Assets/Art/UI2/` tetap aktif untuk UI.

---

## 2. Aturan pakai (baca dulu)

1. **Tempel blok `STYLE` di awal setiap prompt.** Tanpa itu palet antar-aset akan berbeda.
2. **Latar harus kosong** dari semua objek yang bisa diklik. Ini akar masalah "berantakan".
3. **Jangan pernah minta AI menulis teks.** Semua teks dipasang lewat TextMeshPro.
4. Minta **4 variasi** per latar; kirim semuanya.
5. Kirim lewat **ZIP / Google Drive**, jangan WhatsApp.
6. Urutan kerja: **Base Map → Dapur → Rumah → Kebun → objek → karakter → UI.**
7. Sertakan 1–2 referensi gaya (lampiran gambar), mis. `Assets/Art/Scenes/Dapur.png` dan `Assets/Art/Learning2/Rumah/lemari.png`.
8. **Jangan menimpa** file di folder aktif. Simpan hasil mentah ke:

```text
Assets/foto/Aset_Baru_UAS/
├── Base_Map.png
├── Dapur.png
├── Rumah.png
├── Kebun.png
├── objek/Dapur/<id>.png
├── objek/Rumah/<id>.png
├── objek/Kebun/<id>.png
├── karakter/yuuta3_<aksi>_<arah>.png
└── ui/<nama>.png
```

Saya yang memproses impor, ukuran, pivot, dan penempatan. Folder aktif lama tidak disentuh sampai aset lolos checklist.

---

## 3. Blok wajib

### STYLE (tempel di setiap prompt)

Palet di bawah **diambil dari latar yang sekarang** supaya aset baru tidak bentrok dengan 33 aset terlindungi dan objek lama.

```text
STYLE — 2D top-down 3/4 view game art (camera about 55–60 degrees above the horizon), crisp hand-painted pixel art, cozy Indonesian / Timor village. Saturated warm palette sampled from the existing game: grass #6CCC0C #84CC0C #9CE40C, dark grass #54B40C #548424, path and sand #FCCC54 #E8B84A, light wood #B4843C, mid wood #9C6C3C, dark wood #6C3C24, soil #84543C, terracotta #B5532A, stone #9C9C8C, water #3C9CD8, outline and deep shadow #241008. Thick 2–3 px dark-brown outline, strong readable silhouettes, soft ambient occlusion, sunlight from top-left, no bloom, no blur.
```

### NEGATIVE — latar scene

```text
NEGATIVE — text, letters, numbers, UI, HUD, buttons, watermark, signature, people, characters, animals, duplicate furniture, clutter, fisheye, 3D render, photorealistic, blurry, low resolution, jpeg artifacts, frame, border, cropped buildings.
```

### NEGATIVE — objek & ikon

```text
NEGATIVE — text, letters, numbers, labels, watermark, signature, background, floor, checkerboard, shadow outside the sprite bounds, photorealistic, 3D render, glossy plastic, neon, blurry, low resolution.
```

### Catatan transparansi

Kalau generator tidak mendukung PNG transparan, minta latar **magenta rata `#FF00FF`** (bukan putih/checkerboard). Saya akan hapus warnanya saat impor. Jangan minta latar putih: tepi putih akan tersisa di sprite.

---

## 4. Spesifikasi file

| Aset | Ukuran | Format | Catatan |
|---|---|---|---|
| Latar scene | **2752×1536** | PNG opaque | alternatif 2048×1152 |
| Sprite objek | 512×512 (objek besar 1024×1024) | PNG transparan | pivot bawah-tengah |
| Strip karakter | **1048×473** (4 sel × 262×473) | PNG transparan | 4 frame per strip |
| Ikon UI | 256×256 | PNG transparan | padding 24 px |
| Panel 9-slice | 1024×1024 | PNG transparan | border 64 px |
| Tombol | 512×160 | PNG transparan | border 44 px |
| Latar menu | 2048×1152 | PNG opaque | ruang untuk 4 tombol |
| Ikon Android | 512×512 ×2 | PNG | foreground + background |

**Penamaan objek wajib sama dengan `id` data:** `periuk.png`, `tungku.png`, `lemari.png`, `pohon_mangga.png`, dst.

---

## 5. Base Map

### Geometri nyata (dari `GameSceneBuilder.cs`)

| Elemen | x | y (dari atas) | Sumber |
|---|---:|---:|---|
| Gerbang + spawn pemain | 48,5% | 83,7% | `Start (13.35, 2.5)` |
| Rumah bulat (Dapur) | 23–40% | 15–30% | `House_Round` |
| Pintu Dapur | **32,5%** | **31,6%** | `Door_Dapur (8.95, 10.5)` |
| Rumah kayu (Rumah) | 63–83% | 11–28% | `House_Wood` |
| Pintu Rumah | **73,4%** | **28,4%** | `Door_Rumah (20.2, 11.0)` |
| Kebun berpagar | 66–97% | 43–74% | `Kebun_Fence_*` |
| Gerbang Kebun | **81,0%** | **73,0%** | `Door_Kebun (22.3, 4.15)` |
| Sumur | 30,2% | 52,5% | `Well (8.3, 7.3)` |
| Sungai (kiri) | 1–23% | 0–100% | `River_Upper/Lower` |
| Jembatan (celah sungai) | 1–23% | 32,6–41,7% | celah antar collider sungai |
| NPC pemandu | 43,2% | 72,0% | `CreateNpc (11.9, 4.3)` |
| Pagar keliling + gerbang bawah | x 44–54% (celah) | ~88–93% | `Fence_Bottom_*`, `GatePillar_*` |

**Pohon dekor wajib** (ada collider, jadi harus ada pohonnya): 15,6%/59%; 23,6%/81%; 61,4%/74,6%; 94,5%/32,3%; 95,2%/58,3%; 95,6%/88,9%.

### Prompt

```text
A top-down 3/4 view village map for a 2D learning game. Landscape 16:9, 2752×1536 px. All coordinates are percentages from the top-left corner.

NON-NEGOTIABLE PLACEMENT:
- 48.5%/83.7%: village gate with wooden pillars, open toward the bottom edge; player spawns just inside it. Keep 44–54% x clear as the entrance path.
- Left-center, footprint x 23–40%, y 15–30%: round thatched house (Ume Kbubu) with woven bamboo walls and a conical straw roof. Its front door faces down-right at exactly 32.5%/31.6%, with a short stone step.
- Right, footprint x 63–83%, y 11–28%: rectangular raised wooden house on a grey stone base, thatched roof, veranda. Front door at exactly 73.4%/28.4%, facing the path.
- Right, x 66–97%, y 43–74%: vegetable garden enclosed by a weathered wooden fence, with a bamboo gate at exactly 81%/73%. Inside: neat EMPTY raised soil beds only (no plants, no tools, no baskets, no well).
- 30.2%/52.5%: stone well with a wooden bucket and small thatched roof (decorative only).
- Left edge, x 1–23%: shallow river with rocky banks running vertically; a wooden bridge crosses it at y 32.6–41.7%. Keep both banks readable.
- 43.2%/72%: a small open dirt clearing for the village guide (no objects, no plants).
- Decorative trees with trunks at: 15.6%/59%, 23.6%/81%, 61.4%/74.6%, 94.5%/32.3%, 95.2%/58.3%, 95.6%/88.9%.
- Main path: from the bottom gate at 48.5%/88% up to 48.5%/45%, then one branch left to the round-house door (32.5%/31.6%) and one branch right to the wooden-house door (73.4%/28.4%); a third short branch runs right-down to the garden gate (81%/73%). Path width about 5–6% of the image.
- Boundary wooden fence along the bottom (y 87–95%), with the gate opening at x 44–54%. Fence also along the top edge.
- Leave a 4% empty margin on every edge.

FILL: grass, bushes, flowers, rocks. Keep all paths, the three doors, the garden gate, the spawn clearing and the well clear.
LIGHT: late morning, warm sun from top-left, soft shadows to the bottom-right.
NO characters, NO text, NO UI, NO icons, NO labels, NO extra buildings.
```

### Checklist terima

- [ ] Pintu Dapur tepat 32,5%/31,6%; pintu Rumah 73,4%/28,4%; gerbang Kebun 81%/73%.
- [ ] Jalan menyambung dari gerbang bawah ke ketiga pintu.
- [ ] Kebun: petak tanah **kosong**, tanpa tanaman/alat/sumur.
- [ ] Sungai + jembatan di kiri, sumur di 30%/52,5%.
- [ ] Enam pohon dekor ada di koordinat collider.
- [ ] Area NPC (43%/72%) kosong.
- [ ] Tanpa teks/watermark/bingkai.

---

## 6. Dapur (interior rumah bulat)

### Geometri nyata

| Elemen | Nilai | Catatan |
|---|---|---|
| Pagar/dinding elips | pusat 50%/48,6%, setengah sumbu 42% × 26% | area lantai x 8–92%, y 22–75% |
| Celah pintu keluar | x 44,7–55,2%, y ≥ 80% | koridor ke `Exit_Dapur (13.76, 1.75)` = 50%/88,6% |
| Bukaan cahaya atas | x 44–56%, y 0–12% | dekorasi, bukan jalan keluar |
| Dua tiang | ±40% dan ±60%, y 8–40% | jangan menutup objek |
| Jendela | kiri ~20%/12%, kanan ~78%/12% | pemandangan hijau |

### Prompt

```text
Interior of a round thatched house (Ume Kbubu), top-down 3/4 view, 16:9, 2752×1536 px.

ARCHITECTURE:
- A circular ring of woven bamboo wall panels, warm brown with visible weave, filling the outer frame.
- Two round structural wooden pillars, one at 40%/24% and one at 60%/24%, dark wood with grey stone bases.
- Bright open doorway at top center, x 44–56%, y 0–12%, warm daylight spilling onto the floor.
- A second opening (exit) at bottom center, x 44.7–55.2%, y 82–100%: a gap in the woven fence with a short dirt corridor leading out.
- Two small windows with green foliage outside: left at 20%/14%, right at 78%/14%.
- Packed-earth floor with subtle cracks, straw specks and a soft worn patch in the center.

MUST BE EMPTY: the entire floor x 12–88%, y 24–80% must contain NOTHING. No pots, no hearth, no firewood, no corn, no baskets, no mats, no benches, no jars, no tools, no cloth, no hammock, no food. Interactive objects are separate sprites and will be placed later by the developer. Do not draw any object on the floor.

LIGHT: warm sunlight beam from the top doorway, soft gradient shadow at the bottom.
NO characters, NO text, NO UI.
```

### Penempatan objek baru (menggantikan tabel lama)

Koordinat ini sudah diuji terhadap pagar elips dan `floorY 2,6`.

| Objek | x | y | Tinggi target |
|---|---:|---:|---:|
| tempayan | 22% | 64% | 1,4 unit |
| bakul | 20% | 53% | 0,9 unit |
| kayu_bakar | 30% | 68% | 0,6 unit |
| tungku | 36% | 62% | 1,3 unit |
| periuk (di atas tungku) | 36% | 53% | 0,85 unit |
| cobek | 46% | 68% | 0,7 unit |
| bangku | 55% | 70% | 0,9 unit |
| tikar | 52% | 62% | lebar 2,6 unit |
| jagung | 66% | 68% | 0,8 unit |
| alat_masak | 76% | 57% | 1,1 unit |

Koridor x 44–56%, y 72–90% dibiarkan kosong agar pemain bisa masuk/keluar.

---

## 7. Rumah (interior rumah persegi)

### Geometri nyata

| Elemen | Nilai |
|---|---|
| Dinding atas | y 8–19% |
| Dinding kiri/kanan | x 3,6–8% dan 87,6–92% |
| Dinding bawah + pondasi | y 75,6–88% |
| Pintu keluar | x 44,1–54,7%, `Exit_Rumah (13.6, 2.8)` = 49,4%/81,8% |
| Lantai dalam | x 8–87%, y 19–75%; objek maksimum y 70% |

### Prompt

```text
Interior of a rectangular raised wooden house, top-down 3/4 view, 16:9, 2752×1536 px.

ARCHITECTURE:
- Woven bamboo wall panels with a dark wooden frame, sitting on a grey stone foundation. Stone foundation visible only at the bottom edge.
- Thatched roof edge visible along the top border only.
- Two windows with green outside: left wall at 12%/26%, right wall at 88%/26%.
- Front doorway at bottom center, x 44–55%, y 80–100%, open, with a short wooden ramp and warm light spilling in.
- Four structural posts at 10%/30%, 90%/30%, 10%/70%, 90%/70%.
- Packed-earth floor with a subtle woven texture, a few straw specks, worn center.

MUST BE EMPTY: the whole floor x 10–90%, y 24–78% must contain NOTHING. No beds, no wardrobe, no shelves, no table, no benches, no chest, no jars, no plates, no baskets, no mats, no folded textiles, no wall decorations. Interactive objects are separate sprites placed later. Do not draw furniture against the walls either.

LIGHT: warm afternoon light from the door and the two windows, soft ambient shadow.
NO characters, NO text, NO UI.
```

### Penempatan objek baru

| Objek | x | y | Tinggi target |
|---|---:|---:|---:|
| lemari | 26% | 34% | 1,7 unit |
| rak_piring | 44% | 31% | 1,4 unit |
| peti_kayu | 62% | 33% | 1,2 unit |
| keranjang | 74% | 36% | 1,3 unit |
| tempat_tidur | 15% | 52% | 1,5 unit |
| tikar_rumah | 50% | 60% | lebar 2,8 unit |
| selimut_tenun | 28% | 66% | lebar 2,2 unit |
| tempayan_rumah | 68% | 60% | 1,2 unit |
| meja_makan | 80% | 52% | 1,4 unit |
| bangku_rumah | 80% | 63% | 0,9 unit |

Koridor x 44–55%, y 60–85% tetap terbuka.

---

## 8. Kebun

### Geometri nyata

| Elemen | Nilai |
|---|---|
| Pagar | kiri 6–10%, kanan 91,6–96%, atas y 0–11%, bawah y 79–89% |
| Gerbang | x 44,5–54,5%, `Exit_Kebun (13.76, 2.6)` = 50%/83,1% |
| Gudang | x 67–89%, y 2,3–29,7% (ada collider → wajib digambar) |
| Jalan tengah | x 44–56% dari gerbang ke y ~34% |
| Lantai objek | y maksimum 75,3% |

**Sumur di latar harus DIHILANGKAN.** Saat ini ada `Well` di 12,9%/46% **dan** objek belajar `sumur`. Sumur hanya boleh muncul sebagai sprite objek.

### Prompt

```text
Fenced vegetable garden in an Indonesian village, top-down 3/4 view, 16:9, 2752×1536 px.

ARCHITECTURE:
- Weathered wooden fence around the perimeter, with a bamboo double gate at bottom center, x 44.5–54.5%, y 80–100%.
- Top-right, x 67–89%, y 2–30%: a small open thatched tool shed with a stone base and wooden posts. Interior of the shed is empty (no tools, no sacks).
- Central dirt path from the gate straight up to y 34%, then a cross path running left and right at y 30–36%.
- Two large EMPTY planting areas: left x 12–42%, y 38–76%; right x 58–90%, y 38–76%. Show only tilled dark-brown soil with straw mulch edges and faint furrows. NO plants, NO vegetables, NO seedlings, NO tools, NO baskets, NO well.
- Irrigation channel (shallow, sandy) along the right edge of the central path.
- Grass strips between the beds, a few rocks and flowers at the fence line.

DECOR ONLY: at the outer corners you may add distant palm silhouettes outside the fence; do not add trees inside the fence.
NO characters, NO text, NO UI, NO well, NO water feature inside the garden.
```

### Penempatan objek baru

| Objek | x | y | Tinggi/lebar target |
|---|---:|---:|---|
| daun_pisang | 13% | 36% | 2,4 unit |
| pohon_jeruk | 20% | 26% | 2,8 unit |
| pohon_mangga | 38% | 24% | 3,2 unit |
| terong | 18% | 48% | 0,9 unit |
| kebun_sayur | 23% | 56% | lebar 2,8 unit |
| bibit | 17% | 68% | 0,6 unit |
| tomat | 26% | 70% | 0,8 unit |
| cabai | 34% | 68% | 0,9 unit |
| sumur | 66% | 44% | 2,2 unit |
| pohon_kelapa | 86% | 60% | 3,4 unit |
| alat_kebun | 64% | 66% | 1,0 unit |
| bakul_hasil | 74% | 70% | 0,9 unit |

Koridor gerbang x 44–56%, y 72–88% tetap kosong.

---

## 9. Objek (32 sprite terpisah)

### Template

```text
Single object sprite for a 2D game, isolated on a transparent background, 512×512 px canvas.
The object is centered horizontally; its base sits at 90% of the canvas height (bottom-center pivot). Front 3/4 view, same camera angle as the scene.
High-resolution pixel art matching the STYLE palette, 2–3 px dark-brown outline.
Soft contact shadow directly under the object only — no cast shadow to the side, no floor, no background, no text, no border.
SUBJECT: <isi dari tabel>
```

### Ukuran & PPU (cara menyamakan skala)

Tinggi target di tabel = tinggi **di dunia** (unit). Saat impor:

```text
PPU objek = tinggi_piksel_sprite / tinggi_target_unit
```

Contoh: sprite `lemari` 593 px, target 1,7 unit → PPU 348,8. Semua objek dengan target sama akan terlihat sama besar di game, berapa pun resolusi PNG-nya.

| Kelas | Tinggi dunia | Contoh |
|---|---:|---|
| Kecil | 0,6–1,0 unit | periuk, cobek, bibit, tomat |
| Sedang | 1,1–1,8 unit | tungku, lemari, meja, tempayan |
| Besar | 2,2–3,4 unit | pohon, sumur |
| Hamparan | lebar 2,2–3,2 unit | tikar, selimut, kebun_sayur |

### Dapur (10)

| File | SUBJECT | Target |
|---|---|---:|
| `periuk.png` | blackened clay cooking pot, two small handles, rounded belly | 0,85 |
| `tungku.png` | traditional three-stone hearth with firewood and a low flame | 1,3 |
| `cobek.png` | flat stone mortar with a wooden pestle | 0,7 |
| `bakul.png` | round woven bamboo basket with a wide rim | 0,9 |
| `jagung.png` | two ears of corn, husk partly peeled | 0,8 |
| `kayu_bakar.png` | small bundle of split firewood tied with rattan | 0,6 |
| `alat_masak.png` | wooden ladle, coconut-shell scoop, bamboo strainer grouped | 1,1 |
| `tempayan.png` | large round earthen water jar with woven cover | 1,4 |
| `tikar.png` | flat rectangular woven pandan mat seen from above | lebar 2,6 |
| `bangku.png` | low wooden stool with woven seat | 0,9 |

### Rumah (10)

| File | SUBJECT | Target |
|---|---|---:|
| `lemari.png` | two-door wooden cabinet with carved panels | 1,7 |
| `rak_piring.png` | wooden wall shelf with stacked ceramic plates | 1,4 |
| `peti_kayu.png` | carved wooden chest, hinged lid, rattan straps | 1,2 |
| `tempat_tidur.png` | low wooden bed, woven mattress, folded ikat blanket | 1,5 |
| `tikar_rumah.png` | flat woven mat with geometric border, seen from above | lebar 2,8 |
| `meja_makan.png` | low rectangular wooden dining table | 1,4 |
| `bangku_rumah.png` | low wooden bench with woven seat | 0,9 |
| `tempayan_rumah.png` | medium earthen jar with wooden lid | 1,2 |
| `keranjang.png` | tall woven storage basket with lid | 1,3 |
| `selimut_tenun.png` | neatly folded stack of two ikat textiles | lebar 2,2 |

### Kebun (12)

| File | SUBJECT | Target |
|---|---|---:|
| `kebun_sayur.png` | rectangular raised bed of leafy greens seen from above | lebar 2,8 |
| `daun_pisang.png` | cluster of broad banana leaves from a short stem | 2,4 |
| `pohon_mangga.png` | mango tree, thick trunk, ripe yellow-green fruit | 3,2 |
| `pohon_jeruk.png` | citrus tree, glossy leaves, a few orange fruit | 2,8 |
| `pohon_kelapa.png` | coconut palm, curved trunk, brown coconuts | 3,4 |
| `bibit.png` | young seedling in a small woven leaf pot | 0,6 |
| `tomat.png` | small tomato plant with three ripe red fruit | 0,8 |
| `cabai.png` | chili plant with several small red peppers | 0,9 |
| `terong.png` | eggplant plant with one long purple fruit | 0,9 |
| `sumur.png` | stone well, wooden bucket, small thatched roof | 2,2 |
| `alat_kebun.png` | hoe, spade and sickle leaning together | 1,0 |
| `bakul_hasil.png` | woven harvest basket filled with vegetables | 0,9 |

### Buah di dunia (2 sprite, memperbaiki ikon 13×10 px)

Saat ini buah di Kebun memakai `Assets/Art/UI/Hud/Food_Apple.png` yang hanya **13×10 px** (diperbesar 1,6× di scene) sehingga terlihat pecah. Ganti dengan sprite khusus:

| File | SUBJECT | Target |
|---|---|---:|
| `buah_apel.png` | one ripe red apple with a small green leaf and stem | 0,35 unit |
| `buah_pisang.png` | a bunch of two ripe yellow bananas | 0,35 unit |

Pakai template objek yang sama; latar transparan, pivot bawah-tengah, tanpa bayangan samping.

### Opsi batch (kalau AI mendukung)

```text
A sprite sheet of 10 separate kitchen objects for a 2D game, 5×2 grid on a fully transparent background, 2560×1024 px, 512×512 cells, no grid lines, no labels. Objects: clay pot, three-stone hearth, stone mortar with pestle, woven basket, two ears of corn, firewood bundle, wooden ladles, earthen water jar, woven mat, low stool. Each object centered in its cell, base at the bottom, consistent proportions, matching palette, 2–3 px outline, contact shadow only.
```

Hasil batch tetap harus dipotong; **lebih aman per objek**.

---

## 10. Karakter (memperbaiki animasi monoton)

### Kondisi sekarang (terukur)

- 16 frame: `idle_down`, `walk_down`, `walk_side`, `walk_up` × 4.
- `idle_side` dan `idle_up` **tidak ada** → kode memakai frame pertama walk (`GameSceneBuilder`, baris `idleSide = walkSide[0]`).
- Canvas 262×473, PPU 300, pivot (0,5; 0), `FilterMode.Point`, tanpa mipmap.
- Tinggi figur 431–457 px, telapak kaki 8 px di atas tepi bawah.
- Arah kiri memakai `flipX`; hanya perlu satu arah samping.

**Kesimpulan:** gerakan terasa monoton karena tidak ada animasi diam di samping/atas dan pose walk kurang kontras. Perbaikan: 6 strip.

### Spesifikasi strip

| File | Aksi | Arah | Catatan |
|---|---|---|---|
| `yuuta3_idle_down.png` | diam | bawah | napas 2–3 px |
| `yuuta3_walk_down.png` | jalan | bawah | 4 pose kontras |
| `yuuta3_idle_side.png` | diam | kanan | mesin akan flip untuk kiri |
| `yuuta3_walk_side.png` | jalan | kanan | langkah lebih lebar |
| `yuuta3_idle_up.png` | diam | atas (punggung) | napas 2–3 px |
| `yuuta3_walk_up.png` | jalan | atas | 4 pose kontras |

Ukuran tiap file: **1048 × 473 px** (4 sel 262×473 tanpa celah).

### Prompt template

```text
A horizontal animation strip of 4 frames of the SAME character for a 2D top-down game.
Canvas 1048×473 px, exactly 4 cells of 262×473 px side by side, no gaps, no grid lines, no text, fully transparent background.

CHARACTER: young Dawan boy, 10–12 years old, barefoot, short black hair, woven ikat sarong with red-black geometric pattern, simple brown vest, friendly face. Identical costume, colors and proportions in all 4 frames.
Height 430–455 px in every frame. Feet baseline exactly 8 px above the bottom edge, same baseline in all 4 frames. Character horizontally centered in each 262 px cell with at least 18 px margin. Light from top-left, 2–3 px dark-brown outline. NO baked shadow.

ACTION: <walk | idle>
FACING: <down | side (facing right) | up (back view)>
FRAMES: <walk: 1 contact, 2 passing with body 3 px higher, 3 opposite contact, 4 passing | idle: 1 neutral, 2 shoulders +1 px and slight head lift, 3 neutral, 4 subtle head turn 2 px>
Only limbs, cloth and a 1–3 px body bob may change. Do not change size, hairstyle, colors or costume.
```

### Aturan tambahan

- Kalau hasil 4 sel tidak sejajar, minta ulang dengan kalimat: *"keep the four cells exactly equal width, feet on one baseline, no frame cropped"*.
- Kalau kostum berubah antar frame, minta: *"copy the exact same costume and colors from frame 1 to frames 2–4"*.
- Jangan minta AI membuat bayangan; bayangan dibuat di Unity.
- Jangan minta 4×6 sheet besar; strip per baris jauh lebih akurat.

### Checklist terima

- [ ] 6 file, masing-masing 1048×473, 4 sel sama lebar.
- [ ] Tinggi figur 430–455 px, baseline kaki sama di semua frame.
- [ ] Kostum/warna identik di 24 frame.
- [ ] Tidak ada sel kosong/terpotong.
- [ ] Latar benar-benar transparan.

---

## 11. UI (menu, tombol, ikon)

### Slot sprite yang benar-benar dipakai kode

| Slot di `GameUi.cs` | File sekarang | Dipakai untuk | Aset baru |
|---|---|---|---|
| `panelSprite` | `Assets/Art/UI/UI_Panel_Beige.png` (84×32) | semua panel modal, kartu topik, pill, slider, tombol | `ui/panel.png` 1024², 9-slice border 64 |
| `circleSprite` | `Assets/Art/UI/UI_Circle.png` (128²) | joystick, tombol arah, handle slider | `ui/circle.png` 256² |
| `heartFull` / `heartEmpty` | `Assets/Art/UI2/heart_full.png` (30×27) | HUD hati | `ui/heart_full.png`, `ui/heart_empty.png` 256² |
| `fruitIcon` | `Assets/Art/UI/Hud/Food_Apple.png` (13×10) | penghitung buah | `ui/fruit.png` 256² |
| `dialogPanel` | `Assets/Art/UI2/dialog_panel.png` (1672×941) | panel dialog ketua adat | `ui/dialog_panel.png` 1672×941 |
| `settingsIcon` | `Assets/Art/UI2/settings.png` (63×59) | tombol pengaturan | `ui/gear.png` 256² |
| `dpadUp/Down/Left/Right` | `Assets/Art/UI2/dpad_*.png` (95×97) | tombol arah | `ui/arrow_up.png` dll. 256² |

**Penting untuk `dialog_panel`:** teks dipasang di koordinat tetap. Sisi **kanan** harus berupa perkamen kosong (area teks 560×200 di sekitar 60% kanan), sisi kiri untuk ilustrasi. Jangan menaruh ornamen di area teks.

### Layout menu utama

Canvas 1920×1080.

| Elemen | x | y | Ukuran px |
|---|---:|---:|---|
| Plakat judul | 6–34% | 8–24% | 560×180 |
| Tombol Mulai | 10–34% | 44–52% | 460×88 |
| Tombol Buku | 10–34% | 56–64% | 460×88 |
| Tombol Pengaturan | 10–34% | 68–76% | 460×88 |
| Tombol Keluar | 10–34% | 80–88% | 460×88 |
| Ikon pengaturan | 92–97% | 5–11% | 64×64 |
| Teks versi | 6% | 94% | — |
| Ilustrasi desa | 45–100% | 0–100% | — |

#### Latar menu

```text
Main menu background for a 2D top-down village learning game, 16:9, 2752×1536 px, same STYLE palette and outline as the village map.

COMPOSITION:
- Right 45–100% x: golden-hour village panorama. Round thatched hut at 66%/46%, raised wooden house at 80%/64%, fenced vegetable garden at 90%/36%, dirt path, stone well, palm trees, floating pollen.
- Left 0–45% x: open grassy clearing fading into a warm dark vignette; low contrast, uncluttered.
- Top-left, x 6–34%, y 6–26%: calm sky/foliage, reserved for a title plaque.
- Left column, x 8–36%, y 40–92%: plain grass/dirt with no details, reserved for four stacked buttons.
- Bottom-left, x 4–22%, y 92–98%: plain dark grass for version text.
- Horizon around y 42%.

LIGHT: golden hour, warm sun from top-left, long soft shadows, gentle haze.
NO characters, NO text, NO UI, NO buttons, NO icons.
```

#### Plakat judul (640×240, transparan)

```text
A carved wooden signboard plaque for a game title, front view, isolated on transparent background, 640×240 px. Rounded rectangular wooden board with visible grain, two small rope loops at the top corners, simple ornate border, brown #8B5A2B with ochre #C9A227 trim, 3 px dark-brown outline, soft top-left highlight, carved corner notches. The center is completely blank and flat — no letters, no symbols, no text. No background, no shadow outside the sprite bounds.
```

#### Tombol utama (512×160, 9-slice border 44)

```text
A game UI button, front view, isolated on transparent background, 512×160 px, for 9-slice: flat uniform center, ornament only within the outer 44 px, no text. Rounded wooden plank, brown #9C6C3C face, ochre #C9A227 top highlight, dark brown #241008 3 px outline, subtle bevel, small carved corner notches, light from top-left. No shadow outside the sprite bounds, no text, no icon.
```

Tombol sekunder: sama, tetapi muka kayu lebih gelap `#6C3C24` dengan inset perkamen `#E8D6A8` dan garis ochre tipis.

#### Panel 9-slice (1024×1024)

```text
A parchment and wood UI panel, front view, isolated on transparent background, 1024×1024 px, 9-slice friendly: flat warm parchment center #E8D6A8 with very subtle fiber texture, carved wooden frame exactly 64 px in #9C6C3C, thin ochre inner line, 3 px dark-brown outline, rounded corners, soft top-left light. No text, no icons, no shadow outside the sprite bounds.
```

#### Dialog panel (1672×941)

```text
A dialogue panel for a 2D village game, front view, isolated on transparent background, 1672×941 px. Left 38%: an illustrated portrait of a friendly Dawan village elder (man, about 60, woven headband, brown vest, gentle smile). Right 62%: completely blank warm parchment area with a carved wooden frame, kept empty for text added later. Thin ochre border, 3 px dark-brown outline, soft top-left light. No text, no letters, no UI buttons.
```

#### Ikon — wajib (256×256, transparan)

| File | SUBJECT |
|---|---|
| `play.png` | rounded play triangle on a wooden disc |
| `book.png` | open book with pages and woven cover |
| `gear.png` | wooden gear with eight teeth |
| `close.png` | X made of two carved wooden sticks |
| `back.png` | left-pointing carved wooden arrow |
| `heart_full.png` | rounded heart filled terracotta |
| `heart_empty.png` | same heart, outline only |
| `fruit.png` | papaya with a green leaf |
| `arrow_up.png` / `down` / `left` / `right` | carved wooden arrow, one direction each |
| `hand.png` | pointing hand for the action button |
| `lock.png` | wooden padlock with keyhole |
| `check.png` | bold checkmark carved in wood |

Opsional: `profile.png`, `quiz.png`, `write.png`, `mic.png`, `star.png`, `house.png`, `chevron.png`.

Template:

```text
A single game UI icon, front view, isolated on transparent background, 256×256 px, centered with 24 px padding, bold readable silhouette, 3 px dark-brown outline, warm palette (#C9A227 ochre, #9C6C3C wood, #B5532A terracotta, #FFD98A highlight, #241008 outline), subtle top-left highlight, no text, no shadow outside the icon.
SUBJECT: <isi>
```

#### Joystick

```text
A virtual joystick base, top-down view, isolated on transparent background, 256×256 px: circular wooden ring with a recessed dark center, 3 px dark-brown outline, inner shadow, no knob, no text.
A joystick knob, 128×128 px: round wooden knob, carved top, soft top-left highlight, 3 px outline, transparent background.
```

Joystick sekarang memakai `circleSprite` + tint; aset khusus ini opsional.

---

## 12. Ikon aplikasi Android

```text
Android adaptive app icon foreground, 512×512 px, transparent background, all important shapes inside the central 66% circle. Subject: stylized round Timor thatched hut (Ume Kbubu) with a warm ochre door and a small woven ikat band, bold 2–3 px dark-brown outline, flat warm palette (#9C6C3C, #C9A227, #B5532A, #FFD98A), no text, no letters, no shadow outside the shape, crisp at small sizes.
```

```text
Android adaptive app icon background, 512×512 px, flat radial gradient from #C9A227 center to #3A2418 edges, very subtle woven texture, no objects, no text.
```

Opsional: feature graphic Play Store 1024×500.

---

## 13. Audio (bukan tugas generator gambar)

Kalau Anda memakai AI suara:

| Hal | Ketentuan |
|---|---|
| Format | `.m4a` (AAC) atau `.wav` (16-bit PCM mono 44,1 kHz) — hanya dua ini yang diizinkan `DawanAudioFormat` |
| Lokasi | `Assets/StreamingAssets/Dawan/<id>.m4a` |
| Penamaan | sama dengan `id` objek (`periuk.m4a`) |
| Durasi | ±1,2–2,0 detik per kata |
| Level | puncak ±−3 dBFS, tanpa noise reduction berlebihan |
| Validasi | **wajib** diperiksa penutur asli dialek TTU; TTS hanya untuk placeholder, bukan produksi |
| Jangan | musik latar dengan hak cipta tidak jelas |

Rekaman percakapan dengan penutur asli tetap lebih baik daripada TTS untuk kata seperti *Na'i*, *Tunaf*, *Fatu bena*.

---

## 14. Prioritas kalau waktu mepet

| Prioritas | Aset | Alasan |
|---|---|---|
| P0 | 4 latar kosong (Base_Map, Dapur, Rumah, Kebun) | akar masalah "berantakan" |
| P1 | 6 strip karakter | akar masalah "monoton" |
| P2 | 9 objek inti (periuk, tungku, cobek, lemari, tempat_tidur, meja_makan, alat_kebun, bibit, pohon_mangga) | cukup untuk demo 3 materi |
| P3 | panel, circle, heart×2, fruit, gear, 4 panah, dialog_panel | UI tidak belang |
| P4 | 23 objek sisanya | kelengkapan |
| P5 | menu (latar, plakat, 2 tombol, ikon) + ikon aplikasi | butuh scene baru |

---

## 15. Rencana integrasi (pekerjaan saya setelah aset masuk)

1. **Backup dulu.** Proyek ini bukan repo git. Zip `Assets/Art/Scenes`, `Assets/Art/Learning2`, `Assets/Art/Characters/Yuuta2`, `Assets/Art/UI2` ke `Builds/Backup/`, plus hash manifest.
2. Salin mentah ke `Assets/foto/Aset_Baru_UAS/` (tidak menimpa apa pun).
3. Konversi latar → `Assets/Art/Scenes/` (2752×1536, PPU 100, Bilinear, max 4096, no mipmap).
4. Konversi objek → `Assets/Art/Learning2/<Scene>/` (pivot 0,5/0, PPU dihitung dari tabel §9, FullRect, alphaIsTransparency, max 2048).
5. Perbarui `Assets/Editor/AsetTransparan23Importer.cs`:
   - tabel `Placements` → koordinat §6–§8;
   - PPU objek → hasil rumus;
   - `FlatObjects` tetap `tikar_rumah`, `kebun_sayur` (+ `tikar`, `selimut_tenun` bila perlu).
6. Perbarui `Assets/Editor/GameSceneBuilder.cs`:
   - collider yang berubah: `Well` di Kebun dihapus/dipindah, `Shed` dipertahankan, pagar disesuaikan bila perlu;
   - `idleSide`/`idleUp` memakai strip baru, bukan `walk*[0]`.
7. Buat importer strip karakter baru (meniru `YuutaSheet4x4Importer`, 4 sel per file) → `Assets/Art/Characters/Yuuta3/`.
8. Ganti slot sprite `GameUi` di `GameSceneBuilder.Finish` untuk UI baru, dan ganti sprite buah dunia (`Food_Apple`/`Food_Banana` → `buah_apel`/`buah_pisang`, target 0,35 unit).
9. (Opsional) buat scene `MainMenu` + builder, tombol Mulai → `Base_Map`.
10. Build debug → uji perangkat (CPH1803): masuk 3 lokasi, baca objek, audio, kuis, Save tetap v5.
11. Build produksi universal → perbarui `Builds/Android/dist/`.
12. Verifikasi ulang: 33 aset belajar, 124 soal, 74 manifest, Save fixture, hash APK checkpoint.

**Rollback:** cukup kembalikan folder dari `Builds/Backup/` dan set ulang konstanta path; data pembelajaran tidak pernah disentuh.

---

## 16. Checklist penerimaan global

- [ ] Latar 2752×1536 (atau 2048×1152), opaque, tanpa teks/bingkai/watermark.
- [ ] Zona kosong benar-benar kosong — tidak ada perabot ganda.
- [ ] Pintu/gerbang sesuai koordinat §5–§8.
- [ ] Objek: PNG transparan, margin ≥ 6%, dasar objek 88–92% tinggi kanvas.
- [ ] Skala objek sesuai kelas §9 (tidak ada periuk sebesar ember).
- [ ] Karakter: 6 strip, 4 sel sejajar, baseline sama, kostum identik.
- [ ] Ikon: 256×256, padding 24 px, tanpa bayangan di luar batas.
- [ ] Nama file sama dengan `id` data.

**Tolak / minta ulang bila:** ada teks terbakar di gambar, latar tidak transparan, furnitur tergambar di zona kosong, sel karakter tidak sejajar, atau pintu bergeser > 1,5%.

---

## 17. Prompt revisi (siap tempel)

| Masalah hasil | Prompt perbaikan |
|---|---|
| Masih ada perabot di latar | `Remove ALL objects from the floor. The room must be completely empty except walls, floor, posts, windows and doorway. No pots, no furniture, no mats, no baskets.` |
| Ada teks/angka | `Remove every letter, number and symbol. The image must contain no text at all.` |
| Latar objek tidak transparan | `Redraw on a flat pure magenta #FF00FF background, no gradient, no checkerboard.` |
| Bayangan melebar | `Keep only a small contact shadow directly under the object. Remove any cast shadow to the side.` |
| Skala tidak konsisten | `Redraw at the same visual scale as the reference: the object occupies 60% of the canvas height, centered, base at 90%.` |
| Sel karakter tidak sejajar | `Keep the four cells exactly equal width, feet on one baseline, no frame cropped, same costume in all frames.` |
| Pintu bergeser | `Move the door to exactly 32.5% x and 31.6% y from the top-left. Keep the opening 6% wide.` |
| Terlalu ramai | `Simplify: fewer decorations, larger empty areas, cleaner silhouettes.` |
| Perspektif miring | `Use a strict orthographic top-down 3/4 view, camera 55–60 degrees, no fisheye, no vanishing point.` |

---

## 18. Larangan & risiko

**Larangan**

- Jangan membuat konten bahasa Dawan baru — 33 aset & 124 soal sudah divalidasi penutur asli TTU (19/11/2026).
- Jangan menimpa `Assets/Data/Learning/**`, `Assets/Art/Learning2/**`, Save fixture, atau GUID lama.
- Jangan memasukkan gambar berteks ke latar (teks tidak bisa dihapus tanpa merusak seni).
- Jangan menerbitkan ke Play Store tanpa release keystore + closed testing.

**Risiko yang tersisa**

| Risiko | Dampak | Mitigasi |
|---|---|---|
| AI tidak bisa transparansi | sprite bertepi putih | minta latar magenta, keying saat impor |
| Gaya antar-generate berbeda | palet tidak konsisten | 1 blok STYLE + referensi gambar + 4 variasi |
| Resolusi maksimal 2048 | latar sedikit blur | PPU 74,42 agar ukuran dunia tetap |
| Sel karakter meleset | animasi patah | strip per baris, bukan sheet 4×6 |
| Bukan repo git | sulit rollback | backup zip + hash sebelum integrasi |
| Waktu | integrasi + build + uji ±3–5 jam | kerjakan P0–P2 dulu |

---

## 19. Riwayat versi

| Versi | Tanggal | Perubahan |
|---|---|---|
| 1.0 | 2026-11-23 | Draft awal (2048×1152, koordinat perkiraan) |
| 2.0 | 2026-11-23 | Angka dari kode: dunia 27,52×15,36, latar 2752×1536, palet hasil sampling, posisi pintu nyata, batas `floorY`, ukuran objek dalam unit + rumus PPU, 6 strip karakter 262×473, pemetaan slot UI, catatan audio, rencana integrasi & rollback |
