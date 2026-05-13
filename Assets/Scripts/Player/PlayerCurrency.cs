using UnityEngine;
using TMPro;
using Pixelplacement;

public class PlayerCurrency : MonoBehaviour
{
    public static PlayerCurrency Instance;
    public int totalCurrency = 0;

    [Header("UI")]
    public TextMeshPro currencyText; 
    public Transform currencyIcon;    
    public float flyDuration = 0.5f; 

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        UpdateUI();
    }

    public void AddCurrency(int amount = 1)
    {
        totalCurrency += amount;
        UpdateUI();
    }

    void UpdateUI()
    {
        if (currencyText != null)
            currencyText.text = totalCurrency.ToString();
    }

    public void FlyToUI(Transform coinTransform)
    {
        if (currencyIcon == null) return;

        Vector3 startPos = coinTransform.position;
        Vector3 endPos = currencyIcon.position;

        Tween.Position(coinTransform, endPos, flyDuration, 0f, AnimationCurve.EaseInOut(0,0,1,1), 
            completeCallback: () =>
            {
                AddCurrency(1);        
                Destroy(coinTransform.gameObject); 
            });
    }

    public bool SpendCurrency(int amount)
{
    if (totalCurrency < amount)
        return false;

    totalCurrency -= amount;
    UpdateUI();
    return true;
}

}
