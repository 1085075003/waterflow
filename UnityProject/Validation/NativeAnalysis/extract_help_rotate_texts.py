"""Read original native return-block string literals; write only Validation evidence."""
import pathlib,re,ast,json
root=pathlib.Path('Validation/NativeAnalysis')
methods=['HelpRotate_ObtenerTexto1_10C4138','HelpRotate_ObtenerTexto2_10C4824']
tables=[]
for method in methods:
 lines=(root/'Methods'/(method+'.asm.txt')).read_text(encoding='utf-8').splitlines()
 start=next(i for i,line in enumerate(lines) if 'ComputeStringHash' in line)
 rows=[]
 for i in range(start,len(lines)):
  if 'System.String$$op_Equality' not in lines[i]:continue
  before=next((line for line in reversed(lines[max(start,i-9):i]) if re.search(r'// \[0x[0-9A-F]+\] [\'\"]',line)),None)
  next_line=lines[i+1]
  if re.search(r'\s+b\s+#0x',next_line):
   target=int(next_line.split('#0x',1)[1].split()[0],16)
   target_index=next(k for k,line in enumerate(lines)if line.startswith(f'{target:08X}'))
   after=next((line for line in lines[target_index:target_index+5] if re.search(r'// \[0x[0-9A-F]+\] [\'\"]',line)),None)
  else:
   after=next((line for line in lines[i+1:i+10] if re.search(r'// \[0x[0-9A-F]+\] [\'\"]',line)),None)
  if before is None or after is None:raise ValueError((method,i))
  lang=ast.literal_eval(before.split('// ',1)[1].split('] ',1)[1]);value=ast.literal_eval(after.split('// ',1)[1].split('] ',1)[1])
  rows.append({'language':lang,'text':value,'comparisonVA':lines[i].split()[0],'returnLiteralVA':after.split()[0]})
 tables.append({row['language']:row for row in rows})
langs=sorted(set(tables[0])|set(tables[1]))
assert set(tables[0])==set(tables[1])
result={'methods':methods,'entries':[{'language':lang,'text1':tables[0][lang]['text'],'text2':tables[1][lang]['text'],
                                    'nativeComparisonVAs':[tables[0][lang]['comparisonVA'],tables[1][lang]['comparisonVA']]}for lang in langs],
        'fallback':{'text1':'rotate board','text2':'swipe here'},'arabicRule':'doLanguage reverses both resulting strings by char before assigning UI Text, matching original ARM.'}
(root/'native-help-rotate-texts.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print('Extracted',len(result['entries']),'languages from both original return methods.')
