﻿using System.Text;

void ExtractText(string dbPath, string outPath)
{
    using var file = File.OpenRead(dbPath);
    byte[] buff = new byte[1024 * 1024];
    file.ReadExactly(buff, 0, 16);
    var header = Encoding.ASCII.GetString(buff, 0, 16);
    if (header != "PSE text DB file")
        throw new FormatException($"Wrong header {header}");

    file.ReadExactly(buff, 0, 4);
    var records = BitConverter.ToInt32(buff, 0);

    Record[] offsets = new Record[records];
    for (int i = 0; i < records; i++)
    {
        file.ReadExactly(buff, 0, 8);
        var id = BitConverter.ToInt32(buff, 0);
        var offset = BitConverter.ToInt32(buff, 4);
        //Console.WriteLine($"{id} {offset}");
        offsets[i] = new Record { Id = id, Offset = offset };
    }


    var fileOut = File.OpenWrite(outPath);
    using StreamWriter sw = new(fileOut);
    for (int i = 0; i < offsets.Length; i++)
    {
        file.Seek(offsets[i].Offset, SeekOrigin.Begin);
        int size;
        if (i == records - 1)
            size = (int)(file.Length - offsets[i].Offset - 2);
        else
            size = offsets[i + 1].Offset - offsets[i].Offset - 2;

        file.ReadExactly(buff, 0, size);
        var txt = Encoding.Unicode.GetString(buff, 0, size);

        sw.WriteLine($"[{offsets[i].Id}]: {txt}");
    }
}


var dir = @"D:\Projects\TranslateWeb\Polda\extract_5\OUTPUT\";
var files = Directory.GetFiles(dir);
foreach (var path in files)
{
    var name = Path.GetFileName(path);
    if (name.StartsWith("TEXTDB") && !name.EndsWith(".TXT"))
    {
        Console.WriteLine(path);
        ExtractText(path, Path.Combine(dir, name + ".TXT"));
    }
}