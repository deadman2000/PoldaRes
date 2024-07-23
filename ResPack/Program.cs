using System.IO.Compression;
using System.Text;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
var enc = Encoding.GetEncoding(1250);

var srcDir = @"D:\Projects\TranslateWeb\Polda\pack_5\";
var dstRes = @"C:\Program Files (x86)\Steam\steamapps\common\Polda 5\patch000.res";

byte[] Pack(byte[] src)
{
    using MemoryStream ms = new();
    using ZLibStream zlib = new(ms, CompressionMode.Compress);
    zlib.Write(src);
    zlib.Flush();
    return ms.ToArray();
}

int GetFileType(string name) => Path.GetExtension(name) switch
{
    ".FLC" or ".BMP" or ".PNG" => 1,
    ".OGG" => 2,
    ".FNT" => 3,
    _ => 0,
};

File.Delete(dstRes);
using var fileRes = File.OpenWrite(dstRes);
using BinaryWriter res = new(fileRes);
res.Write(1); // res_count
res.Write(0); // pack_size
res.Write(0); // unpacked_size
res.Write(0); // pack_offset
res.Write(0); // files_count
res.Write(0); // unknown

var files = Directory.GetFiles(srcDir, "*", SearchOption.AllDirectories);
using MemoryStream msMap = new();
using BinaryWriter map = new(msMap);
int files_count = 0;
foreach (var file in files)
{
    if (file.ToUpper().EndsWith(".EXTRA")) continue;
    int res_type = GetFileType(file);

    map.Write(res_type);
    map.Write(1); // cnt

    byte[] extra = null;
    if (res_type == 2)
    {
        extra = Array.Empty<byte>();
        var extraPath = file + ".EXTRA";
        if (File.Exists(extraPath))
            extra = File.ReadAllBytes(extraPath);
    }

    var data = File.ReadAllBytes(file);
    byte[] packed = null;
    if (res_type != 2)
        packed = Pack(data);

    int offset = (int)fileRes.Position;

    if (packed == null || packed.Length > data.Length)
    {
        // Write unpacked

        int raw_size = data.Length;
        if (extra != null)
        {
            res.Write(extra.Length / 4);
            res.Write(extra);
            raw_size += extra.Length + 4;
        }

        res.Write(data);
        map.Write(-1); // Pack size
        map.Write(raw_size);
    }
    else
    {
        // Write packed
        res.Write(packed);
        map.Write(packed.Length); // Pack size
        map.Write(data.Length);
    }

    map.Write(0);
    map.Write(0);
    map.Write(offset);
    map.Write(0);

    var filePath = "\\" + Path.GetRelativePath(srcDir, file);
    map.Write(enc.GetBytes(filePath));
    map.Write((byte)0);

    while (msMap.Position % 4 != 0) map.Write((byte)0);

    files_count++;
}

map.Flush();

var mapData = msMap.ToArray();
var map_packed = Pack(mapData);

int pack_offset = (int)fileRes.Position;
res.Write(map_packed);

res.Seek(4, SeekOrigin.Begin);
res.Write(map_packed.Length);
res.Write(mapData.Length);
res.Write(pack_offset);
res.Write(files_count);

res.Flush();
fileRes.Close();

Console.WriteLine("Packed");