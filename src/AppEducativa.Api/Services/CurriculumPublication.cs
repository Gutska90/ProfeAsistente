using AppEducativa.Shared.Enums;
using System.Linq.Expressions;

namespace AppEducativa.Api.Services;

public static class CurriculumPublication
{
    public static bool IsPublished(EstadoRevision estado, bool vigente) =>
        vigente && (estado == EstadoRevision.Aprobado || estado == EstadoRevision.AprobadoParaPruebas);

    /// <summary>Filtro traducible por EF Core.</summary>
    public static Expression<Func<EstadoRevision, bool, bool>> IsPublishedExpr { get; } =
        (estado, vigente) => vigente && (estado == EstadoRevision.Aprobado || estado == EstadoRevision.AprobadoParaPruebas);
}
