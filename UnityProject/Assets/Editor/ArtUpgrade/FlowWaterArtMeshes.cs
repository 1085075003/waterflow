#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Recovered.FlowWater.Editor
{
    /// <summary>Offline bevel pipeline. Only visual MeshFilter references are changed.</summary>
    public static class FlowWaterArtMeshes
    {
        const string Work = "Validation/ArtUpgrade/MeshWork";
        const string Output = "Assets/ArtUpgrade/Meshes";
        static readonly Regex Documents = new Regex(@"(?ms)^--- !u!(\d+) &(-?\d+)[^\r\n]*\r?\n(.*?)(?=^--- !u!|\z)");
        static readonly Regex Guid = new Regex(@"guid: ([a-f0-9]{32})");
        [Serializable] public sealed class FaceGroup { public int[] indices; }
        [Serializable] public sealed class MeshData
        {
            public string sourceGuid, sourcePath, name;
            public Vector3[] vertices, normals;
            public Vector2[] uv;
            public List<FaceGroup> submeshes = new List<FaceGroup>();
            public Vector3 boundsCenter, boundsSize;
        }
        [Serializable] public sealed class Use
        {
            public string asset, filterId, sourceGuid, objectName, materials;
        }
        [Serializable] public sealed class Manifest
        {
            public string generatedUtc;
            public int scannedFilters, rendererPairs, eligibleRenderers, missingMeshes, unreadableMeshes, sizeFilteredMeshes;
            public List<string> diagnostics = new List<string>();
            public List<MeshData> meshes = new List<MeshData>();
            public List<Use> uses = new List<Use>();
        }
        [Serializable] public sealed class Applied
        {
            public string sourceGuid, newGuid, meshPath;
            public int sourceVertices, newVertices, sourceTriangles, newTriangles, submeshes;
            public float maxBoundsDifference;
        }
        [Serializable] public sealed class Report
        {
            public string completedUtc;
            public int replacedFilters, changedAssets;
            public bool nonMeshFilterDocumentsIdentical = true;
            public List<Applied> meshes = new List<Applied>();
            public List<string> assets = new List<string>();
        }
        sealed class Doc { public string type, id, body, full; }
        static IEnumerable<Doc> Parse(string text)
        {
            foreach (Match m in Documents.Matches(text))
                yield return new Doc {type=m.Groups[1].Value,id=m.Groups[2].Value,body=m.Groups[3].Value,full=m.Value};
        }
        static string Field(string text, string field)
        {
            Match m = Regex.Match(text, @"(?m)^  " + Regex.Escape(field) + @":\s*(.+)$");
            return m.Success ? m.Groups[1].Value.Trim() : "";
        }
        static string ObjectId(Doc d)
        {
            Match m = Regex.Match(Field(d.body,"m_GameObject"),@"fileID: (-?\d+)");
            return m.Success ? m.Groups[1].Value : "";
        }
        static bool StructuralMaterial(string path)
        {
            string name = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            return name.StartsWith("muro-bloco") || name.StartsWith("material_pilar") ||
                   name.StartsWith("mat_pilar") || name == "ladrillos" || name == "walls_1" ||
                   name.StartsWith("fountain-material");
        }
        static bool Eligible(Doc renderer, string objectName, out string description)
        {
            description = "";
            Match m = Regex.Match(renderer.body, @"(?m)^  m_Materials:\r?\n((?:  -[^\r\n]*\r?\n)+)");
            if (!m.Success) return false;
            var paths = Guid.Matches(m.Groups[1].Value).Cast<Match>()
                .Select(g => AssetDatabase.GUIDToAssetPath(g.Groups[1].Value)).ToArray();
            if(paths.Length == 0 || paths.Any(p => !StructuralMaterial(p))) return false;
            // Water surfaces, particles, grass and arrival interfaces never enter this pipeline.
            string n = objectName.ToLowerInvariant();
            if (new[]{"agua","water","plano","chorro","cascade","cascada","grass","hierba","suelo","trigger","particle"}.Any(n.Contains)) return false;
            description = string.Join("; ", paths);
            return true;
        }
        static IEnumerable<string> SourceAssets()
        {
            return Directory.GetFiles("Assets/GameObject", "*.prefab", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles("Assets/Scenes", "*.unity", SearchOption.AllDirectories))
                .Concat(new[]{"Assets/Settings.unity", "Assets/Store.unity"}.Where(File.Exists))
                .Select(p=>p.Replace('\\','/')).OrderBy(p=>p);
        }
        [MenuItem("Tools/Flow Water Art/1 Export structural meshes")]
        public static void Export()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before exporting source assets.");
            Directory.CreateDirectory(Work);
            var manifest = new Manifest {generatedUtc=DateTime.UtcNow.ToString("O")};
            var seen = new HashSet<string>();
            foreach(string asset in SourceAssets())
            {
                var docs=Parse(File.ReadAllText(asset)).ToArray();
                var objects=docs.Where(d=>d.type=="1").ToDictionary(d=>d.id,d=>Field(d.body,"m_Name"));
                var renderers=docs.Where(d=>d.type=="23").GroupBy(ObjectId).ToDictionary(g=>g.Key,g=>g.First());
                foreach(var filter in docs.Where(d=>d.type=="33"))
                {
                    manifest.scannedFilters++;
                    string objectId=ObjectId(filter), materialNames;
                    Doc renderer;
                    if(!renderers.TryGetValue(objectId,out renderer)) continue;
                    manifest.rendererPairs++;
                    string objectName=objects.ContainsKey(objectId)?objects[objectId]:"";
                    if(!Eligible(renderer,objectName,out materialNames)) continue;
                    manifest.eligibleRenderers++;
                    Match guid=Guid.Match(Field(filter.body,"m_Mesh"));
                    if(!guid.Success) continue;
                    string sourceGuid=guid.Groups[1].Value;
                    string path=AssetDatabase.GUIDToAssetPath(sourceGuid);
                    if(path.StartsWith(Output,StringComparison.Ordinal)) continue;
                    Mesh mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if(mesh==null){manifest.missingMeshes++;if(manifest.diagnostics.Count<12)manifest.diagnostics.Add("Missing mesh "+path);continue;}
                    if(!mesh.isReadable){manifest.unreadableMeshes++;if(manifest.diagnostics.Count<12)manifest.diagnostics.Add("Unreadable mesh "+path);continue;}
                    if(mesh.vertexCount<8||mesh.vertexCount>50000){manifest.sizeFilteredMeshes++;continue;}
                    bool triangles=true;
                    for(int i=0;i<mesh.subMeshCount;i++) if(mesh.GetTopology(i)!=MeshTopology.Triangles) triangles=false;
                    if(!triangles) continue;
                    manifest.uses.Add(new Use{asset=asset,filterId=filter.id,sourceGuid=sourceGuid,objectName=objectName,materials=materialNames});
                    if(!seen.Add(sourceGuid)) continue;
                    var data=new MeshData{sourceGuid=sourceGuid,sourcePath=path,name=mesh.name,vertices=mesh.vertices,normals=mesh.normals,uv=mesh.uv,boundsCenter=mesh.bounds.center,boundsSize=mesh.bounds.size};
                    for(int i=0;i<mesh.subMeshCount;i++) data.submeshes.Add(new FaceGroup{indices=mesh.GetIndices(i)});
                    manifest.meshes.Add(data);
                    File.WriteAllText(Path.Combine(Work,sourceGuid+".source.json"),JsonUtility.ToJson(data));
                }
            }
            File.WriteAllText(Path.Combine(Work,"manifest.json"),JsonUtility.ToJson(manifest,true));
            Debug.Log("Art mesh export: "+manifest.meshes.Count+" unique structural meshes, "+manifest.uses.Count+" visual filters. "+Work+"/manifest.json");
        }
        [MenuItem("Tools/Flow Water Art/2 Import and apply bevel meshes")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before applying visual meshes.");
            Manifest manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Path.Combine(Work,"manifest.json")));
            Directory.CreateDirectory(Output);
            AssetDatabase.Refresh();
            var replacements=new Dictionary<string,string>();
            var report=new Report();
            foreach(MeshData source in manifest.meshes)
            {
                string jsonPath=Path.Combine(Work,source.sourceGuid+".bevel.json");
                if(!File.Exists(jsonPath)) continue;
                MeshData data=JsonUtility.FromJson<MeshData>(File.ReadAllText(jsonPath));
                if(data.vertices==null||data.vertices.Length==0||data.submeshes.Count!=source.submeshes.Count) throw new InvalidDataException("Invalid bevel output "+jsonPath);
                var mesh=new Mesh{name=source.name+"_GardenBevel",indexFormat=data.vertices.Length>65535?IndexFormat.UInt32:IndexFormat.UInt16};
                mesh.vertices=data.vertices;
                if(data.normals!=null&&data.normals.Length==data.vertices.Length) mesh.normals=data.normals;
                if(data.uv!=null&&data.uv.Length==data.vertices.Length) mesh.uv=data.uv;
                mesh.subMeshCount=data.submeshes.Count;
                for(int i=0;i<data.submeshes.Count;i++) mesh.SetIndices(data.submeshes[i].indices,MeshTopology.Triangles,i,false);
                mesh.RecalculateBounds();
                if(mesh.normals.Length==0) mesh.RecalculateNormals();
                if(mesh.uv.Length==mesh.vertexCount) mesh.RecalculateTangents();
                float boundsDelta=(mesh.bounds.center-source.boundsCenter).magnitude+(mesh.bounds.size-source.boundsSize).magnitude;
                if(boundsDelta>Mathf.Max(0.00001f,source.boundsSize.magnitude*0.001f)) throw new InvalidDataException("Bevel output changed bounds: "+source.sourcePath+" "+boundsDelta);
                // Preserve the original AABB exactly; runtime camera fitting remains identical.
                mesh.bounds=new Bounds(source.boundsCenter,source.boundsSize);
                string path=Output+"/"+source.sourceGuid+"_GardenBevel.asset";
                Mesh existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(existing!=null){EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);}
                else AssetDatabase.CreateAsset(mesh,path);
                string newGuid=AssetDatabase.AssetPathToGUID(path);
                replacements[source.sourceGuid]=newGuid;
                report.meshes.Add(new Applied{sourceGuid=source.sourceGuid,newGuid=newGuid,meshPath=path,sourceVertices=source.vertices.Length,newVertices=data.vertices.Length,sourceTriangles=source.submeshes.Sum(s=>s.indices.Length/3),newTriangles=data.submeshes.Sum(s=>s.indices.Length/3),submeshes=data.submeshes.Count,maxBoundsDifference=boundsDelta});
            }
            AssetDatabase.SaveAssets();
            foreach(var group in manifest.uses.Where(u=>replacements.ContainsKey(u.sourceGuid)).GroupBy(u=>u.asset))
            {
                string text=File.ReadAllText(group.Key), original=text;
                var wanted=group.ToDictionary(u=>u.filterId);
                text=Documents.Replace(text,m=>{
                    Use use;
                    if(m.Groups[1].Value!="33"||!wanted.TryGetValue(m.Groups[2].Value,out use))return m.Value;
                    string expected="guid: "+use.sourceGuid;
                    if(!m.Value.Contains(expected))
                    {
                        if(m.Value.Contains("guid: "+replacements[use.sourceGuid])) return m.Value;
                        throw new InvalidOperationException("Source MeshFilter changed since export: "+group.Key+" #"+use.filterId);
                    }
                    report.replacedFilters++;
                    return m.Value.Replace(expected,"guid: "+replacements[use.sourceGuid]);
                });
                if(text==original)continue;
                string invariantBefore=Documents.Replace(original,m=>m.Groups[1].Value=="33"?"":m.Value);
                string invariantAfter=Documents.Replace(text,m=>m.Groups[1].Value=="33"?"":m.Value);
                if(invariantBefore!=invariantAfter) throw new InvalidOperationException("Visual mesh edit changed another YAML component: "+group.Key);
                Backup(group.Key);
                // All other YAML documents, including colliders, transforms and scripts, remain byte-identical.
                File.WriteAllText(group.Key,text,new System.Text.UTF8Encoding(false));
                report.changedAssets++;report.assets.Add(group.Key);
            }
            report.completedUtc=DateTime.UtcNow.ToString("O");
            File.WriteAllText("Validation/ArtUpgrade/mesh-apply-report.json",JsonUtility.ToJson(report,true));
            AssetDatabase.Refresh();
            Debug.Log("Art meshes applied: "+report.meshes.Count+" assets, "+report.replacedFilters+" visual references. Collider and gameplay documents preserved.");
        }
        static void Backup(string asset)
        {
            string target="Validation/ArtUpgrade/Before/"+asset;
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            if(!File.Exists(target))File.Copy(asset,target);
            if(File.Exists(asset+".meta")&&!File.Exists(target+".meta"))File.Copy(asset+".meta",target+".meta");
            string meshTarget="Validation/ArtUpgrade/Before/VisualMeshes/"+asset;
            Directory.CreateDirectory(Path.GetDirectoryName(meshTarget));
            if(!File.Exists(meshTarget))File.Copy(asset,meshTarget);
        }
    }
}
#endif
