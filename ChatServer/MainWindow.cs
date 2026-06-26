using System;
using System.Windows.Forms;

namespace ChatServer
{
    public partial class MainWindow : Form
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnStartServer_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtPort.Text))
            {
                MessageBox.Show("Введите порт!");
                return;
            }

            if (!int.TryParse(txtPort.Text, out int port))
            {
                MessageBox.Show("Некорректный порт!");
                return;
            }

            btnStartServer.Enabled = false;
            txtPort.Enabled = false;
            Program.StartServer(port);
        }

        public void AddLog(string message)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action<string>(AddLog), message);
                return;
            }

            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            txtLog.ScrollToCaret();
        }
    }
}