
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
        // if the command typed now in not confirm this wipes the to confirm command.
        WaitingForConfirmCommand cc = confirmCommand;
        confirmCommand = null;
        switch (args[0]) {
            case "help": {
                return "List of commands:\n\n" +

                    "createteam [team_name] [team_password]\n" +
                    "- Creates a new team\n\n" +

                    "removeteam [team_password]\n" +
                    "- Removes a team using its password\n\n" +

                    "setteamcolor [team_password] [R] [G] [B]\n" +
                    "- Sets a team's color using RGB values\n\n" +

                    "teams\n" +
                    "- Lists all teams\n\n" +

                    "createtask [task_name]\n" +
                    "- Creates a new task\n\n" +

                    "removetask [task_name]\n" +
                    "- Removes a task\n\n" +

                    "clearscores [task_name]\n" +
                    "clears scores for a task\n\n" +

                    "settaskdescription [task_name] [description]\n" +
                    "- Sets a task's description (spaces are allowed)\n\n" +

                    "tasks\n" +
                    "- Lists all tasks\n\n" +

                    "scores [team_name]\n" +
                    "- Displays the scores for the specified team\n\n" +

                    "clients\n" +
                    "- Displays the number of connected clients\n\n" +

                    "updateclients\n" +
                    "- Forces all clients to update their data from the server\n\n" +

                    "renameadminowned [new_name]\n" +
                    "- Renames admin-owned tasks\n\n" +

                    "setscoreshidden [true/false]\n" +
                    "- Enables or disables hiding scores\n\n" +

                    "settakeovermode [team_name] [mode]\n" +
                    "- Sets the takeover mode for a specific team\n\n" +

                    "settakeovermodeall [mode]\n" +
                    "- Sets the takeover mode for all teams\n\n" +

                    "settakeovermodedefault [mode]\n" +
                    "- Sets the default takeover mode for teams\n\n" +

                    "usescorequeue [true/false]\n" +
                    "- Enables or disables the score queue\n\n" +

                    "linkmaptask [task_name] [map_location]\n" +
                    "- Links a task to a map location\n\n" +

                    "reset\n" +
                    "- Requests a reset of all game data (requires confirmation)\n\n" +

                    "confirm\n" +
                    "- Confirms a pending command, such as a data reset";
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
            case "clearscores": {
                if (args.Length != 2) throw ilegalArgumentCount;
                Server._gameData.ClearScores(args[1]);
                return "Scores Cleared";
            }
            case "settaskdescription": {
                if (args.Length <= 2) throw ilegalArgumentCount;
                string taskName = args[1];
                string taskDescription = args[2];

                for (int i = 3; i<args.Length; i++)
                    taskDescription += " " + args[i];
                Server._gameData.SetTaskDesc(taskName, taskDescription);
                return "Description set";
            }
            case "tasks": {
                if (args.Length != 1) throw ilegalArgumentCount;
                return Server._gameData.GetTaskLog();
            }
            case "scores": {
                if (args.Length != 2) throw ilegalArgumentCount;
                return Server._gameData.GetScoreLog(args[1]);
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
            case "renameadminowned": {
                if (args.Length != 2) throw ilegalArgumentCount;
                Server._gameData.RenameAdminOwnedTasks(args[1]);
                return "Admin owned tasks renamed";
            }
            case "setscoreshidden": {
                if (args.Length != 2) throw ilegalArgumentCount;
                switch (args[1]) {
                    case "true": {
                        Server._gameData.SetScoreHidden(true);
                        break;
                    }
                    case "false": {
                        Server._gameData.SetScoreHidden(false);
                        break;
                    }
                    default: {
                        throw new Exception("Please use true or false");
                    }
                }
                return "Setting changed";
            }
            case "settakeovermode": {
                if (args.Length != 3) throw ilegalArgumentCount;
                Server._gameData.SetTakeoverMode(args[1], args[2]);
                return "Mode change success";
            }
            case "settakeovermodeall": {
                if (args.Length != 2) throw ilegalArgumentCount;
                Server._gameData.SetTakeoverModeAll(args[1]);
                return "Mode change success";
            }
            case "settakeovermodedefault": {
                if (args.Length != 2) throw ilegalArgumentCount;
                Server._gameData.SetDefaultTakeoverMode(args[1]);
                return "Default change success";
            }
            case "usescorequeue": {
                if (args.Length != 2) throw ilegalArgumentCount;
                switch (args[1]) {
                    case "true": {
                        Server._gameData.SetUseScoreQueue(true);
                        break;
                    }
                    case "false": {
                        Server._gameData.SetUseScoreQueue(false);
                        break;
                    }
                    default: {
                        throw new Exception("Please use true or false");
                    }
                }
                return "Setting changed";
            }
            case "linkmaptask": {
                if (args.Length != 3) throw ilegalArgumentCount;
                Server._gameData.LinkTaskToMap(args[1], args[2]);
                return $"Linked task to map location {args[2]}";
            }
            case "reset": {
                if (args.Length != 1) throw ilegalArgumentCount ;
                confirmCommand = new ResetDataCommand();
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

