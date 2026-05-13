using UnityEngine;
using TMPro;
using Pixelplacement;

public class FloatingDamageText : MonoBehaviour
{
    public TextMeshPro text;

    public float floatDistance = 0.6f;
    public float duration = 0.6f;

    public Color normalColor = Color.red;
    public Color critColor = new Color(1f, 0.4f, 0.1f);
    public Color healColor = Color.green;
    public Color mergeColor = Color.yellow;

    [Header("Scale")]
    public float normalScale = 1f;
    public float critScaleMultiplier = 2f;
    public float healScaleMultiplier = 1.3f;
    public float mergeScaleMultiplier = 1.5f;

    // ===================== DAMAGE =====================
    public void Init(int damage, bool isCrit)
    {
        text.text = damage.ToString();

        Color startColor = isCrit ? critColor : normalColor;
        Color endColor = startColor;
        endColor.a = 0f;

        text.color = startColor;

        float targetScale = normalScale * (isCrit ? critScaleMultiplier : 1f);

        PlayAnimation(targetScale, endColor);
    }

    // ===================== HEAL =====================
    public void InitHeal(int value)
    {
        text.text = "+" + value;

        Color startColor = healColor;
        Color endColor = startColor;
        endColor.a = 0f;

        text.color = startColor;

        float targetScale = normalScale * healScaleMultiplier;

        PlayAnimation(targetScale, endColor);
    }

    // ===================== MERGE LEVEL =====================
    public void InitMergeLevel(int level)
    {
        text.text = $"Level {level}";

        Color startColor = mergeColor;
        Color endColor = startColor;
        endColor.a = 0f;

        text.color = startColor;

        float targetScale = normalScale * mergeScaleMultiplier;

        PlayAnimation(targetScale, endColor);
    }

    // ===================== SHARED ANIMATION =====================
    private void PlayAnimation(float targetScale, Color endColor)
    {
        transform.localScale = Vector3.zero;

        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + Vector3.up * floatDistance;

        Tween.Position(transform, endPos, duration, 0f, Tween.EaseOut);

        Tween.LocalScale(
            transform,
            Vector3.one * targetScale,
            duration * 0.25f,
            0f,
            Tween.EaseOut,
            completeCallback: () =>
            {
                Tween.LocalScale(
                    transform,
                    Vector3.one * (targetScale * 0.9f),
                    duration * 0.75f,
                    0f,
                    Tween.EaseIn
                );
            }
        );

        Tween.Color(
            text,
            endColor,
            duration,
            0f,
            completeCallback: () => Destroy(gameObject)
        );
    }
}
