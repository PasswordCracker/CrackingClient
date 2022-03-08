using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace ClientCracking
{
    class Program
    {  
        public static Dictionary<string, string> passwords = new Dictionary<string, string>();
        public static List<string> chunk = new List<string>();
        static List<string> results = new List<string>();
        static void Main(string[] args)
        {
            Console.WriteLine("Client started");

            //Cracking cracker = new Cracking();
            //cracker.RunCracking();

            TcpClient socket = new TcpClient("localhost", 21);
            socket.ReceiveTimeout = 50000;
            socket.SendTimeout = 50000;
            NetworkStream ns = socket.GetStream();
            StreamReader reader = new StreamReader(ns);
            StreamWriter writer = new StreamWriter(ns);
            Console.WriteLine("Client connected");
            Console.WriteLine("Type ready to start.");

            //waiting until the user types ready
            string message = Console.ReadLine();
            while (message.ToUpper()!="READY")
            {
                Console.WriteLine("Unknown command, type 'ready' to start");
                message = Console.ReadLine();
            }
            //sends the ready message
            writer.WriteLine(message);
            writer.Flush();

            string response = reader.ReadLine();

            //waiting for server to say ready
            while (response.ToUpper()!="READY")
            {
                Console.WriteLine(response);
                System.Threading.Thread.Sleep(1000);

                response = reader.ReadLine();
            }
            Console.WriteLine("Server is ready.");

            //receives the passwords
            response = reader.ReadLine();
            passwords = JsonSerializer.Deserialize<Dictionary<string, string>>(response);
            Console.WriteLine("Received the passwords");

            //receives the chunk of the dictionary
            response = reader.ReadLine();
            while (response.ToLower() != "finished")
            {
                chunk = JsonSerializer.Deserialize<List<string>>(response);

                //cracking the passwords
                Cracking cracker = new Cracking();
                Dictionary<string, string> CrackedPasswords = cracker.RunCracking();

                //sending results
                if (CrackedPasswords.Count > 0)
                {
                    string jsonresult = JsonSerializer.Serialize(CrackedPasswords);
                    writer.WriteLine(jsonresult);
                    writer.Flush();
                }
                else
                {
                    writer.WriteLine("empty");
                    writer.Flush();
                }

                response = reader.ReadLine();
            }

            //Task finished
            Console.WriteLine("Finished");
            
        }
    }
}
