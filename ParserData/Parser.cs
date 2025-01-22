
using System;
using System.Collections;
using System.Text;

namespace ParserData
{
    class Packege
    {
        public string Header { get; set; }
        public string AlertVdrop { get; set; }
        public string VBatt { get; set; }
        public PacketInfo PacketInfo { get; set; } = new PacketInfo();
        public string MeterId { get; set; }
        public string PrincipalConsumption { get; set; }
        public List<string> Others { get; set; } = new List<string>();
        public List<string> LP { get; set; } = new List<string>();
        public List<string> Offsets { get; set; } = new List<string>();

        public override string ToString()
        {
            return $"Packege:\n" +
                   $"  Header: {Header}\n" +
                   $"  AlertVdrop: {AlertVdrop}\n" +
                   $"  VBatt: {VBatt}\n" +
                   $"  PacketInfo: {PacketInfo}\n" +
                   $"  MeterId: {MeterId}\n" +
                   $"  PrincipalConsumption: {PrincipalConsumption}\n" +
                   $"  Others: [{string.Join(", ", Others)}]\n" +
                   $"  LP: [{string.Join(", ", LP)}]\n" +
                   $"  Offsets: [{string.Join(", ", Offsets)}]";
        }

    }

    public class PacketInfo
    {
        public string PInfo { get; set; }
        public int IdDataType { get; set; }
        public int ConsumptionSign { get; set; }
        public int ConsumptionLpSize { get; set; }
        public List<int> Other_Status { get; set; } = new List<int>();
        public int Reserved { get; set; }

        public override string ToString()
        {
            return $"PacketInfo:\n" +
                   $"    PInfo: {PInfo}\n" +
                   $"    IdDataType: {IdDataType}\n" +
                   $"    ConsumptionSign: {ConsumptionSign}\n" +
                   $"    ConsumptionLpSize: {ConsumptionLpSize}\n" +
                   $"    Other_Status: [{string.Join(", ", Other_Status)}]\n" +
                   $"    Reserved: {Reserved}";
        }

    }

    internal class Parser
    {
        public readonly Packege info;

        public Parser(byte[] data)
        {
            if (data is null || data.Length < 2)
            {
                Console.WriteLine("Input byte array must contain at least 2 bytes for packet_info.");
            }

            info = CreatePackege(data);
        }

        private Packege CreatePackege(byte[] data)
        {
            Packege packege = new Packege();
            var binaryString = string.Join("", data.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));
            int iterator = 0;
            packege.Header = binaryString.Substring(iterator, 8);
            iterator += packege.Header.Length;
            packege.AlertVdrop = binaryString.Substring(iterator, 8);
            iterator += packege.AlertVdrop.Length;
            packege.VBatt = binaryString.Substring(iterator, 8);
            iterator += packege.VBatt.Length;
            packege.PacketInfo.PInfo = binaryString.Substring(iterator, 16);
            iterator += packege.PacketInfo.PInfo.Length;

            ParsePacketInfo(packege);
            iterator = CreateMeterId(packege, binaryString, iterator);
            iterator = ParseOther(packege, binaryString, iterator);
            iterator = ParseOffests(packege, binaryString, iterator);
            binaryString = AddPaddingBits(binaryString);
            return packege;
        }

        private static string AddPaddingBits(string binaryString)
        {
            var num = binaryString.Length % 8;
            if (num != 0)
            {
                for (int i = 0; i < 8 - num; i++)
                {
                    binaryString += "1";
                }
            }
            return binaryString;
        }

        private static int ParseOffests(Packege packege, string binaryString, int iterator)
        {
            if (packege.PacketInfo.IdDataType == 1)
            {
                int endIndex = binaryString.Length - 1;
                while (endIndex >= 0 && binaryString[endIndex] == '1')
                {
                    endIndex--;
                }

                binaryString = binaryString.Substring(0, endIndex + 1);
            }

            for (int i = 0; i < 11; i++)
            {
                var offset = Convert.ToInt64(binaryString.Substring(iterator, packege.PacketInfo.ConsumptionLpSize), 2);
                packege.Offsets.Add(offset == 15 ? "N/A" : offset.ToString());
                iterator += packege.PacketInfo.ConsumptionLpSize;
            }

            packege.LP.Add((int.Parse(packege.PrincipalConsumption) - int.Parse(packege.Offsets[0])).ToString());
            for (int i = 1; i < packege.Offsets.Count; i++)
            {
                int offset;
                if (int.TryParse(packege.Offsets[i], out offset))
                {
                    packege.LP.Add((int.Parse(packege.LP[i - 1]) - offset).ToString());
                }
                else
                {
                    packege.LP.Add("N/A");
                }
            }

            return iterator;
        }

        private static int ParseOther(Packege packege, string binaryString, int iterator)
        {
            for (int i = 0; i < packege.PacketInfo.Other_Status.Count; i++)
            {
                if (packege.PacketInfo.Other_Status[i] == 0 || packege.PacketInfo.Other_Status[i] == 7)
                {
                    Console.WriteLine($"Your status{i} not in use or not available");
                }
                else
                {
                    packege.Others.Add(Convert.ToInt32(binaryString.Substring(iterator, packege.PacketInfo.Other_Status[i]), 2).ToString("X"));
                    iterator += packege.PacketInfo.Other_Status[i];
                }
            }

            return iterator;
        }

        private static void ParsePacketInfo(Packege packege)
        {
            int iterator = 0;
            var tempInfo = packege.PacketInfo.PInfo;

            packege.PacketInfo.IdDataType = Convert.ToInt32(tempInfo.Substring(iterator, 1), 2);
            iterator++;

            packege.PacketInfo.ConsumptionSign = Convert.ToInt32(tempInfo.Substring(iterator, 1));
            iterator++;

            packege.PacketInfo.ConsumptionLpSize = Convert.ToInt32(tempInfo.Substring(iterator, 4), 2);
            iterator += 4;

            packege.PacketInfo.Other_Status.Add(Convert.ToInt32(tempInfo.Substring(iterator, 3), 2));
            iterator += 3;

            packege.PacketInfo.Other_Status.Add(Convert.ToInt32(tempInfo.Substring(iterator, 3), 2));
            iterator += 3;

            packege.PacketInfo.Other_Status.Add(Convert.ToInt32(tempInfo.Substring(iterator, 3), 2));
            iterator += 3;

            packege.PacketInfo.Reserved = Convert.ToInt32(tempInfo.Substring(iterator, 1), 2);
        }

        private static int CreateMeterId(Packege packetInfo, string binaryString, int iterator)
        {
            if (packetInfo.PacketInfo.IdDataType == 0)
            {
                packetInfo.MeterId = Convert.ToInt64(binaryString.Substring(iterator, 8 * 5), 2).ToString();
                iterator += 8 * 5;
                packetInfo.PrincipalConsumption = Convert.ToInt32(binaryString.Substring(iterator, 8 * 5), 2).ToString();
                iterator += 8 * 5;
            }
            else
            {
                byte IdAsciiSize = Convert.ToByte(binaryString.Substring(iterator, 4), 2);
                iterator += 4;
                string encryptString = string.Empty;
                encryptString += IdAsciiSize;
                for (int i = 1; i <= IdAsciiSize; i++)
                {
                    encryptString += IdAsciiSize.ToString() + i.ToString();
                }

                packetInfo.MeterId = string.Join("", new ASCIIEncoding().GetBytes(encryptString));
                iterator += packetInfo.MeterId.Length;
                packetInfo.PrincipalConsumption = Convert.ToInt64(binaryString.Substring(iterator, 8 * 5), 2).ToString();
                iterator += 8 * 5;
            }

            return iterator;
        }
    }
}
