import json,re,struct
import disassemble as d
from pathlib import Path
ui=re.search(r'public class UIManagerScript :[^\n]+\n\{(.*?)\n\}',d.dump_text,re.S)[1]
fields={int(off,16):name for name,off in re.findall(r'(?:public|private) (?!static )[\w\[\]<>.]+ (\w+); // (0x[\da-fA-F]+)',ui)}
records=[]
floor_branches={0x1074374:'material3',0x1074394:'material1',0x10743b4:'material2',0x10743d4:'material4',0x10743f4:'material5',0x1074414:'material6'}
for part in range(1,40):
    target=0x10641c8+d.read_va(0x62169b+part-1,1)[0]*4
    load=list(d.md.disasm(d.read_va(target,4),target))[0]
    bgfield=fields[load.operands[1].mem.disp]
    floortarget=0x1074374+d.read_va(0x62177c+part-1,1)[0]*4 if part<=38 else 0x1074394
    records.append({'parte':part,'backgroundField':bgfield,'floorMaterialField':floor_branches[floortarget],
                    'disableDirectionalLight':part>=36})
floatat=lambda a:struct.unpack('<f',d.read_va(a,4))[0]
document={'evidence':['UIManagerScript.doElegirFondo VA 0x1064148 table VA0x62169B',
                      'ConfigSuelo.Awake VA0x10742A8 table VA0x62177C'],
    'parts':records,
    'floor':{'removeTierraIfLocalYLessThan':floatat(0x6000a0)},
    'night':[{'parte':36,'assetRange':[1851,1900],'backgroundField':fields[0x1c0],
             'defaultBackgroundRGB':floatat(0x60009c),'defaultAmbientRGB':floatat(0x5ffef8),
             'overrides':[{'assets':[1851,1860],'backgroundRGB':floatat(0x6001b0),'ambientRGB':floatat(0x600188)},
                          {'assets':[1861,1870],'backgroundRGB':floatat(0x5ffef8),'ambientRGB':floatat(0x5ffd70)}]},
            {'parte':37,'assetRange':[1901,1950],'backgroundField':fields[0x1a0],
             'defaultBackgroundRGB':floatat(0x600054),'defaultAmbientRGB':floatat(0x5ffef8),
             'overrides':[{'assets':[1901,1910],'backgroundRGB':floatat(0x5ffc30),'ambientRGB':floatat(0x5ffc98)},
                          {'assets':[1911,1920],'backgroundRGB':floatat(0x5ffb50),'ambientRGB':floatat(0x5ffc98)}]},
            {'parte':[38,39],'backgroundField':fields[0x1a0],
             'backgroundRGB':floatat(0x6000a8),'ambientRGB':floatat(0x5ffc94)}]}
(d.ROOT/'native-presentation-map.json').write_text(json.dumps(document,ensure_ascii=False,indent=2),'utf-8')
print(json.dumps(document,ensure_ascii=False))
