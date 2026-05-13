using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance;

    [Header("Health")]
    public int maxHP = 100;
    public int currentHP;

    [Header("2D UI")]
    public Transform bloodFill;
    public TextMeshPro hpText;

    [Header("Fill Settings")]
    public float minFillScaleY = 0f;
    public float maxFillScaleY = 3.2f;
    public float fillLerpSpeed = 10f;
    public float damagePop = 0.2f;

    [Header("Damage Particles")]
    public ParticleSystem damageParticles;

    [Header("Pop Settings")]
    public float popCooldownTime = 0.25f;

    private float targetFillY;
    private bool popCooldown;

    private bool firstDamageTriggered = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        currentHP = maxHP;
        targetFillY = maxFillScaleY;
        UpdateUIInstant();
    }

    void Update()
    {
        if (bloodFill != null)
        {
            Vector3 scale = bloodFill.localScale;
            scale.y = Mathf.Lerp(scale.y, targetFillY, Time.deltaTime * fillLerpSpeed);
            bloodFill.localScale = scale;
        }
    }

    // ================= DAMAGE =================
    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;

        currentHP -= amount;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        UpdateUIText();

        targetFillY = Mathf.Lerp(minFillScaleY, maxFillScaleY, (float)currentHP / maxHP);

        if (!popCooldown && bloodFill != null)
        {
            StartCoroutine(DamagePop());
        }

        if (damageParticles != null)
        {
            damageParticles.Play();
        }

        if (!firstDamageTriggered && amount > 0)
        {
            firstDamageTriggered = true;

            if (TutorialManager.Instance != null)
                TutorialManager.Instance.FirstDamage();
        }

        if (currentHP <= 0)
        {
            Die();
        }
    }

    IEnumerator DamagePop()
    {
        popCooldown = true;

        bloodFill.localScale += Vector3.up * damagePop;

        yield return new WaitForSeconds(popCooldownTime);

        popCooldown = false;
    }

    // ================= UI =================
    void UpdateUIText()
    {
        if (hpText != null)
            hpText.text = $"{currentHP}";
    }

    void UpdateUIInstant()
    {
        UpdateUIText();
        if (bloodFill != null)
        {
            Vector3 scale = bloodFill.localScale;
            scale.y = Mathf.Lerp(minFillScaleY, maxFillScaleY, (float)currentHP / maxHP);
            bloodFill.localScale = scale;
        }
    }

    // ================= GAME OVER =================
    void Die()
    {
        SoundManager.Instance.PlayDefeat();
        TurnTimer.Instance?.StopTimer();

        if (GameOverManager.Instance != null)
        {
            if (GameOverManager.Instance.PlayerAlreadyRestarted)
            {
                ReloadScene();
                return;
            }

            GameOverManager.Instance.ShowGameOver();
        }
        else
        {
            ReloadScene();
        }
    }

    public void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void RestoreHealth(int amount)
    {
        currentHP = Mathf.Clamp(currentHP + amount, 0, maxHP);
        UpdateUIText();
        targetFillY = Mathf.Lerp(minFillScaleY, maxFillScaleY, (float)currentHP / maxHP);
    }

    public void FullHeal()
    {
        currentHP = maxHP;
        UpdateUIInstant();
        targetFillY = Mathf.Lerp(minFillScaleY, maxFillScaleY, (float)currentHP / maxHP);
    }
}
