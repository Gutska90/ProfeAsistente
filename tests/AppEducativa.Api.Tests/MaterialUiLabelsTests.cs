using AppEducativa.Shared.Enums;
using AppEducativa.Shared.Ui;

namespace AppEducativa.Api.Tests;

public class MaterialUiLabelsTests
{
    [Theory]
    [InlineData(EducationalDocumentType.LearningGuide, "Guía")]
    [InlineData(EducationalDocumentType.Exercises, "Actividad")]
    [InlineData(EducationalDocumentType.Assessment, "Prueba")]
    public void Type_UsesSpanishTeacherTerms(EducationalDocumentType type, string expected)
        => Assert.Equal(expected, MaterialUiLabels.Type(type));

    [Theory]
    [InlineData("Draft", "Borrador")]
    [InlineData("UnderReview", "En revisión")]
    [InlineData("Final", "Final")]
    public void Status_UsesSpanish(string raw, string expected)
        => Assert.Equal(expected, MaterialUiLabels.Status(raw));

    [Theory]
    [InlineData("Guía", EducationalDocumentType.LearningGuide)]
    [InlineData("actividad", EducationalDocumentType.Exercises)]
    [InlineData("Prueba", EducationalDocumentType.Assessment)]
    public void ParseTypeLabel_AcceptsSpanish(string label, EducationalDocumentType expected)
        => Assert.Equal(expected, MaterialUiLabels.ParseTypeLabel(label));
}
