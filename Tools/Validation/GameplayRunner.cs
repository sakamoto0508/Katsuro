using System;
using UnityEditor;
using UnityEngine;

public static class GameplayRunner
{
    private static int count;
    private static void Check(bool value, string name)
    {
        if (!value) throw new Exception(name);
        count++; Debug.Log("PASS: " + name);
    }
    public static void Run()
    {
        try
        {
            if (Application.companyName != "KatsuroValidation" || Application.productName != "GameplayRegression")
                throw new Exception("Refusing to touch non-test PlayerPrefs");
            PlayerPrefs.DeleteKey("Katsuro.Champion.v1");
            Check(RunSession.ReadChampion() == null, "First run has no champion");
            Check(RunSession.CleanName("  <A>\n ") == "A ".Trim(), "Names are sanitized");
            Check(RunSession.CleanName("") == "挑戦者", "Empty name has a default");
            Check(RunSession.CleanName(new string('x', 30)).Length == 16, "Name length is bounded");
            RunSession.Begin("Winner", 2, 1);
            Check(RunSession.Lives == 2 && RunSession.Active, "A run starts with two lives");
            Check(RunSession.ConsumeLife() && RunSession.Lives == 1, "First death preserves the run");
            Check(!RunSession.ConsumeLife() && RunSession.Lives == 0, "Second death exhausts lives");
            RunSession.Complete(false);
            Check(RunSession.ReadChampion() == null, "Defeat does not create a champion");
            RunSession.Begin("Winner", 2, 1);
            RunSession.Complete(true);
            var winner = RunSession.ReadChampion();
            Check(winner != null && winner.Name == "Winner" && winner.Attack == 2 && winner.Defense == 1, "Victory persists name and both equipment choices");
            RunSession.Begin("Loser", 0, 0);
            Check(RunSession.Opponent.Name == "Winner", "Next run snapshots the previous champion");
            RunSession.Complete(false);
            Check(RunSession.ReadChampion().Name == "Winner", "Defeat preserves champion");
            RunSession.Complete(true);
            Check(RunSession.ReadChampion().Name == "Winner", "A finished run cannot overwrite its result");
            PlayerPrefs.SetString("Katsuro.Champion.v1", "broken json");
            Check(RunSession.ReadChampion() == null, "Corrupt saves fail safely");
            PlayerPrefs.SetString("Katsuro.Champion.v1", "{\"Version\":1,\"Attack\":99,\"Defense\":0}");
            Check(RunSession.ReadChampion() == null, "Invalid equipment IDs fail safely");
            PlayerPrefs.DeleteKey("Katsuro.Champion.v1");
            RunSession.Begin("Ghost", 0, 0);
            using (var gauge = new SkillGauge(100, 10))
            using (var ghost = new PlayerGhost(gauge))
            {
                Check(ghost.TryBegin() && gauge.Value == 80, "Ghost consumes activation cost once");
                Check(!ghost.TryBegin() && gauge.Value == 80, "Holding cannot restart or charge twice");
                ghost.Tick(1);
                Check(Mathf.Approximately(gauge.Value, 75), "Ghost drains gauge while held");
                ghost.End();
                Check(!ghost.IsGhosting && !ghost.TryBegin(), "Release ends ghost and enforces cooldown");
                ghost.Tick(1);
                gauge.TryConsume(gauge.Value - 3);
                Check(ghost.TryBegin() && ghost.IsBrief && gauge.Value == 0, "Insufficient gauge starts a brief ghost");
                gauge.Add(100);
                ghost.Tick(0.2f);
                Check(!ghost.IsGhosting, "Rewards cannot extend the brief fallback");
                ghost.Tick(1);
                gauge.TryConsume(gauge.Value);
                Check(ghost.TryBegin(), "Zero gauge still gets the specified brief fallback");
                ghost.Tick(0.2f);
                Check(!ghost.TryBegin(), "Empty-gauge spam is rate limited");
            }
            RunSession.Begin("Cost", 0, 1);
            using (var gauge = new SkillGauge(100, 10))
            using (var ghost = new PlayerGhost(gauge))
            {
                ghost.TryBegin();
                Check(Mathf.Approximately(gauge.Value, 86), "Ghost equipment reduces activation cost");
                ghost.Tick(1);
                Check(Mathf.Approximately(gauge.Value, 82.5f), "Ghost equipment reduces ongoing cost");
            }
            var rules = GameplayRules.Current;
            Check(rules.Regen(1) > rules.Regen(0.1f), "High HP improves natural recovery");
            Check(rules.SelfCost(1) < rules.SelfCost(0.1f), "High HP reduces self-sacrifice cost");
            Check(rules.AvoidWindow(1) > rules.AvoidWindow(0.1f), "High HP widens just-avoid timing");
            RunSession.Begin("Equipment", 1, 2);
            Check(RunSession.HitGainMultiplier == 2 && RunSession.RegenMultiplier > 1, "Soul and breath equipment effects apply");
            RunSession.Begin("Equipment", 2, 0);
            Check(RunSession.DamageMultiplier(0.1f) > RunSession.DamageMultiplier(1) && RunSession.IncomingMultiplier < 1, "Low HP offense and defensive armor apply");
            using (var gauge = new SkillGauge(100, 0))
            using (var self = new PlayerSelfSacrifice(gauge))
            {
                self.CostMultiplier = 0.5f; self.Begin(); self.Tick(1);
                Check(Mathf.Approximately(gauge.Value, 95), "Self-sacrifice uses its HP-dependent cost multiplier");
            }
            PlayerPrefs.DeleteKey("Katsuro.Champion.v1"); PlayerPrefs.Save();
            Debug.Log("GAMEPLAY_RESULT: " + count + " passed");
            EditorApplication.Exit(0);
        }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }
}
