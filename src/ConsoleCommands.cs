
using shared;

public class ConsoleCommands {
    static Exception ilegalArgumentCount = new Exception("Wrong number of arguments");

    static WaitingForConfirmCommand confirmCommand = null;

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
        WaitingForConfirmCommand cc = confirmCommand;
        confirmCommand = null;
        switch (args[0]) {
            case "help": {
                return "List of commands:\n" +
                    "createteam [team_name] [team_password]\n" +
                    "- creates a new team\n\n" +
                    "removeteam [team_password]\n" +
                    "- removes a team\n/n" +
                    "setteamcolor [team_password] [R] [G] [B]\n" +
                    "- sets the color for a given team\n\n" +
                    "teams\n" +
                    "- lists all teams\n\n" +
                    "createtask [task_name]\n" +
                    "- creates a new task\n\n" +
                    "removetask [task_name]\n" +
                    "- creates a new task\n\n" +
                    "settaskdescription [task_name] [description]\n" +
                    "- sets the text descripttion for a task\n\n" +
                    "tasks\n" +
                    "- lists all tasks\n\n" +
                    "clients\n" +
                    "- gives the number of connected clients\n\n" +
                    "updateclients\n" +
                    "- forces all clients to update their data from the server\n\n"+
                    "reset\n" +
                    "- Resets all game data to nothing";
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
                if (args.Length <= 2) throw ilegalArgumentCount;
                string taskName = args[1];
                string taskDescription = args[2];

                for (int i = 3; i<=args.Length; i++)
                    taskDescription += " " + args[i];

                Server._gameData.SetTaskDesc(taskName, taskDescription);

                return "Description set";
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
            case "reset": {
                if (args.Length != 1) throw ilegalArgumentCount ;

                return "Type \"confirm\" to confirm data reset";
            }
            case "confirm": {
                if (args.Length != 1) throw ilegalArgumentCount;
                string toReturn = "No command to confirm";
                if (cc != null) toReturn = cc.Execute();
                return toReturn ;
            }
            default: {
                throw new Exception("Unknown Command");
            }
        }
    }

    private abstract class WaitingForConfirmCommand {
        public abstract string Execute();
    }

    private class ResetDataCommand : WaitingForConfirmCommand {
        public override string Execute() {
            Server._gameData = new();
            Server._gameData .updated = true;
            return "Save data reset to defaults";
        }
    }
}

