using PassWordsCore;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualBasic;
using System.Windows.Forms;
using System.Data;

namespace PassWords2019
{
    public partial class Form1 : Form
    {
        Database db = new Database();

        Queue<Account> toadd = new Queue<Account>();
        Queue<Account> toupdate = new Queue<Account>();
        Queue<Account> todelete = new Queue<Account>();

        List<Account> accounts = new List<Account>();
        bool accountchanges = false;
        bool dorefresh = false;
        bool mustrefresh = false;
        int selecteditem = 0;
        bool dologoff = false;
        bool dodelete = false;
        bool userdidthat = false;

        bool changes = false;
        string current = "";

        public Form1()
        {
            InitializeComponent();
        }


        void loaddbs()
        {
            var dbs = Database.ListDatabases();

            if (dbs.Count() > 0)
            {
                cbSelectDB.Items.Clear();
                foreach (var db in dbs)
                {
                    cbSelectDB.Items.Add(db.Name);
                }
                cbSelectDB.SelectedIndex = 0;
                pnlPass.Show();
            }
            else
                pnlWTD.Show();
        }

        void loadaccounts()
        {
            lbItems.Items.Clear();
            if (accounts != null)
            {
                accounts = accounts.OrderBy(a => a.Title).ToList();
                foreach (var a in accounts)
                {
                    lbItems.Items.Add($"{a.Title}  [{a.Id}]");
                }
            }
            else
                accounts = new List<Account>();
        }

        void login()
        {
            if (cbSelectDB.Text != "Select Database" && !string.IsNullOrEmpty(tbHPW.Text))
            {
                current = cbSelectDB.Text;
                var result = db.Login(current, tbHPW.Text);
                if (result == LoginResult.Success)
                {
                    tbHPW.Clear();
                    pnlPass.Hide();
                    accounts = db.GetAccounts();
                    loadaccounts();
                    pnlContent.Show();
                    pnlTools.Show();
                    update.Start();
                    userdidthat = false;
                    cbTFA.Checked = false;
                    userdidthat = true;
                    pbExport.Enabled = true;
                }
                else if (result == LoginResult.Needs2FA)
                {
                    Clipboard.SetText(db.Get2FA());
                    //Handle 2fa
                    pnlTFAV.BringToFront();
                    pnlTFAV.Show();
                    tbTFAVC.Focus();
                    pbExport.Enabled = true;
                }
                else if (result == LoginResult.PasswordWrong)
                {
                    tbHPW.Clear();
                    MessageBox.Show("Password is wrong", "Main pass wrong", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                    MessageBox.Show("Try again later", "Anything went wrong", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
                MessageBox.Show("Please fill all fields", "Fill all fields", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }







        private void btnMin_Click(object sender, EventArgs e) => this.WindowState = FormWindowState.Minimized;
        private void btnMax_Click(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
                this.WindowState = FormWindowState.Maximized;
            else
                this.WindowState = FormWindowState.Normal;
        }

        private void btnExit_Click(object sender, EventArgs e) => Environment.Exit(0);

        private void Form1_Load(object sender, EventArgs e) => doload();

        private void doload()
        {
            cbSelectDB.Items.Clear();
            pnlPass.BringToFront();
            pnlPass.Location = new Point(ClientSize.Width / 2 - pnlPass.Size.Width / 2, ClientSize.Height / 2 - pnlPass.Size.Width / 2);
            pnlPass.Anchor = AnchorStyles.None;
            pnlWTD.Location = new Point(ClientSize.Width / 2 - pnlWTD.Size.Width / 2, ClientSize.Height / 2 - pnlWTD.Size.Width / 2);
            pnlWTD.Anchor = AnchorStyles.None;
            pnlCreate.Location = new Point(ClientSize.Width / 2 - pnlCreate.Size.Width / 2, ClientSize.Height / 2 - pnlCreate.Size.Width / 2);
            pnlCreate.Anchor = AnchorStyles.None;
            pnlStatus.Location = new Point(ClientSize.Width / 2 - pnlStatus.Size.Width / 2, ClientSize.Height / 2 - pnlStatus.Size.Width / 2);
            pnlStatus.Anchor = AnchorStyles.None;
            pnlImport.Location = new Point(ClientSize.Width / 2 - pnlImport.Size.Width / 2, ClientSize.Height / 2 - pnlImport.Size.Width / 2);
            pnlImport.Anchor = AnchorStyles.None;
            pnlGenerator.Location = new Point(ClientSize.Width / 2 - pnlGenerator.Size.Width / 2, ClientSize.Height / 2 - pnlGenerator.Size.Width / 2);
            pnlGenerator.Anchor = AnchorStyles.None;
            pnlExport.Location = new Point(ClientSize.Width / 2 - pnlExport.Size.Width / 2, ClientSize.Height / 2 - pnlExport.Size.Width / 2);
            pnlExport.Anchor = AnchorStyles.None;
            pnlSettings.Location = new Point(ClientSize.Width / 2 - pnlSettings.Size.Width / 2, ClientSize.Height / 2 - pnlSettings.Size.Width / 2);
            pnlSettings.Anchor = AnchorStyles.None;
            pnlQR.Location = new Point(ClientSize.Width / 2 - pnlQR.Size.Width / 2, ClientSize.Height / 2 - pnlQR.Size.Width / 2);
            pnlQR.Anchor = AnchorStyles.None;
            pnlGTFA.Location = new Point(ClientSize.Width / 2 - pnlGTFA.Size.Width / 2, ClientSize.Height / 2 - pnlGTFA.Size.Width / 2);
            pnlGTFA.Anchor = AnchorStyles.None;
            pnlTFAV.Location = new Point(ClientSize.Width / 2 - pnlTFAV.Size.Width / 2, ClientSize.Height / 2 - pnlTFAV.Size.Width / 2);
            pnlTFAV.Anchor = AnchorStyles.None;
            //Check if database file exist
            if (!System.IO.File.Exists(System.IO.Path.Combine(Application.StartupPath, "Passwords.epwd")))
                Database.EnsureCreated();

            loaddbs();
        }

        private void btnCloseDet_Click(object sender, EventArgs e)
        {
            //update
            if (selecteditem > 0 && changes)
            {
                changes = false;
                var a = accounts.First(f => f.Id == selecteditem);
                a.Title = tbN.Text;
                a.Username = tbUN.Text;
                a.Password = tbPW.Text;
                a.Description = tbD.Text;
                a.Type = cbCategory.Text;
                a.TwoFactorSecret = tbTFAS.Text;

                toupdate.Enqueue(new Account { Id = a.Id, Username = a.Username, Password = a.Password, Description = a.Description, Type = a.Type, Title = a.Title, TwoFactorSecret = a.TwoFactorSecret });
                a.Id = 0;
                loadaccounts();

                accountchanges = true;
                mustrefresh = true;
            }
            //Close details
            isadding = false;
            selecteditem = 0;
            pnlDetails.Hide();
            pbAddAccount.BackColor = Color.Empty;
            lbItems.ClearSelected();
        }

        private void pbShowPW_MouseDown(object sender, MouseEventArgs e) => tbPW.UseSystemPasswordChar = false;
        private void pbShowPW_MouseUp(object sender, MouseEventArgs e) => tbPW.UseSystemPasswordChar = true;
        private void pbShowHPW_MouseDown(object sender, MouseEventArgs e) => tbHPW.UseSystemPasswordChar = false;
        private void pbShowHPW_MouseUp(object sender, MouseEventArgs e) => tbHPW.UseSystemPasswordChar = true;
        private void pbCDShow_MouseUp(object sender, MouseEventArgs e) => tbCDPass.UseSystemPasswordChar = true;
        private void pbCDShow_MouseDown(object sender, MouseEventArgs e) => tbCDPass.UseSystemPasswordChar = false;
        private void pbShowOPW_MouseDown(object sender, MouseEventArgs e) => tbOldPW.UseSystemPasswordChar = false;
        private void pbShowOPW_MouseUp(object sender, MouseEventArgs e) => tbOldPW.UseSystemPasswordChar = true;
        private void pbShowNPW_MouseDown(object sender, MouseEventArgs e) => tbNewPW.UseSystemPasswordChar = false;
        private void pbShowNPW_MouseUp(object sender, MouseEventArgs e) => tbNewPW.UseSystemPasswordChar = true;


        private void pbClosePass_Click(object sender, EventArgs e)
        {
            pnlPass.Visible = false;
            pnlWTD.Visible = true;
        }

        private void btnCDClose_Click(object sender, EventArgs e)
        {
            pnlCreate.Visible = false;
            pnlWTD.Visible = true;
        }

        private void lblExit_Click(object sender, EventArgs e) => Application.Exit();

        private void pbWTDOpen_Click(object sender, EventArgs e)
        {
            pnlWTD.Visible = false;
            pnlPass.Visible = true;
            pnlPass.BringToFront();
            tbHPW.Focus();
        }

        private void lblWTDOpen_Click(object sender, EventArgs e)
        {
            pnlWTD.Visible = false;
            pnlPass.Visible = true;
            pnlPass.BringToFront();
            tbHPW.Focus();
        }

        private void btnNewDB_Click(object sender, EventArgs e)
        {
            pnlCreate.Visible = true;
            pnlCreate.BringToFront();
            pnlWTD.Visible = false;
        }

        private void lblNewDB_Click(object sender, EventArgs e)
        {
            pnlCreate.Visible = true;
            pnlCreate.BringToFront();
            pnlWTD.Visible = false;
        }

        private void lblImport_Click(object sender, EventArgs e)
        {
            pnlImport.BringToFront();
            pnlImport.Visible = true;
        }

        private void pbExitPasswords_Click(object sender, EventArgs e) => Application.Exit();

        private void btnLogin_Click(object sender, EventArgs e) => login();

        private void btnCreateDatabase_Click(object sender, EventArgs e)
        {
            if (tbCDName.Text != "Select Database" && !string.IsNullOrEmpty(tbCDPass.Text))
            {
                if (Database.CreateDB(tbCDName.Text, tbCDPass.Text))
                {
                    pnlCreate.Visible = false;
                    loaddbs();
                    pnlPass.Visible = true;
                    tbCDName.Clear();
                    tbCDPass.Clear();
                }
                else
                    MessageBox.Show("Anything went wrong", "An error occured", MessageBoxButtons.OK, MessageBoxIcon.Error);


            }
            else
                MessageBox.Show("Please fill all fields", "Fill all fields", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        }

        private void lbItems_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (lbItems.SelectedItem != null)
                {
                    selecteditem = Convert.ToInt32(lbItems.SelectedItem.ToString().Split('[')[1].Replace("]", ""));
                    if (selecteditem > 0)
                    {
                        var a = accounts.First(d => d.Id == selecteditem);
                        tbN.Text = a.Title;
                        tbUN.Text = a.Username;
                        tbPW.Text = a.Password;
                        tbD.Text = a.Description;
                        tbTFAS.Text = a.TwoFactorSecret;
                        cbCategory.Text = a.Type;
                        pnlDetails.Show();
                    }
                }
            }
            catch
            {
                MessageBox.Show("Title is not in the correct input format", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            
        }

        private void pbGenerator_Click(object sender, EventArgs e)
        {
            pnlGenerator.Visible = true;
            pnlGenerator.BringToFront();
        }

        private void pbDeleteDB_Click(object sender, EventArgs e)
        {
            if(MessageBox.Show("Are you sure you want to delete this DB?","Are you sure???", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                pbCloseDB.BackColor = Color.White;
                accountchanges = true;
                dologoff = true;
                dodelete = true;
            }
        }

        private void pbChangePW_Click(object sender, EventArgs e)
        {
            pnlCPW.Location = new Point(ClientSize.Width / 2 - pnlCPW.Size.Width / 2, ClientSize.Height / 2 - pnlCPW.Size.Width / 2);
            pnlCPW.Anchor = AnchorStyles.None;
            pnlCPW.BringToFront();
            pnlCPW.Visible = true;
        }


        bool isadding = false;

        private void pbAddAccount_Click(object sender, EventArgs e)
        {
            if (isadding)
            {
                if (tbN.Text != string.Empty)
                {
                    pbAddAccount.BackColor = Color.Empty;
                    pnlDetails.Hide();
                    isadding = false;
                    lbItems.Items.Clear();

                    var a = new Account
                    {
                        Title = tbN.Text,
                        Password = tbPW.Text,
                        Description = tbD.Text,
                        Type = cbCategory.Text,
                        Username = tbUN.Text,
                        TwoFactorSecret = tbTFAS.Text
                    };

                    toadd.Enqueue(a);
                    accounts.Add(a);

                    loadaccounts();
                    accountchanges = true;
                    mustrefresh = true;
                }
                else
                    MessageBox.Show("Please enter a title");

            }
            else
            {
                tbN.Clear();
                tbUN.Clear();
                tbPW.Clear();
                tbD.Clear();
                tbTFAS.Clear();
                cbCategory.SelectedIndex = 0;
                pnlDetails.Show();
                pbAddAccount.BackColor = Color.White;
                isadding = true;
                selecteditem = -1;
                lbItems.ClearSelected();
            }
        }

        private void pbDelAccount_Click(object sender, EventArgs e)
        {
            if (selecteditem > 0)
            {
                var a = accounts.First(f => f.Id == selecteditem);
                if (MessageBox.Show("Do you want to delete " + a.Title + "?", "Delete account?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    selecteditem = 0;
                    pnlDetails.Hide();
                    lbItems.Items.Clear();

                    accounts.Remove(a);
                    todelete.Enqueue(a);

                    loadaccounts();
                    accountchanges = true;
                }
            }
        }

        private void lblDetails_Click(object sender, EventArgs e)
        {

        }

        private void pbCloseDB_Click(object sender, EventArgs e)
        {
            pbCloseDB.BackColor = Color.White;
            accountchanges = true;
            dologoff = true;
        }

        private void pbCopy_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(tbPW.Text);
        }

        private void tbHPW_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                login();
        }

        private void pbInport_Click(object sender, EventArgs e)
        {
            pnlImport.BringToFront();
            pnlImport.Visible = true;
        }

        private void pbExport_Click(object sender, EventArgs e)
        {
            pnlExport.Visible = true;
            pnlExport.BringToFront();
        }

        string templname = "";

        private void pbImportF_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Passwords File (*.pwd)|*.pwd|Old Passwords file (*.pwdb)|*.pwdb";
            if (ofd.ShowDialog() == DialogResult.OK) {
                templname = ofd.FileName;
                btnRestore.Visible = true;
                tbImportName.Text = Path.GetFileNameWithoutExtension(ofd.FileName);
            }
        }

        private void btnRestore_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(templname))
            {
                if (templname.EndsWith(".pwd"))
                {
                    if (Database.Restore(templname,tbImportName.Text))
                    {
                        pnlImport.Hide();
                        loaddbs();
                        MessageBox.Show("Import succeed!!");
                        btnRestore.Visible = false;
                        pnlWTD.Hide();
                    }
                    else
                        MessageBox.Show("Import failed...");
                }
                else
                {
                    string input = Interaction.InputBox("Enter the main password of your database", "ENTER THE PASS", string.Empty, -1, -1);
                    if (input != string.Empty)
                    {
                        if (DecryptOLDPWDB(templname, input))
                            RestorePWDB(templname, input);
                        else
                            MessageBox.Show("Can't open the database. Please try again, check your password");
                    }
                }
            }
        }

        private void btnCloseImport_Click(object sender, EventArgs e)
        {
            pnlImport.Visible = false;
        }

        private void btnChangePW_Click(object sender, EventArgs e)
        {
            if (tbOldPW.Text != "" && tbNewPW.Text != "")
            {
                if (db.UpdatePassword(tbOldPW.Text, tbNewPW.Text))
                    MessageBox.Show("Password saved!");
                else
                    MessageBox.Show("Changing failed! Is your password wrong?");

                btnCloseCPW.PerformClick();

            }
            else
                MessageBox.Show("Please fill all fields!");
        }

        private void btnCloseCPW_Click(object sender, EventArgs e)
        {
            pnlCPW.Visible = false;
            tbOldPW.Clear();
            tbNewPW.Clear();
        }

        private void pbGen_Click(object sender, EventArgs e) =>
            tbGen.Text = Database.RandomString(Convert.ToInt32(tbAantal.Text), cbLetter.Checked, cbCapital.Checked, cbNumeric.Checked, cbSpecial.Checked);

        private void pbCopy2_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(tbGen.Text);
        }

        private void pbCloseGen_Click(object sender, EventArgs e)
        {
            pnlGenerator.Visible = false;
        }

        private void pbCloseExport_Click(object sender, EventArgs e)
        {
            pnlExport.Visible = false;
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Passwords File (*.pwd)|*.pwd";
            if (sfd.ShowDialog() == DialogResult.OK)
                if (db.Backup(sfd.FileName))
                    MessageBox.Show("Export finished!");
                else
                    MessageBox.Show("Export failed");

            pnlExport.Hide();
        }

        private void pbSettings_Click(object sender, EventArgs e)
        {
            pnlSettings.Visible = true;
            pnlSettings.BringToFront();
            lblVersion.Text = Application.ProductVersion.ToString();
        }

        private void pbSearch_Click(object sender, EventArgs e)
        {
            pnlSearch.Visible = true;
            pnlSearch.BringToFront();
            tbSearch.Focus();
        }

        private void cbSeachCat_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbSeachCat.Text == "All")
            {
                loadaccounts();
                pnlSearch.Hide();
            }   
            else
            {
                var results = accounts.Where(a => a.Type == cbSeachCat.Text);
                var ba = accounts;
                accounts = results.ToList();
                loadaccounts();
                accounts = ba;
                pnlSearch.Hide();
            }
            
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            var results = accounts.Where(a => a.Title.IndexOf(tbSearch.Text, StringComparison.CurrentCultureIgnoreCase) >= 0);
            var ba = accounts;
            accounts = results.ToList();
            loadaccounts();
            accounts = ba;
            pnlSearch.Hide();
            tbSearch.Clear();
        }

        private void tbSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                var results = accounts.Where(a => a.Title.IndexOf(tbSearch.Text, StringComparison.CurrentCultureIgnoreCase) >= 0);
                var ba = accounts;
                accounts = results.ToList();
                loadaccounts();
                accounts = ba;
                pnlSearch.Hide();
                tbSearch.Clear();
            }
        }

        private void tbSearch_Enter(object sender, EventArgs e) => tbSearch.Clear();

        private void tbSearch_Leave(object sender, EventArgs e) => tbSearch.Text = "Search...";

        private void pbCloseSearch_Click(object sender, EventArgs e) => pnlSearch.Visible = false;

        private void pbCloseSettings_Click(object sender, EventArgs e) => pnlSettings.Visible = false;

        private void pbResetFilter_Click(object sender, EventArgs e)
        {
            loadaccounts();
            cbSeachCat.SelectedIndex = 0;
            pnlSearch.Hide();
            tbSearch.Clear();
        }


        private void cbCategory_SelectedIndexChanged(object sender, EventArgs e) { }
        private void tbN_TextChanged(object sender, EventArgs e) { }
        private void tbUN_TextChanged(object sender, EventArgs e) { }
        private void tbPW_TextChanged(object sender, EventArgs e) { }
        private void tbD_TextChanged(object sender, EventArgs e) { }


        private void backupper_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            accountchanges = false;

            while (toadd.Count() > 0)
                db.Add(toadd.Dequeue());

            while (toupdate.Count() > 0)
                db.Update(toupdate.Dequeue());

            while (todelete.Count() > 0)
                db.Delete(todelete.Dequeue());

            if (mustrefresh)
            {
                accounts = db.GetAccounts();
                dorefresh = true;
                mustrefresh = false;
            }
            
        }

        
        //Backup optie: Wanneer de applicatie niet op de juiste wijze wordt afgesloten
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
           
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case 0x84:
                    base.WndProc(ref m);
                    if ((int)m.Result == 0x1)
                        m.Result = (IntPtr)0x2;
                    return;
            }

            base.WndProc(ref m);
        }

        private void Update_Tick(object sender, EventArgs e)
        {
            if (accountchanges && !background.IsBusy)
                background.RunWorkerAsync();

            if (dorefresh)
            {
                dorefresh = false;
                loadaccounts();
            }

            if (dologoff)
            {
                if (!background.IsBusy)
                {
                    dologoff = false;
                    pbCloseDB.BackColor = Color.Empty;
                    update.Stop();
                    db.Logout();
                    pnlContent.Hide();
                    pnlDetails.Hide();
                    pnlTools.Hide();
                    pnlWTD.Show();
                    pbExport.Enabled = false;

                    if (dodelete)
                    {
                        Database.DeleteDB(current);
                        pnlWTD.Hide();
                        loaddbs();
                    }
                }
            }
        }

        private void CbCategory_Click(object sender, EventArgs e) => changes = true;

        private void TbN_KeyUp(object sender, KeyEventArgs e) => changes = true;

        private void TbUN_KeyUp(object sender, KeyEventArgs e) => changes = true;

        private void TbPW_KeyUp(object sender, KeyEventArgs e) => changes = true;

        private void TbD_KeyUp(object sender, KeyEventArgs e) => changes = true;
        private void TbTFAS_KeyUp(object sender, KeyEventArgs e) => changes = true;

        private void BtnCloseQR_Click(object sender, EventArgs e) => pnlQR.Visible = false;

        private void CbTFA_CheckedChanged(object sender, EventArgs e)
        {
            if (userdidthat)
            {
                if (cbTFA.Checked)
                {
                    db.Add2FA();
                    string secret = db.Get2FA();
                    string input = $"otpauth://totp/PassWords:{current}?secret={secret}&issuer=Passwords";
                    Bitmap img = new QRCode(new QRCodeGenerator().CreateQrCode(input, QRCodeGenerator.ECCLevel.Q)).GetGraphic(20);
                    pbQrCode.BackgroundImage = img;
                    pbQrCode.BackgroundImageLayout = ImageLayout.Stretch;
                    lblCode.Text = secret;
                    pnlQR.Show();
                    pnlQR.BringToFront();
                }
                else
                {
                    db.Remove2FA();
                }
            }
        }

        private void PbGCode_Click(object sender, EventArgs e)
        {
            if(tbTFAS.TextLength > 10)
            {
                timer2FA.Start();
                pnlGTFA.BringToFront();
                pnlGTFA.Show();
                lblGTFACode.Text = Database.GenerateCode(tbTFAS.Text);
            }
        }

        private void BtnGTAClose_Click(object sender, EventArgs e) { pnlGTFA.Hide(); timer2FA.Stop(); }

        bool prev = false;
        private void Timer2FA_Tick(object sender, EventArgs e)
        {
            int s = DateTime.Now.Second;
            if(s < 31)
            {
                if (prev == false)
                    lblGTFACode.Text = Database.GenerateCode(tbTFAS.Text);

                prev = true;
                pbrTFA.Value = 30 - s;
            }
            else
            {
                if (prev)
                    lblGTFACode.Text = Database.GenerateCode(tbTFAS.Text);
                prev = false;
                pbrTFA.Value = 30 - (s-30);
            }
            
        }

        private void BtnTFAVC_Click(object sender, EventArgs e) => pnlTFAV.Hide();

        private void TbTFAVC_KeyUp(object sender, KeyEventArgs e)
        {
            lbltfawrong.Hide();
            if (tbTFAVC.TextLength > 5)
            {
                if (db.Login2FA(tbTFAVC.Text))
                {
                    pnlTFAV.Hide();
                    tbHPW.Clear();
                    pnlPass.Hide();
                    accounts = db.GetAccounts();
                    loadaccounts();
                    pnlContent.Show();
                    pnlTools.Show();
                    update.Start();
                    userdidthat = false;
                    cbTFA.Checked = true;
                    userdidthat = true;
                    pbExport.Enabled = true;
                }
                else
                    lbltfawrong.Show();
                tbTFAVC.Clear();
            }
        }

        void handle2fa(bool tfa)
        {
            userdidthat = false;
            if (tfa)
            {
                btncopy2fa.Visible = true;
                cbTFA.Checked = true;
            }
            else
            {
                btncopy2fa.Visible = false;
                cbTFA.Checked = false;
            }
            userdidthat = true;
        }

        private void PbGTFAtoclip_Click(object sender, EventArgs e) => Clipboard.SetText(lblGTFACode.Text);
        private void BtnGTFAtoclip_Click(object sender, EventArgs e) => Clipboard.SetText(lblCode.Text);
        private void Btncopy2fa_Click(object sender, EventArgs e) => Clipboard.SetText(db.Get2FA());


        string connectionstring = "Data Source=" + Application.StartupPath + "\\Default.pwdb;Version=3;";
        private void RestorePWDB(string file, string pass)
        {
            List<Account> newaccounts = new List<Account>();
            connectionstring = "Data Source=" + Application.StartupPath + "\\" + Path.GetFileNameWithoutExtension(file) + ".pwdb.temp;Version=3;";
            SQLiteConnection connection = new SQLiteConnection(connectionstring);
            SQLiteCommand command = new SQLiteCommand("SELECT * FROM Data", connection);
            connection.Open();
            
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                newaccounts.Add(new Account
                {
                    Title = reader["title"].ToString(),
                    Username = reader["username"].ToString(),
                    Password = reader["password"].ToString(),
                    Description = reader["description"].ToString(),
                    Type = reader["catagory"].ToString(),
                    TwoFactorSecret = ""
                });
            }
            connection.Close();

            MessageBox.Show("Loaded data successfully. " + newaccounts.Count().ToString() + " new accounts!");

            string name = (tbImportName.Text != "") ? tbImportName.Text : "_" + Path.GetFileNameWithoutExtension(file);
            Database.CreateDB(name, pass);
            Database db2 = new Database();
            db2.Login(name, pass);
            foreach (var account in newaccounts)
            {
                db2.Add(account);
            }
            db2.Logout();
            MessageBox.Show("Success!! Application will now exit","DONE",MessageBoxButtons.OK,MessageBoxIcon.Information);
            Application.Exit();
        }

        private bool DecryptOLDPWDB(string inputfile, string password)
        {
            string outputfile = Application.StartupPath + "\\" + Path.GetFileNameWithoutExtension(inputfile) + ".pwdb.temp";
            string salt = "PassWordsSalt";

            try
            {
                Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(password, Encoding.Unicode.GetBytes(salt));
                byte[] bkey = key.GetBytes(32);

                using (FileStream fsC = new FileStream(inputfile, FileMode.Open))
                {
                    using (var algoritm = Aes.Create())
                    {
                        byte[] bsalt = new byte[algoritm.IV.Length];

                        fsC.Read(bsalt, 0, bsalt.Length);
                        algoritm.Key = bkey;
                        algoritm.IV = bsalt;

                        using (CryptoStream cs = new CryptoStream(fsC, algoritm.CreateDecryptor(), CryptoStreamMode.Read))
                        {
                            using (FileStream fsOut = new FileStream(outputfile, FileMode.Create))
                            {
                                int read;
                                byte[] buffer = new byte[1048576];

                                while ((read = cs.Read(buffer, 0, buffer.Length)) > 0)
                                {
                                    fsOut.Write(buffer, 0, read);
                                }

                                try
                                {
                                    File.Delete(inputfile);

                                }
                                catch
                                {

                                }
                                return true;
                            }
                        }

                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR===================ERROR" + Environment.NewLine + ex.ToString());
                return false;
            }
        }
    }
}
