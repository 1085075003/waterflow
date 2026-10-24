"""Create an immutable, reviewable localization batch; never writes Assets."""
import hashlib,json,pathlib,re,uuid

PROJECT=pathlib.Path(__file__).resolve().parents[2]
ANALYSIS=PROJECT/'Validation/NativeAnalysis'
OUT=PROJECT/'Validation/NativeLanguageBatch1'
DATA=json.loads((ANALYSIS/'native-language-texts.json').read_text('utf-8'))
assert not DATA['unsupported']

def quote(value): return json.dumps(value,ensure_ascii=False)
def sha(data): return hashlib.sha256(data).hexdigest()
def replace_body(text,name,body):
    m=re.search(r'(^\s*(?:public|private|protected|internal)?\s*(?:static\s+)?[\w.<>\[\]]+\s+'+re.escape(name)+r'\([^\n]*\)\s*\{)',text,re.M)
    if not m: raise ValueError(name)
    start=m.end(); depth=1; end=start
    while depth:
        depth+= (text[end]=='{')-(text[end]=='}');end+=1
    return text[:start]+'\n'+body+'\n\t'+text[end-1:]

def stage():
    if OUT.exists(): raise RuntimeError('Immutable batch already exists; use a new batch version.')
    OUT.mkdir(parents=True);(OUT/'Sources').mkdir();(OUT/'Evidence').mkdir()
    files=[]
    def put(target,text):
        f=OUT/'Sources'/(pathlib.Path(target).name+'.src');f.write_text(text,'utf-8')
        original=PROJECT/target
        files.append({'source':str(f.relative_to(OUT)).replace('\\','/'),'target':target,
                      'baseSha256':sha(original.read_bytes()) if original.exists() else None,
                      'sha256':sha(f.read_bytes())})
        if not original.exists():
            meta=OUT/'Sources'/(pathlib.Path(target).name+'.meta')
            meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n','utf-8')
            files.append({'source':str(meta.relative_to(OUT)).replace('\\','/'),'target':target+'.meta',
                          'baseSha256':None,'sha256':sha(meta.read_bytes())})
    header='''using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Original IL2CPP language literals and the original serialized Text targets.</summary>
public static class RecoveredNativeLocalization
{
    public const string OriginalLibrarySha256 = "'''+DATA['originalLibrarySha256']+'''";
    public static string Language { get { return PlayerPrefs.GetString("language", "en"); } }
    public static string[] SelectorCycle { get { return new[] {'''+','.join(quote(x) for x in DATA['selectorCycle'])+''' }; } }
    public static string[] PortCycle { get { return new[] {'''+','.join(quote(x) for x in DATA['portCycle'])+''' }; } }
    // No OS font replacement: English continues to use the authored Playtime asset.
    public static string Text(string method, string language, string argument = "")
    {
        string value;
        switch (method)
        {
'''
    lines=[header]
    for key,entry in DATA['methods'].items():
        vals=entry['values'];default=vals['unknown'];lines+=['            case '+quote(key)+':\n                switch (language)\n                {\n']
        for code,value in vals.items():
            if code=='unknown' or value==default:continue
            lines+=['                    case '+quote(code)+': value = '+quote(value)+'; break;\n']
        lines+=['                    default: value = '+quote(default)+'; break;\n                }\n                break;\n']
    lines+=['''            default: throw new ArgumentOutOfRangeException("method", method, "Unknown native localization method");
        }
        return value.Replace("{0}", argument ?? "");
    }
    public static string Reverse(string value) { char[] chars = (value ?? "").ToCharArray(); Array.Reverse(chars); return new string(chars); }
    public static void Set(Text text, string value) { if (text != null) text.text = value; }
    static readonly BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    static void Field(object owner, string name, object value)
    { FieldInfo field = owner.GetType().GetField(name, Fields); if (field != null) field.SetValue(owner, value); }
    static string ReadString(object owner, string name)
    { FieldInfo field = owner.GetType().GetField(name, Fields); return field == null ? null : field.GetValue(owner) as string; }
    static string Native(object owner, string method, string argument = "") { return Text(owner.GetType().Name + "$$" + method, Language, argument); }
    public static string SystemLanguageCode(SystemLanguage language)
    {
        switch ((int)language)
        {
''']
    for n,c in DATA['systemLanguageCodes'].items():
        if c!='en':lines+=['            case '+n+': return '+quote(c)+';\n']
    lines+=['''            default: return "en";
        }
    }
    public static void Cycle(UIManagerPortScript owner)
    {
        FieldInfo field = typeof(UIManagerPortScript).GetField("lang", Fields);
        int before = (int)field.GetValue(owner); field.SetValue(owner, unchecked(before + 1));
        string code = before >= 0 && before < PortCycle.Length ? PortCycle[before] : "en";
        if (before == 20) { code = "es"; field.SetValue(owner, 1); }
        Set(owner.TxtLang, code); PlayerPrefs.SetString("language", code); PlayerPrefs.Save();
    }
    public static void Cycle(UIManagerSelScript0 owner)
    {
        FieldInfo field = typeof(UIManagerSelScript0).GetField("lang", Fields);
        int before = (int)field.GetValue(owner); field.SetValue(owner, unchecked(before + 1));
        string code = before >= 0 && before < SelectorCycle.Length ? SelectorCycle[before] : "en";
        Set(owner.TxtLang, code); PlayerPrefs.SetString("language", code); PlayerPrefs.Save();
        // The native selector has no wrap/reset branch after index19.
    }
    public static bool DispatchLanguageButton(Scene scene)
    {
        foreach (var owner in OriginalScenePresentation.Components<UIManagerPortScript>(scene)) { owner.botonLangDown(); return true; }
        foreach (var owner in OriginalScenePresentation.Components<UIManagerSelScript0>(scene)) { owner.botonLangDown(); return true; }
        return false;
    }
    public static void Apply(settingsLang owner)
    {
        Field(owner,"idioma",Language);
        Field(owner,"tex2",Native(owner,"ObtenerTexto2")); Field(owner,"tex3",Native(owner,"ObtenerTexto3")); Field(owner,"tex6",Native(owner,"ObtenerTexto6"));
        // Unattached settingsLang.Start only initialized 2/3/6; 1/4/5 retain their native values.
        for (int i=1;i<=6;i++) Set(typeof(settingsLang).GetField("texto"+i).GetValue(owner) as Text, ReadString(owner,"tex"+i));
        int longest=Math.Max(owner.texto4 == null ? 0 : owner.texto4.text.Length, owner.texto5 == null ? 0 : owner.texto5.text.Length);
        int size=longest<=6 ? 31 : longest<=10 ? 27 : 24;
        foreach(Text text in new[]{owner.texto4,owner.texto5}) if(text!=null) { text.fontSize=size; text.resizeTextMaxSize=size; }
    }
    public static bool IsTablet()
    {
        float dpi=Screen.dpi; if(dpi==0) return false;
        return Mathf.Sqrt(Mathf.Pow(Screen.width/dpi,2)+Mathf.Pow(Screen.height/dpi,2))>=7;
    }
    public static void ResizeExit(exitLang owner)
    {
        RectTransform rect=owner.GetComponent<RectTransform>(); Field(owner,"rectTransform",rect);
        if(rect!=null) rect.sizeDelta=IsTablet() ? new Vector2(Screen.width*.6000000238418579f,Screen.height*.5f) : new Vector2(Screen.width*.8500000238418579f,Screen.height*.699999988079071f);
    }
    public static void Apply(exitLang owner)
    {
        ResizeExit(owner); Field(owner,"idioma",Language); bool korean=Language=="ko";
        if(owner.mensajeTexto!=null) { owner.mensajeTexto.fontSize=korean?45:65; owner.mensajeTexto.horizontalOverflow=korean?HorizontalWrapMode.Overflow:HorizontalWrapMode.Wrap; owner.mensajeTexto.resizeTextMaxSize=korean?70:80; }
        Set(owner.mensajeTexto,Native(owner,"ObtenerTextoTraducido"));
    }
    public static void Apply(noMoreLang owner)
    { Field(owner,"idioma",Language); string value=Native(owner,"ObtenerTexto"); if(Language=="ar") value=Reverse(value); Field(owner,"tex1",value); Set(owner.texto1,value); }
    public static void Apply(SolutionLang owner)
    { Field(owner,"idioma",Language); string value=Native(owner,"ObtenerTexto"); if(Language=="ar") value=Reverse(value); Field(owner,"texto1",value); Set(owner.texto1a,value); }
    public static void Apply(rateUsLang owner)
    {
        Field(owner,"idioma",Language);
        for(int i=1;i<=3;i++) { string value=Native(owner,"ObtenerTexto"+i); Field(owner,"tex"+i,value); Set(typeof(rateUsLang).GetField("texto"+i).GetValue(owner) as Text,value); }
    }
    public static void FitRate(rateUsLang owner)
    {
        Canvas.ForceUpdateCanvases(); if(owner.texto1==null || owner.texto2==null) return;
        int size=Math.Min(owner.texto1.cachedTextGenerator.fontSizeUsedForBestFit,owner.texto2.cachedTextGenerator.fontSizeUsedForBestFit);
        owner.texto1.resizeTextForBestFit=owner.texto2.resizeTextForBestFit=false;
        owner.texto1.fontSize=owner.texto2.fontSize=size;
    }
    public static void Apply(UIManagerOptScript owner)
    {
        Field(owner,"idioma",Language);
        foreach(int i in new[]{2,3,6,7}) { string value=Native(owner,"ObtenerTexto"+i); Field(owner,"tex"+i,value); Set(typeof(UIManagerOptScript).GetField("texto"+i).GetValue(owner) as Text,value); }
    }
    public static void Apply(UIManagerStoreScript owner)
    {
        Field(owner,"idiomaActual",Language);
        string buy=Native(owner,"ObtenerTexto1"); Field(owner,"tex1",buy);
        for(int i=1;i<=12;i++) Set(typeof(UIManagerStoreScript).GetField("texto1_"+i).GetValue(owner) as Text,buy);
        string[] values=new string[12]; values[0]=Native(owner,"ObtenerTexto2");
        string[] packs={"CLASSIC","POOLS","SPRINGS","MECH","JETS","NIGHT"};
        for(int i=0;i<6;i++) { Field(owner,"packName",packs[i]); values[i+1]=Native(owner,"ObtenerTexto3",packs[i]); }
        values[7]=Native(owner,"ObtenerTexto4"); string[] counts={"15","30","80","200"};
        for(int i=0;i<4;i++) { Field(owner,"cantidad",counts[i]); values[i+8]=Native(owner,"ObtenerTexto5",counts[i]); }
        for(int i=0;i<12;i++) { Field(owner,"tex"+(i+2),values[i]); Set(typeof(UIManagerStoreScript).GetField("texto"+(i==0 ? "" : (i+1).ToString())).GetValue(owner) as Text,values[i]); }
    }
    public static void Apply(CajaComplete owner)
    { Field(owner,"idioma",Language); string value=Native(owner,"ObtenerTexto2"); Field(owner,"texto2",value); Set(owner.text2,value); }
    public static void Apply(watchBox owner)
    { Field(owner,"idioma",Language); string value=Native(owner,"ObtenerTexto1"); Field(owner,"tex1",value); Set(owner.texto1,value); }
    public static void ApplyScene(Scene scene)
    {
        foreach(var owner in OriginalScenePresentation.Components<UIManagerOptScript>(scene)) Apply(owner);
        foreach(var owner in OriginalScenePresentation.Components<UIManagerStoreScript>(scene)) Apply(owner);
        foreach(var owner in OriginalScenePresentation.Components<exitLang>(scene)) Apply(owner);
        foreach(var owner in OriginalScenePresentation.Components<rateUsLang>(scene)) Apply(owner);
        foreach(var owner in OriginalScenePresentation.Components<noMoreLang>(scene)) Apply(owner);
        foreach(var owner in OriginalScenePresentation.Components<SolutionLang>(scene)) Apply(owner);
        foreach(var owner in OriginalScenePresentation.Components<CajaComplete>(scene)) Apply(owner);
        foreach(var owner in OriginalScenePresentation.Components<watchBox>(scene)) Apply(owner);
        // Existing HelpRotate owns its two native labels and Arabic reverse lifecycle.
    }
    public static void SelectorLayout(UIManagerSelScript0 owner,string language)
    {
        switch(language)
        {
''']
    for code,actions in DATA['layout'].items():
        if code=='unknown':continue
        lines+=['            case '+quote(code)+':\n']
        for action in actions:
            who='owner.'+action['owner']; prop=action['property'];val=str(action['value'])
            if prop.endswith('Y'):
                prop=prop[:-1];origin=action['preserveXFrom'];read='owner.'+origin['owner']
                lines+=['                if('+who+'!=null && '+read+'!=null) { Vector2 offset='+read+'.rectTransform.'+origin['property']+'; offset.y='+val+'f; '+who+'.rectTransform.'+prop+'=offset; }\n']
            else:lines+=['                if('+who+'!=null) '+who+'.'+prop+'='+val+';\n']
        lines+=['                break;\n']
    lines+=['''            default: if(owner.textUnlock2!=null) { owner.textUnlock2.fontSize=78; owner.textUnlock2.resizeTextMaxSize=78; } break;
        }
    }
    public static bool DispatchDesigner(string method,Scene scene)
    {
        foreach(var owner in OriginalScenePresentation.Components<UIManagerSelScript0>(scene))
        {
            switch(method) { case "botonDesign0Down": owner.botonDesign0Down(); return true; case "botonDesign1Down": owner.botonDesign1Down(); return true; case "botonDesign2Down": owner.botonDesign2Down(); return true; case "botonDesign3Down": owner.botonDesign3Down(); return true; case "botonDesign4Down": owner.botonDesign4Down(); return true; }
        }
        return false;
    }
}
''']
    put('Assets/Scripts/RecoveryUI/RecoveredNativeLocalization.cs',''.join(lines))
    target_classes={key.split('$$')[0] for key in DATA['methods']} - {'HelpRotate'}
    for cls in sorted(target_classes):
        target='Assets/Scripts/Assembly-CSharp/'+cls+'.cs';text=(PROJECT/target).read_text('utf-8-sig')
        for key,entry in DATA['methods'].items():
            c,method=key.split('$$')
            if c!=cls:continue
            lang='idioma' if cls=='UIManagerSelScript0' or cls=='exitLang' else ('idiomaActual' if cls in ['UIManagerStoreScript','Leveltexto2'] else 'idioma')
            argument='packName' if cls=='UIManagerStoreScript' and method=='ObtenerTexto3' or cls=='UIManagerSelScript0' and method=='ObtenerTexto2' else 'cantidad' if cls=='UIManagerStoreScript' and method=='ObtenerTexto5' else '""'
            text=replace_body(text,method,'\t\treturn RecoveredNativeLocalization.Text('+quote(key)+', '+lang+', '+argument+');')
        if cls in ['rateUsLang','noMoreLang','SolutionLang','settingsLang','watchBox']:
            text=replace_body(text,'Start','\t\tRecoveredNativeLocalization.Apply(this);')
        elif cls=='exitLang':
            text=replace_body(text,'OnEnable','\t\tRecoveredNativeLocalization.Apply(this);')
            text=replace_body(text,'AjustarPopup','\t\tRecoveredNativeLocalization.ResizeExit(this);')
            text=replace_body(text,'EsTablet','\t\treturn RecoveredNativeLocalization.IsTablet();')
        elif cls in ['UIManagerOptScript','UIManagerStoreScript','CajaComplete']:
            method='doLanguages' if cls=='UIManagerOptScript' else 'doLanguage'
            text=replace_body(text,method,'\t\tRecoveredNativeLocalization.Apply(this);')
            # Restore only the original language side effect of Start. Other controller work is owned by recovery UI.
            text=replace_body(text,'Start','\t\t'+method+'();')
        if cls=='rateUsLang':text=replace_body(text,'Ajustar','\t\tRecoveredNativeLocalization.FitRate(this);')
        if cls=='Leveltexto2':
            text=replace_body(text,'doLanguage','\t\tidiomaActual=PlayerPrefs.GetString("language","en");\n\t\ttexto2=ObtenerTexto();')
            # Source loading owns native nivelActual. Use the exact original part offsets.
            import sys;sys.path.insert(0,str(ANALYSIS));import disassemble as d
            offsets=struct_unpack=__import__('struct').unpack('<38i',d.read_va(0x621550,38*4))
            text=replace_body(text,'Start','\t\tdoLanguage();\n\t\tint[] offsets={'+','.join(str(x) for x in offsets)+'};\n\t\tint p=UIManagerSelScript0.parte;\n\t\tRecoveredNativeLocalization.Set(texto1,texto2+" "+(ControladorJuego.nivelActual+(p>=2&&p<=39?offsets[p-2]:0)));')
        if cls=='UIManagerSelScript0':
            text=replace_body(text,'botonLangDown','\t\tRecoveredNativeLocalization.Cycle(this);')
            text=replace_body(text,'AjustarLayoutPorIdioma','\t\tRecoveredNativeLocalization.SelectorLayout(this,idioma);')
            text=replace_body(text,'CodigoChinoReal','\t\treturn Application.systemLanguage==SystemLanguage.ChineseTraditional ? "zh-TW" : "zh";')
            text=replace_body(text,'botonDesign0Down','\t\taccesoDesign=4; var_design=var_design==0?1:0;\n\t\tif(panelDesign!=null) panelDesign.SetActive(var_design!=0);')
            text=replace_body(text,'botonDesign1Down','\t\taccesoDesign=accesoDesign==1?2:1;')
            text=replace_body(text,'botonDesign2Down','\t\taccesoDesign=accesoDesign==2?3:1;')
            text=replace_body(text,'botonDesign3Down','\t\taccesoDesign=accesoDesign==3?4:1;')
            text=replace_body(text,'botonDesign4Down','\t\tif(accesoDesign==4) { if(panelDesign0!=null) panelDesign0.SetActive(var_design==0); }\n\t\telse accesoDesign=1;')
        put(target,text)
    target='Assets/Scripts/Assembly-CSharp/UIManagerPortScript.cs';text=(PROJECT/target).read_text('utf-8-sig')
    text=replace_body(text,'Start','\t\t// Restore only native language initialization; recovery UI owns consent/audio/panels.\n\t\tif(callingScene==0) doLanguage();')
    text=replace_body(text,'botonLangDown','\t\tRecoveredNativeLocalization.Cycle(this);')
    text=replace_body(text,'doLanguage','\t\tidioma=ObtenerIdiomaDelSistema();\n\t\tPlayerPrefs.SetString("language",idioma); PlayerPrefs.Save();')
    text=replace_body(text,'ObtenerIdiomaDelSistema','\t\treturn RecoveredNativeLocalization.SystemLanguageCode(Application.systemLanguage);')
    put(target,text)
    golden={'originalLibrarySha256':DATA['originalLibrarySha256'],'cases':[{'method':key,'language':code,'value':value} for key,entry in DATA['methods'].items() for code,value in entry['values'].items()]}
    (OUT/'Evidence/native-language-golden.json').write_text(json.dumps(golden,ensure_ascii=False,indent=2),'utf-8')
    (OUT/'Evidence/native-language-texts.json').write_bytes((ANALYSIS/'native-language-texts.json').read_bytes())
    manifest={'schemaVersion':1,'status':'staged-not-imported-not-Unity-tested','scope':'Pure original localization methods/lifecycle and original selector designer-panel controls only. No game mechanism changes.',
              'files':files,'nativeStringCases':638,'unsupported':DATA['unsupported'],
              'pending':['Recovery UI hook patch based on presentation final source','Actual Unity glyph/layout/screenshots validation'],
              'evidence':[{'path':str(p.relative_to(OUT)).replace('\\','/'),'sha256':sha(p.read_bytes())} for p in (OUT/'Evidence').glob('*')]}
    (OUT/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),'utf-8')
    print(json.dumps({'files':len(files),'out':str(OUT)},ensure_ascii=False))

if __name__=='__main__': stage()
