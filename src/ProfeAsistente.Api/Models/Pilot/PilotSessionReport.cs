namespace ProfeAsistente.Api.Models.Pilot;

/// <summary>Autoreporte del docente al cerrar una sesión de uso (piloto).</summary>
public class PilotSessionReport
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? InstitutionId { get; set; }
    public Guid? ClassId { get; set; }
    /// <summary>Minutos que el docente estima haber ahorrado en esta sesión.</summary>
    public int MinutesSavedEstimate { get; set; }
    public bool? WouldUseAgain { get; set; }
    public bool? MaterialsUsedInClass { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
