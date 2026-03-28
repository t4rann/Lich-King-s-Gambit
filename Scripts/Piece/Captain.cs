using System;
using System.Collections.Generic;
using UnityEngine;

public class Captain : Piece
{
    public override bool IsRangedAttack(Vector2Int target) => true;

public override List<Vector2Int> GetMoves(Tile[,] board, int width, int height)
{
    List<Vector2Int> moves = new List<Vector2Int>();

    Vector2Int[] dirs =
    {
        Vector2Int.up, Vector2Int.down,
        Vector2Int.left, Vector2Int.right,
        new Vector2Int(1,1), new Vector2Int(-1,1),
        new Vector2Int(1,-1), new Vector2Int(-1,-1)
    };

    foreach (var d in dirs)
    {
        int nx = x + d.x;
        int ny = y + d.y;
        if (nx >= 0 && ny >= 0 && nx < width && ny < height)
        {
            Tile tile = board[nx, ny];
            if (tile != null && !tile.isWall && tile.currentPiece == null)
                moves.Add(new Vector2Int(nx, ny));
        }

        int tx = x;
        int ty = y;
        for (int i = 1; i <= 2; i++)
        {
            tx += d.x;
            ty += d.y;

            if (tx < 0 || ty < 0 || tx >= width || ty >= height)
                break;

            Tile tile = board[tx, ty];
            if (tile == null || tile.isWall) 
                break; 

            Piece target = tile.currentPiece;
            if (target != null && target.isWhite != isWhite)
            {
                moves.Add(new Vector2Int(tx, ty));
                break; 
            }
        }
    }

    return moves;
}

    public override void PlayAttackAnimation(Piece target, BoardManager board, Action onComplete)
    {
        if (target == null)
        {
            onComplete?.Invoke();
            return;
        }

        Vector2Int dir = new Vector2Int(target.x - x, target.y - y);

        animator?.PlayCaptainPierce(new Vector2Int(x, y), dir, board, () =>
        {
            onComplete?.Invoke();
        });
    }
}
