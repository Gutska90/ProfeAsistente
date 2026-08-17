namespace ProfeAsistente.Api.Services.Export;

/// <summary>Ayudante de encabezado/pie.</summary>
public static class WordHeaderFooterBuilder
{
    public static WordDocumentBuilder Apply(
        WordDocumentBuilder builder,
        string? header,
        string footer,
        bool pageNumbers)
    {
        if (!string.IsNullOrWhiteSpace(header))
            builder.AddHeader(header);
        builder.AddFooter(footer, pageNumbers);
        return builder;
    }
}
