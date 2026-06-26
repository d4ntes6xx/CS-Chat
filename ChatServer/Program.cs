using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace ChatServer
{
    internal static class Program
    {
        private static readonly List<TcpClient> clients = new List<TcpClient>();
        private static readonly object clientLock = new object();
        private static MainWindow mainWindow;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            mainWindow = new MainWindow();
            Application.Run(mainWindow);
        }

        public static void StartServer(int port)
        {
            Thread serverThread = new Thread(() => RunServer(port));
            serverThread.IsBackground = true;
            serverThread.Start();
        }

        static void RunServer(int port)
        {
            IPAddress address = IPAddress.Any;
            TcpListener listener = new TcpListener(address, port);
            listener.Start();
            mainWindow.AddLog($"Сервер запущен на порту {port}");

            while (true)
            {
                try
                {
                    TcpClient client = listener.AcceptTcpClient();
                    lock (clientLock)
                    {
                        clients.Add(client);
                    }
                    string clientAddress = client.Client.RemoteEndPoint.ToString();
                    mainWindow.AddLog($"[{clientAddress}] подключился");
                    Thread clientThread = new Thread(HandleClient);
                    clientThread.Start(client);
                }
                catch (Exception ex)
                {
                    mainWindow.AddLog($"Ошибка: {ex.Message}");
                }
            }
        }

        static void HandleClient(object obj)
        {
            TcpClient client = (TcpClient)obj;
            string clientAddress = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
            byte[] buffer = new byte[4096];

            try
            {
                NetworkStream stream = client.GetStream();

                while (true)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    mainWindow.AddLog($"[{clientAddress}] получил: {message}");

                    string formattedMessage = $"[{clientAddress}]: {message}";
                    SendMessageToAll(formattedMessage, client);
                }
            }
            catch (Exception ex)
            {
                mainWindow.AddLog($"[{clientAddress}] ошибка: {ex.Message}");
            }
            finally
            {
                lock (clientLock)
                {
                    clients.Remove(client);
                }
                mainWindow.AddLog($"[{clientAddress}] отключился");
                try
                {
                    if (client.Connected)
                    {
                        client.Close();
                    }
                }
                catch { }
            }
        }

        static void SendMessageToAll(string message, TcpClient sender)
        {
            byte[] answer = Encoding.UTF8.GetBytes(message + "\n");

            List<TcpClient> clientsCopy;
            lock (clientLock)
            {
                clientsCopy = new List<TcpClient>(clients);
            }

            foreach (var client in clientsCopy)
            {
                if (client == sender) continue;

                try
                {
                    if (client.Connected)
                    {
                        NetworkStream stream = client.GetStream();
                        stream.Write(answer, 0, answer.Length);
                        stream.Flush();
                    }
                }
                catch (Exception ex)
                {
                    mainWindow.AddLog($"Ошибка отправки: {ex.Message}");
                }
            }
        }
    }
}