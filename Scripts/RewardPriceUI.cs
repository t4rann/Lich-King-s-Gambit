using UnityEngine;
using TMPro;

public class RewardPriceUI : MonoBehaviour
{
    [Header("UI")]
    public TextMeshPro priceText;
    public SpriteRenderer icon;

    [Header("Follow")]
    public Vector3 offset = new Vector3(0f, 1.2f, 0f);

    public Transform target { get; private set; }  
    public Piece targetPiece { get; private set; } 

    public void Init(Transform followTarget, int price, Piece piece)
    {
        target = followTarget;
        targetPiece = piece;
        priceText.text = price.ToString();

        SetSortingOrder(10);
    }

    void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = target.position + offset;
    }

    void SetSortingOrder(int order)
    {
        if (priceText != null)
            priceText.sortingOrder = order;

        if (icon != null)
            icon.sortingOrder = order;
    }
}