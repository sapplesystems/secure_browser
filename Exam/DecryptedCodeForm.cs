using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Runtime.CompilerServices.RuntimeHelpers;
using System.Security.Cryptography;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Button = System.Windows.Forms.Button;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolBar;
using TextBox = System.Windows.Forms.TextBox;

namespace Exam
{
    public partial class DecryptedCodeForm : Form
    {
        public DecryptedCodeForm()
        {
            InitializeComponent();
            //AddCloseButton();
        }

        private void DecryptedCodeForm_Load(object sender, EventArgs e)
        {
            this.Controls.Clear();

            this.WindowState = FormWindowState.Maximized;
            this.FormBorderStyle = FormBorderStyle.None;

            Panel centerPanel = new Panel();
            centerPanel.Size = new Size(400, 400);
            centerPanel.BackColor = Color.FromArgb(220, Color.White);
            centerPanel.BorderStyle = BorderStyle.None;

            centerPanel.Location = new Point(
            (this.ClientSize.Width - centerPanel.Width) / 2,
            (this.ClientSize.Height - centerPanel.Height) / 2
            );

            this.Controls.Add(centerPanel);
            centerPanel.BringToFront();

            Label label1 = new Label();
            label1.Text = "Enter The Code";
            label1.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            label1.ForeColor = Color.FromArgb(60, 60, 60);
            label1.AutoSize = true;
            label1.Location = new Point(70, 95);
            centerPanel.Controls.Add(label1);

            TextBox textcode = new TextBox();
            textcode.Font = new Font("Segoe UI", 12, FontStyle.Regular);
            textcode.Size = new Size(260, 35);
            textcode.BorderStyle = BorderStyle.FixedSingle;
            textcode.BackColor = Color.FromArgb(250, 250, 250);
            textcode.Location = new Point(70, 130);
            centerPanel.Controls.Add(textcode);

            Button btnGetData = new Button();
            btnGetData.Text = "Get Data";
            btnGetData.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            btnGetData.Size = new Size(260, 40);
            btnGetData.Location = new Point(70, 200);
            btnGetData.FlatStyle = FlatStyle.Flat;
            btnGetData.BackColor = Color.OrangeRed;
            btnGetData.ForeColor = Color.White;
            btnGetData.FlatAppearance.BorderSize = 0;

            btnGetData.Click += (s, ev) =>
            {
                string inputCode = textcode.Text.Trim();

                if (string.IsNullOrEmpty(inputCode))
                {
                    MessageBox.Show("Please enter the code!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    // ✅ Get Local IP Address
                    string localIP = Dns.GetHostAddresses(Dns.GetHostName())
                        .FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?
                        .ToString();

                    // ✅ Get MAC Address
                    string macAddress = NetworkInterface
                        .GetAllNetworkInterfaces()
                        .Where(nic => nic.OperationalStatus == OperationalStatus.Up)
                        .Select(nic => nic.GetPhysicalAddress().ToString())
                        .FirstOrDefault();

                    // ✅ Generate SHA256 Hash
                    string sha256Hash = GetSha256Hash(inputCode);

                    // ✅ Show Output
                    string result = $"Mac Address: {macAddress}\n" +
                                    $"Local IP: {localIP}\n" +
                                    $"SHA256 Hash: {sha256Hash}";

                    MessageBox.Show(result, "Decrypted Data", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            centerPanel.Controls.Add(btnGetData);


            Button btnClose = new Button();
            btnClose.Text = "X";
            btnClose.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            btnClose.ForeColor = Color.White;
            btnClose.BackColor = Color.Red;
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.FlatAppearance.BorderSize = 1;
            btnClose.Size = new Size(35, 35);
            btnClose.Location = new Point(this.ClientSize.Width - btnClose.Width - 10, 10);
            btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnClose.MouseEnter += (s, b) => btnClose.BackColor = Color.DarkRed;
            btnClose.MouseLeave += (s, b) => btnClose.BackColor = Color.Red;

            //btnClose.Click += (s, ev) => this.Close();
            btnClose.Click += (s, x) =>
                {
                    // 👇 Go back to Login form
                    this.Hide();
                    Login login = new Login();
                    login.Show();
                };
            this.Controls.Add(btnClose);
            btnClose.BringToFront();


        }
        private string GetSha256Hash(string text)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(text);
                byte[] hashBytes = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

        //private void AddCloseButton()
        //{
        //    // ❌ Close Button
        //    Button btnClose = new Button();
        //    btnClose.Text = "X";
        //    btnClose.Font = new Font("Segoe UI", 12, FontStyle.Bold);
        //    btnClose.ForeColor = Color.White;
        //    btnClose.BackColor = Color.Red;
        //    btnClose.FlatStyle = FlatStyle.Flat;
        //    btnClose.FlatAppearance.BorderSize = 0;
        //    btnClose.Size = new Size(40, 35);
        //    btnClose.Location = new Point(this.ClientSize.Width - btnClose.Width - 10, 10);
        //    btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        //    btnClose.Cursor = Cursors.Hand;

        //    btnClose.Click += (s, e) =>
        //    {
        //        // 👇 Go back to Login form
        //        this.Hide();
        //        Login login = new Login();
        //        login.Show();
        //    };

        //    this.Controls.Add(btnClose);
        //    btnClose.BringToFront();
        //}


    }
}

