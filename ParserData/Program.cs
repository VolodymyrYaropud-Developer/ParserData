

using ParserData;

string hexString = "2E13CD100000006DDF70000000015e560c49ffffffe0";
byte[] byteArray = Enumerable
    .Range(0, hexString.Length / 2)
    .Select(x => Convert.ToByte(hexString.Substring(x * 2, 2), 16))
    .ToArray();

var parser = new Parser(byteArray);

parser.info.ToString();

Console.WriteLine(parser.info.ToString());