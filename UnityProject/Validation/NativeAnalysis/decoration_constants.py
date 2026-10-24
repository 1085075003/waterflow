import struct,json
import disassemble as d
out={}
for va,size in [(0x600350,8),(0x600f60,16),(0x600a10,8),(0x6003f8,8),(0x601d60,16),(0x600468,8),(0x6005a8,8),(0x600910,24),(0x602080,16),(0x601db0,16),(0x601080,16),(0x6017e0,16),(0x6022e0,16),(0x6014a0,16),(0x602140,16),(0x600de0,16)]:
    out[hex(va)]=list(struct.unpack('<'+'f'*(size//4),d.read_va(va,size)))
print(json.dumps(out,indent=2))
print('plant material switch',list(d.read_va(0x6210f4,19)))
print('plant1 material switch',list(d.read_va(0x621107,19)))
print('C side2,4',struct.unpack('<f',struct.pack('<I',0x3f828f5c))[0],struct.unpack('<f',struct.pack('<I',0xbf818937))[0])
print('audio default',struct.unpack('<f',d.read_va(0x6001c8,4))[0])
print('audio add',struct.unpack('<f',d.read_va(0x60008c,4))[0])
print('C water/jet constants',json.dumps({hex(va):struct.unpack('<f',d.read_va(va,4))[0] for va in [0x5ffd2c,0x60003c,0x5ffecc,0x5ffcac,0x5ffcd8,0x600070,0x5ffde0,0x5ffcf0,0x5ffc9c,0x5ffe38]}))
for ins in d.md.disasm(d.read_va(0x224d060,20),0x224d060):print(hex(ins.address),ins.mnemonic,ins.op_str)
