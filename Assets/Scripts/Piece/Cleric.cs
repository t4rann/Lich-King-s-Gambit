using System;
using System.Collections.Generic;
using UnityEngine;

public class Cleric : Piece
{
    [Header("Heal Settings")]
    public int healAmount = 25;

    public override bool IsRangedAttack(Vector2Int target) => true;

    public override List<Vector2Int> GetMoves(Tile[,] board, int width, int height)
    {
        List<Vector2Int> moves = new List<Vector2Int>();
        Vector2Int[] diagonals = { new Vector2Int(1,1), new Vector2Int(-1,1), new Vector2Int(1,-1), new Vector2Int(-1,-1) };

        foreach(var d in diagonals)
        {
            int nx = x + d.x;
            int ny = y + d.y;

            if(nx >=0 && ny >=0 && nx < width && ny < height)
            {
                Tile tile = board[nx, ny];
                if(tile != null && !tile.isWall && tile.currentPiece == null)
                    moves.Add(new Vector2Int(nx, ny));
            }

            int tx = x;
            int ty = y;
            while(true)
            {
                tx += d.x; ty += d.y;
                if(tx <0 || ty <0 || tx>=width || ty>=height) break;

                Tile tile = board[tx, ty];
                if(tile == null || tile.isWall) continue;

                if(tile.currentPiece != null)
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
    if (target == null) { onComplete?.Invoke(); return; }

    animator?.PlayClericCircle(target.transform.position, () =>
    {
        onComplete?.Invoke();
    });
}

}
