from pathlib import Path
import json
source=Path(__file__).with_name('find_static_writes.py').read_text(encoding='utf-8')
source=source.replace("target='UIManagerScript'","target='UIManagerSelScript0'")
source=source.replace("regs={}","regs={'xzr':0}")
source=source.replace("'instruction':n+' '+ins.op_str","'instruction':n+' '+ins.op_str,'value':regs.get(key(ops[0].reg))")
source=source.replace('native-ui-static-writes.json','native-category-context-writes.json')
source=source.replace("print(json.dumps([r for r in records if r['field'].startswith('positionRes')],ensure_ascii=False,indent=2))", "print(json.dumps([r for r in records if r['field'] in ['parte','subparte']],ensure_ascii=False,indent=2))")
exec(compile(source,'find_category_context_writes','exec'))
