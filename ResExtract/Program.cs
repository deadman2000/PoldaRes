using System.IO.Compression;
using System.Text;

var file = File.OpenRead(@"D:\Projects\TranslateWeb\Polda\android\resource.res");
//var file = File.OpenRead(@"D:\Projects\TranslateWeb\Polda\Polda\resource.res");
var destPath = @"D:\Projects\TranslateWeb\Polda\extract\";


byte[] header = new byte[20];
file.ReadExactly(header, 0, header.Length);
var res_count = BitConverter.ToUInt32(header, 0);
var pack_size = BitConverter.ToInt32(header, 1 * 4);
var unpacked_size = BitConverter.ToInt32(header, 2 * 4);
var pack_offset = BitConverter.ToInt32(header, 3 * 4);
var files_count = BitConverter.ToUInt32(header, 4 * 4);

byte[] res_data = new byte[res_count * 20];
file.ReadExactly(res_data, 0, res_data.Length);

byte[] Unpack(byte[] packed, int unp_size)
{
    byte[] unpacked = new byte[unp_size];
    MemoryStream ms = new(packed, 0, packed.Length);
    ZLibStream zlib = new(ms, CompressionMode.Decompress);
    zlib.ReadExactly(unpacked, 0, unp_size);
    return unpacked;
}

byte[] zip = new byte[pack_size];
file.Seek(pack_offset, SeekOrigin.Begin);
file.ReadExactly(zip, 0, zip.Length);
byte[] file_map = Unpack(zip, unpacked_size);

//File.WriteAllBytes(@"D:\Projects\TranslateWeb\Polda\unpacked", file_map);

byte[] buff = new byte[1024];
int offset = 0;
for (int i = 0; i < files_count; i++)
{
    //var file_data = Convert.ToHexString(file_map.Skip(offset).Take(32).ToArray());
    //Console.WriteLine(file_data);

    int file_type = BitConverter.ToInt32(file_map, offset);
    int cnt = BitConverter.ToInt32(file_map, offset + 4);
    if (cnt > 1)
        throw new Exception("Multiple file parts not supported");

    int f_pack_size = BitConverter.ToInt32(file_map, offset + 4 * 2);
    int f_raw_size = BitConverter.ToInt32(file_map, offset + 4 * 3);
    int f4 = BitConverter.ToInt32(file_map, offset + 4 * 4);
    int f5 = BitConverter.ToInt32(file_map, offset + 4 * 5);
    int f_offset = BitConverter.ToInt32(file_map, offset + 4 * 6);
    int f7 = BitConverter.ToInt32(file_map, offset + 4 * 7);

    int str_offset = offset + 8 + 24 * cnt;

    int end = str_offset;
    while (file_map[end] != 0) end++;
    string path = Encoding.ASCII.GetString(file_map, str_offset, end - str_offset);

    // file_type:
    // 0 - PRJ, SCN, SCC, SAV, META, TEXTDB.*
    // 1 - FLC, BMP, PNG
    // 2 - OGG
    // 3 - FNT
    Console.WriteLine($"{file_type}\t {f_pack_size}\t {f_raw_size}\t {f4}\t {f5}\t {f_offset}\t {f7}  {path}");

    string relPath = destPath + "\\" + path;
    Directory.CreateDirectory(Path.GetDirectoryName(relPath));

    if (f_pack_size < 0)
    {
        byte[] f_data = new byte[f_raw_size];
        file.Seek(f_offset, SeekOrigin.Begin);
        int skip = 0;

        if (file_type == 2)
        {
            file.ReadExactly(buff, 0, 4);
            var val = BitConverter.ToInt32(buff, 0);
            file.Seek(val * 4, SeekOrigin.Current);
            skip = val * 4;
        }

        file.ReadExactly(f_data, 0, f_data.Length - skip);
        File.WriteAllBytes(relPath, f_data);
    }
    else
    {
        byte[] f_data_pack = new byte[f_pack_size];
        file.Seek(f_offset, SeekOrigin.Begin);
        file.ReadExactly(f_data_pack, 0, f_data_pack.Length);
        //File.WriteAllBytes(relPath, f_data_pack);

        byte[] f_unpacked = Unpack(f_data_pack, f_raw_size);
        File.WriteAllBytes(relPath, f_unpacked);
    }

    offset = end + 1;
    offset += (4 - offset % 4) % 4; // Padding 4 bytes
}

Console.WriteLine();