using ChessChallenge.Chess;
using MySqlConnector;
using System.Diagnostics;
using System.Text;

namespace ValueNetworkTrainer;
using DTOs;
using System.Text.Json;

public static class DataGenerator
{
    public static void RunV2()
    {
        Program.ConnectToDb();

        var allReplayChunks = new List<PostReplay[]>();
        string root = @"C:\Users\Zehnder\Downloads\KingBase2019-pgn";
        foreach (var file in Directory.GetFiles(root))
        {
            var sw = Stopwatch.StartNew();
            var currentReplayCunks = LoadFromPGNs(file);
            allReplayChunks.AddRange(currentReplayCunks);
            sw.Stop();
            Console.WriteLine($"{Path.GetFileName(file)} -> {sw.ElapsedMilliseconds}ms");
        }

        var sum = 0;
        foreach (var replayChunk in allReplayChunks)
        {
            sum += replayChunk.Length;
            var sw = Stopwatch.StartNew();

            var transaction = Program.Connection.BeginTransaction();

            var values = new StringBuilder();
            for (int i = 0; i < replayChunk.Length; i++)
            {
                if (i != 0)
                {
                    values.Append(",");
                }
                values.Append($"('{JsonSerializer.Serialize(replayChunk[i].Moves)}')");
            }
            ExecuteQuery($"INSERT INTO games_v2 (Moves) VALUES {values}", transaction);

            transaction.Commit();

            sw.Stop();
            Console.WriteLine($"{sum}/{(allReplayChunks.Count * replayChunk.Length)} -> {sw.ElapsedMilliseconds}ms");
        }
        Console.WriteLine("Finished");
    }

    public static void Run()
    {
        Program.ConnectToDb();

        var allReplayChunks = new List<PostReplay[]>();
        string root = @"C:\Users\Zehnder\Downloads\KingBase2019-pgn";
        foreach (var file in Directory.GetFiles(root))
        {
            var sw = Stopwatch.StartNew();
            var currentReplayCunks = LoadFromPGNs(file);
            allReplayChunks.AddRange(currentReplayCunks);
            sw.Stop();
            Console.WriteLine($"{Path.GetFileName(file)} -> {sw.ElapsedMilliseconds}ms");
        }

        var sum = 0;
        foreach (var replayChunk in allReplayChunks)
        {
            sum += replayChunk.Length;
            var sw = Stopwatch.StartNew();

            var transaction = Program.Connection.BeginTransaction();

            var values = new StringBuilder();
            for (int i = 0; i < replayChunk.Length; i++)
            {
                if (i != 0)
                {
                    values.Append(",");
                }
                values.Append($"()");
            }
            var gameIndex = ExecuteQuery($"INSERT INTO games VALUES {values}; SELECT LAST_INSERT_ID();", transaction)!.Value;

            values.Clear();
            for (int i = 0; i < replayChunk.Length; i++)
            {
                var replay = replayChunk[i];

                for (int k = 0; k < replay.Moves.Length; k++)
                {
                    if (i + k != 0)
                    {
                        values.Append(",");
                    }

                    values.Append("(");
                    values.Append($"'{(gameIndex + Convert.ToUInt64(i))}',");
                    values.Append($"'{(k + 1)}',");
                    values.Append($"'{replay.Moves[k]}'");
                    values.Append(")");
                }
            }
            ExecuteQuery($"INSERT INTO moves (moves.GameId, moves.Index, moves.UCI) VALUES {values};", transaction);

            transaction.Commit();

            sw.Stop();
            Console.WriteLine($"{sum}/{(allReplayChunks.Count * replayChunk.Length)} -> {sw.ElapsedMilliseconds}ms");
        }
        Console.WriteLine("Finished");
    }

    public static List<PostReplay[]> LoadFromPGNs(string file)
    {
        var allGames = File.ReadAllText(file)
            .Replace("\r\n", "\n")
            .Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
            .Chunk(2)
            .Select(x => $"{x[0]}\n{x[1].Replace("\n", " ").Replace(".", ". ")}");

        var replays = new List<PostReplay>();

        foreach (var game in allGames)
        {
            var parsedGame = TimHanewich.Chess.PGN.PgnFile.ParsePgn(game);

            var result = parsedGame.Result;
            if (!(result == "1-0" || result == "0-1"))
            {
                continue;
            }

            var board = new Board();
            board.LoadStartPosition();
            var moves = new List<string>();

            foreach (string san in parsedGame.Moves)
            {
                Move move = Move.NullMove;
                try
                {
                    move = MoveUtility.GetMoveFromSAN(board, san);
                }
                catch
                {
                    moves = null;
                    break;
                }

                board.MakeMove(move);
                moves.Add(MoveUtility.GetMoveNameUCI(move));
            }

            if (moves == null)
            {
                continue;
            }

            var replay = new PostReplay(moves.ToArray(), (result == "1-0"));
            replays.Add(replay);
        }

        return replays.Chunk(5000).ToList();
    }

    private static ulong? ExecuteQuery(string query, MySqlTransaction transaction)
    {
        using var command = new MySqlCommand(query, Program.Connection, transaction);

        try
        {
            var result = command.ExecuteScalarAsync().GetAwaiter().GetResult();
            return (ulong?)result;
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            throw ex;
        }
    }
}
