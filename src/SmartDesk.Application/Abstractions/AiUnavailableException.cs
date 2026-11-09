namespace SmartDesk.Application.Abstractions;

/// <summary>La IA (embeddings o redacción) no está disponible ahora mismo. Es un fallo temporal, no "no hay resultados".</summary>
public sealed class AiUnavailableException(string message, Exception? inner = null) : Exception(message, inner);
