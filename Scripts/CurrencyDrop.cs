using UnityEngine;

public class CurrencyDrop : MonoBehaviour
{
    [HideInInspector] public Tile tile;

    public void Collect()
    {
        SoundManager.Instance.PlayApply();

        if (PlayerCurrency.Instance != null)
        {
            PlayerCurrency.Instance.FlyToUI(transform);
        }

        if (tile != null)
            tile.currency = null;
    }
}
