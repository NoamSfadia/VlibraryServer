using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VlibraryServer
{
    internal class Program
    {
        const int portNo = 500;
        private const string ipAddress = "127.0.0.1";//local host IP

        static void Main(string[] args)
        {
            System.Net.IPAddress localAdd = System.Net.IPAddress.Parse(ipAddress);

            TcpListener listener = new TcpListener(localAdd, portNo);
            TcpListener listener2 = new TcpListener(localAdd, 500);
            Console.WriteLine("Simple TCP Server");
            Console.WriteLine("Listening to ip {0} port: {1}", ipAddress, portNo);
            Console.WriteLine("Server is ready.");

            // Start listen to incoming connection requests
            listener.Start();
            //listener2.Start();
            // infinit loop.
            while (true)
            {
                // AcceptTcpClient - Blocking call
                // Execute will not continue until a connection is established
                TcpClient tcp = listener.AcceptTcpClient();
                // We create an instance of Client so the server will be able to 
                // server multiple client at the same time.
                Thread thread = new Thread(() => NewClient(tcp));
                thread.Start();
                
            }

        }
        static void NewClient(TcpClient TcpClient) 
        {
            Client user = new Client(TcpClient);
        }
    }
    
}
