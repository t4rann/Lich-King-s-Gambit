using System.Collections;
using UnityEngine;
using TMPro;

public class TurnComboBar : MonoBehaviour
{
    public static TurnComboBar Instance;

    [Header("Values")]
    public int minValue = 0;
    public int maxValue = 3;

    [Header("Visual")]
    public Transform barVisual;
    public float maxScaleX = 1f;

    [Header("Animation")]
    public float fillSpeed = 8f;

    [Header("Text")]
    public TextMeshPro valueText;

    private int currentValue = 0;
    private Piece lastAllyPiece;

    private Coroutine scaleRoutine;

    private bool chargeTutorialTriggered = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        ResetCharge();
    }

    public bool IsAtMaxCharge()
    {
        return currentValue >= maxValue;
    }

    // ================= RESET =================
    public void ResetCharge()
    {
        currentValue = 0;
        lastAllyPiece = null;

        UpdateVisual();
    }

    // ================= REGISTER MOVE =================
    public void RegisterMove(Piece piece)
    {
        if (piece == null)
            return;

        if (!piece.isWhite)
            return;

        if (piece != lastAllyPiece)
            currentValue += 1;
        else
            currentValue -= 1;

        currentValue = Mathf.Clamp(currentValue, minValue, maxValue);
        lastAllyPiece = piece;

        if (!chargeTutorialTriggered && currentValue >= 2)
        {
            chargeTutorialTriggered = true;

            TutorialManager.Instance?.ChargeTutorial();
        }

        UpdateVisual();
    }

    // ================= VISUAL =================
    private void UpdateVisual()
    {
        float targetT = (float)currentValue / maxValue;
        float targetScaleX = Mathf.Lerp(0f, maxScaleX, targetT);

        if (scaleRoutine != null)
            StopCoroutine(scaleRoutine);

        scaleRoutine = StartCoroutine(AnimateScaleX(targetScaleX));

        if (valueText != null)
        {
            valueText.text = $"Charge {currentValue}/{maxValue}";
        }
    }

    private IEnumerator AnimateScaleX(float targetX)
    {
        Vector3 startScale = barVisual.localScale;
        float startX = startScale.x;

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * fillSpeed;
            float x = Mathf.Lerp(startX, targetX, t);
            barVisual.localScale = new Vector3(x, startScale.y, startScale.z);
            yield return null;
        }

        barVisual.localScale = new Vector3(targetX, startScale.y, startScale.z);
    }
}
