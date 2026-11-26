namespace SmartDesk.Domain.Tickets;

/// <summary>
/// Estado de la clasificación automática. El primer valor es el valor por defecto de CLR y coincide
/// con el default de la base de datos ("Classified"), para que EF Core no sustituya "Pending" por el default de BD.
/// </summary>
public enum ClassificationStatus { Classified, Pending, Failed }
