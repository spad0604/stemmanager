using Microsoft.Extensions.Logging;
namespace STEMLabManager.Desktop;

public sealed class FileLogProvider(string directory) : ILoggerProvider
{
    private readonly object gate=new();
    private readonly string logDirectory=directory;
    public ILogger CreateLogger(string categoryName)=>new FileLogger(this,categoryName);
    public void Dispose(){}
    private sealed class FileLogger(FileLogProvider owner,string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState:notnull=>null;
        public bool IsEnabled(LogLevel level)=>level>=LogLevel.Warning;
        public void Log<TState>(LogLevel level,EventId id,TState state,Exception? exception,Func<TState,Exception?,string> formatter)
        {
            if(!IsEnabled(level))return;
            lock(owner.gate)
            {
                try{Directory.CreateDirectory(owner.logDirectory);File.AppendAllText(Path.Combine(owner.logDirectory,$"app-{DateTime.UtcNow:yyyyMMdd}.log"),$"{DateTime.UtcNow:O} {level} {category} Event={id.Id} Exception={exception?.GetType().Name}\n");}
                catch(IOException){}catch(UnauthorizedAccessException){}
            }
        }
    }
}
