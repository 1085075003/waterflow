// Recovery data verification: static JSON/coordinate replay only, never original game-code execution.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Recovered.FlowWater
{
    [Serializable]
    public sealed class RecoveryDataValidationEntry
    {
        public int assetId;
        public string classification;
        public string reason;
        public int pieceCount;
        public string solutionKind;
        public int stepCount;
        public bool requiresFinalLayoutRebuild;
    }

    [Serializable]
    public sealed class RecoveryDataValidationReport
    {
        public string scope = "Decoded gm/so integrity and saved coordinate replay only. A success does not establish restoration of water physics, complex fijo/excep mechanisms, colors or original source code.";
        public int total, success, unsupported, failure;
        public List<RecoveryDataValidationEntry> entries = new List<RecoveryDataValidationEntry>();
    }

    public static class RecoveryDataValidation
    {
        public static RecoveryDataValidationReport ValidateAllData()
        {
            RecoveryCatalog catalog = RecoveryCatalog.Load();
#if UNITY_EDITOR
            if (catalog == null) { RecoveryCatalogBuilder.Build(); catalog = RecoveryCatalog.Load(); }
#endif
            if (catalog == null) throw new InvalidOperationException("RecoveryCatalog is missing.");
            var aliases = new Dictionary<string, string>(StringComparer.Ordinal);
            RecoveryAliasFile aliasFile = JsonUtility.FromJson<RecoveryAliasFile>(catalog.aliases.text);
            foreach (RecoveryAlias alias in aliasFile.aliases) aliases[alias.originalName] = alias.basePrefab + "#" + alias.colorSuffix;
            var report = new RecoveryDataValidationReport();
            foreach (RecoveryLevelEntry entry in catalog.levels)
            {
                var result = new RecoveryDataValidationEntry { assetId = entry.assetId, classification = "failure" };
                try
                {
                    if (entry.layout == null || entry.solution == null) throw new FormatException("Missing paired gm/so JSON.");
                    RecoverySave layout = JsonUtility.FromJson<RecoveryEnvelope>(entry.layout.text).data;
                    RecoverySave solution = JsonUtility.FromJson<RecoveryEnvelope>(entry.solution.text).data;
                    if (layout == null || !layout.IsValidLayout()) throw new FormatException("Invalid gm layout arrays.");
                    result.pieceCount = layout.pieza.Count;
                    result.solutionKind = solution == null ? "missing" : solution.__type__;
                    foreach (string name in layout.pieza)
                        if (!aliases.ContainsKey(name)) throw new FormatException("Missing prefab alias: " + name);
                    for (int i = 0; i < layout.pieza.Count; i++)
                        if (!Finite(layout.Position(i))) throw new FormatException("Nonfinite layout coordinate.");
                    if (solution != null && solution.IsValidLayout())
                    {
                        var inventory = new Dictionary<string, int>(StringComparer.Ordinal);
                        foreach (string name in layout.pieza) Add(inventory, aliases[name], 1);
                        foreach (string name in solution.pieza)
                        {
                            string key;
                            if (!aliases.TryGetValue(name, out key)) throw new FormatException("Missing solution alias: " + name);
                            Add(inventory, key, -1);
                        }
                        bool same = true;
                        foreach (int count in inventory.Values) if (count != 0) { same = false; break; }
                        for (int i = 0; i < solution.pieza.Count; i++)
                            if (!Finite(solution.Position(i))) throw new FormatException("Nonfinite solution coordinate.");
                        result.requiresFinalLayoutRebuild = !same;
                        result.classification = "success";
                        result.reason = same ? "All original pieces can be assigned to recorded final geometry." :
                            "Original gm/so inventories differ. Native total preview reloads the complete so layout; runtime supports this reconstruction without inventing pieces or movements.";
                    }
                    else if (solution != null && solution.IsValidSteps())
                    {
                        result.stepCount = solution.move.Count;
                        var positions = new Vector3[layout.pieza.Count];
                        for (int i = 0; i < positions.Length; i++) positions[i] = layout.Position(i);
                        bool emptySamePositionCapture = false;
                        for (int i = 0; i < solution.move.Count; i++)
                        {
                            Vector3 origin = RecoveredFlowGame.StepPosition(solution.orix[i], solution.oriy[i], solution.oriz[i]);
                            Vector3 destination = RecoveredFlowGame.StepPosition(solution.desx[i], solution.desy[i], solution.desz[i]);
                            int found = -1; float nearest = .7f * .7f;
                            for (int j = 0; j < positions.Length; j++)
                            {
                                float distance = (positions[j] - origin).sqrMagnitude;
                                if (distance <= nearest) { nearest = distance; found = j; }
                            }
                            bool marker = (origin - destination).sqrMagnitude < .01f;
                            if (found < 0)
                            {
                                if (solution.excep != null && i < solution.excep.Count && solution.excep[i] == "z") continue;
                                // HandleSolutionTotal1034164 skips a missing
                                // capture and103419C always advances the step.
                                // so1225's final empty-excep same-position record
                                // is a legal no-op, not an unknown z mechanism.
                                if (marker) { emptySamePositionCapture = true; continue; }
                                throw new FormatException("Recorded step " + (i + 1) + " has no piece at its origin.");
                            }
                            if (!marker) positions[found] = destination;
                        }
                        result.classification = "success";
                        result.reason = emptySamePositionCapture ?
                            "All actual movements replay. The empty same-position capture is a native no-op: HandleSolutionTotal skips missing actors and advances without inventing a piece." :
                            "Every recorded origin matches a real piece during coordinate replay; excep text remains unexecuted original mechanism data.";
                    }
                    else throw new FormatException("Invalid solution schema/arrays.");
                }
                catch (Exception exception) { result.classification = "failure"; result.reason = exception.Message; }
                report.total++;
                if (result.classification == "success") report.success++;
                else if (result.classification == "unsupported") report.unsupported++;
                else report.failure++;
                report.entries.Add(result);
            }
            return report;
        }

        static bool Finite(Vector3 position)
        {
            return !float.IsNaN(position.x) && !float.IsNaN(position.y) && !float.IsNaN(position.z) &&
                !float.IsInfinity(position.x) && !float.IsInfinity(position.y) && !float.IsInfinity(position.z);
        }

        static void Add(Dictionary<string, int> inventory, string key, int amount)
        {
            int value; inventory.TryGetValue(key, out value); inventory[key] = value + amount;
        }
    }
}
