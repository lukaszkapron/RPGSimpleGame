namespace RPGWinForms
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;
        private Label lblPlayer;
        private Label lblMyHp;
        private Label lblLevel;
        private Label lblClass;
        private Label lblTurn;
        private Label lblEnemy;
        private Label lblEnemyHp;
        private Label lblEnemyLevel;
        private Label lblEnemyClass;
        private Button btnAttack;
        private Button btnHeal;
        private ListBox lstLog;

        private Label lblServer;
        private Button btnMoveServer;
        private Label lblWins;
        private Label lblLosses;
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            lblPlayer = new Label();
            lblMyHp = new Label();
            lblLevel = new Label();
            lblClass = new Label();
            lblTurn = new Label();
            lblEnemy = new Label();
            lblEnemyHp = new Label();
            lblEnemyLevel = new Label();
            lblEnemyClass = new Label();
            btnAttack = new Button();
            btnHeal = new Button();
            lstLog = new ListBox();
            lblServer = new Label();
            btnMoveServer = new Button();
            lblWins = new Label();
            lblLosses = new Label();
            SuspendLayout();
            // 
            // lblPlayer
            // 
            lblPlayer.AutoSize = true;
            lblPlayer.Location = new Point(20, 20);
            lblPlayer.Name = "lblPlayer";
            lblPlayer.Size = new Size(49, 20);
            lblPlayer.TabIndex = 0;
            lblPlayer.Text = "Gracz:";
            // 
            // lblMyHp
            // 
            lblMyHp.AutoSize = true;
            lblMyHp.Location = new Point(20, 45);
            lblMyHp.Name = "lblMyHp";
            lblMyHp.Size = new Size(31, 20);
            lblMyHp.TabIndex = 1;
            lblMyHp.Text = "HP:";
            // 
            // lblLevel
            // 
            lblLevel.AutoSize = true;
            lblLevel.Location = new Point(20, 70);
            lblLevel.Name = "lblLevel";
            lblLevel.Size = new Size(29, 20);
            lblLevel.TabIndex = 2;
            lblLevel.Text = "Lvl:";
            // 
            // lblClass
            // 
            lblClass.AutoSize = true;
            lblClass.Location = new Point(20, 95);
            lblClass.Name = "lblClass";
            lblClass.Size = new Size(47, 20);
            lblClass.TabIndex = 3;
            lblClass.Text = "Klasa:";
            // 
            // lblTurn
            // 
            lblTurn.AutoSize = true;
            lblTurn.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblTurn.Location = new Point(20, 120);
            lblTurn.Name = "lblTurn";
            lblTurn.Size = new Size(50, 23);
            lblTurn.TabIndex = 4;
            lblTurn.Text = "Tura:";
            // 
            // lblEnemy
            // 
            lblEnemy.AutoSize = true;
            lblEnemy.Location = new Point(250, 20);
            lblEnemy.Name = "lblEnemy";
            lblEnemy.Size = new Size(81, 20);
            lblEnemy.TabIndex = 6;
            lblEnemy.Text = "Przeciwnik:";
            // 
            // lblEnemyHp
            // 
            lblEnemyHp.AutoSize = true;
            lblEnemyHp.Location = new Point(250, 45);
            lblEnemyHp.Name = "lblEnemyHp";
            lblEnemyHp.Size = new Size(31, 20);
            lblEnemyHp.TabIndex = 7;
            lblEnemyHp.Text = "HP:";
            // 
            // lblEnemyLevel
            // 
            lblEnemyLevel.AutoSize = true;
            lblEnemyLevel.Location = new Point(250, 70);
            lblEnemyLevel.Name = "lblEnemyLevel";
            lblEnemyLevel.Size = new Size(29, 20);
            lblEnemyLevel.TabIndex = 8;
            lblEnemyLevel.Text = "Lvl:";
            // 
            // lblEnemyClass
            // 
            lblEnemyClass.AutoSize = true;
            lblEnemyClass.Location = new Point(250, 95);
            lblEnemyClass.Name = "lblEnemyClass";
            lblEnemyClass.Size = new Size(47, 20);
            lblEnemyClass.TabIndex = 9;
            lblEnemyClass.Text = "Klasa:";
            // 
            // btnAttack
            // 
            btnAttack.Location = new Point(15, 233);
            btnAttack.Name = "btnAttack";
            btnAttack.Size = new Size(100, 40);
            btnAttack.TabIndex = 10;
            btnAttack.Text = "Atakuj";
            btnAttack.Click += btnAttack_Click;
            // 
            // btnHeal
            // 
            btnHeal.Location = new Point(125, 233);
            btnHeal.Name = "btnHeal";
            btnHeal.Size = new Size(100, 40);
            btnHeal.TabIndex = 11;
            btnHeal.Text = "Lecz się";
            btnHeal.Click += btnHeal_Click;
            // 
            // lstLog
            // 
            lstLog.FormattingEnabled = true;
            lstLog.Location = new Point(15, 293);
            lstLog.Name = "lstLog";
            lstLog.Size = new Size(400, 144);
            lstLog.TabIndex = 13;
            // 
            // lblServer
            // 
            lblServer.AutoSize = true;
            lblServer.Location = new Point(20, 150);
            lblServer.Name = "lblServer";
            lblServer.Size = new Size(57, 20);
            lblServer.TabIndex = 5;
            lblServer.Text = "Serwer:";
            // 
            // btnMoveServer
            // 
            btnMoveServer.Location = new Point(235, 233);
            btnMoveServer.Name = "btnMoveServer";
            btnMoveServer.Size = new Size(150, 40);
            btnMoveServer.TabIndex = 12;
            btnMoveServer.Text = "Przełącz serwer";
            btnMoveServer.Click += btnMoveServer_Click;
            // 
            // lblWins
            // 
            lblWins.AutoSize = true;
            lblWins.Location = new Point(20, 175);
            lblWins.Name = "lblWins";
            lblWins.Size = new Size(71, 20);
            lblWins.TabIndex = 14;
            lblWins.Text = "Wygrane:";
            // 
            // lblLosses
            // 
            lblLosses.AutoSize = true;
            lblLosses.Location = new Point(20, 200);
            lblLosses.Name = "lblLosses";
            lblLosses.Size = new Size(78, 20);
            lblLosses.TabIndex = 15;
            lblLosses.Text = "Przegrane:";
            // 
            // Form1
            // 
            ClientSize = new Size(480, 456);
            Controls.Add(lblPlayer);
            Controls.Add(lblMyHp);
            Controls.Add(lblLevel);
            Controls.Add(lblClass);
            Controls.Add(lblTurn);
            Controls.Add(lblServer);
            Controls.Add(lblEnemy);
            Controls.Add(lblEnemyHp);
            Controls.Add(lblEnemyLevel);
            Controls.Add(lblEnemyClass);
            Controls.Add(btnAttack);
            Controls.Add(btnHeal);
            Controls.Add(btnMoveServer);
            Controls.Add(lstLog);
            Controls.Add(lblWins);
            Controls.Add(lblLosses);
            Name = "Form1";
            Text = "RPG - Gra Turowa";
            FormClosing += Form1_FormClosing;
            ResumeLayout(false);
            PerformLayout();
        }
    }
}