using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;
using System.IO;

namespace Server
{
    class Server
    {

        List<ServerClient> clients;
        List<ServerClient> disconnectList;

        TcpListener server;
        public bool serverStarted; //서버 시작 여부

        readonly int PORT = 8282;
        readonly IPAddress IP = IPAddress.Any;

        bool serverEnd = false;

        int client_number = 0;

        public void Start()
        {
            Task END = new Task(THE_END);
            END.Start();



            ServerCreate();
            while(!serverEnd)
                Update();
        }

        async void THE_END()
        {
            Console.WriteLine("THE_END() : Start");
            while (!serverEnd)
            {
                Task<bool> task = new Task<bool>(() => Console.ReadLine().Equals("exit"));
                task.Start();
                await task;
                serverEnd = task.Result;
            }
            Console.WriteLine("THE_END() : End");
        }

        public void ServerCreate()
        {
            clients = new List<ServerClient>();
            disconnectList = new List<ServerClient>();

            try
            {
                server = new TcpListener(IP, PORT);
                server.Start();

                StartListening();
                serverStarted = true;
                Console.WriteLine($"서버가 {PORT}번 포트에서 시작되었습니다.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        void StartListening()
        {
            server.BeginAcceptTcpClient(AcceptTcpClient, server);
        }

        void AcceptTcpClient(IAsyncResult IAR)
        {
            TcpListener listener = (TcpListener)IAR.AsyncState;
            clients.Add(new ServerClient(listener.EndAcceptTcpClient(IAR), client_number));
            client_number++;
            StartListening();
        }

        void Update()
        {
            Console.WriteLine("Update() : Start");
            while (serverStarted && !serverEnd)
            {
                foreach (ServerClient c in clients)
                {
                    if (!IsConnected(c.tcp))
                    {
                        c.tcp.Close();
                        disconnectList.Add(c);
                        continue;
                    }
                    else
                    {
                        NetworkStream s = c.tcp.GetStream();
                        if(s.DataAvailable)
                        {
                            string type = new StreamReader(s, true).ReadLine();
                            string data = new StreamReader(s, true).ReadLine();
                            if(data != null)
                            {
                                OnIncomingData(c, type, data);
                            }
                        }
                    }
                }
            }
        }

        bool IsConnected(TcpClient c)
        {
            try
            {
                if (c != null && c.Client != null && c.Client.Connected)
                {
                    if (c.Client.Poll(0, SelectMode.SelectRead))
                    {
                        return !(c.Client.Receive(new byte[1], SocketFlags.Peek) == 0);
                    }
                    return true;
                }
                else
                    return false;
            }
            catch
            {
                return false;
            }

        }

        void OnIncomingData(ServerClient c, string type, string data)
        {
            if(type.Equals("Text"))
            {
                Broadcast(c, type, data, clients);
            }
            else
            {
                GameDataCasting(type, data, clients);
            }
        }

        void Broadcast(ServerClient target, string type, string data, List<ServerClient> cl)
        {
            foreach (ServerClient c in cl)
            {
                try
                {
                    StreamWriter writer = new StreamWriter(c.tcp.GetStream());

                    data = SendTypeData(type) + target.clientName + " : " + data;
                    writer.WriteLine(data);
                    writer.Flush();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        void GameDataCasting(string type, string data, List<ServerClient> cl)
        {
            foreach(ServerClient c in cl)
            {
                try
                {
                    StreamWriter writer = new StreamWriter(c.tcp.GetStream());

                    data = SendTypeData(type) + data;
                    writer.WriteLine(data);
                    writer.Flush();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

        }
        
        string SendTypeData(string type)
        {
            return type + "\n";
        }
    }

    class ServerClient
    {
        public TcpClient tcp;
        public string clientName;

        public ServerClient(TcpClient clientSocket, int client_number)
        {
            clientName = "Client" + client_number.ToString();
            tcp = clientSocket;
        }
    }
}

//c.tcp.Client.Receive();
//c.tcp.Client.Send();