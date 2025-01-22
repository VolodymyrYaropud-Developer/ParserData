

using ParserData;

string hexString = "2E13CD8050000006DDF7000000001500ВFВF1FСFF0";
byte[] byteArray = Enumerable
    .Range(0, hexString.Length / 2)
    .Select(x => Convert.ToByte(hexString.Substring(x * 2, 2), 16))
    .ToArray();

var parser = new Parser(byteArray);