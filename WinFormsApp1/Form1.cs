using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Text;
using System.Text.Json;
using Common;

namespace RPGWinForms
{
    public partial class Form1 : Form
    {
        private string playerName;
        private string playerClass;
        private string serverChoice;

        private IConnection connection;
        private IModel channel;

        private string inputQueue;
        private string outputExchange;

        private List<PlayerData> players = new();
        private bool _isConnected = false; 

        public Form1()
        {
            InitializeComponent();
            if (!ShowPlayerInfoDialog() || !ShowServerChoiceDialog())
            {
                Application.Exit(); 
                return;
            }
            InitializeGame();
        }

        private bool ShowPlayerInfoDialog()
        {
            using (var dialog = new Form
            {
                Text = "WprowadŸ dane gracza",
                Width = 350,
                Height = 200,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterScreen,
                MinimizeBox = false,
                MaximizeBox = false
            })
            {
                var lblName = new Label { Text = "Nazwa gracza:", Left = 20, Top = 20, AutoSize = true };
                var txtName = new TextBox { Left = 120, Top = 17, Width = 180, Text = "Gracz" };

                var lblClass = new Label { Text = "Wybierz klasê:", Left = 20, Top = 60, AutoSize = true };
                var cbClass = new ComboBox
                {
                    Left = 120,
                    Top = 57,
                    Width = 180,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                cbClass.Items.AddRange(new string[] { "Elf", "Ork", "Cz³owiek" });
                cbClass.SelectedIndex = 0; 

                var btnOk = new Button { Text = "OK", Left = 80, Top = 120, Width = 75, DialogResult = DialogResult.OK };
                var btnCancel = new Button { Text = "Anuluj", Left = 180, Top = 120, Width = 75, DialogResult = DialogResult.Cancel };

                dialog.Controls.Add(lblName);
                dialog.Controls.Add(txtName);
                dialog.Controls.Add(lblClass);
                dialog.Controls.Add(cbClass);
                dialog.Controls.Add(btnOk);
                dialog.Controls.Add(btnCancel);

                dialog.AcceptButton = btnOk;
                dialog.CancelButton = btnCancel;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    playerName = txtName.Text.Trim();
                    playerClass = cbClass.SelectedItem?.ToString();

                    if (string.IsNullOrWhiteSpace(playerName))
                    {
                        MessageBox.Show("Nazwa gracza nie mo¿e byæ pusta.", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                    if (string.IsNullOrWhiteSpace(playerClass))
                    {
                        MessageBox.Show("Klasa gracza nie zosta³a wybrana.", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                    return true;
                }
                return false; 
            }
        }

        private bool ShowServerChoiceDialog()
        {
            using (var dialog = new Form
            {
                Text = "Wybierz serwer",
                Width = 350,
                Height = 160,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterScreen,
                MinimizeBox = false,
                MaximizeBox = false
            })
            {
                var lblServerr = new Label { Text = "Wybierz serwer:", Left = 20, Top = 20, AutoSize = true };
                var cbServer = new ComboBox
                {
                    Left = 120,
                    Top = 17,
                    Width = 180,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                cbServer.Items.AddRange(new string[] { "Server1", "Server2" });
                cbServer.SelectedIndex = 0; 

                var btnOk = new Button { Text = "OK", Left = 80, Top = 80, Width = 75, DialogResult = DialogResult.OK };
                var btnCancel = new Button { Text = "Anuluj", Left = 180, Top = 80, Width = 75, DialogResult = DialogResult.Cancel };

                dialog.Controls.Add(lblServerr);
                dialog.Controls.Add(cbServer);
                dialog.Controls.Add(btnOk);
                dialog.Controls.Add(btnCancel);

                dialog.AcceptButton = btnOk;
                dialog.CancelButton = btnCancel;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    serverChoice = cbServer.SelectedItem?.ToString();
                    if (string.IsNullOrWhiteSpace(serverChoice))
                    {
                        MessageBox.Show("Serwer nie zosta³ wybrany.", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }

                    inputQueue = serverChoice == "Server1" ? "chat-messages-server1" : "chat-messages-server2";
                    outputExchange = serverChoice == "Server1" ? "chat-exchange-server1" : "chat-exchange-server2";
                    lblServerr.Text = $"Serwer: {serverChoice}";
                    lblServer.Text = $"Serwer: {serverChoice}";
                    return true;
                }
                return false;
            }
        }


        private void InitializeGame()
        {
            players.Clear();
            lstLog.Items.Clear();
            UpdateUI();

            lblPlayer.Text = $"Ty: {playerName} ({playerClass})";

            var factory = new ConnectionFactory()
            {
                HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost"
            };

            const int maxRetries = 5;
            int retryCount = 0;
            bool connected = false;

            while (!connected && retryCount < maxRetries)
            {
                try
                {
                    connection = factory.CreateConnection();
                    channel = connection.CreateModel();
                    connected = true;
                    _isConnected = true;
                }
                catch (BrokerUnreachableException ex)
                {
                    retryCount++;
                    AddLog($"[B£¥D] Nie uda³o siê po³¹czyæ z RabbitMQ ({ex.Message}). Próba {retryCount}/{maxRetries}...");
                    Task.Delay(2000).Wait();
                }
                catch (Exception ex)
                {
                    AddLog($"[B£¥D] Wyst¹pi³ nieoczekiwany b³¹d podczas ³¹czenia: {ex.Message}");
                    _isConnected = false;
                    return;
                }
            }

            if (!connected)
            {
                AddLog("[B£¥D] Nie uda³o siê po³¹czyæ z serwerem po wielu próbach. SprawdŸ, czy serwer RabbitMQ dzia³a.");
                _isConnected = false;
                btnAttack.Enabled = false;
                btnHeal.Enabled = false;
                btnMoveServer.Enabled = false;
                return;
            }

            channel.QueueDeclare(queue: inputQueue, durable: false, exclusive: false, autoDelete: false);
            channel.ExchangeDeclare(exchange: outputExchange, type: ExchangeType.Fanout);

            var queueName = channel.QueueDeclare().QueueName;
            channel.QueueBind(queue: queueName, exchange: outputExchange, routingKey: "");

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                HandleStateUpdate(message);
            };

            channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);

            SendConnect();

            UpdateUI();
        }

        private void Disconnect()
        {
            if (_isConnected)
            {
                try
                {
                    channel?.Close();
                    connection?.Close();
                    AddLog("[INFO] Po³¹czenie z RabbitMQ zosta³o zamkniête.");
                }
                catch (Exception ex)
                {
                    AddLog($"[B£¥D] Wyst¹pi³ b³¹d podczas zamykania po³¹czenia: {ex.Message}");
                }
                finally
                {
                    _isConnected = false;
                    channel = null;
                    connection = null;
                }
            }
        }

        private void SendConnect()
        {
            if (!_isConnected) return; 

            var connectMsg = new Common.Message
            {
                Type = "Connect",
                PlayerName = playerName,
                Class = playerClass,
                CurrentServer = serverChoice 
            };

            SendMessage(connectMsg);
        }

        private void SendAction(string action)
        {
            if (!_isConnected)
            {
                AddLog("[B£¥D] Nie po³¹czono z serwerem. Akcja niemo¿liwa.");
                return;
            }

            var msg = new Common.Message
            {
                Type = "Action",
                PlayerName = playerName,
                Action = action
            };

            SendMessage(msg);
        }

        private void SendMove(string targetServer)
        {
            if (!_isConnected)
            {
                AddLog("[B£¥D] Nie po³¹czono z serwerem. Prze³¹czanie serwera niemo¿liwe.");
                return;
            }

            var msg = new Common.Message
            {
                Type = "Action",
                PlayerName = playerName,
                Action = "move",
                TargetServer = targetServer
            };

            SendMessage(msg);
        }

        private void SendMessage(Common.Message msg)
        {
            if (!_isConnected) return;

            try
            {
                var json = JsonSerializer.Serialize(msg);
                var body = Encoding.UTF8.GetBytes(json);
                channel.BasicPublish(exchange: "", routingKey: inputQueue, body: body);
            }
            catch (Exception ex)
            {
                AddLog($"[B£¥D] Nie uda³o siê wys³aæ wiadomoœci: {ex.Message}");
            }
        }

        // Odbiera aktualizacje stanu gry
        private void HandleStateUpdate(string message)
        {
            try
            {
                if (message.Contains("\"Type\":\"Log\""))
                {
                    var logMsg = JsonSerializer.Deserialize<Common.Message>(message);
                    if (logMsg?.Log != null)
                        Invoke(() => AddLog($"[INFO] {logMsg.Log}"));
                    return;
                }
                else if (message.Contains("\"Type\":\"PlayerDisconnected\""))
                {
                    var disconnectMsg = JsonSerializer.Deserialize<Common.Message>(message);
                    if (disconnectMsg?.PlayerName != null)
                    {
                        Invoke(() =>
                        {
                            players.RemoveAll(p => p.Name == disconnectMsg.PlayerName);
                            AddLog($"[INFO] Gracz {disconnectMsg.PlayerName} opuœci³ serwer.");
                            UpdateUI();
                        });
                    }
                    return;
                }

                var updatedPlayers = JsonSerializer.Deserialize<List<PlayerData>>(message);
                if (updatedPlayers == null) return;

                Invoke(() =>
                {
                    players = updatedPlayers;
                    UpdateUI();
                });
            }
            catch (JsonException ex)
            {
                AddLog($"[B£¥D] B³¹d parsowania JSON: {ex.Message}. Wiadomoœæ: {message.Substring(0, Math.Min(message.Length, 100))}...");
            }
            catch (Exception ex)
            {
                AddLog($"[B£¥D] Wyst¹pi³ nieoczekiwany b³¹d podczas aktualizacji stanu: {ex.Message}");
            }
        }

        private void UpdateUI()
        {
            var me = players.FirstOrDefault(p => p.Name == playerName);
            if (me != null)
            {
                lblMyHp.Text = $"HP: {me.Hp}/{me.MaxHp}";
                lblLevel.Text = $"Lvl: {me.Level}  XP: {me.Xp}";
                lblClass.Text = $"Klasa: {me.Class}";
                lblTurn.Text = me.IsTurn ? "Twoja tura!" : "Tura przeciwnika";
                lblWins.Text = $"Wygrane: {me.Wins}"; 
                lblLosses.Text = $"Przegrane: {me.Loses}";

                btnAttack.Enabled = me.IsTurn && me.Hp > 0 && _isConnected;
                btnHeal.Enabled = me.IsTurn && me.Hp > 0 && _isConnected;
                btnMoveServer.Enabled = me.Hp > 0 && _isConnected;
            }
            else
            {
                lblMyHp.Text = "Brak danych";
                lblLevel.Text = "";
                lblClass.Text = "";
                lblTurn.Text = "";
                lblWins.Text = "";
                lblLosses.Text = ""; 
                btnAttack.Enabled = false;
                btnHeal.Enabled = false;
                btnMoveServer.Enabled = false;
            }

            var enemies = players.Where(p => p.Name != playerName && p.Hp > 0).ToList();

            if (enemies.Count > 0)
            {
                var enemy = enemies[0];
                lblEnemy.Text = $"Przeciwnik ({enemy.Name})";
                lblEnemyHp.Text = $"HP: {enemy.Hp}/{enemy.MaxHp}";
                lblEnemyLevel.Text = $"Lvl: {enemy.Level}";
                lblEnemyClass.Text = $"Klasa: {enemy.Class}";
                lblWins.Text = $"Wygrane: {enemy.Wins}";
                lblLosses.Text = $"Przegrane: {enemy.Loses}";
            }
            else
            {
                lblEnemy.Text = "Brak przeciwników";
                lblEnemyHp.Text = "";
                lblEnemyLevel.Text = "";
                lblEnemyClass.Text = "";
            }

            if (me != null && me.Hp <= 0)
            {
                InitializeGame();
            }
        }


        private void AddLog(string message)
        {
            if (lstLog.InvokeRequired)
            {
                lstLog.Invoke(() => lstLog.Items.Insert(0, message));
            }
            else
            {
                lstLog.Items.Insert(0, message);
            }
        }

        private void btnAttack_Click(object sender, EventArgs e)
        {
            var me = players.FirstOrDefault(p => p.Name == playerName);
            if (me != null && me.IsTurn && me.Hp > 0)
            {
                SendAction("attack");
            }
            else
            {
                AddLog("Nie twoja tura lub jesteœ martwy!");
            }
        }

        private void btnHeal_Click(object sender, EventArgs e)
        {
            var me = players.FirstOrDefault(p => p.Name == playerName);
            if (me != null && me.IsTurn && me.Hp > 0)
            {
                SendAction("heal");
            }
            else
            {
                AddLog("Nie twoja tura lub jesteœ martwy!");
            }
        }

        private void btnMoveServer_Click(object sender, EventArgs e)
        {
            string newServer = serverChoice == "Server1" ? "Server2" : "Server1";

            var confirm = MessageBox.Show($"Czy chcesz prze³¹czyæ siê na {newServer}?", "Prze³¹cz serwer", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm == DialogResult.Yes)
            {
                SendMove(newServer);

                Disconnect();

                Task.Delay(500).ContinueWith(_ =>
                {
                    Invoke(() =>
                    {
                        serverChoice = newServer;
                        inputQueue = serverChoice == "Server1" ? "chat-messages-server1" : "chat-messages-server2";
                        outputExchange = serverChoice == "Server1" ? "chat-exchange-server1" : "chat-exchange-server2";
                        lblServer.Text = $"Serwer: {serverChoice}";

                        InitializeGame(); 
                    });
                });
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Wysy³a wiadomoœæ roz³¹czenia
            if (_isConnected)
            {
                try
                {
                    var disconnectMsg = new Common.Message
                    {
                        Type = "Disconnect",
                        PlayerName = playerName
                    };
                    SendMessage(disconnectMsg);
                    System.Threading.Thread.Sleep(200);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending disconnect message: {ex.Message}");
                }
            }
            Disconnect(); 
        }
    }
}