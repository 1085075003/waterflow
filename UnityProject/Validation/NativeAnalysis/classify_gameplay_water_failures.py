"""Read-only classification of real mode0 Save1 final-water failures.

Never changes runtime state or treats an exported graph as a Unity pass.
Run from UnityProject while/after the root gameplay validation runs.
"""
import json
import pathlib
import re

report_path = pathlib.Path('Validation/Save1Gameplay/save1-gameplay-validation.json')
try:
    report = json.loads(report_path.read_text(encoding='utf-8-sig'))
except (OSError, json.JSONDecodeError):
    print('Report is being written; no classification updated.')
    raise SystemExit(0)

result = {'planned': report.get('plannedLevels'), 'observed': len(report.get('levels', [])),
          'finishedUtc': report.get('finishedUtc'), 'failures': []}
for level in report.get('levels', []):
    failure = level.get('failure', '')
    if '-actual-water-and-completion:' not in failure:
        continue
    globals_active = set(map(int, re.findall(r'conResorte([1-8])=1(?:,|$)', level.get('switches', ''))))
    false_ports, dry_targets = [], []
    for row in level.get('waterGraph', []):
        record_match = re.search(r'record=(\d+)', row)
        incoming_match = re.search(r' incoming=(\d+)', row)
        if not record_match or not incoming_match:
            continue
        record, incoming = int(record_match[1]), int(incoming_match[1])
        if 'target=True color=0 ' in row:
            dry_targets.append(record)
        for port, switch in re.findall(r'conOrigen([1-6])=([^,/]+)/wet=True', row):
            channel = {'A': 1, 'B': 2}.get(switch)
            if switch.isdigit():
                channel = int(switch)
            if int(port) != incoming and channel in globals_active:
                false_ports.append({'record': record, 'actualIncoming': incoming,
                                    'publishedOutputPort': int(port), 'channel': channel})
    result['failures'].append({'id': level['id'], 'active': level.get('finalActive'),
                              'required': level.get('finalRequired'), 'dryTargets': dry_targets,
                              'classification': 'output-port-switch-published' if false_ports else 'needs-independent-flow-analysis',
                              'falsePortCandidates': false_ports,
                              'pending': level.get('waterPending'), 'springBusy': level.get('springsBusy')})

output = pathlib.Path('Validation/NativeAnalysis/gameplay-water-failure-classification.json')
output.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps(result, ensure_ascii=False, indent=2))
