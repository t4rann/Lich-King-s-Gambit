using UnityEngine;
using TMPro;
using System.Collections;

public class TurnTimer : MonoBehaviour
{
    public static TurnTimer Instance;

    [Header("Timer Settings")]
    public float startTime = 120f;
    public float overtimeDamageDelay = 1f;
    public float startDelay = 1.5f;

    [Header("Visual (NO CANVAS)")]
    public Transform fillBar;
    public SpriteRenderer fillRenderer;
    public TextMeshPro timerText;

    [Header("Fill Settings")]
    public float maxFillScaleX = 3.2f;

    [Header("Colors")]
    public Color fullTimeColor = Color.white;
    public Color lowTimeColor = Color.red;

    [Header("Tutorial Trigger (SMART UX)")]
    [Range(0f,1f)]
    public float dangerThreshold = 0.25f;

    [Header("Pause Icon")]
    public GameObject pauseIcon;

    private float currentTime;
    private float overtimeTimer;
    private bool inOvertime;
    private bool isStopped;

    private bool dangerTriggered;

    private Coroutine startDelayCoroutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        ResetTimer();
    }

    void Update()
    {
        if (isStopped)
            return;

        if (!GameManager.Instance.whiteTurn)
            return;

        if (!inOvertime)
        {
            currentTime -= Time.deltaTime;

            if (currentTime <= 0f)
            {
                currentTime = 0f;
                EnterOvertime();
            }

            float normalizedTime = currentTime / startTime;
            if (!dangerTriggered && normalizedTime <= dangerThreshold)
            {
                dangerTriggered = true;
                if (TutorialManager.Instance != null)
                    TutorialManager.Instance.TimerHalf();
            }
        }
        else
        {
            overtimeTimer -= Time.deltaTime;

            if (overtimeTimer <= 0f)
            {
                overtimeTimer = overtimeDamageDelay;
                PlayerHealth.Instance.TakeDamage(1);
            }
        }

        UpdateVisuals();
    }

    // ================= PUBLIC API =================

    public void ResetTimer()
    {
        currentTime = startTime;
        overtimeTimer = overtimeDamageDelay;
        inOvertime = false;
        isStopped = true;
        dangerTriggered = false;

        UpdatePauseIcon();
        UpdateVisuals();

        if (startDelayCoroutine != null) StopCoroutine(startDelayCoroutine);
        startDelayCoroutine = StartCoroutine(StartTimerWithDelay());
    }

    public void StopTimer()
    {
        isStopped = true;
        UpdatePauseIcon();
    }

    public void ResumeTimer()
    {
        isStopped = true;
        UpdatePauseIcon();

        if (startDelayCoroutine != null) StopCoroutine(startDelayCoroutine);
        startDelayCoroutine = StartCoroutine(StartTimerWithDelay());
    }

    public bool IsStopped() => isStopped;

    // ================= INTERNAL =================

    private IEnumerator StartTimerWithDelay()
    {
        if (pauseIcon != null)
            pauseIcon.SetActive(true);

        yield return new WaitForSecondsRealtime(startDelay);

        isStopped = false;
        if (pauseIcon != null)
            pauseIcon.SetActive(false);
    }

    void EnterOvertime()
    {
        inOvertime = true;
        overtimeTimer = overtimeDamageDelay;
    }

    void UpdateVisuals()
    {
        // ===== TEXT =====
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(currentTime / 60f);
            int seconds = Mathf.FloorToInt(currentTime % 60f);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }

        // ===== BAR SCALE =====
        float t = inOvertime ? 0f : currentTime / startTime;

        if (fillBar != null)
        {
            Vector3 scale = fillBar.localScale;
            scale.x = Mathf.Lerp(0f, maxFillScaleX, t);
            fillBar.localScale = scale;
        }

        // ===== COLOR LERP =====
        if (fillRenderer != null)
        {
            fillRenderer.color = Color.Lerp(lowTimeColor, fullTimeColor, t);
        }
    }

    private void UpdatePauseIcon()
    {
        if (pauseIcon != null)
            pauseIcon.SetActive(isStopped);
    }
}
