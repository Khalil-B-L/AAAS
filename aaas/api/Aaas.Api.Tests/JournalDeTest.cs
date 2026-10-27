using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Aaas.Api.Tests;

/// <summary>Fournisseur de journalisation qui garde les entrées de l'API en mémoire.</summary>
public sealed class JournalDeTest : ILoggerProvider
{
    public sealed record Entree(string Categorie, LogLevel Niveau, string Message, Exception? Exception);

    private readonly ConcurrentQueue<Entree> _entrees = new();

    public IReadOnlyCollection<Entree> Entrees => _entrees.ToArray();

    public ILogger CreateLogger(string categoryName) => new Logger(categoryName, _entrees);

    public void Dispose()
    {
    }

    private sealed class Logger(string categorie, ConcurrentQueue<Entree> entrees) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        /// <summary>Avertissements et erreurs de tout le monde ; <c>Information</c> seulement pour le code de l'API (ex. emails en mode Journal).</summary>
        public bool IsEnabled(LogLevel logLevel) =>
            logLevel >= LogLevel.Warning
            || (logLevel >= LogLevel.Information && categorie.StartsWith("Aaas.Api.", StringComparison.Ordinal));

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
                entrees.Enqueue(new Entree(categorie, logLevel, formatter(state, exception), exception));
        }
    }
}
