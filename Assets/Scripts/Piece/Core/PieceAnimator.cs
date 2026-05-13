using UnityEngine;
using Pixelplacement;

[RequireComponent(typeof(Piece))]
public class PieceAnimator : MonoBehaviour
{
    [Header("Move")]
    public float moveDuration = 0.2f;
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Attack")]
    public float attackDuration = 0.2f;
    public float attackScaleAmount = 1.2f;
    public AnimationCurve attackCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Hit / Damage")]
    public float hitDuration = 0.12f;
    public float hitScaleAmount = 0.9f;
    public float hitShakeAmount = 0.08f;
    public ParticleSystem hitParticles;


    [Header("Ranged Visuals")]
    public GameObject rangedVisualPrefab;

    private Vector3 baseScale;
    private Vector3 baseLocalPos;
    private bool isAnimating;

    public bool IsAnimating => isAnimating;

    void Awake()
    {
        baseScale = transform.localScale;
        baseLocalPos = transform.localPosition;

        if (hitParticles != null)
            hitParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public void UpdateBaseScale(Vector3 scale)
    {
        baseScale = scale;
        transform.localScale = baseScale;
    }

    // ===================== MOVE =====================
    public void PlayMove(Vector3 targetPos, System.Action onComplete = null)
    {
        if (isAnimating)
        {
            onComplete?.Invoke();
            return;
        }

        isAnimating = true;

        Tween.Position(transform, targetPos, moveDuration, 0f, moveCurve,
            completeCallback: () =>
            {
                isAnimating = false;
                onComplete?.Invoke();
            });

        Tween.LocalScale(transform, baseScale * 0.9f, moveDuration / 2, 0f, moveCurve,
            completeCallback: () =>
            {
                Tween.LocalScale(transform, baseScale, moveDuration / 2, 0f, moveCurve);
            });

        SoundManager.Instance.PlayMove();
    }

// ===================== MELEE ATTACK =====================
public void PlayAttack(Piece targetPiece, System.Action onComplete = null)
{
    if (isAnimating || targetPiece == null) return;

    SoundManager.Instance.PlayAttack();
    isAnimating = true;

    Vector3 startPos = transform.position;
    Vector3 targetPos = targetPiece.transform.position;

    Vector3 attackStep = startPos + (targetPos - startPos) * 0.5f;

    Tween.Position(transform, attackStep, attackDuration / 2, 0f, attackCurve, completeCallback: () =>
    {
        Tween.Position(transform, startPos, attackDuration / 2, 0f, attackCurve, completeCallback: () =>
        {
            isAnimating = false;
            onComplete?.Invoke();
        });
    });

    Tween.LocalScale(transform, baseScale * attackScaleAmount, attackDuration / 2, 0f, attackCurve, completeCallback: () =>
    {
        Tween.LocalScale(transform, baseScale, attackDuration / 2, 0f, attackCurve);
    });
}

    // ===================== RANGED =====================
    public void PlayRanged(System.Action onComplete = null)
    {
        if (isAnimating) return;

        isAnimating = true;

        Tween.LocalScale(transform, baseScale * 1.3f, 0.15f, 0f, attackCurve,
            completeCallback: () =>
            {
                Tween.LocalScale(transform, baseScale, 0.15f, 0f, attackCurve,
                    completeCallback: () =>
                    {
                        isAnimating = false;
                        onComplete?.Invoke();
                    });
            });
    }

    public void PlayCaptainPierce(Vector2Int attackerPos, Vector2Int targetDir, BoardManager board, System.Action onComplete = null)
    {
        if (rangedVisualPrefab == null || board == null)
        {
            onComplete?.Invoke();
            return;
        }

        SoundManager.Instance.PlayPierce();

        Vector2Int dir = new Vector2Int(
            Mathf.Clamp(targetDir.x, -1, 1),
            Mathf.Clamp(targetDir.y, -1, 1)
        );

        int maxCells = 2;

        Vector2Int endCell = attackerPos;

        for (int i = 1; i <= maxCells; i++)
        {
            Vector2Int next = attackerPos + dir * i;

            if (next.x < 0 || next.y < 0 || next.x >= board.width || next.y >= board.height)
                break;

            Tile tile = board.tiles[next.x, next.y];

            if (tile != null && tile.isWall)
                break;

            endCell = next;
        }

        // координаты мира
        Vector3 startWorld = board.GetWorldPosition(attackerPos.x, attackerPos.y);
        Vector3 endWorld = board.GetWorldPosition(endCell.x, endCell.y);

        float cellSize = Vector3.Distance(
            board.GetWorldPosition(0,0),
            board.GetWorldPosition(1,0)
        );

        int cellsCount = Mathf.Max(Mathf.Abs(endCell.x - attackerPos.x), Mathf.Abs(endCell.y - attackerPos.y));

        float distance = cellSize * cellsCount * 1.5f;

        Vector3 dirVector = new Vector3(dir.x, dir.y, 0f).normalized;

        GameObject visual = Instantiate(rangedVisualPrefab, startWorld, Quaternion.identity);

        float angle = Mathf.Atan2(dirVector.y, dirVector.x) * Mathf.Rad2Deg - 90f;
        visual.transform.rotation = Quaternion.Euler(0, 0, angle);

        float width = 0.2f;
        float maxWidth = 1.0f;

        float moveDuration = 0.075f;
        float holdDuration = 0.06f;
        float returnDuration = 0.1f;
        float pulseDuration = 0.025f;

        AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        visual.transform.localScale = new Vector3(width, 0f, 1f);

        Vector3 centerPos = startWorld + dirVector * (distance / 2f);

        Tween.Position(visual.transform, centerPos, moveDuration, 0f, curve);

        Tween.LocalScale(visual.transform, new Vector3(width, distance, 1f), moveDuration, 0f, curve, completeCallback: () =>
        {
            Tween.LocalScale(visual.transform, new Vector3(maxWidth, distance, 1f), pulseDuration, 0f, curve, completeCallback: () =>
            {
                Tween.LocalScale(visual.transform, new Vector3(width, distance, 1f), pulseDuration, 0f, curve, completeCallback: () =>
                {
                    StartCoroutine(DelayReturnThin(visual, startWorld, width, distance, holdDuration, returnDuration, curve, onComplete));
                });
            });
        });
    }


    private System.Collections.IEnumerator DelayReturnThin(GameObject visual, Vector3 startWorld, float width, float distance, float holdDuration, float returnDuration, AnimationCurve curve, System.Action onComplete)
    {
        yield return new WaitForSeconds(holdDuration);

        Tween.Position(visual.transform, startWorld, returnDuration, 0f, curve);
        Tween.LocalScale(visual.transform, new Vector3(width, 0f, 1f), returnDuration, 0f, curve, completeCallback: () =>
        {
            Destroy(visual);
            onComplete?.Invoke();
        });
    }

    public void PlayMagicBeam(Vector2Int attackerPos, Vector2Int direction, BoardManager board, System.Action onComplete = null)
    {
        if (rangedVisualPrefab == null || board == null)
        {
            onComplete?.Invoke();
            return;
        }

        SoundManager.Instance.PlayLaser();

        int x = attackerPos.x;
        int y = attackerPos.y;
        int dx = Mathf.Clamp(direction.x, -1, 1);
        int dy = Mathf.Clamp(direction.y, -1, 1);

        int lastX = x;
        int lastY = y;

        while (true)
        {
            x += dx;
            y += dy;

            if (x < 0 || y < 0 || x >= board.width || y >= board.height) break;

            Tile tile = board.tiles[x, y];

            if (tile != null && tile.isWall)
                break;

            lastX = x;
            lastY = y;
        }

        if (lastX == attackerPos.x && lastY == attackerPos.y)
        {
            onComplete?.Invoke();
            return;
        }

        Vector3 startWorld = board.GetWorldPosition(attackerPos.x, attackerPos.y);
        Vector3 endWorld = board.GetWorldPosition(lastX, lastY);

        Vector3 dirVector = (endWorld - startWorld).normalized;
        float distance = Vector3.Distance(startWorld, endWorld);

        GameObject visual = Instantiate(rangedVisualPrefab, startWorld, Quaternion.identity);
        float angle = Mathf.Atan2(dirVector.y, dirVector.x) * Mathf.Rad2Deg;
        visual.transform.rotation = Quaternion.Euler(0, 0, angle);

        StartCoroutine(PlayMagicBeamRoutine(visual, startWorld, dirVector, distance, 0.05f, 0.9f, 0.05f, 0.05f, 0.15f, 0.1f, onComplete));
    }


    private System.Collections.IEnumerator PlayMagicBeamRoutine(GameObject visual, Vector3 startPos, Vector3 dir, float totalDistance, float initialWidth, float peakWidth, float finalWidth, float stretchDuration, float holdDuration, float shrinkDuration, System.Action onComplete)
    {
        Tween.LocalScale(visual.transform, new Vector3(totalDistance, peakWidth, 1f), stretchDuration, 0f, AnimationCurve.EaseInOut(0, 0, 1, 1));

        Tween.Position(visual.transform, startPos + dir * (totalDistance / 1.5f), stretchDuration, 0f, AnimationCurve.EaseInOut(0, 0, 1, 1));

        yield return new WaitForSeconds(stretchDuration + holdDuration);

        Tween.LocalScale(visual.transform, new Vector3(totalDistance, finalWidth, 1f), shrinkDuration, 0f, AnimationCurve.EaseInOut(0, 0, 1, 1));

        yield return new WaitForSeconds(shrinkDuration);

        Destroy(visual);
        onComplete?.Invoke();
    }

    public void PlayClericCircle(Vector3 targetWorldPos, System.Action onComplete = null)
    {
        if (rangedVisualPrefab == null)
        {
            onComplete?.Invoke();
            return;
        }

        SoundManager.Instance.PlaySpell();

        GameObject circle = Instantiate(
            rangedVisualPrefab,
            targetWorldPos,
            Quaternion.identity
        );

        circle.transform.localScale = Vector3.zero;

        AnimationCurve easeOutBack = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 3f),
            new Keyframe(0.8f, 1.1f, 0f, 0f),
            new Keyframe(1f, 1f, 0f, 0f)
        );

        // 🔹 Основная анимация масштаба
        Tween.LocalScale(
            circle.transform,
            Vector3.one,
            0.25f,
            0f,
            easeOutBack,
            completeCallback: () =>
            {
                // 🔹 Задержка после полного увеличения
                StartCoroutine(DelayDestroyCircle(circle, 0.15f, onComplete));
            }
        );
    }

    private System.Collections.IEnumerator DelayDestroyCircle(GameObject circle, float delay, System.Action onComplete)
    {
        yield return new WaitForSeconds(delay);
        Destroy(circle);
        onComplete?.Invoke();
    }


public void PlayRangedVisual(Vector3 targetPos, System.Action onComplete = null)
{
    if (rangedVisualPrefab == null)
    {
        onComplete?.Invoke();
        return;
    }

    SoundManager.Instance.PlayArrow();

    Vector3 dir = (targetPos - transform.position).normalized;
    float totalDistance = Vector3.Distance(transform.position, targetPos);

    float tileOffset = 0.8f;
    Vector3 startPos = transform.position + dir * tileOffset;

    float finalLength = totalDistance - tileOffset;

    Vector3 centerPos = startPos + dir * (finalLength / 2f);

    GameObject visual = Instantiate(rangedVisualPrefab, startPos, Quaternion.identity);
    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
    visual.transform.rotation = Quaternion.Euler(0, 0, angle);

    float initialWidth = 0.3f; 
    float finalWidth = 0.05f;  
    visual.transform.localScale = new Vector3(0f, initialWidth, 1f);

    Tween.Position(visual.transform, centerPos, 0.15f, 0f, AnimationCurve.EaseInOut(0, 0, 1, 1));

    Tween.LocalScale(
        visual.transform,
        new Vector3(finalLength, finalWidth, 1f),
        0.15f,
        0f,
        AnimationCurve.EaseInOut(0, 0, 1, 1),
        completeCallback: () =>
        {
            Destroy(visual);
            onComplete?.Invoke();
        });
}

    // ===================== HIT / DAMAGE =====================
public void PlayHit()
{
    isAnimating = true;

    Vector3 baseWorldPos = transform.position;

    if (hitParticles != null)
    {
        hitParticles.transform.position = baseWorldPos;
        hitParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        hitParticles.Clear();
        hitParticles.Play();
    }

    CameraShake.Instance?.Shake(0.05f, 0.05f);

    Tween.LocalScale(
        transform,
        baseScale * hitScaleAmount,
        hitDuration,
        0f,
        moveCurve,
        completeCallback: () =>
        {
            Tween.LocalScale(transform, baseScale, hitDuration, 0f, moveCurve);
        });

    Vector3 offset = new Vector3(
        Random.Range(-hitShakeAmount, hitShakeAmount),
        Random.Range(-hitShakeAmount, hitShakeAmount),
        0f
    );

    Tween.Position(
        transform,
        baseWorldPos + offset,
        hitDuration,
        0f,
        moveCurve,
        completeCallback: () =>
        {
            Tween.Position(
                transform,
                baseWorldPos,
                hitDuration,
                0f,
                moveCurve,
                completeCallback: () =>
                {
                    isAnimating = false;
                });
        });
}


    // ===================== DEATH =====================
    public void PlayDeath(Vector3 hitDirection, System.Action onComplete = null)
    {
        isAnimating = true;

        hitDirection = hitDirection.normalized;

        Vector3 startPos   = transform.position;
        Vector3 startScale = transform.localScale;

        float cellsToFly = 4f;
        float duration   = 0.7f;

        Vector3 liftScale  = startScale * 1.2f;
        Vector3 deathScale = Vector3.zero;

        Vector3 knockbackPos = startPos + hitDirection * cellsToFly;

        AnimationCurve easeOut = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 2f),
            new Keyframe(1f, 1f, 0f, 0f)
        );

        AnimationCurve easeIn = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 0f),
            new Keyframe(1f, 1f, 2f, 0f)
        );


        if (hitParticles != null)
        {
            hitParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            hitParticles.Clear();
            hitParticles.Play();
        }

        CameraShake.Instance?.Shake(0.1f, 0.2f); 

        Tween.Rotation(
            transform,
            new Vector3(0, 0, Random.Range(1080f, 1440f)),
            duration,
            0f,
            AnimationCurve.EaseInOut(0, 0, 1, 1)
        );

        Tween.Position(
            transform,
            knockbackPos,
            duration,
            0f,
            easeOut
        );

        Tween.LocalScale(
            transform,
            liftScale,
            duration * 0.35f,
            0f,
            easeOut
        );

        Tween.LocalScale(
            transform,
            deathScale,
            duration,
            duration * 0.25f,
            easeIn,
            completeCallback: () =>
            {
                isAnimating = false;
                onComplete?.Invoke();
            }
        );
    }

    // ===================== HOVER =====================
    public void Hover(bool enter)
    {
        if (isAnimating) return;

        Tween.LocalScale(
            transform,
            enter ? baseScale * 1.05f : baseScale,
            0.1f,
            0f,
            moveCurve
        );
    }

    public void ClickBounce()
    {
        if (isAnimating) return;

        Tween.LocalScale(transform, baseScale * 1.1f, 0.1f, 0f, moveCurve,
            completeCallback: () =>
            {
                Tween.LocalScale(transform, baseScale, 0.1f, 0f, moveCurve);
            });
    }
}
