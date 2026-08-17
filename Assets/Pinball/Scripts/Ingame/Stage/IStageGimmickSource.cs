namespace Pinball.Ingame.Stage
{
    public interface IStageGimmickSource
    {
        int GimmickCount { get; }
        StageGimmickData GetGimmick(int index);
    }
}
