"""Replays exported graph edges to isolate graph corrections; not Unity validation."""
import collections
import json
import pathlib

ROOT = pathlib.Path(__file__).resolve().parents[1] / 'FullContent'
DIRECTIONS = [(-1,0),(0,1),(1,0),(0,-1)]

def recheck(level):
    graph = json.loads((ROOT / f'gm{level}-water-graph.json').read_text(encoding='utf-8-sig'))
    nodes = graph['nodes']
    colors = [0] * len(nodes)
    incoming = [0] * len(nodes)
    queue = collections.deque()
    changes = []
    def receive(target,color,port):
        for candidate in nodes:
            i=candidate['index']
            if colors[i] or candidate['expectedColor']not in(0,color):continue
            if candidate!=target:
                a,b=candidate['position'],target['position']
                if candidate['pieceIndex']==target['pieceIndex']or candidate['source']or abs(a['x']-b['x'])>.8001 or abs(a['z']-b['z'])>.8001 or abs(a['y']-b['y'])>.4 or not candidate['ports'][port-1]:continue
                changes.append((target['sourceRecord'],candidate['sourceRecord'],'coincident independent input triggers'))
            colors[i]=color;incoming[i]=port;queue.append(i)
    for node in nodes:
        if node['source']:
            index = node['index']
            colors[index] = node['expectedColor'] or node['waterColor'] or 1
            incoming[index] = node['incoming']
            queue.append(index)
    while queue:
        index = queue.popleft()
        source = nodes[index]
        edges = list(source['links'])
        for port, target_index in enumerate(edges):
            if incoming[index] == port + 1:
                continue
            if port < 4:
                if target_index >= 0:
                    target = nodes[target_index]
                    if target['position']['y'] < source['position']['y'] - 3 and not target['ports'][4]:
                        changes.append((source['sourceRecord'], target['sourceRecord'], 'reject fall into side intake'))
                        continue
                # Old snapshot used .4 tolerance and omitted slightly offset pools.
                if target_index < 0 and source['rotation'] == dict(x=0., y=0., z=0.):
                    dx, dz = DIRECTIONS[port]
                    position = source['position']
                    candidates=[]
                    for target in nodes:
                        point=target['position']
                        if target['source'] or target['pieceIndex'] == source['pieceIndex'] or point['y'] > position['y'] + .45:
                            continue
                        if abs(point['x']-position['x']-20*dx)>.8001 or abs(point['z']-position['z']-20*dz)>.8001:
                            continue
                        # Add only missing edges beyond old .4, not blocked native probes.
                        if abs(point['x']-position['x']-20*dx)<=.4 and abs(point['z']-position['z']-20*dz)<=.4:
                            continue
                        accepts = target['ports'][4] if point['y']<position['y']-3 else target['ports'][(port+2)%4]
                        if accepts:
                            candidates.append(target)
                    if candidates:
                        target=max(candidates,key=lambda n:n['position']['y'])
                        target_index=target['index']
                        changes.append((source['sourceRecord'],target['sourceRecord'],'trigger radius .8'))
            if target_index < 0:
                continue
            target=nodes[target_index]
            receive(target,colors[index],6 if port==4 and source['pedestal'] else 5 if target['position']['y']<source['position']['y']-3 and target['ports'][4] else (port+2)%4+1 if port<4 else 5)
        for route in source.get('jets',[]):
            receiver_record=route.get('receiverRecord',-1)
            for target in nodes:
                target_index=target['index']
                if target['sourceRecord']==receiver_record:
                    receive(target,colors[index],5)
                    break
    targets=[n for n in nodes if n['target']]
    return dict(level=level,old=sum(bool(n['waterColor'])for n in targets),new=sum(bool(colors[n['index']])for n in targets),total=len(targets),dry=[n['sourceRecord']for n in targets if not colors[n['index']]],changes=changes)

if __name__=='__main__':
    report=json.loads((ROOT/'all-runtime-validation.json').read_text(encoding='utf-8-sig'))
    for entry in report['levels']:
        if entry['failures'] and (ROOT/f"gm{entry['assetId']}-water-graph.json").exists():
            print(recheck(entry['assetId']))
