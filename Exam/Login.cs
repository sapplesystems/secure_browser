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
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
            Func<Task> doLogin = async () =>
            {
                string userId = txtUser.Text.Trim();
                string password = txtPass.Text.Trim();

                bool apiReachable = false;
                //try
                //{
                //    using (HttpClient client = new HttpClient())
                //    {
                //        client.Timeout = TimeSpan.FromSeconds(3);
                //        var response = await client.GetAsync(Globals.default_login_url);
                //        apiReachable = response.IsSuccessStatusCode;
                //    }
                //}
                //catch
                //{
                //    apiReachable = false;
                //}

                if (apiReachable)
                {
                    try
                    {
                       

                        var requestData = new
                        {
                            user_id = userId,
                            password = password

                        };
                        using (HttpClient client = new HttpClient())
                        {
                            var json = JsonSerializer.Serialize(requestData);
                            var content = new StringContent(json, Encoding.UTF8, "application/json");
                        
                           
                            var response = await client.PostAsync(Globals.default_login_url, content);
                            string result = await response.Content.ReadAsStringAsync();

                            if (response.IsSuccessStatusCode)
                            {
                                var _json = JsonDocument.Parse(result).RootElement;
                                if (_json.GetProperty("success").GetBoolean())
                                {
                                    this.Hide();
                                    Exam exam = new Exam();
                                    exam.Show();
                                    Globals.KillDistractionApps();
                                    return;
                                }
                                else
                                {
                                    MessageBox.Show("Login failed: " + _json.GetProperty("message").GetString(), "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    return;
                                }
                            }
                            else
                            {
                                var _json = JsonDocument.Parse(result).RootElement;
                                MessageBox.Show("Error: " + _json.GetProperty("messages").GetProperty("error").GetString(), "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("API Error: " + ex.Message, "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                else
                {
                    // Hardcoded credentials
                    if ((userId.ToLower() == "admin" && password == "Admin123") ||
                        (userId.ToLower() == "demo" && password == "Demo123"))
                    {
                        this.Hide();
                        Exam exam = new Exam();
                        exam.Show();
                        Globals.KillDistractionApps();
                        return;
                    }
                    else if (userId == "Admin" && password == "Decrypt")
                    {
                        this.Hide();
                        DecryptedCodeForm decryptedCodeForm = new DecryptedCodeForm();
                        decryptedCodeForm.Show();
                        return;
                    }
                    else
                    {
                        MessageBox.Show("Invalid Username or Password!", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
            };

            // ===== Event Handlers =====
            btnLogin.Click += async (s, ev) => await doLogin();

            KeyEventHandler handleEnter = (s, ev) =>
            {
                if (ev.KeyCode == Keys.Enter)
                {
                    ev.SuppressKeyPress = true;
                    _ = doLogin();
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
    }
}
