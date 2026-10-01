using System.IO;
using System.IO.Compression;
using System.Security;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TagWriter.Models;

namespace TagWriter.Services;

public static class DocumentExportService
{
    static string Esc(string? value) => SecurityElement.Escape(value ?? "") ?? "";

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

    static async Task WriteEntryAsync(ZipArchive zip, string name, string content, CompressionLevel level = CompressionLevel.Optimal)
    {
        var entry = zip.CreateEntry(name, level);
        await using var stream = entry.Open();
        await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        await writer.WriteAsync(content);
    }

    public static async Task ExportDocxAsync(string path, ProjectDocument document)
    {
        if (File.Exists(path)) File.Delete(path);
        await using var fs = File.Create(path);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Create, false, Encoding.UTF8);

        const string contentTypes = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/word/document.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml\"/></Types>";
        const string rels = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"word/document.xml\"/></Relationships>";

        var body = new StringBuilder();
        foreach (var p in Paragraphs(document))
        {
            var size = p.Level == 0 ? "32" : p.Level == 1 ? "27" : "22";
            body.Append("<w:p><w:pPr><w:spacing w:after=\"")
                .Append(p.Level == 0 ? "260" : p.Level == 1 ? "180" : "80")
                .Append("\"/></w:pPr><w:r><w:rPr><w:rFonts w:ascii=\"Malgun Gothic\" w:eastAsia=\"맑은 고딕\"/>")
                .Append("<w:sz w:val=\"").Append(size).Append("\"/><w:szCs w:val=\"").Append(size).Append("\"/>");
            if (p.Bold) body.Append("<w:b/>");
            body.Append("</w:rPr><w:t xml:space=\"preserve\">").Append(Esc(p.Text)).Append("</w:t></w:r></w:p>");
        }

        var doc = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body>"
            + body
            + "<w:sectPr><w:pgSz w:w=\"11906\" w:h=\"16838\"/><w:pgMar w:top=\"1440\" w:right=\"1440\" w:bottom=\"1440\" w:left=\"1440\"/></w:sectPr></w:body></w:document>";

        await WriteEntryAsync(zip, "[Content_Types].xml", contentTypes);
        await WriteEntryAsync(zip, "_rels/.rels", rels);
        await WriteEntryAsync(zip, "word/document.xml", doc);
    }

    public static async Task ExportHwpxAsync(string path, ProjectDocument document)
    {
        if (File.Exists(path)) File.Delete(path);
        await using var fs = File.Create(path);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Create, false, Encoding.UTF8);

        await WriteEntryAsync(zip, "mimetype", "application/hwp+zip", CompressionLevel.NoCompression);

        const string container = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><ocf:container xmlns:ocf=\"urn:oasis:names:tc:opendocument:xmlns:container\"><ocf:rootfiles><ocf:rootfile full-path=\"Contents/content.hpf\" media-type=\"application/hwpml-package+xml\"/></ocf:rootfiles></ocf:container>";
        var content = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><opf:package xmlns:opf=\"http://www.idpf.org/2007/opf\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\" version=\"1.0\"><opf:metadata><dc:title>"
            + Esc(document.Project.Title)
            + "</dc:title><dc:creator>" + Esc(document.Project.Author) + "</dc:creator><dc:date>" + DateTime.Now.ToString("yyyy-MM-dd") + "</dc:date><dc:language>ko-KR</dc:language></opf:metadata><opf:manifest><opf:item id=\"header\" href=\"header.xml\" media-type=\"application/xml\"/><opf:item id=\"section0\" href=\"section0.xml\" media-type=\"application/xml\"/><opf:item id=\"settings\" href=\"settings.xml\" media-type=\"application/xml\"/></opf:manifest><opf:spine><opf:itemref idref=\"section0\"/></opf:spine></opf:package>";

        const string header = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><hh:head xmlns:hh=\"http://www.hancom.co.kr/hwpml/2011/head\"><hh:beginNum page=\"1\" footnote=\"1\" endnote=\"1\" pic=\"1\" tbl=\"1\" equation=\"1\"/><hh:refList><hh:fontfaces><hh:fontface lang=\"HANGUL\"><hh:font name=\"맑은 고딕\" type=\"TTF\"/></hh:fontface></hh:fontfaces><hh:borderFills><hh:borderFill id=\"0\"/></hh:borderFills><hh:charProperties><hh:charPr id=\"0\" height=\"1100\" textColor=\"#000000\"/><hh:charPr id=\"1\" height=\"1300\" textColor=\"#000000\"><hh:bold/></hh:charPr></hh:charProperties><hh:paraProperties><hh:paraPr id=\"0\" align=\"LEFT\"/></hh:paraProperties><hh:styles><hh:style id=\"0\" type=\"PARA\" name=\"바탕글\" paraPrIDRef=\"0\" charPrIDRef=\"0\"/></hh:styles><hh:bullets/><hh:numberings/></hh:refList></hh:head>";

        var section = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?><hs:sec xmlns:hs=\"http://www.hancom.co.kr/hwpml/2011/section\" xmlns:hp=\"http://www.hancom.co.kr/hwpml/2011/paragraph\">");
        var id = 0;
        foreach (var p in Paragraphs(document))
        {
            section.Append("<hp:p id=\"").Append(id++).Append("\" paraPrIDRef=\"0\" styleIDRef=\"0\"><hp:run charPrIDRef=\"")
                .Append(p.Bold ? "1" : "0").Append("\"><hp:t>").Append(Esc(p.Text)).Append("</hp:t></hp:run></hp:p>");
        }
        section.Append("</hs:sec>");

        const string settings = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><ha:HWPApplicationSetting xmlns:ha=\"http://www.hancom.co.kr/hwpml/2011/app\"/>";
        const string version = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><ha:HCFVersion xmlns:ha=\"http://www.hancom.co.kr/hwpml/2011/app\" targetApplication=\"WORDPROC\" major=\"5\" minor=\"1\" micro=\"0\" buildNumber=\"0\" os=\"Windows\"/>";

        await WriteEntryAsync(zip, "META-INF/container.xml", container);
        await WriteEntryAsync(zip, "Contents/content.hpf", content);
        await WriteEntryAsync(zip, "Contents/header.xml", header);
        await WriteEntryAsync(zip, "Contents/section0.xml", section.ToString());
        await WriteEntryAsync(zip, "Contents/settings.xml", settings);
        await WriteEntryAsync(zip, "version.xml", version);
        await WriteEntryAsync(zip, "Preview/PrvText.txt", string.Join(Environment.NewLine, Paragraphs(document).Select(x => x.Text)));
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
        void StartObj(int n) { while (offsets.Count <= n) offsets.Add(0); offsets[n] = stream.Position; Text($"{n} 0 obj\n"); }
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
