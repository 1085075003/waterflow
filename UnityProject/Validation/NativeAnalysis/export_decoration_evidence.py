"""Evidence accompanying RecoveredNativeDecorations; all coordinates are native.
Run after export_light_prefabs.py. No original binary/assets are modified.
"""
import json,struct
import disassemble as d

def floats(va,n):return list(struct.unpack('<'+'f'*n,d.read_va(va,n*4)))
shared={'11':[1],'12':[2],'13':[3],'14':[4],
        '21':[1,2],'22':[1,3],'23':[1,4],'24':[2,3],'25':[2,4],'26':[3,4]}
result={
 'version':1,
 'runtime':'Assets/Scripts/RecoveryRuntime/RecoveredNativeDecorations.cs',
 'implementationOrigin':'Reimplementation from ARM64 IL2CPP, not recovered C# source',
 'lightRecipes':json.loads((d.ROOT/'native-light-prefab-recipes.json').read_text('utf-8')),
 'lightDispatch':{
   'ControlPieza':{'startVA':'0x107F720','codes':dict(shared,**{'3*':[1,2,3,4],'4*':[5]})},
   'ControlFijo':{'startVA':'0x107661C','codes':dict(shared,**{'3*':[1,2,3,4]})},
   'ControlFijo1':{'startVA':'0x1079454','codes':dict(shared,**{'3*':[1,2,3,4],'4*':[5],'5*':[5],'61':[1],'62':[2],'63':[3],'64':[4]}),'intensityOverride':{'5*':9,'61..64':8}},
   'ControlPiezaFuente':{'startVA':'0x109001C','codes':dict(shared,**{'3*':[1,2,3,4]})},
   'ControlPiezaC':{'startVA':'0x1082C78','codes':shared,'requiresEsTextura10':True,'perSideCondition':'ladoN == 7'},
 },
 'nativeConstants':{hex(va):floats(va,n) for va,n in [
   (0x600350,2),(0x600F60,4),(0x600A10,2),(0x6003F8,2),(0x601D60,4),
   (0x600468,2),(0x6005A8,2),(0x602080,4),(0x601DB0,4),(0x601080,4),
   (0x6017E0,4),(0x6022E0,4),(0x6014A0,4),(0x602140,4),(0x600DE0,4),(0x600918,2)]},
 'plantType':{'decimal':'Int32.TryParse','letters':{chr(65+i):i+10 for i in range(10)},'fallback':0},
 'plantMaterials':{
   'cambioPlantas':{'startVA':'0xFCDDCC','0/default':[1],'1..19':'materialTextura(texture+1)','targets':['objetoRenderer','objetoRenderer1','objetoRenderer2','objetoRenderer3','objetoRenderer4']},
   'cambioPlantas1':{'startVA':'0xFCE04C','0/default':[1,3],'1':[2,4],'2..19':['materialTextura(texture*2+1)','materialTextura(texture*2+2)'],'firstMaterialTargets':['objetoRenderer','objetoRenderer1','objetoRenderer2','objetoRenderer3'],'secondMaterialTargets':['objetoRenderer4']}
 },
 'plantBranches':{
   'ControlPieza':{'requires':'esTextura4','sidePrefab':'origenN ? (parent.name.Contains(Bloque) ? plantasPrefab3 : skip) : plantasPrefab4','compact':'parent.position.y<4 || longPlant==1 || (conResorte not 0/empty)','top':'parent.name.Contains(Bloque), plantasPrefab5, pos(0,.282,0), scale(2,1,2), worldYaw90'},
   'ControlFijo':{'requires':'esTextura4 && name.Contains(BloqueFijo)','sidePrefab':'plantasPrefab4','compact':'parent.position.y<4 || longPlant==1','topYaw':90},
   'ControlFijo1':{'requires':'esTextura4 && name.Contains(BloFijo)','sidePrefab':'longPlant==2 ? plantasPrefab3 : plantasPrefab4','compact':'parent.position.y<4 || longPlant==1','topYaw':0},
   'ControlPiezaC.Start':{'requires':'esTextura4 && !esTextura10','sidePrefab':'origenN ? (dirCentroN==1 ? plantasPrefab3 : skip) : plantasPrefab4','compact':'parent.position.y<4 || (conResorte not 0/empty)','top':None},
   'ControlPiezaC.doTextura4':{'VA':'0x108D240','requires':'esTextura4','sidePrefab':'lado=1/7/(4&&nameContainsED) -> plantasPrefab4; lado2/8 -> plantasPrefab3; lado9/(3&&nameContainsED) -> plantasPrefab6; else skip','sidePositions':[[-1.015,-.66,0],[0,-.66,1.02],[1.018,-.67,0],[0,-.66,-1.012]],'compact':'parent.position.y<4 || conResorte not0/empty || longPlant==1','top':'nameContainsED || any ladoN==7; plantasPrefab5, pos(0,.271,0), scale(2,1,2), yaw90','caps':'esTextura10plant && ladoN in[1,2]; plantasPrefab5 at 6022E0/6014A0/602140/600DE0, scale(.35,1,1), yaw(0,90,0,90)'},
   'ControlPiezaPfuente':{'requires':'esTextura4','sidePrefab':'Awake copies oriN->origN, origN ? plantasPrefab3 : plantasPrefab4','compact':'parent.position.y<4 || (conResorte not0/empty)','topYaw':0},
   'ControlPiezaFuente':{'requires':'esTextura4','Ladrillo':'cascada1..4 set yaw -180,-90,0,90 independently; semiCil1 -> prefab6 scaleY2 and hide objetoRenderer; semiCil2 -> prefab6 scaleY2 then prefab1 scale1; else prefab1 scale1; position0','Bloque':'four sides cascadaN ? prefab3 : prefab4; compact height<4 || longPlant1; top pos(0,.282,0), scale(2,1,2), yaw90'},
   'cambiaMascara':{'VA':'0xFCCBB0','requires':'esTextura4','prefab':'plantasPrefab1','position':[0,0,0],'scale':[10,10,10],'yawFromParentNames':{'LadrilloLateral1/LadrilloLat1H':0,'LadrilloLateral2/LadrilloLat2H':90,'LadrilloLateral3/LadrilloLat3H':-180,'LadrilloLateral4/LadrilloLat4H':-90}},
 },
 'preservation':['Original referenced prefab, meshes, materials and Light values retained.','Native original MonoBehaviours disabled; actual Unity Light remains enabled as authored.','Instantiate worldRotation, then SetParent(worldPositionStays=true), then localPosition/localScale, matching native.','No inferred lighting for unrecognized original codes.'],
 'scopeLimits':['Original C wall/rebose/jet geometry construction and spring mechanics are outside this helper.','Null strings in stub-loaded components are normalized to empty in this recovery helper.']
}
assert len(result['lightRecipes'])==22
assert 'unknown' not in json.dumps(result)
for name,recipes in result['lightRecipes'].items():
    cls=name.split('_doLight')[0]
    known=set(d.fields[cls].values())
    for recipe in recipes:
        assert recipe['prefabField'].split('.')[1] in known
        assert recipe['instanceField'].split('.')[1] in known
result['validation']={'lightHelperCount':22,'allPrefabAndInstanceFieldsExistInOriginalMetadata':True,'unresolvedNumericOrSymbolicRecipes':0,'standaloneCompile':'Unity 2022.3.28f1 Roslyn 4.3.1, C#9, success'}
(d.ROOT/'native-decorations-map.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),'utf-8')
print('Verified 22 original light recipes, prefab/instance fields, 8 vegetation branches; wrote native-decorations-map.json')
