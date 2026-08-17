using ProfeAsistente.CurriculumImporter.Diff;
using ProfeAsistente.Shared.Dtos;
using ProfeAsistente.Shared.Enums;

namespace ProfeAsistente.CurriculumImporter.Tests;

public class FieldDiffHelperTests
{
    [Fact]
    public void DiffService_ClassifiesNewUnchangedAndPossibleRemoval()
    {
        var service = new CurriculumDiffService();
        var proposed = new CurriculumExtractionResult
        {
            LearningObjectives =
            [
                new() { Code = "OA 1", Description = "Descripción nueva del objetivo uno." },
                new() { Code = "OA 2", Description = "Otro objetivo completamente nuevo aquí." }
            ]
        };
        var current = new CurriculumExtractionResult
        {
            LearningObjectives =
            [
                new() { Code = "OA 1", Description = "Descripción nueva del objetivo uno." },
                new() { Code = "OA 9", Description = "Objetivo que podría desaparecer del programa." }
            ]
        };

        var result = service.Compare(proposed, current);
        Assert.Contains(result.Items, i => i.Tipo == TipoCambioCurricular.SinCambios && i.Clave == "OA 1");
        Assert.Contains(result.Items, i => i.Tipo == TipoCambioCurricular.Nuevo && i.Clave == "OA 2");
        Assert.Contains(result.Items, i => i.Tipo == TipoCambioCurricular.PosiblementeEliminado && i.Clave == "OA 9");
    }

    [Fact]
    public void DiffService_DetectsModifiedDescription()
    {
        var service = new CurriculumDiffService();
        var proposed = new CurriculumExtractionResult
        {
            LearningObjectives =
            [
                new() { Code = "OA 1", Description = "Resolver y explicar ejercicios de fracciones." }
            ]
        };
        var current = new CurriculumExtractionResult
        {
            LearningObjectives =
            [
                new() { Code = "OA 1", Description = "Resolver ejercicios de fracciones." }
            ]
        };

        var result = service.Compare(proposed, current);
        Assert.Contains(result.Items, i => i.Tipo == TipoCambioCurricular.Modificado && i.Clave == "OA 1");
    }
}
