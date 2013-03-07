using System;
using System.IO.Ports;
using System.Net;
using System.Text;
using System.Threading;
using EventStore.ClientAPI;
using Newtonsoft.Json;

namespace Reader
{
    class Program
    {
        private static EventStoreConnection esCon;

        static void Main(string[] args)
        {
            var port = new SerialPort("/dev/ttyUSB0", 2400, Parity.Even, 7) {Handshake = Handshake.XOnXOff};

            esCon = EventStoreConnection.Create();
            esCon.Connect(new IPEndPoint(IPAddress.Parse("10.3.1.103"),1113));

            //port.DataReceived += DataReceivedHandler;
            port.Open();

            var readerThread = new Thread(ReceivedBalanceReading);
            readerThread.Start(port);

            Console.WriteLine("Press any key to continue...");
            Console.WriteLine();
            Console.ReadKey();
            port.Close();
            readerThread.Abort();
        }

        private static void DataReceivedHandler(
                            object sender,
                            SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string indata = sp.ReadLine();
            string indata2 = sp.ReadLine();
            string indata3 = sp.ReadLine();

            var readEvent = new BalanceRead();
            readEvent.BalanceId = indata.Split(':')[1].Trim();
            readEvent.ProjectId = indata2.Split(':')[1].Trim();
            readEvent.Reading = Decimal.Parse(indata3.Substring(0, 8).Trim());
            readEvent.Unit = indata3.Substring(8, 2).Trim();
            readEvent.Stable = !indata3.Contains("?");

            Console.WriteLine(readEvent.ToString());

            var json = JsonConvert.SerializeObject(readEvent);
            var bytes = Encoding.UTF8.GetBytes(json);
            esCon.AppendToStreamAsync("balanceevents", ExpectedVersion.Any,
                                      new EventData(Guid.NewGuid(), "BalanceRead", true, bytes, new byte[] {}));
        }

        private static void ReceivedBalanceReading(object o)
        {
            var port = (SerialPort) o;
            while (true)
            {
                var indata = ReadLine(port);
                var indata2 = ReadLine(port);
                var indata3 = ReadLine(port);

                var readEvent = new BalanceRead();
                readEvent.BalanceId = indata.Split(':')[1].Trim();
                readEvent.ProjectId = indata2.Split(':')[1].Trim();
                readEvent.Reading = Decimal.Parse(indata3.Substring(0, 8).Trim());
                readEvent.Unit = indata3.Substring(8, 2).Trim();
                readEvent.Stable = !indata3.Contains("?");

                Console.WriteLine(readEvent.ToString());

                var json = JsonConvert.SerializeObject(readEvent);
                var bytes = Encoding.UTF8.GetBytes(json);
                esCon.AppendToStreamAsync("balanceevents", ExpectedVersion.Any,
                                          new EventData(Guid.NewGuid(), "BalanceRead", true, bytes, new byte[] {}));
            }
        }

        private static string ReadLine(SerialPort port)
        {
            var rxString = "";

            var tmpByte = (byte)port.ReadByte();

            while (tmpByte != 13)
            {
                rxString += ((char)tmpByte);
                tmpByte = (byte)port.ReadByte();
            }
            port.ReadByte(); //Dump last new line byte

            return rxString;
        }
    }

    class BalanceRead
    {
        public string BalanceId { get; set; }
        public string ProjectId { get; set; }
        public decimal Reading { get; set; }
        public string Unit { get; set; }
        public bool Stable { get; set; }

        public override string ToString()
        {
            return string.Format("BalanceId: {0}, ProjectId: {1}, Reading: {2}, Unit: {3}, Stable: {4}", BalanceId, ProjectId, Reading, Unit, Stable);
        }
    }
}
