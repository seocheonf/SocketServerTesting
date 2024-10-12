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

        char[] splitdata = { ':' };


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
            ServerClient client = new ServerClient(listener.EndAcceptTcpClient(IAR), client_number);
            clients.Add(client);

            IPEndPoint IP_PORT = (IPEndPoint)client.tcp.Client.LocalEndPoint;
            Console.WriteLine($"Connection : IP : {IP_PORT.Address} / PORT : {IP_PORT.Port} / clientName : {client.clientName}");

            client_number++;
            StartListening();
        }

        void Update()
        {
            Console.WriteLine("Update() : Start");
            while (serverStarted && !serverEnd)
            {
                for(int i = 0; i < clients.Count; i++)
                {
                    ServerClient c = clients[i];
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
                            string all_data = new StreamReader(s, true).ReadLine();
                            string[] split_data = SplitData(all_data);

                            string type = split_data[0];
                            string data = split_data[1];
                            if(data != null)
                            {
                                OnIncomingData(c, type, data);
                            }
                        }
                    }
                }
                for(int i = 0; i< disconnectList.Count; i++)
                {
                    clients.Remove(disconnectList[i]);
                    disconnectList.RemoveAt(i);
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
                Console.WriteLine(data);
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
            return type + ":";
        }

        string[] SplitData(string data)
        {
            return data.Split(splitdata, 2);
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