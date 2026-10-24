"""Read-only raw bundle versus exported water material/texture evidence."""
import hashlib, json, pathlib, re, sys

project=pathlib.Path(__file__).resolve().parents[2]
workspace=project.parent
sys.path.insert(0,str(project/'Validation/ShaderAnalysis/python'))
import UnityPy
from PIL import Image

environment=UnityPy.load(str(workspace/'raw/base/assets/bin/Data/data.unity3d'))
objects={obj.path_id:obj for obj in environment.objects}
material_names={'MatGrRo_1','MatGrRo_C','MatGrRo_chorro 6','MatGr16','MatGr16_2','MatGrZ_1'}
texture_names={'fontain21','fontain20','fontain1','fontain5'}
rows=[]
for obj in environment.objects:
    if obj.type.name not in {'Material','Texture2D'}: continue
    data=obj.read()
    if (obj.type.name=='Material' and data.m_Name in material_names):
        tree=obj.read_typetree(); properties=tree.get('m_SavedProperties',{})
        shader=data.m_Shader.read(); texenv=dict(properties.get('m_TexEnvs',[])).get('_MainTex',{})
        rows.append({'kind':'Material','name':data.m_Name,'pathId':obj.path_id,'shader':shader.m_ParsedForm.m_Name,
                     'customRenderQueue':tree.get('m_CustomRenderQueue'),'MainTex':texenv,
                     'colors':dict(properties.get('m_Colors',[]))})
    elif(obj.type.name=='Texture2D' and data.m_Name in texture_names):
        tree=obj.read_typetree(); image=data.image.convert('RGBA'); path=project/'Assets/Texture2D'/(data.m_Name+'.png')
        exported=Image.open(path).convert('RGBA')
        alpha=image.getchannel('A').getextrema()
        rows.append({'kind':'Texture2D','name':data.m_Name,'pathId':obj.path_id,'size':list(image.size),
                     'originalTextureFormat':tree.get('m_TextureFormat'),'originalColorSpace':tree.get('m_ColorSpace'),
                     'originalTextureSettings':tree.get('m_TextureSettings'),'originalMips':tree.get('m_MipCount'),
                     'alphaRange':list(alpha),'rawDecodedEqualsExportPNG':image.size==exported.size and image.tobytes()==exported.tobytes(),
                     'verticalFlipDecodedEqualsExportPNG':image.size==exported.size and image.transpose(Image.Transpose.FLIP_TOP_BOTTOM).tobytes()==exported.tobytes(),
                     'rawDecodedPixelSHA256':hashlib.sha256(image.tobytes()).hexdigest(),'exportPNGPixelSHA256':hashlib.sha256(exported.tobytes()).hexdigest()})

output=pathlib.Path(__file__).with_name('raw-water-material-texture-audit.json')
output.write_text(json.dumps({'source':'raw/base/assets/bin/Data/data.unity3d','rows':rows},ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(rows,ensure_ascii=True,indent=2))
