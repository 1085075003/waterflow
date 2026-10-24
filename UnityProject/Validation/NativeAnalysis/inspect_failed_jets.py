import pathlib,json
p=pathlib.Path('Validation/FullContent')
for level in [1744,1840,1969,2000,2022,2029,2032]:
    graph=json.loads((p/f'gm{level}-water-graph.json').read_text(encoding='utf-8-sig'))
    print('\nLEVEL',level)
    print('DRY',[(n['index'],n['sourceRecord'],n['prefab'],n['position'],n['ports'])for n in graph['nodes']if n['target']and not n['waterColor']])
    print('JETS',[(n['sourceRecord'],n['waterColor'],n['position'],n['jets'])for n in graph['nodes']if n.get('jets')])
    print('BLOCKED',graph['blockedFlow'])
    dry=[n for n in graph['nodes']if n['target']and not n['waterColor']]
    for target in dry:
        print('NEIGHBORS for',target['sourceRecord'],'color',target['expectedColor'])
        for node in graph['nodes']:
            dx=node['position']['x']-target['position']['x'];dz=node['position']['z']-target['position']['z']
            if abs(dx)<=21 and abs(dz)<=21:
                print(' ',node['index'],node['sourceRecord'],node['prefab'],node['position'],'c',node['waterColor'],node['expectedColor'],'in',node['incoming'],'ports',node['ports'],'links',node['links'])
