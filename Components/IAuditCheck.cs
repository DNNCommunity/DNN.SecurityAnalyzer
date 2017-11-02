namespace DNN.Modules.SecurityAnalyzer.Components
{
    public interface IAuditCheck
    {
        string Id { get; }

        bool LazyLoad { get; }

        CheckResult Execute();
    }
}