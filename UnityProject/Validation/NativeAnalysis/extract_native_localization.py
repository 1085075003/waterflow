"""Read-only ARM64 localization extraction. Never executes the original library."""
import ast
import hashlib
import json
import pathlib
import re
import struct
import disassemble as d
from extract_water_recipes import Extractor

OUT = d.ROOT
CODES = ['es', 'en', 'fr', 'de', 'pt', 'zh', 'zh-TW', 'hi', 'ru', 'ja', 'it',
         'ko', 'nl', 'tr', 'pl', 'th', 'vi', 'sv', 'el', 'id', 'ar', 'unknown']
CLASSES = ['settingsLang', 'exitLang', 'rateUsLang', 'noMoreLang', 'SolutionLang',
           'UIManagerOptScript', 'UIManagerStoreScript', 'UIManagerSelScript0',
           'CajaComplete', 'Leveltexto2', 'watchBox', 'HelpRotate']

class LanguageExtractor(Extractor):
    def call(self, address):
        name = d.method_map.get(address, {}).get('Name', '')
        if name == 'System.String$$Concat':
            signature = d.method_map[address]['Signature']
            count = signature.count('System_String_o* str')
            if count not in [2, 3]:
                raise ValueError(('Unexpected Concat overload', signature))
            args = [self.refs.get(self.get('x' + str(i)), '') for i in range(count)]
            self.put('x0', self.ref(''.join(args)))
        elif name == 'UnityEngine.Component$$GetComponent<object>':
            # Layout uses the Text's own RectTransform. Keep the same owner.
            pass
        elif name in ['UnityEngine.UI.Text$$set_fontSize',
                      'UnityEngine.UI.Text$$set_resizeTextMaxSize']:
            self.calls.append({'owner': self.refs[self.get('x0')], 'property': name.split('$$set_')[1],
                               'value': self.get('w1'), 'nativeVA': hex(self.pc)})
        elif name in ['UnityEngine.RectTransform$$get_offsetMax',
                      'UnityEngine.RectTransform$$get_offsetMin']:
            self.last_offset_source = {'owner': self.refs[self.get('x0')], 'property': name.split('$$get_')[1]}
            self.put('s0', 101.0); self.put('s1', 202.0)
        elif name in ['UnityEngine.RectTransform$$set_offsetMax',
                      'UnityEngine.RectTransform$$set_offsetMin']:
            assert self.get('s0') == 101.0, 'Native layout unexpectedly replaces X'
            self.calls.append({'owner': self.refs[self.get('x0')], 'property': name.split('$$set_')[1] + 'Y',
                               'value': self.get('s1'), 'preserveXFrom': self.last_offset_source,
                               'nativeVA': hex(self.pc)})
        else:
            super().call(address)

def literal_pointer(address):
    target = d.relocations.get(address, int.from_bytes(d.read_va(address, 8), 'little'))
    return ast.literal_eval(d.labels[target])

def extract():
    result = {'originalLibrarySha256': hashlib.sha256(d.binary).hexdigest(),
              'mechanism': 'Read-only narrow instruction evaluation; original IL2CPP is not executed.',
              'languages': CODES, 'methods': {}, 'layout': {}, 'unsupported': {}}
    for method in d.data['ScriptMethod']:
        cls, _, short = method['Name'].partition('$$')
        if cls not in CLASSES or not short.startswith('ObtenerTexto'):
            continue
        values = {}
        for code in CODES:
            fields = d.fields[cls]
            lang_field = next((v for v in fields.values() if v in ['idioma', 'idiomaActual']), None)
            inputs = {lang_field: code} if lang_field else {}
            for field in ['packName', 'cantidad']:
                if field in fields.values(): inputs[field] = '{0}'
            ex = LanguageExtractor(inputs, cls)
            if cls in ['exitLang', 'UIManagerSelScript0']:
                ex.put('x1', ex.ref(code)); ex.put('x2', ex.ref('{0}'))
            try:
                ex.run(method['Name'])
                value = ex.refs.get(ex.get('x0'))
                if not isinstance(value, str): raise ValueError(('Nonstring result', hex(ex.get('x0'))))
                values[code] = value
            except Exception as error:
                result['unsupported'][method['Name'] + '|' + code] = str(error)
        result['methods'][method['Name']] = {'nativeVA': hex(method['Address']), 'values': values}
    for code in CODES:
        ex = LanguageExtractor({}, 'UIManagerSelScript0')
        for field in ['textUnlock1', 'textUnlock2']:
            offset = next(k for k, v in ex.fields.items() if v == field)
            ex.write(ex.base + offset, ex.ref(field), 8)
        ex.put('x1', ex.ref(code))
        try:
            ex.run('UIManagerSelScript0$$AjustarLayoutPorIdioma')
            result['layout'][code] = ex.calls
        except Exception as error:
            result['unsupported']['layout|' + code] = str(error)
    result['selectorCycle'] = [literal_pointer(0x2253908 + i * 8) for i in range(20)]
    result['portCycle'] = ['es','en','fr','de','pt','zh','zh-TW','hi','ru','ja','it','ko','nl','tr','pl','th','vi','sv','el','id']
    result['portCycleNativeTargets'] = [hex(0x1038450 + 4 * b) for b in d.read_va(0x6215e8, 21)]
    result['systemLanguageCodes'] = {str(i + 6): literal_pointer(0x22537e0 + i * 8) for i in range(37)}
    result['exitPopup'] = {'tabletDiagonalInches':7.0,'tabletWidthFactor':0.6000000238418579,
                           'tabletHeightFactor':0.5,'phoneWidthFactor':0.8500000238418579,
                           'phoneHeightFactor':0.699999988079071,'koreanFontSize':45,
                           'koreanMaxSize':70,'otherFontSize':65,'otherMaxSize':80}
    (OUT/'native-language-texts.json').write_text(json.dumps(result, ensure_ascii=False, indent=2), 'utf-8')
    print(json.dumps({'methods':len(result['methods']), 'evaluatedStrings':sum(len(v['values']) for v in result['methods'].values()),
                      'layoutLanguages':len(result['layout']), 'unsupported':result['unsupported']},ensure_ascii=False))
    return result

if __name__ == '__main__':
    extract()
