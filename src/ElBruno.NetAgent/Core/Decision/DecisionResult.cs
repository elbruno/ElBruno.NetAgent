namespace ElBruno.NetAgent.Core.Decision
{
    public enum DecisionAction { None, NotifyUser, RestartAdapter, SwitchAdapter, Throttle }
    public record DecisionResult(DecisionAction Action, string Reason);
}
