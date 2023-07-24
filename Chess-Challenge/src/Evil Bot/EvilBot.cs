using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessChallenge.Example;

public class EvilBot : IChessBot
{
    int[] pieceValues = { 0, 1, 3, 3, 5, 9, 200 };
    static Random random = new();

    Dictionary<ulong, float> transpositionTable;
    int level;

    public Move Think(Board board, Timer timer)
    {
        level = 4;

        transpositionTable = new Dictionary<ulong, float>();
        var (expectedScore, move) = MiniMax(board, level, float.NegativeInfinity, float.PositiveInfinity, true, board.IsWhiteToMove, timer);

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

    (float, Move) MiniMax(Board board, int depth, float alpha, float beta, bool maximizer, bool isWhite, Timer timer)
    {
        if (depth == 0 /*|| timer.MillisecondsElapsedThisTurn > 1_500*/)
        {
            if (board.IsInCheckmate())
            {
                return (float.PositiveInfinity, Move.NullMove);
            }

            float value = board.GetAllPieceLists().Sum(pl => pl.Sum(p => (p.IsWhite == isWhite/*board.IsWhiteToMove*/) ? pieceValues[(int)p.PieceType] : -pieceValues[(int)p.PieceType]));
            value += (0.1f * board.GetLegalMoves().Length);


            /*if (value == 0)
            {
                if (random.NextDouble() > 0.5)
                {
                    value = +0.5f;
                }
                else
                {
                    value = -0.5f;
                }
            }*/

            return (value, Move.NullMove);
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
                    transpositionTable.TryAdd(board.ZobristKey, float.PositiveInfinity);

                    return (float.PositiveInfinity, move);
                }

                if (board.IsDraw())
                {
                    board.UndoMove(move);
                    continue;
                }

                if (!transpositionTable.TryGetValue(board.ZobristKey, out float eval))
                {
                    eval = MiniMax(board, depth - 1, alpha, beta, false, isWhite, timer).Item1;

                    transpositionTable.TryAdd(board.ZobristKey, eval);
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
                    transpositionTable.TryAdd(board.ZobristKey, float.NegativeInfinity);

                    return (float.NegativeInfinity, move);
                }

                if (board.IsDraw())
                {
                    board.UndoMove(move);
                    continue;
                }


                if (!transpositionTable.TryGetValue(board.ZobristKey, out float eval))
                {
                    eval = MiniMax(board, depth - 1, alpha, beta, true, isWhite, timer).Item1;

                    transpositionTable.TryAdd(board.ZobristKey, eval);
                }

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