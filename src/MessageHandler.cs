using shared;

public static class MessageHandler {
    public static ISerializable HandleMessage(
        ClientConnection client,
        ISerializable message) {
        lock (Server.GameDataLock) {
            if (message is LoginAttemptMessage loginAttempt) {
                return handleLoinAttempt(client, loginAttempt);
            } else if (message is ConsoleCommandMessage consoleCommandMessage) {
                return HandleConsoleCommandMessage(client, consoleCommandMessage);
            } else if (message is RequestDataUpdateMessage) {
                Console.WriteLine(
                    $"Client {client.Id} requested game data.");
                return new DataUpdateMessage(Server._gameData);
            } else if (message is SubmitScoreMessage submitScoreMessage) {
                return HandleSubmitScoreMessage(client, submitScoreMessage);    
            } else if (message is AcceptScoreMessage acceptScoreMessage) {
                Server._gameData.AcceptScore();
                return new DataUpdateMessage(Server._gameData);
            } else if (message is DenyScoreMessage denyScoreMessage) {
                Server._gameData.DenyScore();
                return new DataUpdateMessage(Server._gameData);
            } else throw new InvalidDataException(
                $"Unhandled message type: {message.GetType().FullName}");
        }
    }

    private static ISerializable handleLoinAttempt(ClientConnection client, LoginAttemptMessage loginAttempt) {
        // Avoid logging the password itself.
        Console.WriteLine(
            $"Client {client.Id} attempted to log in.");

        if (loginAttempt.password == Server.adminPassword) {
            client.TeamPassword = null;
            Console.WriteLine(
                $"Client {client.Id} logged in as admin.");
            return new LoginResultMessage(
                true,
                loginAttempt.password);
        }

        if (Server._gameData.teamExists(loginAttempt.password)) {
            GameTeam team =
                Server._gameData.GetTeamData(loginAttempt.password);

            client.TeamPassword = loginAttempt.password;

            Console.WriteLine(
                $"Client {client.Id} logged in as {team.name}.");

            return new LoginResultMessage(
                team,
                loginAttempt.password);
        }

        client.TeamPassword = null;
        Console.WriteLine("Login failed: team not found.");

        return new LoginResultMessage();
    }

    private static ISerializable HandleConsoleCommandMessage(ClientConnection client, ConsoleCommandMessage consoleCommandMessage) {
        String returnMessage = ConsoleCommands.HandleCommand(consoleCommandMessage.command);
        if (returnMessage != null && returnMessage!="") {
            return new CommandLineMessage(returnMessage);
        }
        return null;
    }

    private static ISerializable HandleSubmitScoreMessage(ClientConnection client, SubmitScoreMessage submitScoreMessage) {
        Console.WriteLine($"Client {client.Id} submited a scoring");
        Server._gameData.addScore(submitScoreMessage.taskName, submitScoreMessage.teamPass, submitScoreMessage.score);
        return null;
    }
}