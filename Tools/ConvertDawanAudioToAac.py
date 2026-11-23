"""Konversi audio pelafalan Dawan (WAV) menjadi AAC/M4A untuk fallback MediaPlayer.

Sebagian perangkat Android (mis. OPPO A3s / ColorOS 8.1) tidak menyediakan
output PCM float yang diminta Unity 6, sehingga FMOD gagal inisialisasi dan
semua audio Unity senyap. Untuk perangkat seperti itu aplikasi memutar
pelafalan lewat android.media.MediaPlayer yang memakai PCM 16-bit.

MediaPlayer tidak bisa membaca berkas di dalam APK (StreamingAssets) langsung
sebagai AudioClip, jadi audio dikemas sebagai M4A/AAC di
`Assets/StreamingAssets/Dawan/` lalu disalin ke penyimpanan aplikasi saat
pertama diputar.

Jalankan dari akar proyek:
    python Tools/ConvertDawanAudioToAac.py

Membutuhkan ffmpeg (imageio-ffmpeg atau ffmpeg di PATH). Berkas WAV sumber
tidak diubah.
"""

import os
import shutil
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SOURCE = os.path.join(ROOT, "Assets", "Audio", "Dawan")
TARGET = os.path.join(ROOT, "Assets", "StreamingAssets", "Dawan")


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
    total_bytes = 0

    for name in sorted(os.listdir(SOURCE)):
        if not name.lower().endswith(".wav"):
            continue

        source = os.path.join(SOURCE, name)
        output = os.path.join(TARGET, os.path.splitext(name)[0] + ".m4a")
        result = subprocess.run(
            [
                ffmpeg, "-hide_banner", "-loglevel", "error", "-y",
                "-i", source, "-ac", "1", "-ar", "44100",
                "-c:a", "aac", "-b:a", "64k", output,
            ],
            capture_output=True,
            text=True,
        )

        if result.returncode == 0 and os.path.exists(output):
            converted += 1
            total_bytes += os.path.getsize(output)
            print(f"OK   {name} -> {os.path.basename(output)} "
                  f"({os.path.getsize(output) / 1024:.0f} KB)")
        else:
            failed.append(name)
            print(f"GAGAL {name}: {result.stderr.strip()}")

    print(f"Selesai: {converted} berkas, total {total_bytes / 1024 / 1024:.2f} MB.")
    if failed:
        print("Gagal: " + ", ".join(failed))
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
