using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class MyBot : IChessBot
{
    int[] pieceValues = { 0, 1, 3, 3, 5, 9, 200 };
    static Random random = new();

    Dictionary<ulong, float> transpositionTable;
    int level;

    public Move Think(Board board, Timer timer)
    {
        level = 4;

        transpositionTable = new();
        var (expectedScore, move) = NegaMax(board, level, float.NegativeInfinity, float.PositiveInfinity, timer);

        if (move.IsNull)
        {
            Console.WriteLine("NullMove");
        }
        board.MakeMove(move);
        float value = -board.GetAllPieceLists().Sum(pl => pl.Sum(p => (p.IsWhite == board.IsWhiteToMove) ? pieceValues[(int)p.PieceType] : -pieceValues[(int)p.PieceType]));
        board.UndoMove(move);

        Console.WriteLine(
                $"{(board.IsWhiteToMove ? "White" : "Black")} {move} " +
                $"-> Value: {(float.IsInfinity(value) ? $"{(float.IsPositiveInfinity(value) ? "+" : "-")}Infinity" : $"{value:.00}")} " +
                $"(Expected: {(float.IsInfinity(expectedScore) ? $"{(float.IsPositiveInfinity(expectedScore) ? "+" : "-")}Infinity" : $"{expectedScore:.00}")})");

        return move;
    }

    (float, Move) NegaMax(Board board, int depth, float alpha, float beta, Timer timer)
    {
        if (depth == 0 /*|| timer.MillisecondsElapsedThisTurn > 1_500*/)
        {
            float value = board.GetAllPieceLists().Sum(pl => pl.Sum(p => (p.IsWhite == board.IsWhiteToMove) ? pieceValues[(int)p.PieceType] : -pieceValues[(int)p.PieceType]));
            value += (0.1f * board.GetLegalMoves().Length);


            if (value == 0)
            {
                if (random.NextDouble() > 0.5)
                {
                    value = +0.5f;
                }
                else
                {
                    value = -0.5f;
                }
            }

            return (value, Move.NullMove);
        }

        var moves = board.GetLegalMoves();
        Move? chosen = null;

        var maxEval = float.NegativeInfinity;

        foreach (var move in moves)
        {
            board.MakeMove(move);

            if (board.IsInCheckmate())
            {
                board.UndoMove(move);
                return (9999, move);
            }

            if (board.IsDraw())
            {
                board.UndoMove(move);
                continue;
            }

            if (!transpositionTable.TryGetValue(board.ZobristKey, out float eval))
            {
                eval = -NegaMax(board, depth - 1, -beta, -alpha, timer).Item1;

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

        return (maxEval, chosen ?? moves[0]);
    }
}