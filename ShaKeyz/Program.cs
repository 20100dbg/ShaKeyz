using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;


namespace ShaKeyz
{
    class Program
    {

        /* basically :
         * launch two instance of this program. One in server, the other one in client
         * use the client to connect to the server
         * Both client and server can search, request or send file
         * 
         * commands :
         * search : "search anyfile.jpg" and you get a list of results. Usuals wildcards works
         * request : "request datfile.png" and the other guy send you the file
         * send : "send derp.tiff" you send the file
         * 
         * That's it.
         * Warning : this code is meant to be extra simple. It lacks of checks, like asking confirmation for overwritting files
         * I think this is a good start to make a more efficient/featured/nice file share program
         * 
         * Note : i'm using TCP, networkStream and serialize. It's a way to do it, it's a simple way, and it's seems to work.
         * You probably can do better, faster, safer, etc... 
         * 
         * Take it, make it better, give it.
         */

        //default values
        public static Int64 buffersize = 1024 * 1024 * 10; //buffer is 10mo. 
        //small buffer = low transfer rate, few ressources used. big buffer = high transfer rate, more ressources used
        static int port = 9876;
        static String incomingFolder = AppDomain.CurrentDomain.BaseDirectory; //current dictory + an incoming folder
        

        static Thread t; //used to listen incoming data
        static NetworkStream ns; //used to send/receive data
        static BinaryFormatter bf = new BinaryFormatter(); //used to (de)serialize data
        static Boolean alive = true;

        static void Main(string[] args)
        {
            Console.Write("[?] Client/server : (C/S) ");

            //Servers gonna serv
            if (Console.ReadLine().ToUpper() == "S")
            {
                ListenTCP();
            }
            else
            {
                Console.Write("[?] Connect to : (IP:port) ");
                IPEndPoint iep = IPEndPointFromString(Console.ReadLine());

                Connect(iep);
            }

            //Start listening
            t = new Thread(new ThreadStart(ReceiveData));
            t.Start();

            Console.WriteLine("[HELP] commands : search, request, send, exit");
            
            
            //ready to input commands
            InputLoop();
            t.Abort();
        }


        //Listen for a client, get the network stream and stop listening
        static void ListenTCP()
        {
            TcpListener tl = new TcpListener(new IPEndPoint(IPAddress.Any, port));
            try
            {
                tl.Start();
                Console.WriteLine("[+] Listening " + tl.LocalEndpoint.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("[-] Listening failed : " + e.Message);
            }

            //waiting for a client
            TcpClient tc = tl.AcceptTcpClient();
            ns = tc.GetStream(); //Using NetworkStream

            Console.WriteLine("[+] Connected to " + tc.Client.RemoteEndPoint.ToString());

            tl.Stop(); //Only one client in this example
        }

        //connect to the server and get network stream
        static void Connect(IPEndPoint iep)
        {
            TcpClient tc = new TcpClient();

            try
            {
                tc.Connect(iep);
                ns = tc.GetStream(); //Using NetworkStream
                Console.WriteLine("[+] Connected to " + tc.Client.RemoteEndPoint.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("[-] Connect failed : " + iep.ToString() + " : " + e.Message);
            }
        }


        //looping for input commands
        //available commands : search, send, request
        static void InputLoop()
        {
            while (alive)
            {
                String txt = Console.ReadLine();
                String[] args = txt.Split(' ');

                if (args[0] == "search")
                {
                    if (args.Length == 2) SendData(new DataFileSearch { search = args[1] });
                    else Console.WriteLine("[-] Please provide an argument. Ex : search *.jpg");
                }
                else if (args[0] == "send")
                {
                    if (args.Length == 2)
                    {
                        if (File.Exists(incomingFolder + args[1]))
                        {
                            sFile f = new sFile(incomingFolder + args[1]);
                            if (f.size == 0) Console.WriteLine("[-] Specified file is empty");
                            else SendFile(f);
                        }
                        else Console.WriteLine("[-] File doesn't exist");
                    }
                    else Console.WriteLine("[-] Please provide an argument. Ex : send myfile.png");
                }
                else if (args[0] == "request")
                {
                    if (args.Length == 2) SendData(new DataFileRequest { request = args[1] });
                    else Console.WriteLine("[-] Please provide an argument. Ex : request yourfile.png");
                }
                else if (args[0] == "exit")
                {
                    alive = false;
                    Console.WriteLine("[+] Exiting, good bye");
                }
                else
                    Console.WriteLine("[-] Unknow command : " + args[0]);
            }
        }


        //just read a file and send it in packets
        static void SendFile(sFile sfile)
        {
            Int64 bytesRead = 0;
            Byte[] buffer;

            FileStream fs = new FileStream(Program.incomingFolder + sfile.name, FileMode.Open);

            while (bytesRead < fs.Length)
            {
                //adjust buffer size
                if (buffersize > fs.Length - bytesRead) buffersize = fs.Length - bytesRead;
                buffer = new Byte[buffersize];

                int x = fs.Read(buffer, 0, (int)buffersize);
                SendData(new DataFileSend { fileinfo = sfile, data = buffer, position = bytesRead });
                bytesRead += x;
            }

            fs.Close();
        }


        //receive packet and write it
        static void ReceiveFile(DataFileSend f)
        {
            FileStream fs = new FileStream(Program.incomingFolder + f.fileinfo.name, FileMode.OpenOrCreate);
            fs.Position = f.position;
            fs.Write(f.data, 0, f.data.Length);
            fs.Close();
        }


        static void SendData(Data data)
        {
            try
            {
                bf.Serialize(ns, data); //serialize data and send it
            }
            catch (Exception e)
            {
                Console.WriteLine("[-] Sent failed : " + data.type.ToString() + " " + e.Message);
                ns.Flush();
                alive = false;
            }
        }


        static void ReceiveData()
        {
            while (alive)
            {
                try
                {
                    Object o = bf.Deserialize(ns);
                    if (o is Data) HandleData((Data)o);
                }
                catch (Exception e)
                {
                    Console.WriteLine("[-] Received failed : " + e.Message);
                    ns.Flush();
                    alive = false;
                }
            }
        }


        // Processing Data objects received
        static void HandleData(Data data)
        {
            if (data.type == DataType.FileSearch)
            {
                DataFileSearch dfs = (DataFileSearch)data;
                Console.WriteLine("[+] Received search for " + dfs.search);

                String[] tabFile = Directory.GetFiles(Program.incomingFolder, dfs.search);
                for (int i = 0; i < tabFile.Length; i++) tabFile[i] = Path.GetFileName(tabFile[i]);

                SendData(new DataFileResults { results = tabFile });
            }
            else if (data.type == DataType.FileResults)
            {
                DataFileResults dfr = (DataFileResults)data;

                Console.WriteLine("[+] Received this results : ");
                for (int i = 0; i < dfr.results.Length; i++)
                {
                    Console.WriteLine("- " +dfr.results[i]);
                }
            }
            else if (data.type == DataType.FileRequest)
            {
                DataFileRequest dfr = (DataFileRequest)data;

                sFile f = new sFile(incomingFolder + dfr.request);
                SendFile(f);

                Console.WriteLine("[+] Peer requested for " + dfr.request + ", file sent");
            }
            else if (data.type == DataType.FileSend)
            {
                DataFileSend dfs = (DataFileSend)data;
                ReceiveFile(dfs);
                
                //BASIC check
                //since TCP is reliable, there should be no problem. But you can add checksum for integrity
                if (dfs.fileinfo.size == new FileInfo(incomingFolder + dfs.fileinfo.name).Length)
                    Console.WriteLine("[+] Received " + dfs.fileinfo.name);
            }
        }


        static IPEndPoint IPEndPointFromString(String endPoint)
        {
            String[] tab = endPoint.Split(':');
            IPAddress ip;
            int port;

            if (!IPAddress.TryParse(tab[0], out ip)) ip = IPAddress.Parse("0.0.0.0");
            if (tab.Length == 1 || !Int32.TryParse(tab[1], out port)) port = 0;

            return new IPEndPoint(ip, port);
        }


    }
}
