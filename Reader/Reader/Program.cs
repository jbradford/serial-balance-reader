using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reader
{
    class Program
    {
        static void Main(string[] args)
        {
            var ports = SerialPort.GetPortNames();
            foreach (var port in ports)
            {
                Console.WriteLine(port);
            }
            Console.ReadLine();
        }
    }
}
