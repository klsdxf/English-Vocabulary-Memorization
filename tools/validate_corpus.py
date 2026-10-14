"""Offline audit against retained licensed source evidence; standard-library only.
Rules verify structure/provenance and detect simple language anomalies, not universal
linguistic truth. Run: python tools/validate_corpus.py (paths resolve from this file).
"""
import collections,hashlib,json,re,sys
from pathlib import Path
from phonetics import ipa,forms,allowed
root=Path(__file__).resolve().parents[1];audit=root/'data/audit'
book=json.loads((root/'data/builtin.json').read_text(encoding='utf-8'));words=book['Words']
sources={}
for name in ['CET6','CET4','TEM8']:
 for r in json.loads((audit/(name+'_source.json')).read_text(encoding='utf-8')):
  sources[name+':'+r['source_key']]=r
cmu=json.loads((audit/'cmu-used.json').read_text(encoding='utf-8'))
corrections=json.loads((audit/'text-corrections.json').read_text(encoding='utf-8'))
cross={w.lower() for w in (audit/'CET6_cross_headwords.txt').read_text(encoding='utf-8').splitlines()}
issues=[];counts=collections.Counter();seen=set();ids=set();sentences=set();multiple=0
def flag(w,category,detail):issues.append({'word':w['English'],'category':category,'detail':detail})
for i,w in enumerate(words):
 key=w['English'].lower();source=sources.get(w['SourceKey'])
 fields=[key,w.get('PartOfSpeech'),w.get('UsIpa'),w.get('Meaning'),w['Examples'][0]['English'] if w.get('Examples') else '',w['Examples'][0].get('Chinese') if w.get('Examples') else '']
 for label,value in zip(['word','pos','us_ipa','meaning','example','translation'],fields):
  if value and value.strip():counts[label]+=1
  else:flag(w,'completeness',label)
 if key.lower() in seen:flag(w,'duplicate','headword')
 seen.add(key.lower())
 if not w.get('Id') or w['Id'] in ids:flag(w,'duplicate','missing/duplicate stable ID')
 ids.add(w.get('Id'))
 if not re.fullmatch('[a-z]+(?:-[a-z]+)?',key):flag(w,'spelling','headword format')
 if w['OriginalIndex']!=i or not w['Formal']:flag(w,'structure','source order/formal flag')
 if source is None or source['word'].lower()!=key:flag(w,'provenance','unmatched source entry');continue
 if source['word'] in {'Christian','Roman','Thanksgiving'} and w['English']!=source['word']:flag(w,'spelling','proper-name capitalization differs from source')
 if source['book']!='CET6' and key not in cross:flag(w,'scope','not cross-referenced CET6')
 expected=list(dict.fromkeys(ipa(x) for x in cmu.get(key,[])))
 if len(expected)>1:multiple+=1
 if expected:
  if w['UsIpa']!=' · '.join(expected):flag(w,'pronunciation','CMU conversion mismatch')
 else:
  us=re.search(r'US /([^/]+)/',source['ipa'])
  if not us or w['UsIpa']!='/'+us.group(1)+'/':flag(w,'pronunciation','no verified US source')
 definitions=[(d['pos'].replace('modal v.','modal.').replace('int.','interj.'),d['definition']) for d in source['definitions']]
 if [(d['PartOfSpeech'],d['Definition']) for d in w['Senses']]!=definitions:flag(w,'definition','POS/definition differs from source')
 if w['PartOfSpeech']!=' / '.join(dict.fromkeys(d['PartOfSpeech'] for d in w['Senses'])):flag(w,'definition','display POS differs from senses')
 expected_meaning='；'.join(d['PartOfSpeech']+' '+(d['UsIpa']+' ' if d.get('UsIpa') else '')+d['Definition'] for d in w['Senses'])
 if w['Meaning']!=expected_meaning:flag(w,'definition','display definition differs from senses')
 for d in w['Senses']:
  if d['PartOfSpeech'] not in allowed or not re.search('[\u4e00-\u9fff]',d['Definition']):flag(w,'definition','POS or Chinese missing')
  if any(n<0 or n>=len(w['Examples']) for n in d['ExampleIndexes']):flag(w,'structure','sense example index')
  if d.get('UsIpa') and any(x not in expected for x in d['UsIpa'].split(' · ')):flag(w,'pronunciation','sense IPA variant not in US source')
 original_pairs=set(zip(source['en'],source['cn']))
 for e in w['Examples']:
  en,cn=e['English'],e['Chinese']
  if (en,cn) not in original_pairs and not any(c['word']==key and c['english']==en and c['after']==cn and (en,c['before']) in original_pairs for c in corrections):flag(w,'bilingual','pair differs from source without a documented correction')
  if not forms(key).intersection(re.findall('[a-z]+(?:-[a-z]+)?',en.lower())):flag(w,'usage','target word/inflection absent')
  if len(en.split())<4 or not re.search('[\u4e00-\u9fff]',cn):flag(w,'bilingual','sentence incomplete')
  if re.search(r'\b([a-z]+)\s+\1\b',en,re.I):flag(w,'grammar','repeated adjacent token')
  if any(t in en.lower() for t in ['lorem ipsum','example sentence','insert word','[word]']):flag(w,'grammar','template text')
  if en in sentences:flag(w,'duplicate','shared English sentence')
  sentences.add(en)
excluded=json.loads((audit/'excluded.json').read_text(encoding='utf-8'))
old=json.loads((root/'tests/fixtures/v1-builtin.json').read_text(encoding='utf-8'))
old_words={x['English'].lower() for x in old['Words']}
report={'formal_words':len(words),'within_2800_3200':2800<=len(words)<=3200,'field_completeness':{k:{'count':counts[k],'percent':round(100*counts[k]/len(words),2)} for k in ['word','pos','us_ipa','meaning','example','translation']},'unique_english_examples':len(sentences),'source_books':dict(collections.Counter(sources[w['SourceKey']]['book'] for w in words)),'blocking_issues':issues,'issue_counts':dict(collections.Counter(x['category'] for x in issues)),'multiple_US_variants':multiple,'human_reviewed_words':0,'AI_developer_heteronym_reviews':8,'AI_text_corrections':len(corrections),'excluded_unique_words':len({x['word'] for x in excluded}),'old_matched':len(old_words&seen),'old_unmatched':len(old_words-seen),'sha256':hashlib.sha256((root/'data/builtin.json').read_bytes()).hexdigest(),'limitations':['Source-aligned bilingual pairs are not a proof of translation accuracy. No claim of all-human review.','POS and senses checked against licensed source, not an independently licensed bilingual dictionary.','CMU US phonemes converted to broad IPA; stress placement uses maximal onset and variants are retained.','Grammar checks detect repeated tokens/template gaps and target-word usage only; they do not prove all sentences grammatical.','ExampleIndexes link literal Chinese phrases only; missing indices leave examples at word level without guessed sense assignment.']}
(audit/'quality-report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({k:report[k] for k in ['formal_words','within_2800_3200','field_completeness','issue_counts','multiple_US_variants','excluded_unique_words']},ensure_ascii=False,indent=2))
sys.exit(0 if report['within_2800_3200'] and not issues else 1)
