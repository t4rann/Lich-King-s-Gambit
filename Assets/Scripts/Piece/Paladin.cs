using System.Collections.Generic;
using UnityEngine;

public class Paladin : Piece
{
    public override List<Vector2Int> GetMoves(Tile[,] board, int width, int height)
    {
        List<Vector2Int> moves = new List<Vector2Int>();

        Vector2Int[] allDirs =
        {
            Vector2Int.up,        Vector2Int.down,
            Vector2Int.left,      Vector2Int.right,
            new Vector2Int(1, 1),   new Vector2Int(-1, 1),
            new Vector2Int(1, -1),  new Vector2Int(-1, -1)
        };

        foreach (var dir in allDirs)
        {
            int nx = x + dir.x;
            int ny = y + dir.y;

            if (nx < 0 || ny < 0 || nx >= width || ny >= height)
                continue;

            Tile tile = board[nx, ny];
            if (tile == null || tile.isWall) continue;

            Piece target = tile.currentPiece;
            
            bool isStraightMove = dir.x == 0 || dir.y == 0; 
            
            if (target == null)
            {
                if (isStraightMove)
                {
                    moves.Add(new Vector2Int(nx, ny));
                }
            }
            else if (target.isWhite != isWhite)
            {
                moves.Add(new Vector2Int(nx, ny));
            }
        }

        return moves;
    }

    public override void PlayAttackAnimation(Piece target, BoardManager board, System.Action onComplete)
    {
    }
}
