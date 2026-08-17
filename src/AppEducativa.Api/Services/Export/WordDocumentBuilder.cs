using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using OpenXmlDoc = DocumentFormat.OpenXml.Wordprocessing.Document;

namespace AppEducativa.Api.Services.Export;

public sealed class WordDocumentBuilder : IDisposable
{
    private readonly MemoryStream _stream = new();
    private readonly WordprocessingDocument _doc;
    private readonly Body _body;
    private readonly WordTemplateSettings _settings;
    private readonly string _font;
    private bool _disposed;

    public WordDocumentBuilder(WordTemplateSettings? settings = null)
    {
        _settings = settings ?? new WordTemplateSettings();
        _settings.Validate();
        _font = _settings.FontFamily;
        _doc = WordprocessingDocument.Create(_stream, WordprocessingDocumentType.Document, true);
        var main = _doc.AddMainDocumentPart();
        main.Document = new OpenXmlDoc(new Body());
        _body = main.Document.Body!;
        ApplyPageSetup(main);
        EnsureStyles(main);
    }

    public WordDocumentBuilder AddTitle(string title)
    {
        _body.AppendChild(CreateParagraph(title, _settings.TitleFontSize, bold: true, justify: JustificationValues.Center, styleId: "Title"));
        return this;
    }

    public WordDocumentBuilder AddSubtitle(string subtitle)
    {
        _body.AppendChild(CreateParagraph(subtitle, _settings.Heading2FontSize, italic: true, justify: JustificationValues.Center, styleId: "Subtitle"));
        return this;
    }

    public WordDocumentBuilder AddHeading(string text, int level)
    {
        var size = level switch
        {
            1 => _settings.Heading1FontSize,
            2 => _settings.Heading2FontSize,
            _ => _settings.Heading3FontSize
        };
        var style = level switch { 1 => "Heading1", 2 => "Heading2", _ => "Heading3" };
        var p = CreateParagraph(text, size, bold: true, styleId: style);
        p.ParagraphProperties ??= new ParagraphProperties();
        p.ParagraphProperties.AppendChild(new KeepNext());
        p.ParagraphProperties.AppendChild(new KeepLines());
        _body.AppendChild(p);
        return this;
    }

    public WordDocumentBuilder AddParagraph(string text, bool bold = false, bool italic = false)
    {
        if (string.IsNullOrWhiteSpace(text)) return this;
        _body.AppendChild(CreateParagraph(text, _settings.BodyFontSize, bold, italic));
        return this;
    }

    public WordDocumentBuilder AddInstruction(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return this;
        _body.AppendChild(CreateParagraph(text, _settings.BodyFontSize, italic: true, styleId: "Instructions"));
        return this;
    }

    public WordDocumentBuilder AddTeacherNote(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return this;
        _body.AppendChild(CreateParagraph("Nota docente: " + text, _settings.BodyFontSize, italic: true, styleId: "TeacherNote"));
        return this;
    }

    public WordDocumentBuilder AddAnswer(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return this;
        _body.AppendChild(CreateParagraph(text, _settings.BodyFontSize, bold: true, styleId: "Answer"));
        return this;
    }

    public WordDocumentBuilder AddWarning(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return this;
        _body.AppendChild(CreateParagraph(text, _settings.BodyFontSize, bold: true, styleId: "Warning"));
        return this;
    }

    public WordDocumentBuilder AddCurriculumReference(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return this;
        _body.AppendChild(CreateParagraph(text, 9, italic: true, styleId: "CurriculumReference"));
        return this;
    }

    public WordDocumentBuilder AddBulletList(IEnumerable<string> items)
    {
        foreach (var item in items.Where(i => !string.IsNullOrWhiteSpace(i)))
        {
            var p = CreateParagraph("• " + item.Trim(), _settings.BodyFontSize);
            _body.AppendChild(p);
        }
        return this;
    }

    public WordDocumentBuilder AddNumberedList(IEnumerable<string> items)
    {
        var n = 1;
        foreach (var item in items.Where(i => !string.IsNullOrWhiteSpace(i)))
        {
            _body.AppendChild(CreateParagraph($"{n}. {item.Trim()}", _settings.BodyFontSize));
            n++;
        }
        return this;
    }

    public WordDocumentBuilder AddTable(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        if (headers.Count == 0) return this;
        var table = new Table();
        table.AppendChild(new TableProperties(
            new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4 },
                new BottomBorder { Val = BorderValues.Single, Size = 4 },
                new LeftBorder { Val = BorderValues.Single, Size = 4 },
                new RightBorder { Val = BorderValues.Single, Size = 4 },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 }),
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct }));

        table.AppendChild(CreateTableRow(headers, header: true));
        foreach (var row in rows)
            table.AppendChild(CreateTableRow(row, header: false));

        var props = table.GetFirstChild<TableProperties>() ?? new TableProperties();
        // CantSplit on rows
        _body.AppendChild(table);
        _body.AppendChild(CreateParagraph(string.Empty, _settings.BodyFontSize));
        return this;
    }

    public WordDocumentBuilder AddPageBreak()
    {
        _body.AppendChild(new Paragraph(new Run(new Break { Type = BreakValues.Page })));
        return this;
    }

    public WordDocumentBuilder AddBlankLines(int count)
    {
        for (var i = 0; i < Math.Max(0, count); i++)
            _body.AppendChild(CreateParagraph(string.Empty, _settings.BodyFontSize));
        return this;
    }

    public WordDocumentBuilder AddAnswerSpace(int lineCount)
    {
        for (var i = 0; i < Math.Max(1, lineCount); i++)
            _body.AppendChild(CreateParagraph(new string('_', 72), _settings.BodyFontSize, styleId: "StudentResponseLine"));
        return this;
    }

    public WordDocumentBuilder AddCheckbox(string label, bool selected = false)
    {
        var mark = selected ? "☑" : "☐";
        _body.AppendChild(CreateParagraph($"{mark} {label}", _settings.BodyFontSize));
        return this;
    }

    public WordDocumentBuilder AddHeader(string text)
    {
        var main = _doc.MainDocumentPart!;
        if (main.HeaderParts.Any()) return this;
        var headerPart = main.AddNewPart<HeaderPart>();
        var headerId = main.GetIdOfPart(headerPart);
        headerPart.Header = new Header(CreateParagraph(text, 9, italic: true));
        EnsureSectionProperties().AppendChild(new HeaderReference { Type = HeaderFooterValues.Default, Id = headerId });
        return this;
    }

    public WordDocumentBuilder AddFooter(string text, bool includePageNumber)
    {
        var main = _doc.MainDocumentPart!;
        if (main.FooterParts.Any()) return this;
        var footerPart = main.AddNewPart<FooterPart>();
        var footerId = main.GetIdOfPart(footerPart);
        var para = new Paragraph();
        para.AppendChild(new Run(CreateRunProperties(9, italic: true), new Text(text)));
        if (includePageNumber)
        {
            para.AppendChild(new Run(CreateRunProperties(9), new Text("  ·  Página ")));
            para.AppendChild(new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }));
            para.AppendChild(new Run(new FieldCode(" PAGE ")));
            para.AppendChild(new Run(new FieldChar { FieldCharType = FieldCharValues.Separate }));
            para.AppendChild(new Run(new Text("1")));
            para.AppendChild(new Run(new FieldChar { FieldCharType = FieldCharValues.End }));
            para.AppendChild(new Run(CreateRunProperties(9), new Text(" de ")));
            para.AppendChild(new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }));
            para.AppendChild(new Run(new FieldCode(" NUMPAGES ")));
            para.AppendChild(new Run(new FieldChar { FieldCharType = FieldCharValues.Separate }));
            para.AppendChild(new Run(new Text("1")));
            para.AppendChild(new Run(new FieldChar { FieldCharType = FieldCharValues.End }));
        }

        footerPart.Footer = new Footer(para);
        EnsureSectionProperties().AppendChild(new FooterReference { Type = HeaderFooterValues.Default, Id = footerId });
        return this;
    }

    public async Task SaveAsync(string filePath, CancellationToken cancellationToken)
    {
        EnsureSectionProperties();
        _doc.MainDocumentPart!.Document.Save();
        _doc.Dispose();
        _disposed = true;
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        await using var fs = File.Create(filePath);
        _stream.Position = 0;
        await _stream.CopyToAsync(fs, cancellationToken);
    }

    public byte[] ToArray()
    {
        EnsureSectionProperties();
        _doc.MainDocumentPart!.Document.Save();
        _doc.Dispose();
        _disposed = true;
        return _stream.ToArray();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _doc.Dispose();
        _stream.Dispose();
        _disposed = true;
    }

    private void ApplyPageSetup(MainDocumentPart main)
    {
        // A4 in twips: 11906 x 16838
        var sect = EnsureSectionProperties();
        sect.AppendChild(new PageSize { Width = 11906, Height = 16838 });
        sect.AppendChild(new PageMargin
        {
            Top = (int)(_settings.MarginTopCm * 567),
            Bottom = (int)(_settings.MarginBottomCm * 567),
            Left = (uint)(_settings.MarginLeftCm * 567),
            Right = (uint)(_settings.MarginRightCm * 567)
        });
    }

    private SectionProperties EnsureSectionProperties()
    {
        var sect = _body.Elements<SectionProperties>().LastOrDefault();
        if (sect is not null) return sect;
        sect = new SectionProperties();
        _body.AppendChild(sect);
        return sect;
    }

    private static void EnsureStyles(MainDocumentPart main)
    {
        var stylesPart = main.StyleDefinitionsPart ?? main.AddNewPart<StyleDefinitionsPart>();
        stylesPart.Styles ??= new Styles();
        EnsureStyle(stylesPart.Styles, "Normal", "Normal", StyleValues.Paragraph);
        EnsureStyle(stylesPart.Styles, "Title", "Title", StyleValues.Paragraph);
        EnsureStyle(stylesPart.Styles, "Subtitle", "Subtitle", StyleValues.Paragraph);
        EnsureStyle(stylesPart.Styles, "Heading1", "Heading 1", StyleValues.Paragraph);
        EnsureStyle(stylesPart.Styles, "Heading2", "Heading 2", StyleValues.Paragraph);
        EnsureStyle(stylesPart.Styles, "Heading3", "Heading 3", StyleValues.Paragraph);
        EnsureStyle(stylesPart.Styles, "Instructions", "Instructions", StyleValues.Paragraph);
        EnsureStyle(stylesPart.Styles, "CurriculumReference", "Curriculum Reference", StyleValues.Paragraph);
        EnsureStyle(stylesPart.Styles, "TeacherNote", "Teacher Note", StyleValues.Paragraph);
        EnsureStyle(stylesPart.Styles, "Answer", "Answer", StyleValues.Paragraph);
        EnsureStyle(stylesPart.Styles, "Warning", "Warning", StyleValues.Paragraph);
        EnsureStyle(stylesPart.Styles, "StudentResponseLine", "Student Response Line", StyleValues.Paragraph);
        EnsureStyle(stylesPart.Styles, "TableHeader", "Table Header", StyleValues.Paragraph);
        EnsureStyle(stylesPart.Styles, "TableCell", "Table Cell", StyleValues.Paragraph);
    }

    private static void EnsureStyle(Styles styles, string id, string name, StyleValues type)
    {
        if (styles.Elements<Style>().Any(s => s.StyleId == id)) return;
        styles.AppendChild(new Style
        {
            Type = type,
            StyleId = id,
            StyleName = new StyleName { Val = name },
            PrimaryStyle = new PrimaryStyle()
        });
    }

    private Paragraph CreateParagraph(
        string text,
        int fontSizePt,
        bool bold = false,
        bool italic = false,
        JustificationValues? justify = null,
        string? styleId = null)
    {
        var p = new Paragraph();
        var props = new ParagraphProperties();
        if (justify is not null)
            props.AppendChild(new Justification { Val = justify });
        if (!string.IsNullOrWhiteSpace(styleId))
            props.AppendChild(new ParagraphStyleId { Val = styleId });
        p.AppendChild(props);
        p.AppendChild(new Run(CreateRunProperties(fontSizePt, bold, italic), new Text(text ?? string.Empty)));
        return p;
    }

    private RunProperties CreateRunProperties(int fontSizePt, bool bold = false, bool italic = false)
    {
        var rp = new RunProperties(
            new RunFonts { Ascii = _font, HighAnsi = _font, ComplexScript = _font },
            new FontSize { Val = (fontSizePt * 2).ToString() });
        if (bold) rp.AppendChild(new Bold());
        if (italic) rp.AppendChild(new Italic());
        return rp;
    }

    private TableRow CreateTableRow(IReadOnlyList<string> cells, bool header)
    {
        var row = new TableRow(new TableRowProperties(new CantSplit()));
        foreach (var cell in cells)
        {
            var tc = new TableCell(
                new TableCellProperties(new TableCellWidth { Type = TableWidthUnitValues.Auto }),
                CreateParagraph(cell ?? string.Empty, header ? 10 : 9, bold: header,
                    styleId: header ? "TableHeader" : "TableCell"));
            row.AppendChild(tc);
        }
        return row;
    }

    public static int AnswerLinesForPoints(decimal points) => points switch
    {
        <= 1 => 2,
        <= 3 => 4,
        <= 6 => 8,
        _ => 12
    };
}
