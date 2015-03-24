using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Linq;
using System.Text;

namespace mySockets
{
    public class net
    {
        System.Net.Sockets.Socket s = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        IPEndPoint endIp = null;
        Socket serverSocket = null;
        string addr;

        public net(string recipient)
        {
            try
            {
                endIp = new IPEndPoint(IPAddress.Parse(recipient), 12345);
            }
            catch
            {
                ;
            }

            addr = recipient;
        }

        public net()
        {
            ;
        }

        public void changeAdress(string newAdress)
        {
            endIp = new IPEndPoint(IPAddress.Parse(newAdress), 12345);
        }


        public void server()
        {
            s.Bind(new IPEndPoint(IPAddress.Any, 12345));
            s.Listen(1);

            serverSocket = s.Accept();
        }

        public string serverReceive()
        {
            if (serverSocket.Available != 0)
            {
                byte[] msg = new byte[serverSocket.Available];
                serverSocket.Receive(msg);
                return Encoding.UTF8.GetString(msg);
            }
            else
            {
                return "";
            }
        }

        public void serverSend(string message)
        {
            if (serverSocket != null)
            {
                serverSocket.Send(Encoding.UTF8.GetBytes(message));
            }
        }

        public void close()
        {
            //s.Shutdown(SocketShutdown.Send);
            s.Close();
        }


        public bool client()
        {
            try
            {
                /*
                Socket tempSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                tempSocket.Connect(endIp);
                s = tempSocket;
                 */

                s.Connect(endIp);

                return true;
            }
            catch (Exception e)
            {
                //Console.WriteLine(e);
                return false;
            }
        }

        public string clientReceive()
        {
            if (s.Available != 0)
            {
                byte[] msg = new byte[s.Available];
                s.Receive(msg);
                return Encoding.UTF8.GetString(msg);
            }
            else
            {
                return "";
            }
        }

        public void clientSend(string message)
        {
            byte[] msg = Encoding.UTF8.GetBytes(message);
            s.Send(msg);
        }
    }
}
