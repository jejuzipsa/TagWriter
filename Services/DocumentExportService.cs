using System.IO;
using System.IO.Compression;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Xml.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using W = DocumentFormat.OpenXml.Wordprocessing;
using TagWriter.Models;

namespace TagWriter.Services;

public static class DocumentExportService
{
    static List<(string Text, bool Bold, int Level)> Paragraphs(ProjectDocument document)
    {
        var list = new List<(string, bool, int)>
        {
            (document.Project.Title, true, 0)
        };

        foreach (var chapter in document.Chapters.OrderBy(x => x.Order))
        {
            list.Add((chapter.Title, true, 1));
            foreach (var scene in chapter.Scenes.OrderBy(x => x.Order))
            {
                var lines = (scene.Content ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
                foreach (var line in lines) list.Add((line, false, 2));
                list.Add(("", false, 2));
            }
        }

        return list;
    }

    public static Task ExportDocxAsync(string path, ProjectDocument document)
    {
        if (File.Exists(path)) File.Delete(path);

        using var word = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
        var main = word.AddMainDocumentPart();
        main.Document = new W.Document();
        var body = main.Document.AppendChild(new W.Body());

        foreach (var item in Paragraphs(document))
        {
            var paragraph = new W.Paragraph();
            var paragraphProps = new W.ParagraphProperties(
                new W.SpacingBetweenLines
                {
                    After = item.Level == 0 ? "260" : item.Level == 1 ? "180" : "80",
                    Line = "300",
                    LineRule = W.LineSpacingRuleValues.Auto
                });
            paragraph.Append(paragraphProps);

            var runProps = new W.RunProperties(
                new W.RunFonts
                {
                    Ascii = "Malgun Gothic",
                    HighAnsi = "Malgun Gothic",
                    EastAsia = "맑은 고딕"
                },
                new W.FontSize { Val = item.Level == 0 ? "32" : item.Level == 1 ? "27" : "22" },
                new W.FontSizeComplexScript { Val = item.Level == 0 ? "32" : item.Level == 1 ? "27" : "22" });

            if (item.Bold) runProps.Append(new W.Bold());

            var run = new W.Run(runProps, new W.Text(item.Text ?? "") { Space = SpaceProcessingModeValues.Preserve });
            paragraph.Append(run);
            body.Append(paragraph);
        }

        body.Append(new W.SectionProperties(
            new W.PageSize { Width = 11906, Height = 16838 },
            new W.PageMargin { Top = 1440, Right = 1440, Bottom = 1440, Left = 1440, Header = 720, Footer = 720, Gutter = 0 }));

        main.Document.Save();
        return Task.CompletedTask;
    }

    public static async Task ExportHwpxAsync(string path, ProjectDocument document)
    {
        var templateRoot = Path.Combine(AppContext.BaseDirectory, "Assets", "HwpxTemplate");
        var sectionTemplate = Path.Combine(templateRoot, "Contents", "section0.xml");
        if (!File.Exists(sectionTemplate))
            throw new FileNotFoundException("HWPX 기본 템플릿을 찾을 수 없습니다.", sectionTemplate);

        if (File.Exists(path)) File.Delete(path);

        await using var fs = File.Create(path);
        using (var zip = new ZipArchive(fs, ZipArchiveMode.Create, false, Encoding.UTF8))
        {
            await WriteTextEntryAsync(zip, "mimetype", "application/hwp+zip", CompressionLevel.NoCompression);

            await CopyTemplateEntryAsync(zip, templateRoot, "version.xml");
            await CopyTemplateEntryAsync(zip, templateRoot, "settings.xml");
            await CopyTemplateEntryAsync(zip, templateRoot, "META-INF/container.xml");
            await CopyTemplateEntryAsync(zip, templateRoot, "META-INF/container.rdf");
            await CopyTemplateEntryAsync(zip, templateRoot, "META-INF/manifest.xml");
            await CopyTemplateEntryAsync(zip, templateRoot, "Contents/content.hpf");
            await CopyTemplateEntryAsync(zip, templateRoot, "Contents/header.xml");

            var sectionXml = BuildHwpxSection(sectionTemplate, document);
            await WriteTextEntryAsync(zip, "Contents/section0.xml", sectionXml);

            var preview = string.Join(Environment.NewLine, Paragraphs(document).Select(x => x.Text));
            await WriteTextEntryAsync(zip, "Preview/PrvText.txt", preview);
        }

        ValidateHwpxPackage(path);
    }

    static string BuildHwpxSection(string templatePath, ProjectDocument document)
    {
        var doc = XDocument.Load(templatePath, LoadOptions.PreserveWhitespace);
        var root = doc.Root ?? throw new InvalidDataException("HWPX section 템플릿이 올바르지 않습니다.");
        XNamespace hp = "http://www.hancom.co.kr/hwpml/2011/paragraph";

        uint id = 1000000000;
        foreach (var item in Paragraphs(document))
        {
            var t = new XElement(hp + "t", item.Text ?? "");
            if (!string.IsNullOrEmpty(item.Text))
                t.SetAttributeValue(XNamespace.Xml + "space", "preserve");

            var p = new XElement(hp + "p",
                new XAttribute("id", id++),
                new XAttribute("paraPrIDRef", "0"),
                new XAttribute("styleIDRef", "0"),
                new XAttribute("pageBreak", "0"),
                new XAttribute("columnBreak", "0"),
                new XAttribute("merged", "0"),
                new XElement(hp + "run",
                    new XAttribute("charPrIDRef", "0"),
                    t));

            root.Add(p);
        }

        using var sw = new Utf8StringWriter();
        using var writer = XmlWriter.Create(sw, new XmlWriterSettings
        {
            OmitXmlDeclaration = false,
            Encoding = new UTF8Encoding(false),
            Indent = true,
            NewLineChars = "\n"
        });
        doc.Save(writer);
        writer.Flush();
        return sw.ToString();
    }

    static async Task CopyTemplateEntryAsync(ZipArchive zip, string templateRoot, string relativePath)
    {
        var diskPath = Path.Combine(templateRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(diskPath))
            throw new FileNotFoundException($"HWPX 템플릿 파일이 없습니다: {relativePath}", diskPath);

        var entry = zip.CreateEntry(relativePath, CompressionLevel.Optimal);
        await using var input = File.OpenRead(diskPath);
        await using var output = entry.Open();
        await input.CopyToAsync(output);
    }

    static async Task WriteTextEntryAsync(ZipArchive zip, string name, string content, CompressionLevel level = CompressionLevel.Optimal)
    {
        var entry = zip.CreateEntry(name, level);
        await using var stream = entry.Open();
        await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        await writer.WriteAsync(content);
    }

    static void ValidateHwpxPackage(string path)
    {
        using var zip = ZipFile.OpenRead(path);
        var entries = zip.Entries.Select(e => e.FullName).ToList();

        string[] required =
        [
            "mimetype",
            "version.xml",
            "settings.xml",
            "META-INF/container.xml",
            "META-INF/container.rdf",
            "META-INF/manifest.xml",
            "Contents/content.hpf",
            "Contents/header.xml",
            "Contents/section0.xml",
            "Preview/PrvText.txt"
        ];

        foreach (var name in required)
            if (!entries.Contains(name, StringComparer.Ordinal))
                throw new InvalidDataException($"HWPX 필수 항목이 없습니다: {name}");

        if (!string.Equals(zip.Entries.FirstOrDefault()?.FullName, "mimetype", StringComparison.Ordinal))
            throw new InvalidDataException("HWPX mimetype 항목은 ZIP의 첫 번째 항목이어야 합니다.");

        var mime = zip.GetEntry("mimetype") ?? throw new InvalidDataException("HWPX mimetype 항목이 없습니다.");
        using var reader = new StreamReader(mime.Open(), Encoding.UTF8, false);
        if (!string.Equals(reader.ReadToEnd(), "application/hwp+zip", StringComparison.Ordinal))
            throw new InvalidDataException("HWPX mimetype 값이 올바르지 않습니다.");
    }

    sealed class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => new UTF8Encoding(false);
    }

    public static void ExportPdf(string path, FlowDocument document)
    {
        document.PageWidth = 794;
        document.PageHeight = 1123;
        document.PagePadding = new Thickness(58);
        document.ColumnWidth = double.PositiveInfinity;

        var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;
        paginator.PageSize = new Size(794, 1123);
        paginator.ComputePageCount();

        var images = new List<(byte[] Bytes, int Width, int Height)>();
        for (var i = 0; i < paginator.PageCount; i++)
        {
            var page = paginator.GetPage(i);
            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, 794, 1123));
                if (page.Visual != null)
                {
                    var brush = new VisualBrush(page.Visual) { Stretch = Stretch.Fill };
                    dc.DrawRectangle(brush, null, new Rect(0, 0, 794, 1123));
                }
            }

            var bitmap = new RenderTargetBitmap(794, 1123, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            var encoder = new JpegBitmapEncoder { QualityLevel = 94 };
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var ms = new MemoryStream();
            encoder.Save(ms);
            images.Add((ms.ToArray(), 794, 1123));
        }

        WriteImagePdf(path, images);
    }

    static void WriteImagePdf(string path, List<(byte[] Bytes, int Width, int Height)> images)
    {
        using var stream = File.Create(path);
        var offsets = new List<long> { 0 };

        void Text(string value)
        {
            var bytes = Encoding.ASCII.GetBytes(value);
            stream.Write(bytes, 0, bytes.Length);
        }

        void StartObj(int n)
        {
            while (offsets.Count <= n) offsets.Add(0);
            offsets[n] = stream.Position;
            Text($"{n} 0 obj\n");
        }

        void EndObj() => Text("endobj\n");

        Text("%PDF-1.4\n%TagWriter\n");
        StartObj(1); Text("<< /Type /Catalog /Pages 2 0 R >>\n"); EndObj();
        StartObj(2);
        Text("<< /Type /Pages /Kids [");
        for (var i = 0; i < images.Count; i++) Text($"{3 + i * 3} 0 R ");
        Text($"] /Count {images.Count} >>\n");
        EndObj();

        for (var i = 0; i < images.Count; i++)
        {
            var pageObj = 3 + i * 3;
            var contentObj = pageObj + 1;
            var imageObj = pageObj + 2;
            var img = images[i];

            StartObj(pageObj);
            Text($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /XObject << /Im0 {imageObj} 0 R >> >> /Contents {contentObj} 0 R >>\n");
            EndObj();

            var content = Encoding.ASCII.GetBytes("q\n595 0 0 842 0 0 cm\n/Im0 Do\nQ\n");
            StartObj(contentObj);
            Text($"<< /Length {content.Length} >>\nstream\n");
            stream.Write(content, 0, content.Length);
            Text("endstream\n");
            EndObj();

            StartObj(imageObj);
            Text($"<< /Type /XObject /Subtype /Image /Width {img.Width} /Height {img.Height} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length {img.Bytes.Length} >>\nstream\n");
            stream.Write(img.Bytes, 0, img.Bytes.Length);
            Text("\nendstream\n");
            EndObj();
        }

        var xref = stream.Position;
        Text($"xref\n0 {offsets.Count}\n");
        Text("0000000000 65535 f \n");
        for (var i = 1; i < offsets.Count; i++) Text($"{offsets[i]:0000000000} 00000 n \n");
        Text($"trailer\n<< /Size {offsets.Count} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
    }
}
