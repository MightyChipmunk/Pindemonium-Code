namespace Pinball.Ingame.Reward
{
    public record StageClearReward(
        int ClearMoney,
        string ClearMoneySourceNames,
        int RemainingBallCount,
        int RemainingBallMoney,
        string RemainingBallSourceNames,
        int InterestMoney,
        int InterestMaxMoney,
        string InterestSourceNames);
}
