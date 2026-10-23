using shared;
using System;
using System.Collections.Generic;
using System.Linq;
using static GameTask;

public class GameData : ISerializable {

    public bool updated = true;
    public bool scoresHiden = true;
    public string adminOwnedTaskName = "none";
    public string defaultTakeoverMode = "high";

    public bool shouldQueueScore = false;



    // <passWord, Team>
    readonly Dictionary<string, GameTeam> teams = new Dictionary<string, GameTeam>();
    readonly Dictionary<string, GameTask> tasks = new Dictionary<string, GameTask>();

    readonly List<Tuple<string, Scoring>> scoreQueue = new List<Tuple<string, GameTask.Scoring>>();

    public void addScore(string taskName, string teamPassWord, double score) {
        if (!shouldQueueScore) {
            if (taskExists(taskName) && teamExists(teamPassWord))
                tasks[taskName].setScore(score, teams[teamPassWord], teamPassWord);
        } else {
            scoreQueue.Add(new Tuple<string, Scoring>(taskName, new Scoring(score, teams[teamPassWord], teamPassWord)));
        }
        updated = true;
    }

    public Tuple<string, Scoring> GetFirstInQueue() {
        return scoreQueue.First();
    }

    public void AcceptScore() {
        string task = scoreQueue.First().Item1;
        Scoring scoring = scoreQueue.First().Item2;

        scoreQueue.RemoveAt(0);

        if (taskExists(task) && teamExists(scoring.teamPass))
            tasks[task].setScore(scoring);
        updated = true;
    }

    public bool taskExists(string name) {
        return tasks.ContainsKey(name);
    }

    public bool teamExists(string passWord) {
        return teams.ContainsKey(passWord);
    }

    public void CreateTask(string name) {
        if (tasks.ContainsKey(name)) throw new Exception("Name already in use");
        tasks.Add(name, new GameTask());
        SetTakeoverMode(name, defaultTakeoverMode);
        updated = true;
    }

    public void RemoveTask(string name) {
        if (!tasks.ContainsKey(name)) throw new Exception("Task does not exist");
        tasks.Remove(name);
        updated = true;
    }

    public void CreateTeam(string name, string pass) {
        if (teams.ContainsKey(pass)) throw new Exception("Password already in use");
        teams.Add(pass, new GameTeam(name));
        updated = true;
    }

    public void RemoveTeam(string pass) {
        if (!teams.ContainsKey(pass)) throw new Exception("Team with this password does not exist");
        foreach (GameTask task in tasks.Values) {
            task.TeamRemoved(teams[pass]);
        }
        teams.Remove(pass);
        updated = true;
    }

    public void SetTeamColor(string pass, string r, string g, string b) {
        if (!teams.ContainsKey(pass)) throw new Exception("Team with this password does not exist");
        try {
            teams[pass].setColor(int.Parse(r), int.Parse(g), int.Parse(b));
            updated = true;
        } catch {
            throw new Exception("color values are non numbers");
        }
    }

    public void SetTaskDesc(string taskName, string taskDescription) {
        if (!tasks.ContainsKey(taskName)) throw new ArgumentException("Task does not exist.");
        tasks[taskName].description = taskDescription;
        updated = true;
    }

    public void LinkTaskToMap(string taskName, string mapLink) {
        if (!tasks.ContainsKey(taskName)) throw new ArgumentException("Task does not exist.");
        tasks[taskName].linkedMap = mapLink;
        updated = true;
    }

    public void SetTakeoverModeAll(string mode) {
        foreach (GameTask task in tasks.Values) {
            task.SetTakeoverMode(mode);
            updated = true;
        }
    }

    public void SetTakeoverMode(string taskName, string mode) {
        if (!tasks.ContainsKey(taskName)) throw new ArgumentException("Task does not exist.");
        tasks[taskName].SetTakeoverMode(mode);
        updated = true;
    }

    public void SetDefaultTakeoverMode(string mode) {
        if (mode != "high" && mode != "low" && mode != "simple") {
            throw new Exception("No valid mode given");
        }
        defaultTakeoverMode = mode;
        updated = true;
    }

    public void SetScoreHidden(bool val) {
        scoresHiden = val;
        updated = true;
    }

    internal void RenameAdminOwnedTasks(string v) {
        adminOwnedTaskName = v;
        updated = true;
    }

    internal void SetUseScoreQueue(bool val) {
        shouldQueueScore = val;
        updated = true;
    }

    public void Serialize(Packet pPacket) {
        pPacket.Write(scoresHiden);
        pPacket.Write(adminOwnedTaskName);
        pPacket.Write(defaultTakeoverMode);
        pPacket.Write(shouldQueueScore);
        pPacket.Write(teams.Count);
        pPacket.Write(tasks.Count);
        pPacket.Write(scoreQueue.Count);
        foreach (KeyValuePair<string, GameTeam> pair in teams) {
            pPacket.Write(pair.Key);
            pPacket.Write(pair.Value);
        }
        foreach (KeyValuePair<string, GameTask> pair in tasks) {
            pPacket.Write(pair.Key);
            pPacket.Write(pair.Value);
        }
        foreach (Tuple<string, GameTask.Scoring> score in scoreQueue) {
            pPacket.Write(score.Item1);
            pPacket.Write(score.Item2);
        }
    }

    public void Deserialize(Packet pPacket) {
        scoresHiden = pPacket.ReadBool();
        adminOwnedTaskName = pPacket.ReadString();
        defaultTakeoverMode = pPacket.ReadString();
        shouldQueueScore = pPacket.ReadBool();
        int teamCount = pPacket.ReadInt();
        int taskCount = pPacket.ReadInt();
        int queueCount = pPacket.ReadInt();
        teams.Clear();
        tasks.Clear();
        scoreQueue.Clear();
        while (teamCount > 0) {
            teams.Add(pPacket.ReadString(), (GameTeam)pPacket.ReadObject());
            teamCount--;
        }
        while (taskCount > 0) {
            tasks.Add(pPacket.ReadString(), (GameTask)pPacket.ReadObject());
            taskCount--;
        }
        while (queueCount > 0) {
            scoreQueue.Add(new Tuple<string, GameTask.Scoring>(pPacket.ReadString(), (GameTask.Scoring)pPacket.ReadObject()));
            queueCount--;
        }
    }

    public string GetTeamLog() {
        string toReturn = "";
        foreach (KeyValuePair<string, GameTeam> pair in teams) {
            toReturn += $" - {pair.Value.name}, [{pair.Key}]\n";
        }
        toReturn = toReturn.TrimEnd('\n');
        return toReturn;
    }

    public string GetTaskLog() {
        string toReturn = "";
        foreach (KeyValuePair<string, GameTask> pair in tasks) {
            toReturn += $" - {pair.Key}\n";
        }
        toReturn = toReturn.TrimEnd('\n');
        return toReturn;
    }

    public GameTeam GetTeamData(string pass) {
        return teams[pass];
    }

    public int TeamAmount() {
        return teams.Count;
    }

    public int TaskAmount() {
        return tasks.Count;
    }

    public Dictionary<string, GameTask> GetTasks() {
        return tasks;
    }

    internal string GetScoreLog(string taskName) {
        if (!tasks.ContainsKey(taskName)) throw new ArgumentException("Task does not exist.");
        return tasks[taskName].GetScoreLog();
    }
}

public class GameTeam : ISerializable {

    public string name { get; private set; }
    public int R { get; private set; }
    public int G { get; private set; }
    public int B { get; private set; }
    public GameTeam() { }
    public GameTeam(string name) { this.name = name; }

    public void setColor(int r, int g, int b) {
        R = r;
        G = g;
        B = b;
    }

    public void Serialize(Packet pPacket) {
        pPacket.Write(name);
        pPacket.Write(R);
        pPacket.Write(G);
        pPacket.Write(B);
    }

    public void Deserialize(Packet pPacket) {
        name = pPacket.ReadString();
        R = pPacket.ReadInt();
        G = pPacket.ReadInt();
        B = pPacket.ReadInt();
    }


}

public class GameTask : ISerializable {
    private readonly List<Scoring> scores = new();
    private Scoring bestScore;
    public string description = "";
    public string linkedMap = "";
    public string takeoverMode = "high";
    public bool HasTeamScore(GameTeam team) {
        return scores.Exists(s => s.team == team);
    }

    public bool HasTeamScore(string pass) {
        return scores.Exists(s => s.teamPass == pass);
    }

    public double getScore(GameTeam team) {
        if (!HasTeamScore(team)) return 0;
        double score = 0;
        foreach (Scoring scoring in scores) {
            if (scoring.team == team && scoring.score > score) {
                score = scoring.score;
            }
        }
        return score;
    }

    public void setScore(double score, GameTeam team, string pass) {
        scores.Add(new Scoring(score, team, pass));
    }

    public void setScore(Scoring scoring) {
        scores.Add(scoring);
    }

    public void Serialize(Packet pPacket) {
        pPacket.Write(scores.Count);
        foreach (Scoring scoring in scores) {
            pPacket.Write(scoring);
        }
        pPacket.Write(description);
        pPacket.Write(linkedMap);
        pPacket.Write(takeoverMode);
    }

    public void Deserialize(Packet pPacket) {
        int scoreCount = pPacket.ReadInt();
        scores.Clear();
        while (scoreCount > 0) {
            scores.Add((Scoring)pPacket.ReadObject());
            scoreCount--;
        }
        description = pPacket.ReadString();
        linkedMap = pPacket.ReadString();
        takeoverMode = pPacket.ReadString();
    }

    public int R() {
        if (bestScore == null) return 0;
        return bestScore.team.R;
    }
    public int G() {
        if (bestScore == null) return 0;
        return bestScore.team.G;
    }
    public int B() {
        if (bestScore == null) return 0;
        return bestScore.team.B;
    }

    public void RecalculateBestScore() {
        bestScore = null;
        foreach (Scoring score in scores) {
            if (bestScore == null) {
                bestScore = score;
                continue;
            }
            if (score.score > bestScore.score) {
                bestScore = score;
            }
        }
    }

    public Scoring GetBestScore() {
        return bestScore;
    }

    public void TeamRemoved(GameTeam team) {
        for (int i = scores.Count - 1; i >= 0; i--) {
            Scoring score = scores[i];
            if (score.team == team) {
                scores.Remove(score);
            }
        }

        if (bestScore != null && bestScore.team == team) {
            RecalculateBestScore();
        }
    }

    public void SetTakeoverMode(string mode) {
        if (mode != "high" && mode != "low" && mode != "simple") {
            throw new Exception("No valid mode given");
        }
        takeoverMode = mode;
    }

    public string GetScoreLog() {
        string toReturn = "";

        foreach (Scoring score in scores) {
            toReturn += score.team.name + "-" + score.score + "-" + score.timestamp + "\n";
        }
        toReturn.TrimEnd('\n');
        return toReturn;
    }

    public class Scoring : ISerializable {
        public double score;
        public GameTeam team;
        public string teamPass;
        public string timestamp;

        public Scoring() { }

        public Scoring(double score, GameTeam team, string pass) {
            this.score = score;
            this.team = team;
            this.teamPass = pass;
            this.timestamp = DateTime.UtcNow.AddHours(2).ToString("o");
        }


        public void Serialize(Packet pPacket) {
            pPacket.Write(score);
            pPacket.Write(team);
            pPacket.Write(teamPass);
            pPacket.Write(timestamp);
        }
        public void Deserialize(Packet pPacket) {
            score = pPacket.ReadDouble();
            team = (GameTeam)pPacket.ReadObject();
            teamPass = pPacket.ReadString();
            timestamp = pPacket.ReadString();
        }
    }

}

