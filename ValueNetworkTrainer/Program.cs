using ChessChallenge.Chess;
using FireNeurons.NET;
using FireNeurons.NET.Objects;
using FireNeurons.NET.Optimisation;
using FireNeurons.NET.Optimisation.Optimisers;
using MySqlConnector;

namespace ValueNetworkTrainer;

class Program
{
    public static MySqlConnection Connection { get; private set; }

    static readonly StockfishAPI stockfish = new();
    static NeuralNetwork network;

    private static void Main(string[] args)
    {
        DataGenerator.Run();
        return;

        ConnectToDb();
        InitNetwork();

        ulong currentGameId = 1;
        while (true)
        {
            var dbResult = ReadFromDb($"SELECT * FROM moves WHERE GameId = {currentGameId++}")
                .GetAwaiter()
                .GetResult()
                .OrderBy(x => x["GameId"])
                .ThenBy(x => x["Index"]);
            var moves = dbResult.Select(x => x["UCI"].ToString());

            stockfish.SendLine("ucinewgame");

            //Console.WriteLine("---------- New Game ----------");
            var evals = new Dictionary<string, double>(); // FEN, Eval

            var playedMoves = new List<string>();
            foreach (var move in moves)
            {
                playedMoves.Add(move!);
                stockfish.SendLine("position startpos moves " + string.Join(" ", playedMoves));
                var fen = stockfish.GetFenPosition();

                if (!fen.Contains('w'))
                {
                    continue;
                }

                evals.Add(fen, stockfish.GetEvaluation() * 0.01);
            }

            double lossBefore_avg = 0;
            double lossAfter_avg = 0;
            int counter = 0;

            foreach (var (fen, eval) in evals)
            {
                var lossInfo = Train(fen, eval);

                if (lossInfo.HasValue)
                {
                    lossBefore_avg += lossInfo.Value.Item1;
                    lossAfter_avg += lossInfo.Value.Item2;
                    counter++;
                }
            }

            Console.WriteLine($"Avg: {(lossBefore_avg / counter):.00} -> {(lossAfter_avg / counter):.00}");
        }
    }

    public static void ConnectToDb()
    {
        var connectionStringBuilder = new MySqlConnectionStringBuilder
        {
            { "server", "localhost" },
            { "port", 3306 },
            { "database", "ChessChallenge" },
            { "user", "root" },
            { "password", "La2003Sh" }
        };

        Connection = new MySqlConnection(connectionStringBuilder.ConnectionString);
        Connection.Open();
        if (Connection.State != System.Data.ConnectionState.Open)
        {
            throw new Exception();
        }
    }

    static async Task<List<Dictionary<string, object>>> ReadFromDb(string query)
    {
        using var command = new MySqlCommand(query, Connection);

        var entries = new List<Dictionary<string, object>>();

        try
        {
            using var dataReader = await command.ExecuteReaderAsync();
            var columns = await dataReader.GetColumnSchemaAsync();

            while (await dataReader.ReadAsync())
            {
                var values = new object[dataReader.FieldCount];
                dataReader.GetValues(values);

                var entry = new Dictionary<string, object>();
                for (int i = 0; i < values.Length; i++)
                {
                    entry.Add(columns[i].ColumnName, values[i]);
                }

                entries.Add(entry);
            }
        }
        catch (Exception exp)
        {
            throw exp;
        }

        return entries;
    }

    static int outputLayer = 8;
    static void InitNetwork()
    {
        network = new(new Adam((n, target) => -2 * (n.Value - target)));

        network.Add(8, 0, Activation.Identity); // PosX
        network.Add(8, 1, Activation.Identity); // PosY
        network.Add(6, 2, Activation.Identity); // PieceType
        network.Add(2, 3, Activation.Identity); // Color

        network.Add(20, 4, Activation.LeakyRelu, 0, 1, 2, 3);
        network.Add(10, 5, Activation.LeakyRelu, 4);
        network.Add(10, 6, Activation.LeakyRelu, 5);
        network.Add(10, 7, Activation.LeakyRelu, 6);

        network.Add(1, outputLayer, Activation.Identity, 7);

        network.Randomize();
    }

    static (double, double)? Train(string fen, double stockfishEval)
    {
        var info = FenUtility.PositionFromFen(fen);

        var allInputData = GetInputDataFromInfo(info);

        double lossBefore_avg = 0;
        double lossAfter_avg = 0;
        foreach (var inputData in allInputData)
        {
            var trainingData = new TrainingData(inputData, new Data().Add(outputLayer, stockfishEval));

            var evaluateBefore = network.Evaluate(trainingData.InputData, outputLayer)[outputLayer][0];
            var lossBefore = Math.Pow((stockfishEval - evaluateBefore), 2);
            lossBefore_avg += lossBefore / allInputData.Count;

            network.Train(trainingData);

            var evaluateAfter = network.Evaluate(trainingData.InputData, outputLayer)[outputLayer][0];
            var lossAfter = Math.Pow((stockfishEval - evaluateAfter), 2);
            lossAfter_avg += lossAfter / allInputData.Count;
        }

        //Console.WriteLine($"{lossBefore_avg:.00} -> {lossAfter_avg:.00}");
        return (lossBefore_avg, lossAfter_avg);
    }

    static List<Data> GetInputDataFromInfo(FenUtility.PositionInfo info)
    {
        var allData = new List<Data>();

        if (!info.whiteToMove)
        {
            //fen = FenUtility.FlipFen(fen);
            //info = FenUtility.PositionFromFen(fen);
        }

        for (int index = 0; index < info.squares.Count; index++)
        {
            if (info.squares[index] == 0)
            {
                continue;
            }

            int pieceColor = PieceHelper.PieceColour(info.squares[index]);

            double[] input_posX = new double[8];
            double[] input_posY = new double[8];
            double[] input_type = new double[6];
            double[] input_color = new double[2];

            input_posX[index % 8] = 1;
            input_posY[index / 8] = 1;
            input_type[(info.squares[index] | pieceColor) - pieceColor - 1] = 1;
            input_color[pieceColor / 8] = 1;

            var data = new Data();
            data.Add(0, input_posX);
            data.Add(1, input_posY);
            data.Add(2, input_type);
            data.Add(3, input_color);
            allData.Add(data);
        }

        return allData;
    }
}