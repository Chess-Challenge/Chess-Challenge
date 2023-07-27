using System.Diagnostics;

namespace ValueNetworkTrainer;

public class StockfishAPI
{
    private readonly Process uciProcess;

    public StockfishAPI()
    {
        uciProcess = Process.Start(new ProcessStartInfo()
        {
            FileName = Environment.CurrentDirectory + "\\stockfish-windows-x86-64-avx2.exe",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true
        })!;

        uciProcess.Start();
        uciProcess.BeginErrorReadLine();

        this.SendLine("uci");
        this.SendLine("isready");
    }

    public void SendLine(string command) => this.SendLineAsync(command).GetAwaiter().GetResult();
    public async Task SendLineAsync(string command)
    {
        await uciProcess.StandardInput.WriteLineAsync(command);
        await uciProcess.StandardInput.FlushAsync();
    }

    public string GetFenPosition() => this.GetFenPositionAsync().GetAwaiter().GetResult();
    public async Task<string> GetFenPositionAsync()
    {
        this.SendLine("d");

        while (true)
        {
            var readLine = await uciProcess.StandardOutput.ReadLineAsync();

            if (readLine.StartsWith("Fen: "))
            {
                return readLine.Substring(5);
            }
        }
    }

    public void SetFenPosition(IEnumerable<string> playedMoves) => this.SetFenPositionAsync(playedMoves).GetAwaiter().GetResult();
    public async Task SetFenPositionAsync(IEnumerable<string> playedMoves)
    {
        await this.SendLineAsync("position startpos moves " + string.Join(" ", playedMoves));
    }

    public int GetEvaluation() => this.GetEvaluationAsync().GetAwaiter().GetResult();
    public async Task<int> GetEvaluationAsync()
    {
        string fenPosition = await this.GetFenPositionAsync();
        int color = (fenPosition.Contains("w") ? 1 : (-1));

        await this.SendLineAsync($"go depth 1");

        while (true)
        {
            var readLine = await uciProcess.StandardOutput.ReadLineAsync();

            if (readLine.StartsWith("info"))
            {
                var info = readLine.Split(' ');

                if (info.Contains("score"))
                {
                    return int.Parse(info[info.ToList().IndexOf("score") + 2]) * color;
                }
            }
        }
    }
}
