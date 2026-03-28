using UnityEngine;
using TMPro;
using Pixelplacement;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance;

    [Header("Panels")]
    public GameObject panel;
    public TextMeshPro text;

    [Header("Buttons")]
    public Transform tryAgainButton;
    public Transform startOverButton;
    public BoxCollider2D tryAgainCollider;
    public BoxCollider2D startOverCollider;

    [Header("Rating")]
    public TextMeshPro linkText;
    public BoxCollider2D linkCollider;
    public string gameLink;

    [Header("References")]
    public BoardManager boardManager;
    public PlayerHealth player;

    [Header("Settings")]
    public float scaleDuration = 0.3f;
    public float escapeRadius = 2f;
    public float jumpHeight = 0.5f;
    public float jumpDuration = 0.15f;

    private Vector3 tryAgainFixedXPos;

    private bool firstTryAgainClicked = false;
    private bool trickModeActive = false;
    private bool canRetry = false;
    private bool isEscaping = false;

    private bool playerAlreadyRestarted = false;
    public bool PlayerAlreadyRestarted => playerAlreadyRestarted;

    void Awake()
    {
        Instance = this;

        panel.SetActive(false);
        linkText.gameObject.SetActive(false);

        tryAgainFixedXPos = tryAgainButton.position;

        panel.transform.localScale = Vector3.zero;
        tryAgainButton.localScale = Vector3.zero;
        startOverButton.localScale = Vector3.zero;
        linkText.transform.localScale = Vector3.zero;
    }

    public void ShowGameOver()
    {
        firstTryAgainClicked = false;
        trickModeActive = false;
        canRetry = false;
        isEscaping = false;

        if (boardManager != null && boardManager.tiles != null)
        {
            boardManager.OnBoardDisappearComplete = ShowPanel;
            boardManager.ClearBoardAnimated();
        }
    }

    private void ShowPanel()
    {
        panel.SetActive(true);

        tryAgainButton.gameObject.SetActive(true);
        startOverButton.gameObject.SetActive(true);

        tryAgainCollider.enabled = true;
        startOverCollider.enabled = true;

        linkText.gameObject.SetActive(false);

        text.text = "You lost, but if you want you can try playing this level again — just press Try Again.";

        Tween.LocalScale(panel.transform, Vector3.one, scaleDuration, 0f, Tween.EaseInOut);
        Tween.LocalScale(tryAgainButton, Vector3.one, scaleDuration, 0f, Tween.EaseInOut);
        Tween.LocalScale(startOverButton, Vector3.one, scaleDuration, 0f, Tween.EaseInOut);
    }

    void Update()
    {
        if (!playerAlreadyRestarted && trickModeActive && !isEscaping)
        {
            Vector2 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            if (tryAgainCollider.enabled && tryAgainCollider.bounds.Contains(mouse))
            {
                EscapeTryAgain();
            }
        }

        if (!Input.GetMouseButtonDown(0)) return;

        Vector2 clickPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(clickPos, Vector2.zero);

        if (!hit) return;

        if (hit.collider == startOverCollider)
            RestartLevel();

        else if (hit.collider == tryAgainCollider)
            OnTryAgain();

        else if (hit.collider == linkCollider)
            OnLinkClicked();
    }

    void OnTryAgain()
    {
        if (playerAlreadyRestarted)
        {
            RestartLevel();
            return;
        }

        if (!firstTryAgainClicked)
        {
            firstTryAgainClicked = true;

            startOverCollider.enabled = false;

            tryAgainFixedXPos = new Vector3(0, tryAgainButton.position.y, tryAgainButton.position.z);
            tryAgainButton.position = tryAgainFixedXPos;

            startOverButton.gameObject.SetActive(false);

            linkText.gameObject.SetActive(true);
            text.text = "Since you really want to continue, maybe rate the game first?";
            linkText.text = "Rate the game";

            linkCollider.enabled = true;

            linkText.transform.localScale = Vector3.zero;
            Tween.LocalScale(linkText.transform, Vector3.one, scaleDuration, 0f, Tween.EaseInOut);
        }
        else if (!trickModeActive && !canRetry)
        {
            trickModeActive = true;
            text.text = "Just rate the game.";
        }
        else if (canRetry)
        {
            RestartLevel();
        }
    }

    void OnLinkClicked()
    {
        SoundManager.Instance.PlayApply();

        Application.OpenURL(gameLink);

        Tween.Position(tryAgainButton, tryAgainFixedXPos, 0.2f, 0f, Tween.EaseOut);
        Tween.Position(tryAgainCollider.transform, tryAgainFixedXPos, 0.2f, 0f, Tween.EaseOut);

        canRetry = true;
        trickModeActive = false;

        linkCollider.enabled = false;

        text.text = "Thank you";

        tryAgainCollider.enabled = true;
    }

    void EscapeTryAgain()
    {
        isEscaping = true;
        tryAgainCollider.enabled = false;

        Vector2 randomOffset = Random.insideUnitCircle * escapeRadius;
        Vector3 targetPos = tryAgainFixedXPos + (Vector3)randomOffset;

        Camera cam = Camera.main;

        Vector3 viewportPos = cam.WorldToViewportPoint(targetPos);
        viewportPos.x = Mathf.Clamp(viewportPos.x, 0.05f, 0.95f);
        viewportPos.y = Mathf.Clamp(viewportPos.y, 0.05f, 0.95f);

        targetPos = cam.ViewportToWorldPoint(viewportPos);
        targetPos.z = tryAgainButton.position.z;

        Vector3 jumpPos = targetPos + Vector3.up * jumpHeight;

        Tween.Position(tryAgainButton, jumpPos, jumpDuration, 0f, Tween.EaseOut,
            completeCallback: () =>
            {
                Tween.Position(tryAgainButton, targetPos, jumpDuration, 0f, Tween.EaseIn);
            });

        Tween.Position(tryAgainCollider.transform, targetPos, jumpDuration * 2f, 0f, Tween.EaseInOut,
            completeCallback: () =>
            {
                tryAgainCollider.enabled = true;
                isEscaping = false;
            });
    }

    void RestartLevel()
    {
        playerAlreadyRestarted = true;

        panel.SetActive(false);

        if (player != null)
            player.FullHeal();

        LevelManager.Instance.RestartLevel();
    }
}
