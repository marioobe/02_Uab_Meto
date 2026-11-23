using UnityEngine;

/// <summary>
/// Data kecil yang harus bertahan saat pindah scene (desa <-> dalam gubuk).
/// Disimpan sebagai static karena scene hanya di-reload, bukan aplikasinya.
/// </summary>
public static class GameSession
{
    public const int MaxHearts = 3;

    public static int Hearts = MaxHearts;
    public static int FoodCollected;

    /// <summary>Indeks gubuk yang sedang dimasuki (-1 = tidak ada).</summary>
    public static int PendingHutIndex = -1;

    /// <summary>Posisi pemain di desa saat kembali dari dalam gubuk.</summary>
    public static Vector2 VillageReturnPosition;
    public static bool HasReturnPosition;
}
