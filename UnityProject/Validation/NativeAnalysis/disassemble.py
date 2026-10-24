"""Disassemble recovered IL2CPP ARM64 methods; annotate metadata and call targets.

All dependencies are portable wheels extracted under Tools/python. No code execution
from the recovered application is performed. Method bounds use the next unique
Il2CppDumper method address and therefore can include padding/thunks.
"""
import json
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / 'Tools/python'))
from capstone import Cs, CS_ARCH_ARM64, CS_MODE_ARM
from elftools.elf.elffile import ELFFile

data = json.loads((ROOT / 'Tools/Il2CppDumper/script.json').read_text('utf-8'))
strings = json.loads((ROOT / 'Tools/Il2CppDumper/stringliteral.json').read_text('utf-8'))
method_map = {m['Address']: m for m in data['ScriptMethod']}
addresses = sorted(method_map)
labels = {int(s['address'], 16): repr(s['value']) for s in strings}
for k in ['ScriptMetadata', 'ScriptMetadataMethod']:
    for v in data.get(k, []):
        labels[v['Address']] = v.get('Name', '')
binary_path = ROOT.parents[2] / 'raw/split_config.arm64_v8a/lib/arm64-v8a/libil2cpp.so'
binary = binary_path.read_bytes()
stream = binary_path.open('rb')
elf = ELFFile(stream)
segments = [s for s in elf.iter_segments() if s['p_type'] == 'PT_LOAD']
relocations = {r['r_offset']: r['r_addend'] for s in elf.iter_sections()
               if s.name.startswith('.rela') for r in s.iter_relocations()}
for src, target in relocations.items():
    if target in labels:
        labels[src] = labels[target]
def read_va(addr, size):
    for s in segments:
        if s['p_vaddr'] <= addr < s['p_vaddr'] + s['p_filesz']:
            off = addr - s['p_vaddr'] + s['p_offset']
            return binary[off:off+size]
    return b''

md = Cs(CS_ARCH_ARM64, CS_MODE_ARM)
md.detail = True
dump_text = (ROOT / 'Tools/Il2CppDumper/dump.cs').read_text('utf-8-sig')
fields = {}
for block in re.finditer(r'public class (\w+) :[^\n]+\n\{(.*?)\n\}', dump_text, re.S):
    fields[block[1]] = {int(off, 16): field for field, off in re.findall(
        r'(?:public|private) (?!static )[\w\[\]<>.]+ (\w+); // (0x[\da-fA-F]+)', block[2])}

def disassemble(m):
    start = m['Address']
    ix = addresses.index(start)
    end = addresses[ix+1] if ix+1 < len(addresses) else start+128
    registers = {}
    lines = [f"// {m['Name']} VA 0x{start:X}, inferred length {end-start}", m['Signature']]
    for ins in md.disasm(read_va(start, end-start), start):
        annotations = []
        # Track ADRP + ADD/LDR chains for metadata references.
        ops = ins.operands
        if ins.mnemonic in ('adrp', 'adr') and len(ops) == 2:
            registers[ins.reg_name(ops[0].reg)] = ops[1].imm
        elif ins.mnemonic == 'add' and len(ops) == 3 and ops[2].type == 2:
            dst, src = ins.reg_name(ops[0].reg), ins.reg_name(ops[1].reg)
            if src in registers:
                registers[dst] = registers[src] + ops[2].imm
                if registers[dst] in labels:
                    annotations.append(labels[registers[dst]])
            else:
                registers.pop(dst, None)
        elif ins.mnemonic in ('ldr', 'ldrb', 'ldrh', 'ldrsw') and len(ops) == 2 and ops[1].type == 3:
            dst, src = ins.reg_name(ops[0].reg), ins.reg_name(ops[1].mem.base)
            if src in registers:
                va = registers[src] + ops[1].mem.disp
                if va in labels:
                    annotations.append(f"[0x{va:X}] {labels[va]}")
                registers.pop(dst, None)
        if ins.mnemonic in ('b', 'bl') and ops and ops[0].type == 2:
            target = method_map.get(ops[0].imm)
            if target:
                annotations.append(target['Name'])
        cls = m['Name'].split('$$')[0]
        for op in ops:
            if op.type == 3 and ins.reg_name(op.mem.base) == 'x19' and op.mem.disp in fields.get(cls, {}):
                annotations.append('self.' + fields[cls][op.mem.disp])
        lines.append(f'{ins.address:08X}  {ins.mnemonic:8} {ins.op_str:38}' + (' // ' + '; '.join(annotations) if annotations else ''))
        if ins.mnemonic in ('bl', 'blr'):
            for reg in [f'x{i}' for i in range(19)]:
                registers.pop(reg, None)
    return '\n'.join(lines) + '\n'

if __name__ == '__main__':
    queries = sys.argv[1:] or ['UIManagerSelScript$$doCallGame','UIManagerSelScript0$$botonPartDown',
                               'SaveLoadManager$$doCodigo','SaveLoadManager$$doRescatarFijos',
                               'SaveLoadManager$$doRescatarConfig','ConfigSuelo$$','ConfigFondo$$',
                               'ControlPieza$$Awake','ControlPieza$$Start']
    out = ROOT / 'Methods'
    out.mkdir(exist_ok=True)
    selection = [m for m in data['ScriptMethod'] if any(q in m['Name'] for q in queries)]
    for m in selection:
        name = re.sub(r'[^\w.]+', '_', m['Name'])
        target = out / f"{name}_{m['Address']:X}.asm.txt"
        target.write_text(disassemble(m), 'utf-8')
        print(m['Name'], hex(m['Address']), target.name)
