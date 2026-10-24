"""Preserve the newly restored original language preference across batch QA."""
import hashlib,json
from pathlib import Path
v=Path(__file__).resolve().parent; w=v.parent.parent
k='Assets/Editor/RecoveredTools/FlowWaterValidationPreferenceGuard.cs'
source=w/'.validation-project'/k; raw=source.read_bytes()
text=source.read_text(encoding='utf-8-sig')
changes=[
 ('        static FlowWaterValidationPreferenceGuard()', '        const string LanguageExists = "FlowWater.Validation.HelpPreferenceGuard.LanguageExists";\n        const string LanguageValue = "FlowWater.Validation.HelpPreferenceGuard.LanguageValue";\n        static FlowWaterValidationPreferenceGuard()'),
 ('                SessionState.SetBool(Active, true);', '                SessionState.SetBool(LanguageExists, PlayerPrefs.HasKey("language"));\n                SessionState.SetString(LanguageValue, PlayerPrefs.GetString("language", ""));\n                SessionState.SetBool(Active, true);'),
 ('            PlayerPrefs.Save();', '            bool languageExisted = SessionState.GetBool(LanguageExists, false);\n            string languageValue = SessionState.GetString(LanguageValue, "");\n            if (languageExisted) PlayerPrefs.SetString("language", languageValue);\n            else PlayerPrefs.DeleteKey("language");\n            PlayerPrefs.Save();'),
 ('            string directory = Path.Combine', '            bool languageRestored = PlayerPrefs.HasKey("language") == languageExisted &&\n                (!languageExisted || PlayerPrefs.GetString("language", "") == languageValue);\n            string directory = Path.Combine'),
 ('                    productName = Application.productName, existedBefore = existed, valueBefore = value, restored = restored }, true));', '                    productName = Application.productName, existedBefore = existed, valueBefore = value, restored = restored,\n                    languageExistedBefore = languageExisted, languageValueBefore = languageValue, languageRestored = languageRestored }, true));'),
 ('            public int valueBefore;', '            public int valueBefore;\n            public bool languageExistedBefore, languageRestored;\n            public string languageValueBefore;')]
for a,b in changes:
    assert text.count(a)==1,a
    text=text.replace(a,b)
batch=v/'LanguagePreferenceGuardBatch1'; target=batch/k
target.parent.mkdir(parents=True,exist_ok=True);target.write_text(text,encoding='utf-8',newline='\n')
(batch/'manifest.json').write_text(json.dumps(dict(scope='Editor-only batch validation cleanup. The original language setter is production behavior; all batch suites must restore the launch preference even if they predate localization recovery. Interactive Unity behavior unchanged.',files=[dict(target=k,source=str(target),previousSha256=hashlib.sha256(raw).hexdigest(),sha256=hashlib.sha256(target.read_bytes()).hexdigest())]),indent=2)+'\n',encoding='utf-8')
print(batch/'manifest.json')
