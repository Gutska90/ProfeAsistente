namespace AppEducativa.Api.Services.Export;

/// <summary>Ayudante para tablas; la implementación vive en <see cref="WordDocumentBuilder.AddTable"/>.</summary>
public static class WordTableBuilder
{
    public static WordDocumentBuilder Append(
        WordDocumentBuilder builder,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows)
        => builder.AddTable(headers, rows);
}
