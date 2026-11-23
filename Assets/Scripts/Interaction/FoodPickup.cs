using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hasil kebun yang bisa diambil dengan berjalan menabraknya.
/// Menambah hitungan di HUD lalu menghilang. Nama yang sudah diambil disimpan
/// statis supaya tidak muncul lagi setelah pemain keluar-masuk gubuk.
/// </summary>
public class FoodPickup : MonoBehaviour
{
    private static readonly HashSet<string> Taken = new HashSet<string>();

    [SerializeField] private string foodName = "Buah";

    private bool collected;

    public string FoodName => foodName;

    public void Bind(string name)
    {
        foodName = name;
    }

    private void Start()
    {
        // Scene desa dibangun ulang setiap kembali dari gubuk; sembunyikan yang sudah diambil.
        if (Taken.Contains(foodName))
        {
            gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected || other.GetComponent<PlayerMovement2D>() == null)
        {
            return;
        }

        collected = true;
        Taken.Add(foodName);
        GameSession.FoodCollected++;
        HudController.RefreshAll();
        Debug.Log($"Mengambil {foodName}. Total hasil kebun: {GameSession.FoodCollected}");

        gameObject.SetActive(false);
    }
}
