using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class MoveController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BoardManager board;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TurnComboBar comboBar;

    [Header("Animation Settings")]
    [SerializeField] private float animationDelayBetweenActions = 0.1f;

    private Piece movingPiece;
    private Tile targetTile;
    private Tile startTile;
    private Vector2Int targetPosition;
    private Piece lastMovedPiece;

    #region Public API

    public bool TryExecuteMove(Piece selectedPiece, Tile targetTile, System.Collections.Generic.List<Vector2Int> currentMoves)
    {
        if (!ValidateMove(selectedPiece, targetTile, currentMoves))
            return false;

        InitializeMove(selectedPiece, targetTile);

        Piece targetPiece = targetTile.currentPiece;
        bool isRanged = IsRangedAttack();

        if (targetPiece != null)
            ExecuteAttack(targetPiece, isRanged);
        else
            ExecuteMovement();

        gameManager.UpdateTileCollidersImmediately();
        return true;
    }

    public void CancelCurrentMove()
    {
        ResetMoveState();
    }

    public bool HasLineOfSight(Piece attacker, Vector2Int target)
    {
        if (board == null || board.tiles == null || attacker == null)
            return false;

        int dx = Mathf.Clamp(target.x - attacker.x, -1, 1);
        int dy = Mathf.Clamp(target.y - attacker.y, -1, 1);

        int x = attacker.x;
        int y = attacker.y;

        while (true)
        {
            x += dx;
            y += dy;

            if (x < 0 || y < 0 || x >= board.width || y >= board.height)
                return false;

            Tile tile = board.tiles[x, y];

            if (tile == null)
                continue;

            if (tile.isWall)
                return false;

            if (x == target.x && y == target.y)
                return true;

            if (tile.currentPiece != null)
                return false;
        }
    }

    public void DealPiercingDamage(Piece attacker, Vector2Int target, int damage, bool isCrit)
    {
        int dx = Mathf.Clamp(target.x - attacker.x, -1, 1);
        int dy = Mathf.Clamp(target.y - attacker.y, -1, 1);

        int x = attacker.x;
        int y = attacker.y;

        int maxDistance = (attacker is Captain) ? 2 : int.MaxValue;

        for (int step = 1; step <= maxDistance; step++)
        {
            x += dx;
            y += dy;

            if (x < 0 || y < 0 || x >= board.width || y >= board.height)
                break;

            Tile tile = board.tiles[x, y];

            if (tile == null)
                continue;

            if (tile.isWall)
                break;

            Piece piece = tile.currentPiece;
            if (piece != null && piece.isWhite != attacker.isWhite)
            {
                Vector3 hitDir = (piece.transform.position - attacker.transform.position).normalized;
                piece.TakeDamage(damage, isCrit, hitDir);
            }
        }
    }

    #endregion

    #region Validation / Init

    private bool ValidateMove(Piece selectedPiece, Tile targetTile, System.Collections.Generic.List<Vector2Int> currentMoves)
    {
        if (selectedPiece == null || targetTile == null)
            return false;

        Vector2Int target = new Vector2Int(targetTile.x, targetTile.y);
        if (!currentMoves.Contains(target))
        {
            gameManager.ClearHighlights();
            return false;
        }

        return true;
    }

    private void InitializeMove(Piece selectedPiece, Tile tile)
    {
        movingPiece = selectedPiece;
        lastMovedPiece = selectedPiece;

        targetTile = tile;
        startTile = board.tiles[selectedPiece.x, selectedPiece.y];
        targetPosition = new Vector2Int(tile.x, tile.y);

        if (startTile == null)
            throw new InvalidOperationException("Start tile is null");
    }

    #endregion

    #region Attack Logic

    private void ExecuteAttack(Piece targetPiece, bool isRanged)
    {
        if (isRanged)
            ExecuteRangedAttack(targetPiece);
        else
            ExecuteMeleeAttack(targetPiece);
    }

    private void ExecuteMeleeAttack(Piece targetPiece)
    {
        movingPiece.animator.PlayAttack(targetPiece, () =>
        {
            movingPiece.DealDamage(targetPiece);
            CompleteAttack();
        });
    }

    private void ExecuteRangedAttack(Piece targetPiece)
    {
        if (!HasLineOfSight(movingPiece, targetPosition))
        {
            ResetMoveState();
            return;
        }

        Piece mover = movingPiece;

        mover.PlayAttackAnimation(targetPiece, board, () =>
        {
            if (mover is Cleric cleric && targetPiece.isWhite == cleric.isWhite)
            {
                cleric.HealTarget(targetPiece, cleric.attack);
            }
            else if (mover is Mage || mover is Captain)
            {
                int damage = mover.attack;
                bool isCrit = Random.value < mover.critChance;

                if (TurnComboBar.Instance != null && TurnComboBar.Instance.IsAtMaxCharge() && mover.isWhite)
                    damage *= 2;

                DealPiercingDamage(mover, new Vector2Int(targetPiece.x, targetPiece.y), damage, isCrit);
            }
            else
            {
                mover.DealDamage(targetPiece);
            }

            ResetMoveState();
            Invoke(nameof(EndTurn), animationDelayBetweenActions);
        });
    }

    private void CompleteAttack()
    {
        gameManager.ClearHighlights();
        ResetMoveState();
        Invoke(nameof(EndTurn), animationDelayBetweenActions);
    }

    #endregion

    #region Movement

    private void ExecuteMovement()
    {
        startTile.currentPiece = null;

        movingPiece.animator.PlayMove(
            board.GetWorldPosition(targetTile.x, targetTile.y),
            OnMoveAnimationComplete
        );
    }

    private void OnMoveAnimationComplete()
    {
        movingPiece.ForceSetPosition(targetTile.x, targetTile.y);
        targetTile.SetPiece(movingPiece);

        gameManager.ClearHighlights();
        ResetMoveState();
        Invoke(nameof(EndTurn), animationDelayBetweenActions);
    }

    #endregion

    #region Utils

    private bool IsRangedAttack()
    {
        return movingPiece?.IsRangedAttack(targetPosition) ?? false;
    }

    private void ResetMoveState()
    {
        movingPiece = null;
        targetTile = null;
        startTile = null;
    }

    private void EndTurn()
    {
        comboBar?.RegisterMove(lastMovedPiece);
        gameManager.EndTurn();
    }

    #endregion
}
