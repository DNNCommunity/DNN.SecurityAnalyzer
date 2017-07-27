namespace DNN.Modules.SecurityAnalyzer.Components
{
    public interface IAuditCheck
    {
        string Id { get; }
        CheckResult Execute();
    }
}