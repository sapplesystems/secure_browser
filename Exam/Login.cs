using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Exam
{
    public partial class Login : Form
    {

        public Login()
        {
            InitializeComponent();
        }
        private void Login_Load_1(object sender, EventArgs e)
        {
            this.Controls.Clear();

            // ===== Background =====
            PictureBox background = new PictureBox
            {
                BackColor = Color.White,
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.StretchImage
            };
            this.Controls.Add(background);

            // ===== Form Settings =====
            this.Text = "Login";
            this.WindowState = FormWindowState.Maximized;
            this.FormBorderStyle = FormBorderStyle.None;

            // ===== Left Image =====
            string contentPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", "student.png");
            PictureBox personPic = new PictureBox
            {
                Image = Image.FromFile(contentPath),
                BackColor = Color.White,
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(450, 550)
            };
            this.Controls.Add(personPic);
            personPic.BringToFront();

            // ===== Center Login Panel =====
            Panel centerPanel = new Panel
            {
                Size = new Size(400, 360),
                BackColor = Color.FromArgb(220, Color.White),
                BorderStyle = BorderStyle.None
            };
            this.Controls.Add(centerPanel);
            centerPanel.BringToFront();

            // ===== Title =====
            Label lblTitle = new Label
            {
                Text = "Welcome Back!",
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = Color.FromArgb(45, 45, 45),
                AutoSize = true,
                Location = new Point(90, 30)
            };
            centerPanel.Controls.Add(lblTitle);

            // ===== Username =====
            Label lblUser = new Label
            {
                Text = "Username",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60),
                AutoSize = true,
                Location = new Point(70, 85)
            };
            centerPanel.Controls.Add(lblUser);

            TextBox txtUser = new TextBox
            {
                Font = new Font("Segoe UI", 12, FontStyle.Regular),
                Size = new Size(260, 35),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(250, 250, 250),
                Location = new Point(70, 120)
            };
            centerPanel.Controls.Add(txtUser);

            // ===== Password =====
            Label lblPass = new Label
            {
                Text = "Password",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60),
                AutoSize = true,
                Location = new Point(70, 165)
            };
            centerPanel.Controls.Add(lblPass);

            TextBox txtPass = new TextBox
            {
                Font = new Font("Segoe UI", 12, FontStyle.Regular),
                Size = new Size(260, 35),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(250, 250, 250),
                PasswordChar = '●',
                Location = new Point(70, 200)
            };
            centerPanel.Controls.Add(txtPass);

            // ===== Eye Icon =====
            Label lblEye = new Label
            {
                Text = "👁️",
                Font = new Font("Segoe UI Emoji", 12),
                AutoSize = true,
                Cursor = Cursors.Hand,
                Location = new Point(txtPass.Right - 33, txtPass.Top + 5)
            };
            centerPanel.Controls.Add(lblEye);
            lblEye.BringToFront();

            bool isPasswordVisible = false;
            lblEye.Click += (s, ev) =>
            {
                isPasswordVisible = !isPasswordVisible;
                txtPass.PasswordChar = isPasswordVisible ? '\0' : '●';
            };

            // ===== Login Button =====
            Button btnLogin = new Button
            {
                Text = "Login",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Size = new Size(260, 40),
                Location = new Point(70, 255),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.OrangeRed,
                ForeColor = Color.White
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            centerPanel.Controls.Add(btnLogin);

            // ===== Login Logic =====
            Action doLogin = () =>
            {
                if (txtUser.Text == "Admin" && txtPass.Text == "Admin")
                {
                    this.Hide();
                    Exam exam = new Exam();
                    exam.Show();
                    KillDistractionApps();
                }
                else if (txtUser.Text == "Admin" && txtPass.Text == "Decrypt")
                {
                    this.Hide();
                    DecryptedCodeForm decryptedCodeForm = new DecryptedCodeForm();
                    decryptedCodeForm.Show();
                }
                else
                {
                    MessageBox.Show("Invalid Username or Password!", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnLogin.Click += (s, ev) => doLogin();

            // Handle Enter key for both username and password textboxes
            KeyEventHandler handleEnter = (s, ev) =>
            {
                if (ev.KeyCode == Keys.Enter)
                {
                    ev.SuppressKeyPress = true; // Prevent system beep
                    doLogin();
                }
            };

            txtUser.KeyDown += handleEnter;
            txtPass.KeyDown += handleEnter;


            // ===== Instructions =====
            Panel instructionPanel = new Panel
            {
                Width = 800,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.FromArgb(255, 250, 240),
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(instructionPanel);
            instructionPanel.BringToFront();

            Label lblInstructions = new Label
            {
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                AutoSize = true,
                MaximumSize = new Size(760, 0),
                Location = new Point(20, 15),
                Text =
                "🔒 Secure Browser Instructions\r\n\r\n" +
                "👉 Use the Secure Browser Only\r\n" +
                "All courses and assessments must be accessed exclusively through the Secure Browser. Other browsers or applications are not permitted.\r\n\r\n" +
                "🧩 Starting an Assessment\r\n" +
                "When you begin an assessment, you will be prompted to enter a verification code displayed at the top of your screen.\r\n\r\n" +
                "⌨️ Entering the Code\r\n" +
                "1. You may type the code manually or paste it using Ctrl + V.\r\n" +
                "2. Do not modify or alter the code in any way.\r\n" +
                "3. Once the code is verified, you will be able to proceed with your assessment.\r\n\r\n" +
                "⚠️ Important\r\n" +
                "Closing or minimizing the Secure Browser, switching applications, or attempting to open other windows will end your session immediately."
            };
            instructionPanel.Controls.Add(lblInstructions);

            // ===== Position panels dynamically =====
            int shiftRight = 400;

            Action layout = () =>
            {
                centerPanel.Location = new Point(
                    (this.ClientSize.Width - centerPanel.Width) / 2 + shiftRight,
                    (int)(this.ClientSize.Height * 0.35) - centerPanel.Height / 2
                );

                instructionPanel.Location = new Point(
                    (this.ClientSize.Width - instructionPanel.Width) / 2,
                    centerPanel.Bottom + 60
                );

                personPic.Location = new Point(
                    centerPanel.Left - personPic.Width - 320,
                    centerPanel.Top + (centerPanel.Height - personPic.Height) / 2
                );
            };

            layout();
            this.Resize += (s, ev) => layout();
        }
        private void KillDistractionApps()
        {
            // Extended list of distraction / communication / dev tools
            string[] apps =
            {
        // Chat / Communication
        "whatsapp", "telegram", "discord", "skype", "teams", "slack", "zoom", "viber", "signal",
        "line", "wechat", "messenger", "yourphone",

        // Social media & browsers
        "chrome", "firefox", "opera", "brave", "vivaldi",

        // Dev / API tools
        "postman", "insomnia", "fiddler", "charles", "wireshark",

        // File sharing / remote control
        "anydesk", "teamviewer", "ultraviewer", "rdpclip",

        // Media / entertainment
        "spotify", "vlc", "netflix", "itunes", "music", "videos",

        // Miscellaneous distractions
        "notion", "obsidian", "todoist", "evernote"
    };

            foreach (var p in Process.GetProcesses())
            {
                try
                {
                    if (apps.Any(a => p.ProcessName.ToLower().Contains(a)))
                    {
                        p.Kill();
                    }
                }
                catch
                {
                    // Ignored intentionally (some system processes may not be killable)
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == "Admin" && textBox2.Text == "Admin")
            {
                this.Hide();
                Exam exam = new Exam();
                exam.BringToFront();
                exam.Show();
            }
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                if (!string.IsNullOrEmpty(textBox1.Text) && !string.IsNullOrEmpty(textBox2.Text))
                {
                    if (textBox1.Text == "Admin" && textBox2.Text == "Admin")
                    {
                        this.Hide();
                        Exam exam = new Exam();
                        exam.BringToFront();
                        exam.Show();
                    }
                }
            }
        }

        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                if (!string.IsNullOrEmpty(textBox1.Text) && !string.IsNullOrEmpty(textBox2.Text))
                {
                    if (textBox1.Text == "Admin" && textBox2.Text == "Admin")
                    {
                        this.Hide();
                        Exam exam = new Exam();
                        exam.BringToFront();
                        exam.Show();
                    }
                }
            }
        }
    }
}
