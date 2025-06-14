using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Text;
using System.Text.Json;
using Common;

public class SerwerService : BackgroundService
{
    private IConnection _connection;
    private IModel _channel;
    private const string InputQueue = "chat-messages-server2";
    private const string OutputExchange = "chat-exchange-server2";

    private Dictionary<string, PlayerData> players = new();
    private List<string> turnOrder = new();
    private int currentTurnIndex = 0;
    // private int roundNumber = 1;

    // Metoda do uruchamiania serwisu w tle
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory()
        {
            HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost"
        };

        const int maxRetries = 10;
        int retryCount = 0;

        while (!stoppingToken.IsCancellationRequested && retryCount < maxRetries)
        {
            try
            {
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();
                Console.WriteLine($"[Server2] Połączono z RabbitMQ.");
                break;
            }
            catch (BrokerUnreachableException)
            {
                retryCount++;
                Console.WriteLine($"[Server2 Retry] Nie udało się połączyć z RabbitMQ. Ponawiam próbę ({retryCount}/{maxRetries})...");
                await Task.Delay(2000, stoppingToken);
            }
        }

        if (_channel == null)
        {
            Console.WriteLine("[Server2] Nie udało się połączyć z RabbitMQ po wielu próbach. Serwis zostanie zamknięty.");
            return;
        }

        _channel.QueueDeclare(queue: InputQueue, durable: false, exclusive: false, autoDelete: false);
        _channel.ExchangeDeclare(exchange: OutputExchange, type: ExchangeType.Fanout);

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            Console.WriteLine($"[Server2] Otrzymano: {message}");

            try
            {
                var msgObj = JsonSerializer.Deserialize<Message>(message);
                if (msgObj == null) return;

                if (msgObj.Type == "Connect")
                {
                    PlayerData newPlayer;
                    if (!string.IsNullOrWhiteSpace(msgObj.PlayerDataSerialized))
                    {
                        // Gracz z innego serwera
                        newPlayer = JsonSerializer.Deserialize<PlayerData>(msgObj.PlayerDataSerialized);
                        Console.WriteLine($"Gracz {newPlayer.Name} przeniósł się z {newPlayer.CurrentServer} na Server2.");
                        SendMessageToAll($"Gracz {newPlayer.Name} przeniósł się na Server2.");
                    }
                    else
                    {
                        // Nowe połączenie gracza
                        newPlayer = CreatePlayer(msgObj.PlayerName, msgObj.Class);
                        Console.WriteLine($"{newPlayer.Name} dołączył do gry jako {newPlayer.Class} na Server2.");
                        SendMessageToAll($"{newPlayer.Name} dołączył do gry jako {newPlayer.Class} na Server2.");
                    }

                    if (!players.ContainsKey(newPlayer.Name))
                    {
                        newPlayer.CurrentServer = "Server2";
                        players[newPlayer.Name] = newPlayer;
                        turnOrder.Add(newPlayer.Name);

                        if (players.Count == 1)
                        {
                            StartNewGame();
                        }
                        else if (!turnOrder.Any(p => players[p].IsTurn))
                        {
                            currentTurnIndex = 0;
                            players[turnOrder[currentTurnIndex]].IsTurn = true;
                        }
                    }
                    else
                    {
                        players[newPlayer.Name] = newPlayer;
                        SendMessageToAll($"Gracz {newPlayer.Name} ponownie połączył się z Server2.");
                    }

                    CheckGameOverAndRestartIfNeeded();
                    AdvanceTurn();
                    SendStateToAll();
                }
                else if (msgObj.Type == "Disconnect")
                {
                    if (players.Remove(msgObj.PlayerName))
                    {
                        turnOrder.Remove(msgObj.PlayerName);
                        Console.WriteLine($"Gracz {msgObj.PlayerName} opuścił serwer.");
                        SendMessageToAll($"Gracz {msgObj.PlayerName} opuścił serwer.");


                        SendPlayerDisconnectedMessage(msgObj.PlayerName);

                        CheckGameOverAndRestartIfNeeded();
                        AdvanceTurn();
                        SendStateToAll();
                    }
                }
                else if (msgObj.Type == "Action")
                {
                    if (!players.ContainsKey(msgObj.PlayerName))
                    {
                        Console.WriteLine($"Gracz {msgObj.PlayerName} nie istnieje na tym serwerze. Ignoruję akcję.");
                        return;
                    }
                    var player = players[msgObj.PlayerName];

                    if (!player.IsTurn || player.Hp <= 0)
                    {
                        Console.WriteLine($"[Server2] Nie jest tura gracza {player.Name} lub jest martwy. Akcja '{msgObj.Action}' zignorowana.");
                        SendMessageToClient(player.Name, "Nie twoja tura lub jesteś martwy!");
                        return;
                    }

                    if (msgObj.Action == "attack")
                    {
                        var opponents = players.Values.Where(p => p.Name != player.Name && p.Hp > 0).ToList();
                        if (opponents.Count == 0)
                        {
                            Console.WriteLine("Brak przeciwników do ataku.");
                            SendMessageToClient(player.Name, "Brak przeciwników do ataku.");
                        }
                        else
                        {
                            var rnd = new Random();
                            var target = opponents[rnd.Next(opponents.Count)];

                            int damage = player.Attack - target.Defense;
                            if (damage < 1) damage = 1;

                            target.Hp -= damage;
                            if (target.Hp < 0) target.Hp = 0;

                            Console.WriteLine($"{player.Name} zaatakował {target.Name} z obrażeniami {damage}. {target.Name} HP: {target.Hp}");
                            SendMessageToAll($"{player.Name} zaatakował {target.Name} z obrażeniami {damage}. {target.Name} HP: {target.Hp}");


                            if (target.Hp == 0)
                            {
                                target.Loses++;
                                player.Wins++;
                                Console.WriteLine($"{target.Name} został pokonany przez {player.Name}!");
                                SendMessageToAll($"{target.Name} został pokonany przez {player.Name}!");
                                player.Xp += 50;
                                Console.WriteLine($"{player.Name} zdobywa 50 XP!");
                                SendMessageToAll($"{player.Name} zdobywa 50 XP!");

                                turnOrder.Remove(target.Name);

                                if (player.Xp >= player.Level * 100)
                                {
                                    player.Level++;
                                    player.MaxHp += 20;
                                    player.Attack += 5;
                                    player.Defense += 2;
                                    player.Hp = player.MaxHp;
                                    player.Xp = 0;
                                    Console.WriteLine($"{player.Name} awansował na poziom {player.Level}!");
                                    SendMessageToAll($"{player.Name} awansował na poziom {player.Level}!");
                                }
                            }
                        }
                    }
                    else if (msgObj.Action == "heal")
                    {
                        int healAmount = 20 + player.Level * 5;
                        player.Hp += healAmount;
                        if (player.Hp > player.MaxHp) player.Hp = player.MaxHp;

                        Console.WriteLine($"{player.Name} leczy się o {healAmount}. HP: {player.Hp}");
                        SendMessageToAll($"{player.Name} leczy się o {healAmount}. HP: {player.Hp}");
                    }
                    else if (msgObj.Action == "move")
                    {
                        string targetServer = msgObj.TargetServer;
                        if (targetServer == "Server1")
                        {
                            Console.WriteLine($"Gracz {player.Name} prosi o przeniesienie na {targetServer}");
                            SendMessageToAll($"Gracz {player.Name} przenosi się na {targetServer}");

                            SendPlayerMoveToOtherServer(player, targetServer);

                            if (players.Remove(player.Name))
                            {
                                turnOrder.Remove(player.Name);
                                Console.WriteLine($"Gracz {player.Name} usunięty z Server2 po przeniesieniu.");
                                SendPlayerDisconnectedMessage(player.Name);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Nieznany serwer docelowy: {targetServer}");
                            SendMessageToClient(player.Name, $"Nieznany serwer docelowy: {targetServer}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Nieznana akcja: {msgObj.Action}");
                        SendMessageToClient(player.Name, $"Nieznana akcja: {msgObj.Action}");
                    }
                    CheckGameOverAndRestartIfNeeded();
                    AdvanceTurn();
                    SendStateToAll();
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[BŁĄD] Błąd parsowania JSON na serwerze: {ex.Message}. Wiadomość: {message.Substring(0, Math.Min(message.Length, 100))}...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BŁĄD] Wystąpił nieoczekiwany błąd podczas przetwarzania wiadomości: {ex.Message}");
            }
        };

        _channel.BasicConsume(queue: InputQueue, autoAck: true, consumer: consumer);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    // Metoda do wysyłania wiadomości o przeniesieniu gracza do innego serwera
    private void SendPlayerMoveToOtherServer(PlayerData player, string targetServer)
    {
        var factory = new ConnectionFactory()
        {
            HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost"
        };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        string targetQueue = targetServer == "Server2" ? "chat-messages-server2" : "chat-messages-server1";

        player.CurrentServer = targetServer;

        var moveMsg = new Message
        {
            Type = "Connect",
            PlayerName = player.Name,
            Class = player.Class,
            PlayerDataSerialized = JsonSerializer.Serialize(player)
        };

        var json = JsonSerializer.Serialize(moveMsg);
        var body = Encoding.UTF8.GetBytes(json);

        channel.QueueDeclare(queue: targetQueue, durable: false, exclusive: false, autoDelete: false);
        channel.BasicPublish(exchange: "", routingKey: targetQueue, body: body);

        Console.WriteLine($"[Server2] Gracz {player.Name} został przekazany do {targetServer}");
    }

    // Metoda do tworzenia nowego gracza na podstawie klasy
    private PlayerData CreatePlayer(string name, string className)
    {
        return className switch
        {
            "Elf" => new PlayerData { Name = name, Class = "Elf", Level = 1, MaxHp = 80, Hp = 80, Attack = 20, Defense = 3, Xp = 0 },
            "Ork" => new PlayerData { Name = name, Class = "Ork", Level = 1, MaxHp = 120, Hp = 120, Attack = 15, Defense = 8, Xp = 0 },
            "Człowiek" => new PlayerData { Name = name, Class = "Człowiek", Level = 1, MaxHp = 100, Hp = 100, Attack = 18, Defense = 5, Xp = 0 },
            _ => new PlayerData { Name = name, Class = "Nieznana", Level = 1, MaxHp = 100, Hp = 100, Attack = 15, Defense = 5, Xp = 0 },
        };
    }

    // Metoda do przechodzenia do następnej tury
    private void AdvanceTurn()
    {
        turnOrder.RemoveAll(pName => !players.ContainsKey(pName) || players[pName].Hp <= 0);

        if (turnOrder.Count == 0)
        {
            Console.WriteLine("Brak aktywnych graczy. Koniec tury.");
            return;
        }

        foreach (var player in players.Values)
        {
            player.IsTurn = false;
        }

        if (currentTurnIndex >= turnOrder.Count)
        {
            currentTurnIndex = 0;
        }
        players[turnOrder[currentTurnIndex]].IsTurn = true;

        Console.WriteLine($"Tura gracza: {turnOrder[currentTurnIndex]}");
        SendMessageToAll($"Tura gracza: {turnOrder[currentTurnIndex]}");

        currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Count;
    }

    // Metoda do wysyłania stanu gry do wszystkich graczy
    private void SendStateToAll()
    {
        var stateJson = JsonSerializer.Serialize(players.Values.ToList());
        var stateBody = Encoding.UTF8.GetBytes(stateJson);
        _channel.BasicPublish(exchange: OutputExchange, routingKey: "", body: stateBody);
    }

    // Metoda do wysyłania wiadomości do wszystkich klientów
    private void SendMessageToAll(string text)
    {
        var msg = new Message
        {
            Type = "Log",
            Log = text
        };
        var json = JsonSerializer.Serialize(msg);
        var body = Encoding.UTF8.GetBytes(json);
        _channel.BasicPublish(exchange: OutputExchange, routingKey: "", body: body);
    }

    // Metoda do wysyłania wiadomości do konkretnego klienta
    private void SendMessageToClient(string playerName, string text)
    {
        var msg = new Message
        {
            Type = "Log",
            Log = text
        };
        var json = JsonSerializer.Serialize(msg);
        var body = Encoding.UTF8.GetBytes(json);
        _channel.BasicPublish(exchange: OutputExchange, routingKey: "", body: body);
    }

    // Metoda do wysyłania wiadomości o rozłączeniu gracza
    private void SendPlayerDisconnectedMessage(string disconnectedPlayerName)
    {
        var msg = new Message
        {
            Type = "PlayerDisconnected",
            PlayerName = disconnectedPlayerName
        };
        var json = JsonSerializer.Serialize(msg);
        var body = Encoding.UTF8.GetBytes(json);
        _channel.BasicPublish(exchange: OutputExchange, routingKey: "", body: body);
    }

    // Metoda do startowania nowej gry
    private void StartNewGame()
    {
        if (players.Count == 0)
        {
            Console.WriteLine("Brak graczy, nie można rozpocząć nowej gry.");
            return;
        }

        foreach (var player in players.Values)
        {
            player.IsTurn = false;
            player.Hp = player.MaxHp;
            player.Xp = 0;
        }

        turnOrder = players.Keys.ToList();
        currentTurnIndex = 0;

        players[turnOrder[currentTurnIndex]].IsTurn = true;

        Console.WriteLine("Nowa gra rozpoczęta. Tura gracza: " + turnOrder[currentTurnIndex]);
        SendMessageToAll("Nowa gra rozpoczęta. Tura gracza: " + turnOrder[currentTurnIndex]);
        SendStateToAll();
    }

    // Metoda do sprawdzania zakończenia gry i restartu
    private void CheckGameOverAndRestartIfNeeded()
    {
        var alivePlayers = players.Values.Where(p => p.Hp > 0).ToList();
        if (alivePlayers.Count <= 1)
        {
            if (alivePlayers.Count == 1 && players.Count > 1)
            {
                SendMessageToAll($"Gra zakończona! Zwycięzca: {alivePlayers[0].Name}");
            }

            StartNewGame();
        }
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}