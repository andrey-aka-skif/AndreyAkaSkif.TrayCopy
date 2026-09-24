// Собирает src/AndreyAkaSkif.TrayCopy/Assets/app.ico из SVG-исходников этого каталога.
//
//   dotnet run assets/icon/build-icon.cs
//   dotnet run assets/icon/build-icon.cs -- --preview <файл.png>
//
// С --preview дополнительно рисует лист всех кадров на светлом и тёмном фоне: крупно
// (×4, без сглаживания) и в натуральную величину.

#:package Svg.Skia

using SkiaSharp;
using Svg.Skia;

// Кадры 16–24 px — трей при масштабе 100–150 %: у каждого свой рисунок по пиксельной сетке
// своего размера, иначе при растяжении тонкие детали размываются. Остальные кадры — трей
// при 200–250 %, «Пуск», Explorer, инсталлятор — из общего рисунка
(int Size, string Source)[] frames =
[
    (16, "icon-16.svg"),
    (20, "icon-20.svg"),
    (24, "icon-24.svg"),
    (32, "icon.svg"),
    (40, "icon.svg"),
    (48, "icon.svg"),
    (64, "icon.svg"),
    (256, "icon.svg"),
];

var iconDir = AppContext.GetData("EntryPointFileDirectoryPath") as string
    ?? throw new InvalidOperationException("Скрипт запускается как file-based app: dotnet run build-icon.cs");
var output = Path.GetFullPath(Path.Combine(iconDir, "..", "..", "src", "AndreyAkaSkif.TrayCopy", "Assets", "app.ico"));

var bitmaps = frames.Select(f => Render(Path.Combine(iconDir, f.Source), f.Size)).ToList();

using (var stream = File.Create(output))
{
    WriteIco(stream, bitmaps);
}
Console.WriteLine($"{output}: {string.Join(", ", frames.Select(f => f.Size))} px");

if (args is ["--preview", var previewPath])
{
    WritePreview(Path.GetFullPath(previewPath), bitmaps);
    Console.WriteLine($"Предпросмотр: {Path.GetFullPath(previewPath)}");
}

static SKBitmap Render(string svgPath, int size)
{
    using var svg = new SKSvg();
    var picture = svg.Load(svgPath) ?? throw new InvalidOperationException($"Не удалось прочитать {svgPath}");
    var bounds = picture.CullRect;

    var bitmap = new SKBitmap(new SKImageInfo(size, size, SKColorType.Bgra8888, SKAlphaType.Premul));
    using var canvas = new SKCanvas(bitmap);
    canvas.Clear(SKColors.Transparent);
    canvas.Scale(size / bounds.Width, size / bounds.Height);
    canvas.Translate(-bounds.Left, -bounds.Top);
    canvas.DrawPicture(picture);
    canvas.Flush();
    return bitmap;
}

// Раскладка ICO: кадры меньше 256 px — 32-битный DIB с маской прозрачности, 256 px — PNG.
// Её понимают все потребители иконки: Explorer, компилятор (ApplicationIcon), Inno Setup
static void WriteIco(Stream stream, IReadOnlyList<SKBitmap> bitmaps)
{
    var images = bitmaps.Select(b => b.Width >= 256 ? EncodePng(b) : EncodeDib(b)).ToList();

    using var writer = new BinaryWriter(stream);
    writer.Write((ushort)0); // зарезервировано
    writer.Write((ushort)1); // тип: иконка
    writer.Write((ushort)images.Count);

    var offset = 6 + 16 * images.Count;
    for (var i = 0; i < images.Count; i++)
    {
        var size = bitmaps[i].Width;
        writer.Write((byte)(size >= 256 ? 0 : size)); // ширина, 0 означает 256
        writer.Write((byte)(size >= 256 ? 0 : size)); // высота
        writer.Write((byte)0);   // палитры нет
        writer.Write((byte)0);   // зарезервировано
        writer.Write((ushort)1); // плоскостей
        writer.Write((ushort)32); // бит на пиксель
        writer.Write(images[i].Length);
        writer.Write(offset);
        offset += images[i].Length;
    }

    foreach (var image in images)
    {
        writer.Write(image);
    }
}

static byte[] EncodePng(SKBitmap bitmap)
{
    using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
    return data.ToArray();
}

static byte[] EncodeDib(SKBitmap bitmap)
{
    var size = bitmap.Width;
    var maskStride = (size + 31) / 32 * 4;
    var colorBytes = size * size * 4;
    var maskBytes = maskStride * size;

    using var memory = new MemoryStream();
    using var writer = new BinaryWriter(memory);

    // BITMAPINFOHEADER; высота удвоена: цветные пиксели и маска идут одним изображением
    writer.Write(40);
    writer.Write(size);
    writer.Write(size * 2);
    writer.Write((ushort)1);
    writer.Write((ushort)32);
    writer.Write(0); // BI_RGB
    writer.Write(colorBytes + maskBytes);
    writer.Write(0);
    writer.Write(0);
    writer.Write(0);
    writer.Write(0);

    // Строки снизу вверх, BGRA без предумножения альфы (GetPixel отдаёт именно так)
    for (var y = size - 1; y >= 0; y--)
    {
        for (var x = 0; x < size; x++)
        {
            var c = bitmap.GetPixel(x, y);
            writer.Write(c.Blue);
            writer.Write(c.Green);
            writer.Write(c.Red);
            writer.Write(c.Alpha);
        }
    }

    // Маска: бит 1 — пиксель прозрачен
    for (var y = size - 1; y >= 0; y--)
    {
        var row = new byte[maskStride];
        for (var x = 0; x < size; x++)
        {
            if (bitmap.GetPixel(x, y).Alpha == 0)
            {
                row[x / 8] |= (byte)(0x80 >> (x % 8));
            }
        }
        writer.Write(row);
    }

    writer.Flush();
    return memory.ToArray();
}

static void WritePreview(string path, IReadOnlyList<SKBitmap> bitmaps)
{
    const int zoom = 4;
    const int gap = 16;
    SKColor[] backgrounds = [new(0xF3, 0xF3, 0xF3), new(0x20, 0x20, 0x20)];

    var small = bitmaps.Where(b => b.Width <= 64).ToList();
    var large = bitmaps.Single(b => b.Width == 256);
    var zoomedWidth = small.Sum(b => b.Width * zoom + gap) + large.Width + gap;
    var actualWidth = bitmaps.Sum(b => b.Width + gap);
    var panelWidth = Math.Max(zoomedWidth, actualWidth) + gap;
    var panelHeight = gap + Math.Max(64 * zoom, 256) + gap + 256 + gap;

    using var sheet = new SKBitmap(panelWidth, panelHeight * backgrounds.Length);
    using var canvas = new SKCanvas(sheet);
    var nearest = new SKSamplingOptions(SKFilterMode.Nearest);

    for (var i = 0; i < backgrounds.Length; i++)
    {
        var top = i * panelHeight;
        using var background = new SKPaint { Color = backgrounds[i] };
        canvas.DrawRect(0, top, panelWidth, panelHeight, background);

        var x = gap;
        foreach (var b in small)
        {
            Draw(canvas, b, x, top + gap, zoom, nearest);
            x += b.Width * zoom + gap;
        }
        Draw(canvas, large, x, top + gap, 1, nearest);

        x = gap;
        var baseline = top + gap + Math.Max(64 * zoom, 256) + gap;
        foreach (var b in bitmaps)
        {
            Draw(canvas, b, x, baseline, 1, nearest);
            x += b.Width + gap;
        }
    }

    using var data = sheet.Encode(SKEncodedImageFormat.Png, 100);
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    using var file = File.Create(path);
    data.SaveTo(file);

    static void Draw(SKCanvas canvas, SKBitmap bitmap, int x, int y, int zoom, SKSamplingOptions sampling)
    {
        using var image = SKImage.FromBitmap(bitmap);
        canvas.DrawImage(image, SKRect.Create(x, y, bitmap.Width * zoom, bitmap.Height * zoom), sampling);
    }
}
