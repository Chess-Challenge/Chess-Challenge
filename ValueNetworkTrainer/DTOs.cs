namespace ValueNetworkTrainer.DTOs;

public record Replay(
    ulong Id,
    string[] Moves,
    bool WhiteWinner
);
public record PostReplay(
    string[] Moves,
    bool WhiteWinner
);
