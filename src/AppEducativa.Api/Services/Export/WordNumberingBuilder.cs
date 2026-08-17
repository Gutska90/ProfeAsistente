namespace AppEducativa.Api.Services.Export;

/// <summary>Ayudante de listas numeradas.</summary>
public static class WordNumberingBuilder
{
    public static WordDocumentBuilder Append(
        WordDocumentBuilder builder,
        IEnumerable<string> items)
        => builder.AddNumberedList(items);
}
