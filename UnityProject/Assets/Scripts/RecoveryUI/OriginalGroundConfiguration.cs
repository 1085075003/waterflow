using System;
using System.Collections.Generic;
using Recovered.FlowWater;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Authored ground configuration recovered from SaveLoadManager native methods.</summary>
public static class OriginalGroundConfiguration
{
    public sealed class GroundChange
    { public string name; public int code; public Vector3 before, after; }
    public static IReadOnlyList<GroundChange> LastChanges { get { return changes; } }
    public static int GeneratedBorderCount { get; private set; }
    public static float NativeFloorPlaneY { get; private set; } = -8;
    static readonly List<GroundChange> changes = new List<GroundChange>();
    static readonly Dictionary<Transform,Vector3> baseline = new Dictionary<Transform,Vector3>();
    static readonly Dictionary<GameObject,bool> tierraVisibility = new Dictionary<GameObject,bool>();
    static int sceneHandle = -1;
    static GameObject borders;
    static readonly float[] deltas = { 0, -30.399999618530273f, -22.799999237060547f, -15.199999809265137f,
        -7.599999904632568f, 7.599999904632568f, 15.199999809265137f, 22.799999237060547f, 30.399999618530273f };

    public static void Apply(Transform pieceRoot, int assetId)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!OriginalScenePresentation.IsGameplayScene(scene) || pieceRoot == null) return;
        if (sceneHandle != scene.handle)
        {
            baseline.Clear(); tierraVisibility.Clear(); sceneHandle = scene.handle;
            foreach (Transform tile in OriginalScenePresentation.Components<Transform>(scene))
                if (!tile.IsChildOf(pieceRoot) && tile.CompareTag("Map")) baseline[tile] = tile.position;
            foreach (ConfigSuelo floor in OriginalScenePresentation.Components<ConfigSuelo>(scene))
                if (floor.tierra != null) tierraVisibility[floor.tierra] = floor.tierra.activeSelf;
        }
        foreach (var item in baseline) if (item.Key != null) item.Key.position = item.Value;
        // The recovered runtime reuses play scenes. Restore the initial Awake
        // result before applying another layout, including after theme refreshes.
        foreach (var item in tierraVisibility) if (item.Key != null) item.Key.SetActive(item.Value);
        if (borders != null) { borders.SetActive(false); UnityEngine.Object.Destroy(borders); }
        borders = null; GeneratedBorderCount = 0; changes.Clear(); NativeFloorPlaneY = -8;
        RecoveryPiece[] pieces = pieceRoot.GetComponentsInChildren<RecoveryPiece>(true);
        foreach (RecoveryPiece piece in pieces)
        {
            // ControlLevel.Awake hides its editor-only marker in normal game mode.
            ControlLevel marker = piece.GetComponent<ControlLevel>();
            if (marker != null && UIManagerSelScript.var_design == 0 && marker.objeto != null) marker.objeto.SetActive(false);
            if (piece.BasePrefab.IndexOf("ConfigLevel", StringComparison.Ordinal) < 0) continue;
            NativeFloorPlaneY = -8;
            string text = piece.OriginalConfiguration ?? "";
            int start = text.IndexOf("sc",StringComparison.Ordinal), end = text.IndexOf("ec",StringComparison.Ordinal);
            if (start < 0 || end <= start+2) continue;
            // doRescatarConfig resets these two adjacent floats for each ConfigLevel.
            for (int offset = start+2; offset+2 < end; offset += 3)
            {
                string name = "Base" + text.Substring(offset,2);
                int code = text[offset+2]-'0'; if (code < 1 || code > 8) continue;
                foreach (var tile in baseline)
                {
                    if (tile.Key == null || !tile.Key.gameObject.activeInHierarchy || tile.Key.name.IndexOf(name,StringComparison.Ordinal) < 0) continue;
                    if (code == 1) NativeFloorPlaneY = -36;
                    else if (code == 2) NativeFloorPlaneY = Mathf.Min(NativeFloorPlaneY,-28);
                    else if (code == 3) NativeFloorPlaneY = Mathf.Min(NativeFloorPlaneY,-24);
                    else if (code == 4) NativeFloorPlaneY = Mathf.Min(NativeFloorPlaneY,-14);
                    Vector3 before = tile.Key.position; tile.Key.position = before + Vector3.up * deltas[code];
                    changes.Add(new GroundChange { name=tile.Key.name, code=code, before=before, after=tile.Key.position });
                }
            }
        }
        SaveLoadManager loader = null;
        foreach (SaveLoadManager item in OriginalScenePresentation.Components<SaveLoadManager>(scene)) { loader=item; break; }
        if (loader == null) return;
        if (loader.customPlane != null)
        { Vector3 position=loader.customPlane.transform.position; position.y=NativeFloorPlaneY; loader.customPlane.transform.position=position; }
        int part=OriginalScenePresentation.PartForLevel(assetId);
        bool subterranean = part > 11 || part < 0 || ((0xf5e & (1 << part)) == 0);
        if (!subterranean || loader.prefabBorde == null) return;
        int size = SizeForPart(part); float edge = size == 5 ? 0 : size == 7 ? -40 : -20;
        float minY=0;
        foreach (RecoveryPiece piece in pieces)
            if (piece.BasePrefab.IndexOf("BloqueTierraFijo",StringComparison.Ordinal)>=0 &&
                (piece.InitialPosition.x==edge || piece.InitialPosition.z==edge)) minY=Mathf.Min(minY,piece.InitialPosition.y);
        if (minY >= 0) return;
        int bottom=Mathf.RoundToInt((minY-3.6500000953674316f)/7.599999904632568f);
        borders=new GameObject("Recovered original underground border");
        string lastRow="Base"+(char)('A'+size-1);
        // Native doSubterraneo iterates active Map2 children of each authored Base.
        foreach (Transform item in OriginalScenePresentation.Components<Transform>(scene))
        {
            if (!item.gameObject.activeInHierarchy || !item.CompareTag("Map2") || item.parent==null || item.IsChildOf(pieceRoot)) continue;
            Transform tile=item.parent; string tileName=tile.name;
            int top=Mathf.RoundToInt(tile.localPosition.y/7.599999904632568f);
            if (tileName.IndexOf("BaseA",StringComparison.Ordinal)>=0) AddColumn(loader.prefabBorde,tile.position,top,bottom,new Vector3(0,0,-9.949999809265137f),Quaternion.identity);
            bool firstColumn=false,lastColumn=false;
            for(char row='A';row<='G';row++)
            { firstColumn |= tileName=="Base"+row+"1"; lastColumn |= tileName=="Base"+row+size; }
            if(firstColumn) AddColumn(loader.prefabBorde,tile.position,top,bottom,new Vector3(-9.949999809265137f,0,0),Quaternion.Euler(0,90,0));
            if(lastColumn) AddColumn(loader.prefabBorde,tile.position,top,bottom,new Vector3(9.949999809265137f,0,0),Quaternion.Euler(0,-90,0));
            if(tileName.IndexOf(lastRow,StringComparison.Ordinal)>=0) AddColumn(loader.prefabBorde,tile.position,top,bottom,new Vector3(0,0,9.949999809265137f),Quaternion.identity);
        }
    }
    static void AddColumn(GameObject prefab,Vector3 position,int top,int bottom,Vector3 offset,Quaternion rotation)
    {
        for(int height=top;height>bottom;height--)
        {
            Vector3 location=position+offset; location.y=height*7.599999904632568f-3.6500000953674316f;
            GameObject item=UnityEngine.Object.Instantiate(prefab,location,rotation); item.transform.SetParent(borders.transform,true);
            GeneratedBorderCount++;
        }
    }
    static int SizeForPart(int part)
    {
        switch(part)
        {
            case 1:case 2:case 3:case 4:case 9:case 10:case 13:case 20:case 26:case 32:case 36:return 5;
            case 8:case 12:case 16:case 23:case 28:case 35:case 39:return 7;
            default:return 6;
        }
    }
}
