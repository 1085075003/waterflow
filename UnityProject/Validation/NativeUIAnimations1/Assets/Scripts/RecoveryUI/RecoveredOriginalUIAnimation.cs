using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

/// <summary>Replays existing UI controllers and the original serialized star delay list.</summary>
public sealed class RecoveredOriginalUIAnimation : MonoBehaviour
{
    const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    public int StarTargetCount { get; private set; }
    public int StarsEnabledCount { get; private set; }
    public int StartCount { get; private set; }

    public static void PlayState(GameObject panel, string state)
    {
        if (panel == null) return;
        Animator animator = panel.GetComponent<Animator>();
        if (animator == null || animator.runtimeAnimatorController == null) return;
        animator.enabled = true;
        int hash = Animator.StringToHash(state);
        if (animator.HasState(0, hash)) { animator.Play(hash, 0, 0); animator.Update(0); }
    }

    public static RecoveredOriginalUIAnimation ShowComplete(GameObject panel)
    {
        if (panel == null) return null;
        // ControladorJuego.mostrarComplete1074F78 selects exactly these states.
        PlayState(panel, PlayerPrefs.GetInt("esTablet", 0) == 1 ? "an_complete2d" : "an_complete");
        var driver = panel.GetComponent<RecoveredOriginalUIAnimation>();
        if (driver == null) driver = panel.AddComponent<RecoveredOriginalUIAnimation>();
        driver.BeginOriginalStarDelays();
        return driver;
    }

    void BeginOriginalStarDelays()
    {
        StopAllCoroutines(); StarTargetCount = StarsEnabledCount = 0; StartCount++;
        // Native EnableObjectsWithDelay.Start1068038 disables only the assigned
        // targets and starts one WaitForSeconds for each serialized startDelay.
        // Source scenes assign Star_B(.2); the other two entries are null.
        foreach (MonoBehaviour original in GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (original == null || original.GetType().FullName != "Game.EnableObjectsWithDelay.EnableObjectsWithDelay") continue;
            var field = original.GetType().GetField("objectsWithDelays", Fields);
            var entries = field == null ? null : field.GetValue(original) as IList;
            if (entries == null) continue;
            foreach (object entry in entries)
            {
                if (entry == null) continue;
                var target = entry.GetType().GetField("targetObject", Fields).GetValue(entry) as GameObject;
                if (target == null) continue;
                float delay = Convert.ToSingle(entry.GetType().GetField("startDelay", Fields).GetValue(entry));
                StarTargetCount++; target.SetActive(false);
                StartCoroutine(EnableAfterDelay(target, delay));
            }
        }
    }

    IEnumerator EnableAfterDelay(GameObject target, float delay)
    {
        // Original coroutine1068608 uses scaled WaitForSeconds, then SetActive.
        yield return new WaitForSeconds(delay);
        if (target != null) { target.SetActive(true); StarsEnabledCount++; }
    }

    public void PlayConfiguredStarEnd()
    {
        // Native Update1068320 maps E(0x65) to this debug animation. It applies
        // to the same assigned list; never enable the unused Star_D objects.
        foreach (MonoBehaviour original in GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (original == null || original.GetType().FullName != "Game.EnableObjectsWithDelay.EnableObjectsWithDelay") continue;
            var field = original.GetType().GetField("objectsWithDelays", Fields);
            var entries = field == null ? null : field.GetValue(original) as IList;
            if (entries == null) continue;
            foreach (object entry in entries)
            {
                if (entry == null) continue;
                var target = entry.GetType().GetField("targetObject", Fields).GetValue(entry) as GameObject;
                if (target != null) PlayState(target, "Star_End");
            }
        }
    }

    void Update() { if (Input.GetKeyDown(KeyCode.E)) PlayConfiguredStarEnd(); }
}
