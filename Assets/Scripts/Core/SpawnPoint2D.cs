using UnityEngine;

/// <summary>
/// Menempatkan pemain di pintu gubuk saat kembali dari dalam gubuk.
/// Dipakai di scene desa saja; scene dalam gubuk memakai posisi tetap.
/// </summary>
public class SpawnPoint2D : MonoBehaviour
{
    private void Start()
    {
        if (!GameSession.HasReturnPosition)
        {
            return;
        }

        transform.position = GameSession.VillageReturnPosition;
        GameSession.HasReturnPosition = false;
    }
}
