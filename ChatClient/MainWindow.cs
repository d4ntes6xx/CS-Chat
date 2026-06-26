using System;
using System.Drawing;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace ChatClient
{
    public partial class MainWindow : Form
    {
        private TcpClient client;
        private NetworkStream stream;
        private Thread receiveThread;
        private bool isConnected = false;
        private string username;

        public MainWindow()
        {
            InitializeComponent();
            txtUserMessage.Enabled = false;
            btnSendMessage.Enabled = false;
            txtChatMessages.ReadOnly = true;
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (isConnected)
            {
                Disconnect();
                return;
            }

            if (string.IsNullOrEmpty(txtServerAddress.Text) ||
                string.IsNullOrEmpty(txtServerPort.Text) ||
                string.IsNullOrEmpty(txtLogin.Text))
            {
                MessageBox.Show("Заполните все поля!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(txtServerPort.Text, out int port))
            {
                MessageBox.Show("Некорректный порт!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                client = new TcpClient(txtServerAddress.Text, port);
                stream = client.GetStream();
                username = txtLogin.Text;

                receiveThread = new Thread(ReceiveMessages);
                receiveThread.IsBackground = true;
                receiveThread.Start();

                isConnected = true;
                btnConnect.Text = "Отключиться";
                txtServerAddress.Enabled = false;
                txtServerPort.Enabled = false;
                txtLogin.Enabled = false;
                txtPassword.Enabled = false;
                txtUserMessage.Enabled = true;
                btnSendMessage.Enabled = true;

                AddMessage("Система", "Подключено к серверу!", Color.Green);
                SendMessage($"Пользователь {username} вошел в чат");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSendMessage_Click(object sender, EventArgs e)
        {
            SendMessage();
        }

        private void txtUserMessage_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter && !string.IsNullOrEmpty(txtUserMessage.Text))
            {
                SendMessage();
                e.Handled = true;
            }
        }

        private void SendMessage(string message = null)
        {
            if (!isConnected || client == null || !client.Connected)
            {
                MessageBox.Show("Нет подключения к серверу!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string text = message ?? txtUserMessage.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            try
            {
                byte[] data = Encoding.UTF8.GetBytes(text);
                stream.Write(data, 0, data.Length);
                stream.Flush();

                if (message == null)
                {
                    AddMessage("Я", text, Color.Blue);
                    txtUserMessage.Clear();
                }
            }
            catch (Exception ex)
            {
                AddMessage("Ошибка", $"Не удалось отправить: {ex.Message}", Color.Red);
                Disconnect();
            }
        }

        private void ReceiveMessages()
        {
            byte[] buffer = new byte[4096];

            try
            {
                while (client.Connected)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    string[] messages = message.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var msg in messages)
                    {
                        if (msg.Contains("вошел в чат"))
                            AddMessage("Система", msg, Color.Green);
                        else if (msg.Contains("вышел из чата") || msg.Contains("отключился"))
                            AddMessage("Система", msg, Color.Orange);
                        else
                            AddMessage("Сообщение", msg, Color.Black);
                    }
                }
            }
            catch
            {
                // Соединение закрыто
            }
            finally
            {
                if (isConnected)
                    Disconnect();
            }
        }

        private void AddMessage(string type, string text, Color color)
        {
            if (txtChatMessages.InvokeRequired)
            {
                txtChatMessages.Invoke(new Action<string, string, Color>(AddMessage), type, text, color);
                return;
            }

            if (type == "Сообщение" && color == Color.Black)
            {
                Random rand = new Random();
                color = Color.FromArgb(rand.Next(50, 200), rand.Next(50, 200), rand.Next(50, 200));
            }

            txtChatMessages.SelectionStart = txtChatMessages.TextLength;
            txtChatMessages.SelectionLength = 0;
            txtChatMessages.SelectionColor = Color.Gray;
            txtChatMessages.AppendText($"[{DateTime.Now:HH:mm:ss}] ");

            txtChatMessages.SelectionColor = color;
            txtChatMessages.AppendText($"{text}{Environment.NewLine}");

            txtChatMessages.ScrollToCaret();
        }

        private void Disconnect()
        {
            if (isConnected)
            {
                isConnected = false;
                try
                {
                    if (client != null && client.Connected)
                    {
                        SendMessage($"Пользователь {username} вышел из чата");
                        stream?.Close();
                        client?.Close();
                    }
                }
                catch { }

                receiveThread = null;
                stream = null;
                client = null;

                btnConnect.Text = "Подключиться";
                txtServerAddress.Enabled = true;
                txtServerPort.Enabled = true;
                txtLogin.Enabled = true;
                txtPassword.Enabled = true;
                txtUserMessage.Enabled = false;
                btnSendMessage.Enabled = false;

                AddMessage("Система", "Отключено от сервера", Color.Red);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Disconnect();
            base.OnFormClosing(e);
        }
    }
}