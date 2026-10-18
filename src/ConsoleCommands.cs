
using shared;

public class ConsoleCommands {
    static Exception ilegalArgumentCount = new Exception("Wrong number of arguments");

    public static string HandleCommand(string line) {
        try {
            return HandleCommandUnsafe(line);
        } catch (Exception e) {
            Console.WriteLine(e.Message);
            return e.Message;
        }
    }

    public static string HandleCommandUnsafe(string line) {
        string[] args = line.ToLower().Split(' ');
        switch (args[0]) {
            case "help": {
                return "List of commands:\n" +
                    "createteam [team_name] [team_password]\n" +
                    "- creates a new team\n" +
                    "removeteam [team_password]\n" +
                    "- removes a team\n" +
                    "setteamcolor [team_password] [R] [G] [B]\n" +
                    "- sets the color for a given team\n" +
                    "teams\n" +
                    "- lists all teams\n" +
                    "createtask [task_name]\n" +
                    "- creates a new task\n" +
                    "settaskdescription [description]\n" +
                    "- sets the text descripttion for a task\n" +
                    "tasks\n" +
                    "- lists all tasks\n" +
                    "clients\n" +
                    "- gives the number of connected clients\n" +
                    "updateclients\n" +
                    "- forces all clients to update their data from the server";
            }
            case "createteam": {
                if (args.Length != 3) throw ilegalArgumentCount;
                Server._gameData.CreateTeam(args[1], args[2]);
                return $"Team created Name:{args[1]} pass:{args[2]}";
            }
            case "removeteam": {
                if (args.Length != 2) throw ilegalArgumentCount;
                Server._gameData.RemoveTeam(args[1]);
                return $"Team removed";
            }
            case "setteamcolor": {
                if (args.Length != 5) throw ilegalArgumentCount;
                Server._gameData.SetTeamColor(args[1], args[2], args[3], args[4]);
                return "Color set";
            }
            case "teams": {
                if (args.Length != 1) throw ilegalArgumentCount;
                return Server._gameData.GetTeamLog();
            }
            case "createtask": {
                if (args.Length != 2) throw ilegalArgumentCount;
                Server._gameData.CreateTask(args[1]);
                return $"Task created Name:{args[1]}";
            }
            case "removetask": {
                if (args.Length != 2) throw ilegalArgumentCount;
                Server._gameData.RemoveTask(args[1]);
                return "Task removed";
            }
            case "settaskdescription": {
                if (args.Length == 0) throw ilegalArgumentCount;



                return "command not yet implemented";
            }
            case "tasks": {
                if (args.Length != 1) throw ilegalArgumentCount;
                return Server._gameData.GetTaskLog();
            }
            case "clients": {
                if (args.Length != 1) throw ilegalArgumentCount;
                return $"Client Count: {Server.clients.Count}";
            }
            case "updateclients": {
                if (args.Length != 1) throw ilegalArgumentCount;
                Server.SendGameUpdateToAll();
                return $"Updating data for all clients";
            }
            default: {
                throw new Exception("Unknown Command");
            }
        }
    }
}

