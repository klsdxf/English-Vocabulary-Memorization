import re
phones={'AA':'ɑ','AE':'æ','AH':'ʌ','AO':'ɔ','AW':'aʊ','AY':'aɪ','B':'b','CH':'tʃ','D':'d','DH':'ð','EH':'ɛ','ER':'ɝ','EY':'eɪ','F':'f','G':'ɡ','HH':'h','IH':'ɪ','IY':'i','JH':'dʒ','K':'k','L':'l','M':'m','N':'n','NG':'ŋ','OW':'oʊ','OY':'ɔɪ','P':'p','R':'ɹ','S':'s','SH':'ʃ','T':'t','TH':'θ','UH':'ʊ','UW':'u','V':'v','W':'w','Y':'j','Z':'z','ZH':'ʒ'}
onsets={tuple(x.split()) for x in ['P R','B R','T R','D R','K R','G R','F R','TH R','SH R','P L','B L','K L','G L','F L','S L','S M','S N','S P','S T','S K','S W','T W','D W','K W','G W','S P R','S T R','S K R','S P L','S K L','S K W']}
def ipa(tokens):
    bases=[re.sub('[012]$','',x) for x in tokens]
    vowels=[i for i,x in enumerate(tokens) if x[-1:] in {'0','1','2'}]
    stress={}
    for n,i in enumerate(vowels):
        if tokens[i][-1] not in '12':continue
        start=0 if n==0 else vowels[n-1]+1
        if n:
            choices=[j for j in range(start,i) if i-j==1 or tuple(bases[j:i]) in onsets]
            start=min(choices) if choices else i
        stress[start]='ˈ' if tokens[i][-1]=='1' else 'ˌ'
    values=[]
    for i,t in enumerate(tokens):
        b=bases[i];value='ə' if t=='AH0' else 'ɚ' if t=='ER0' else phones[b]
        values.append(stress.get(i,'')+value)
    return '/'+''.join(values)+'/'
allowed={'n.','v.','vt.','vi.','adj.','adv.','prep.','pron.','num.','conj.','interj.','aux.','modal.'}
def forms(word):
    values={word,word+'s',word+'es',word+'ed',word+'ing',word+'er',word+'est'}
    if word.endswith('e'):values|={word[:-1]+'ing',word+'d'}
    if word.endswith('y'):values|={word[:-1]+'ies',word[:-1]+'ied'}
    if len(word)>2:values|={word+word[-1]+'ed',word+word[-1]+'ing'}
    values.update({'cling':['clung'],'wring':['wrung'],'can':['could'],'should':['should'],'ought':['ought']}.get(word,[]))
    return values
