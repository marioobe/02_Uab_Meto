"""Konversi rekaman penutur Dawan (Ogg Opus) menjadi WAV PCM untuk Unity.

Rekaman asli dari penutur berada di docs/rekaman penutur/*.ogg dengan codec
Ogg Opus. Importer audio Unity tidak bisa men-decode Opus, sehingga berkas
dikonversi lebih dulu ke WAV PCM 16-bit mono. Berkas sumber tidak diubah.

Jalankan dari akar proyek:
    python Tools/ConvertDawanAudio.py

Membutuhkan ffmpeg. Skrip memakai binary bawaan paket imageio-ffmpeg bila ada
(`pip install imageio-ffmpeg`); jika tidak, memakai ffmpeg dari PATH.
"""

import os
import shutil
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SOURCE = os.path.join(ROOT, "docs", "rekaman penutur")
TARGET = os.path.join(ROOT, "Assets", "Audio", "Dawan")


def find_ffmpeg():
    try:
        import imageio_ffmpeg

        return imageio_ffmpeg.get_ffmpeg_exe()
    except Exception:
        pass

    ffmpeg = shutil.which("ffmpeg")
    if ffmpeg:
        return ffmpeg

    sys.exit("ffmpeg tidak ditemukan. Jalankan: pip install imageio-ffmpeg")


def main():
    ffmpeg = find_ffmpeg()
    os.makedirs(TARGET, exist_ok=True)

    converted = 0
    failed = []
    for name in sorted(os.listdir(SOURCE)):
        if not name.lower().endswith(".ogg"):
            continue

        source = os.path.join(SOURCE, name)
        output = os.path.join(TARGET, os.path.splitext(name)[0] + ".wav")
        result = subprocess.run(
            [
                ffmpeg, "-hide_banner", "-loglevel", "error", "-y",
                "-i", source, "-ac", "1", "-ar", "44100",
                "-c:a", "pcm_s16le", output,
            ],
            capture_output=True,
            text=True,
        )

        if result.returncode == 0 and os.path.exists(output):
            converted += 1
            print(f"OK   {name} -> {os.path.basename(output)}")
        else:
            failed.append(name)
            print(f"GAGAL {name}: {result.stderr.strip()}")

    print(f"Selesai: {converted} berkas dikonversi.")
    if failed:
        print("Gagal: " + ", ".join(failed))
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
