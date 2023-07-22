using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class MyBot : IChessBot
{
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 100000 };
    static Random random = new();

    Dictionary<ulong, float> transpositionTable;
    int level = 4;

    public Move Think(Board board, Timer timer)
    {
        transpositionTable = new Dictionary<ulong, float>();
        return MiniMax(board, level, float.NegativeInfinity, float.PositiveInfinity, true, timer).Item2;
    }

    (float, Move) MiniMax(Board board, int depth, float alpha, float beta, bool maximizer, Timer timer)
    {
        if (depth == 0 /*|| timer.MillisecondsElapsedThisTurn > 1_500*/)
        {
            float value = board.GetAllPieceLists().Sum(pl => pl.Sum(p => (p.IsWhite == board.IsWhiteToMove) ? pieceValues[(int)p.PieceType] : -pieceValues[(int)p.PieceType]));

            if (value == 0)
            {
                if (random.NextDouble() > 0.5)
                {
                    value = +100;
                }
                else
                {
                    value = -100;
                }
            }

            return (level % 2 == 0 ? value : -value, Move.NullMove);
        }

        var moves = board.GetLegalMoves();

        var chosen = Move.NullMove;
        if (maximizer)
        {
            var maxEval = float.NegativeInfinity;

            foreach (var move in moves)
            {
                board.MakeMove(move);

                if (board.IsInCheckmate())
                {
                    board.UndoMove(move);
                    return (float.PositiveInfinity, move);
                }

                if (board.IsDraw())
                {
                    board.UndoMove(move);
                    continue;
                }

                if (!transpositionTable.TryGetValue(board.ZobristKey, out float eval))
                {
                    eval = MiniMax(board, depth - 1, alpha, beta, false, timer).Item1;
                    transpositionTable.Add(board.ZobristKey, eval);
                }


                if (eval > maxEval)
                {
                    maxEval = eval;
                    chosen = move;
                }
                alpha = Math.Max(alpha, eval);

                board.UndoMove(move);

                if (beta <= alpha)
                {
                    break;
                }
            }

            if (chosen == Move.NullMove)
            {
                if (moves.Length == 0)
                {
                    return (maxEval, Move.NullMove);
                }

                chosen = moves[random.Next(moves.Length)];
            }
            return (maxEval, chosen);
        }
        else
        {
            var minEval = float.PositiveInfinity;

            foreach (var move in moves)
            {
                board.MakeMove(move);

                if (board.IsInCheckmate())
                {
                    board.UndoMove(move);
                    return (float.PositiveInfinity, move);
                }

                if (board.IsDraw())
                {
                    board.UndoMove(move);
                    continue;
                }

                var eval = MiniMax(board, depth - 1, alpha, beta, true, timer).Item1;
                if (eval < minEval)
                {
                    minEval = eval;
                    chosen = move;
                }
                beta = Math.Min(beta, eval);

                board.UndoMove(move);

                if (beta <= alpha)
                {
                    break;
                }
            }

            if (chosen == Move.NullMove)
            {
                if (moves.Length == 0)
                {
                    return (minEval, Move.NullMove);
                }

                chosen = moves[random.Next(moves.Length)];
            }
            return (minEval, chosen);
        }
    }
}