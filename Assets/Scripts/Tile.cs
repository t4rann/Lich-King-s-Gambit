using System.Collections;
using UnityEngine;
using Pixelplacement;

public class Tile : MonoBehaviour
{
    [Header("Grid Position")]
    public int x;
    public int y;

    [Header("State")]
    public bool isWall;
    public Piece currentPiece;
    public CurrencyDrop currency;

    [Header("Optional Objects")]

    private SpriteRenderer sr;
    private Collider2D tileCollider;

    private Color baseColor;
    private Vector3 originalScale;
    private bool inputLocked = false;

    // ===================== HIGHLIGHT =====================
    [Header("Highlight Sprite")]
    [SerializeField] private SpriteRenderer highlightSR;

    private Coroutine highlightCoroutine;
    private Color currentHighlightColor = Color.white;

    // ===================== UNITY =====================
    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        tileCollider = GetComponent<Collider2D>();
        originalScale = transform.localScale;

        if (highlightSR != null)
            highlightSR.gameObject.SetActive(false);

        if (tileCollider == null)
            Debug.LogWarning($"Tile at ({x},{y}) has no Collider2D!");
    }
    // ===================== INIT =====================
    public void Init(int xPos, int yPos, bool wall = false)
    {
        x = xPos;
        y = yPos;
        isWall = wall;

        name = wall ? $"Wall {x},{y}" : $"Tile {x},{y}";

        SetColliderEnabled(!isWall);
    }

    public void SetColliderEnabled(bool enabled)
    {
        if (tileCollider != null)
            tileCollider.enabled = enabled;
    }

    // ===================== HIGHLIGHT API =====================
    public void HighlightMove()
    {
        if (isWall) return;
        StartHighlight(Color.white);
    }

    public void HighlightAttack()
    {
        if (isWall) return;
        StartHighlight(Color.red);
    }

    public void HighlightHeal()
    {
        if (isWall) return;
        StartHighlight(Color.green);
    }

    public void HighlightMerge()
    {
        if (isWall) return;
        StartHighlight(Color.yellow);
    }

    public void StartHighlight(Color color)
    {
        if (isWall) return;

        StopHighlight();

        if (highlightSR == null)
            return;

        currentHighlightColor = color;
        highlightSR.color = new Color(color.r, color.g, color.b, 1f);
        highlightSR.gameObject.SetActive(true);

        StartPulsingHighlight();
    }

    private void StartPulsingHighlight()
    {
        if (highlightCoroutine != null)
            StopCoroutine(highlightCoroutine);
            
        highlightCoroutine = StartCoroutine(PulseHighlight());
    }

    private IEnumerator PulseHighlight()
    {
        float duration = 1f;
        float elapsed = 0f;
        
        while (true)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.PingPong(elapsed, duration / 2) / (duration / 2);
            
            float alpha = Mathf.Lerp(0.5f, 1f, t);
            
            highlightSR.color = new Color(
                currentHighlightColor.r,
                currentHighlightColor.g,
                currentHighlightColor.b,
                alpha
            );
            
            yield return null;
        }
    }

    public void ResetHighlight()
    {
        StopHighlight();
    }

    private void StopHighlight()
    {
        if (highlightCoroutine != null)
        {
            StopCoroutine(highlightCoroutine);
            highlightCoroutine = null;
        }

        if (highlightSR != null)
            highlightSR.gameObject.SetActive(false);
    }

    // ===================== CLICK / HOVER =====================
    void OnMouseDown()
    {
        if (isWall || inputLocked) return;

        SoundManager.Instance?.PlaySelect();

        if (LevelManager.Instance != null &&
            LevelManager.Instance.IsWaitingForRewardPlacement())
        {
            if (LevelManager.Instance.HandleRewardClick(this))
                return;
        }

        Tween.LocalScale(
            transform,
            originalScale * 0.95f,
            0.1f,
            0f,
            completeCallback: () =>
                Tween.LocalScale(transform, originalScale, 0.1f, 0f)
        );

        GameManager.Instance.TryMove(this);
    }

    void OnMouseEnter()
    {
        if (isWall || inputLocked) return;
        Tween.LocalScale(transform, originalScale * 1.05f, 0.1f, 0f);
    }

    void OnMouseExit()
    {
        if (isWall || inputLocked) return;
        Tween.LocalScale(transform, originalScale, 0.1f, 0f);
    }

    public void SetInputLocked(bool locked)
    {
        inputLocked = locked;

        if (tileCollider != null)
            tileCollider.enabled = !locked && !isWall;
    }

    // ===================== PIECE =====================
    public void SetPiece(Piece piece)
    {
        if (isWall) return;

        currentPiece = piece;

        if (currency != null)
        {
            if (piece.isWhite)
                currency.Collect();
            else
                Destroy(currency.gameObject);

            currency = null;
        }

        GameManager.Instance?.UpdateTileCollidersImmediately();
    }

    // ===================== CURRENCY =====================
    public void PlaceCurrency(CurrencyDrop drop)
    {
        if (drop == null || isWall) return;

        currency = drop;
        currency.tile = this;
        currency.transform.position =
            GameManager.Instance.board.GetWorldPosition(x, y);
    }

    // ===================== CLEANUP =====================
    void OnDestroy()
    {
        StopHighlight();
    }
}