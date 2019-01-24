// Dictionary.cpp : Implementation of DLL Exports.


#include "stdafx.h"
#include "resource.h"


// The module attribute causes DllMain, DllRegisterServer and DllUnregisterServer to be automatically implemented for you
[ module(dll, uuid = "{B1FDA49E-3899-4F62-90F3-4EECDEC113B8}", 
		 name = "Dictionary", 
		 helpstring = "Dictionary 1.0 Type Library",
		 resource_name = "IDR_DICTIONARY") ]
class CDictionaryModule
{
public:
// Override CAtlDllModuleT members
};
		 


// Dict.cpp : Implementation of CDict

#include "stdafx.h"
#include "Dict.h"


// CDict




CDict::CDict():
		m_fHavePartNib(0),
		m_nCancel(0),
		m_nLookups(0),
		m_pCallback(0),
		m_nDictLoaded(0) {
	put_DictNum(1);
	m_TAB1=" acdegilmnorstu\x00\x01";	// leading space because 
	m_TAB2=" bfhjkpqvwxzy";
	m_pWords= new CComObject<CWords>;	//create the Words collection
	m_pWords->m_pDict = this;
	m_pWords->AddRef();
}
	CDict::~CDict() {
		m_pWords->Release();
		m_pCallback=0;
	}


HRESULT CDict::get_DictNum(ULONG *pnDict) {
	*pnDict = m_nDictLoaded;
	return S_OK;
}
HRESULT CDict::put_DictNum(ULONG nDict) {
	char *resrc;
	if (nDict <1 || nDict > 2) 
		return E_INVALIDARG;
	if (nDict ==m_nDictLoaded) {
		return S_OK;
	}
	if (nDict == 1) {
		resrc=(char *)IDR_DICTBIG;
	} else {
		resrc=(char *)IDR_DICTSMALL;
	}
	HRSRC hrsrc;
	HGLOBAL hg;
	hrsrc =FindResource(_pModule->GetResourceInstance(),(char *)resrc,"DICT");
	hg = LoadResource(_pModule->GetResourceInstance(),hrsrc);
	m_DictBase = (NIB *)hg;
	m_nDictionaryTotalWordCount = *(int *)(m_DictBase + 26*26*4);
	m_nDictLoaded = nDict;
	return S_OK;
}

HRESULT CDict::IsWord(BSTR bstrWord, VARIANT_BOOL* nRetval) {
	USES_CONVERSION;
	char *szIsWord;
	szIsWord=W2A(bstrWord);
	if (!*szIsWord) {
		return E_INVALIDARG;
	}

	_strlwr(szIsWord);
	if (isWord(szIsWord)) {
		*nRetval = 1;
	} else {
		*nRetval = 0;
	}
	return S_OK;
}

BOOL CDict::isWord(char *szWord) {
	char c2;
	if (!szWord[1]) {
		if (szWord[0]=='a' || szWord[0]=='i')	// only 2 one letter words
			return 1;
		return 0;
	}
	c2 = szWord[1];
	StartDict(szWord[0],c2);
	while (c2 == m_szWord[1] || m_szWord[1] == 0  ) {
		GetNextWord();
		if (strcmp(szWord, m_szWord)==0) {
			return 1;
		}
	}
	return 0;
}

void CDict::StartDict(char cLet1, char cLet2) {
	int nOff;
	int nNib;
	m_fHavePartNib  = 0;
	_ASSERTE((cLet2 >='a' && cLet2 <= 'z') || cLet2==0);
	nOff = ((cLet2 == 0 ? 0 : (cLet2 - 'a')) + (cLet1 - 'a') * 26) * 4;
	nNib=*(int *)(m_DictBase +nOff);	// index lookup
	m_pNib = nNib/2 + m_DictBase + BOFFSET;			// pointer directly into dict
	m_szWord[0] = cLet1;
	m_szWord[1] = cLet2;

	if ((int )nNib & 1) {
		GetNextNib();
	}
}
NIB CDict::GetNextNib() {
	NIB cNib;
	m_fHavePartNib = 1-m_fHavePartNib;
	if (!m_fHavePartNib) {
		return m_PartNib;
	}
	cNib = *m_pNib++;
	m_PartNib = cNib & 0xf;
	return ((unsigned char)cNib)>>4;
}
char *CDict::GetNextWord() {
	NIB cNib;
	NIB *ptr;
	int nKeep=0;
	m_nLookups++;
	do {
		cNib = GetNextNib();
		nKeep+=cNib;
	} while (cNib == 15);
	
	ptr = m_szWord + nKeep;

	while (cNib = GetNextNib()) {
		if (cNib < 15) {	// first table
			*ptr++= m_TAB1[cNib];
		} else {
			cNib = GetNextNib();
			*ptr++= m_TAB2[cNib];
		}
	}
	_ASSERTE(ptr - m_szWord < sizeof(m_szWord));
	*ptr=0;
	return m_szWord;
}

HRESULT CDict::RandWord(LONG nRandomSeed, BSTR *bstrRandWord) {
	int nRand;
	int nCnt=0;
	int fGotit;
	int i,j;
	int ndx;


	switch(nRandomSeed ) {
	case 0:
		break;
	case 1:
		srand(GetTickCount());
		break;
	case 2:
		srand(nRandomSeed);
		break;
	}
	nRand = (int)(m_nDictionaryTotalWordCount * (((double)rand()) / RAND_MAX));
	int *WordCounts = (int *)(m_DictBase + (26*26+1)*4);

	for (i =fGotit=ndx= 0 ; i < 26 ; i++) {
		for (j = 0 ; j<26 ; j++) {
			if (nCnt + WordCounts[ndx] < nRand) {
				nCnt+= WordCounts[ndx];
			} else {
				fGotit=1;
				break;
			}
			ndx++;
		}
		if (fGotit)
			break;
	}
	StartDict(97+i,97+j);
	while (nCnt < nRand) {
		nRand--;
		GetNextWord();
	}

	CComBSTR bstrResult = m_szWord;
	bstrResult.CopyTo(bstrRandWord );

//	m_pWords->AddWord(m_szWord);
	
	return S_OK;
}

HRESULT CDict::get_Words( IWords** pVal){
	*pVal = m_pWords;
	m_pWords->AddRef();
	return S_OK;
}


char *CDict::soundex(char *s, char *ans) {
  int i,j,c,ch;
  static const char tab[]={"x123x12xx22455x12623x1x2x2"};
  ans[0]=*s++;
  ans[1]=ans[2]=ans[3]=ans[4]=0;
  for (i=strlen(s),j=1 ; i>0 && j<4 ; i--) {
    if ((c=tab[(ch=*s++)-'a']) != 'x' && c != ans[j-1]) {
      if (!(j==1 && ch == ans[0])) 
		  ans[j++]=c;
    }
  }
  return ans;
}



HRESULT CDict::Soundex([in] BSTR bstrWord, [out,retval] ULONG * pnResults) {
	char ans[5];
	char *szWord,sndx[5],*t;
	szWord=CW2A(bstrWord);
	strlwr(szWord);
	m_pWords->Clear();
	sndx,soundex(szWord, sndx);
	StartDict(szWord[0],0);
	while (*(t = GetNextWord()) == szWord[0]) {
		soundex(t,ans);
		if (lstrcmp(ans,sndx) == 0) {
			m_pWords->AddWord(t);
		}
	}
	m_pWords->get_Count(pnResults);
	return S_OK;
}

HRESULT CDict::DoAnagram(int nlevel) {
	char cTmp,cTmp2,cTmp3;;
	int trylen, flag;
	HRESULT hr;
	trylen= m_nLen  - nlevel;
	for (int i=0 ; i < trylen ; i++) {
		cTmp = m_szPattern[nlevel];		// swap nlevel and nlevel+i
		m_szPattern[nlevel] = m_szPattern[nlevel + i];
		m_szPattern[nlevel + i] = cTmp;
		if (nlevel < m_nLen-1) {
			if (nlevel > 1) { // check for partial matches.. if none, don't search this tree
				cTmp2 = m_szPattern[nlevel+1];
				cTmp3 = m_szPattern[nlevel+2];
				m_szPattern[nlevel+1]='*';	//add wildcard
				m_szPattern[nlevel+2]=0;	
				flag = FindMatch2(m_szPattern[0],m_szPattern[1],m_szPattern);
				m_szPattern[nlevel+1] = cTmp2;
				m_szPattern[nlevel+2] = cTmp3;
				if (!flag) {
					m_szPattern[nlevel + i] = m_szPattern[nlevel];	//restore the swap
					m_szPattern[nlevel] = cTmp;
					continue;		//no partial match, so don't recur
				}
				if (nlevel <= m_nSubwords) {
					m_szPattern[nlevel+1] = 0;	// temp nullterm
					if (isWord(m_szPattern)) {
						m_pWords->AddWord(m_szWord);
					}
					m_szPattern[nlevel+1] = cTmp2;
				}
			}
			hr=DoAnagram(nlevel+1);
			if (hr == E_ABORT || m_nCancel)
				return E_ABORT;
			m_szPattern[nlevel + i] = m_szPattern[nlevel];	//restore
			m_szPattern[nlevel] = cTmp;
		} else {	//got a full anagram: see if it's in the dict
			if (isWord(m_szPattern)) {
				m_pWords->AddWord(m_szWord);
				if (m_nCancel)
					return E_ABORT;
			}
			// no restore necessary because i == 0
		}
	}	
	return S_OK;
}

HRESULT CDict::FindAnagram(BSTR bstrWord, LONG nSubWords, ULONG *pnRetval) {
	char cTemp,lastc,*p,*q,*t;
	char szTemp[MAXWORDLEN];
	int i,j;
	HRESULT hr = S_OK;
	m_nCancel=0;
	m_pWords->Clear();
	strcpy(m_szPattern,CW2A(bstrWord));
	strlwr(m_szPattern);

	*pnRetval = 0;
	m_nLen = strlen(m_szPattern);
	if (nSubWords) {
		m_nSubwords=nSubWords;
		hr = DoAnagram(0);
		
	} else {
		for (i = 0 ; i < m_nLen ; i++) {
			for (j=0 ; j < i ; j++) {
				if (m_szPattern[i] < m_szPattern[j]) {
					cTemp =m_szPattern[i];
					m_szPattern[i] = m_szPattern[j];
					m_szPattern[j] = cTemp ;
				}
			}
		}
		lastc=0;

		for (i=0 ; i<m_nLen ; i++) {
			if ((cTemp=m_szPattern[i])==lastc) 
				continue;
			lastc=cTemp;
			StartDict(cTemp,0);
			while (1) {
				if (*(t=GetNextWord()) != lastc)
					break;
				if (strlen(t) != m_nLen) 
					continue;
				strcpy(szTemp,t);
				for (p=m_szPattern ; *p ; p++) {
					if (!(q=strchr(szTemp,*p))) 
						break;
					*q='0';
				}
				if (!*p) {
					m_pWords->AddWord(t);
				}
			}
			if (m_nCancel) {
				hr = E_ABORT;
				break;
			}
		}
	}
	m_pWords->get_Count(pnRetval);


	return hr;
}

HRESULT CDict::FindMatch(BSTR bstrPattern, BSTR * bstrRetval) {
	char cLastChar;
	char cLastChar2='z';
	strcpy(m_szPattern,CW2A(bstrPattern));
	strlwr(m_szPattern);
	*bstrRetval = 0;
	if (IsCharAlpha(cLastChar=*m_szPattern))	{//first char could be wild card
		if (IsCharAlpha(m_szPattern[1])) {
			StartDict(m_szPattern[0],m_szPattern[1]);
			cLastChar2=m_szPattern[1];
		} else {
			StartDict(m_szPattern[0]);
		}
	} else {
		StartDict('a');	// start at the beginning
		cLastChar='z';
	}
	if (FindMatch2(cLastChar,cLastChar2)) {
		CComBSTR bstr(m_szWord);
		bstr.CopyTo(bstrRetval);
	}

	return S_OK;
}
HRESULT CDict::FindMatches(BSTR bstrPattern , ULONG * pnRetval) {
	char cLastChar;
	char cLastChar2='z';
	HRESULT hr = S_OK;
	
	strcpy(m_szPattern,CW2A(bstrPattern));
	strlwr(m_szPattern);
	*pnRetval = 0;
	m_pWords->Clear();
	if (IsCharAlpha(cLastChar=*m_szPattern))	{//first char could be wild card
		if (IsCharAlpha(m_szPattern[1])) {
			StartDict(m_szPattern[0],m_szPattern[1]);
			cLastChar2=m_szPattern[1];
		} else {
			StartDict(m_szPattern[0]);
		}
	} else {
		StartDict('a');	// start at the beginning
		cLastChar='z';
	}
	while (FindMatch2(cLastChar,cLastChar2)) {
		m_pWords->AddWord(m_szWord);
		if (m_nCancel) {
			hr = E_ABORT;
			break;
		}
	}
	m_pWords->get_Count(pnRetval);
	return hr;
}
HRESULT CDict::NextMatch(BSTR * bstrRetval) {
	char cLastChar;
	char cLastChar2='z';
	*bstrRetval = 0;
	if (!m_szPattern || !m_szWord[0]) {
		return E_INVALIDARG;
	}
	if (IsCharAlpha(cLastChar=*m_szPattern))	{//first char could be wild card
		if (IsCharAlpha(m_szPattern[1])) {
			cLastChar2=m_szPattern[1];
		}
	} else {
		cLastChar='z';
	}
	if (!FindMatch2(cLastChar,cLastChar2)) {
		m_szWord[0] = 0;
	}
	CComBSTR bstr(m_szWord);
	bstr.CopyTo(bstrRetval);
	return S_OK;
}
BOOL CDict::FindMatch2(char LastChar,char LastChar2,char *szStart) {	
	// compares with m_szPattern. Start from szStart (defaults to curpos). End with LastChar+LastChar2 inclusive
	char *t;
	if (szStart) {
		if (szStart[1]) {
			StartDict(szStart[0],szStart[1]);
		} else {
			StartDict(szStart[0]);
		}
	}

	while ((t=GetNextWord()) && *t) {
		if (m_szWord[0] > LastChar || m_szWord[1] > LastChar2 ) 
			break;
		if (ismatch())
			return 1;
	}
	return 0;
}

BOOL CDict::letmatch(int c,char *q) {
	switch(c) {
	case '?':
		return 1;
	case '0':
		return (*q=='a' || *q=='e' || *q=='i' || *q=='o' || *q=='u');
	case '1':
		return (*q && *q!='a' && *q!='e' && *q!='i' && *q!='o' && *q!='u');
	case '2':
		return *q==*(q-1);
	}
	return c==*q;
}

BOOL CDict::ismatch() {
	int flag;
	char *p,*q,c;
	for (p=m_szPattern,q=m_szWord ; *p ; p++,q++) {
		if ((c=*p)=='*') {
			if (!(c=*++p)) 
				return 1;
			while (!(flag=letmatch(c,q)) && *q) 
				q++;
			if (!flag) {
				return 0;
			}
		} else {
			if (!letmatch(c,q)) 
				return 0;
		}
		if (!*q) 
			return 0;
	}
	return !*q;
}

//	JGLQIN XR QYL DBYXLPLULTQ GE QYL RNTQYLRXR GE YNDBXTQYR DTA CXRBHXQR.  BDIF RDTACHIZ


HRESULT CDict::Cryptogram([in] BSTR bstrCryptogram, [in] ULONG nMode, [out, retval] BSTR * bstrRetval) {
	char *ptrCrypt,*ptrWord;
	int i,nLen;
	int nDoInsert;
	int nCompare;
	CMyWord *pMyWord;
	*bstrRetval = 0;
	m_pbstrRetval = bstrRetval;
	if (SysStringLen(bstrCryptogram) > sizeof(m_szCrypt)) {
		return E_INVALIDARG;
	}
	strcpy (m_szCrypt ,CW2A(bstrCryptogram));	// dtor of CW2A fires: if string too long, then it's gone, so we need to copy it
	strlwr(m_szCrypt);
	strcat(m_szCrypt," ");// so will nullterm last word
	CMyWord oWord;
	// sort words by len, alpha into array, longest first, remove dupes
	m_nWords=0;
	for (ptrCrypt = m_szCrypt,ptrWord=oWord.szWord ; *ptrCrypt ; ptrCrypt++) {
		if (IsCharAlpha(*ptrCrypt)) {
			if (ptrWord == oWord.szWord) {
				oWord.nPos = ptrCrypt - m_szCrypt;
			}
			if (ptrWord - oWord.szWord >= sizeof(oWord.szWord)) {
				return E_INVALIDARG;
			}
			*ptrWord++ = *ptrCrypt;
		} else {
			*ptrWord = 0;
			nLen = ptrWord - oWord.szWord;
			oWord.nLen = nLen;
			if (nLen) {
				for (nDoInsert=1, i = 0,pMyWord = m_arWords.GetData() ; i < m_nWords ; i++, pMyWord++) {
					if (pMyWord->nLen > nLen || (pMyWord->nLen == nLen && (nCompare=strcmp(pMyWord->szWord, oWord.szWord)) < 0)) {
						continue;
					}
					nDoInsert=0;
					if (pMyWord->nLen == nLen && nCompare == 0) {	// dupe
						break;
					}
					_ASSERTE(i==0 || oWord.nLen <= m_arWords.GetAt(i-1).nLen);
					m_arWords.InsertAt(i,oWord);
					m_nWords++;
					break;
				}
				if (nDoInsert) {
					_ASSERTE(m_nWords ==0 || oWord.nLen <= m_arWords.GetAt(m_nWords-1).nLen);
					m_arWords.Add(oWord);
					m_nWords++;
				}
			}
			ptrWord=oWord.szWord;
#if _DEBUG
			ZeroMemory(&oWord, sizeof(oWord));
#endif _DEBUG
		}
	}
	if (m_nWords==0) {
		return E_INVALIDARG;
	}
	m_nCryptCount=0;
	m_nCancel=0;
	m_nLookups = 0;
	ZeroMemory(m_CryptKey,sizeof(m_CryptKey));
	if (!TryCrypt(0)) {	// start at first word, beginning of dict
		if (nMode > 0) {
			// no result. Let's try skipping a single word:
			for (i = 0,pMyWord = m_arWords.GetData()  ; i < m_nWords ; i++,pMyWord++) {
				pMyWord->fSkipit = 1;
				if (TryCrypt(0)) {
					break;
				}
				pMyWord->fSkipit = 0;
			}
		}
	} else {
		char szSolution[MAXCIPHER];
		ApplyCrypt(szSolution);
		CComBSTR bstr(szSolution);
		bstr.CopyTo(m_pbstrRetval);
	}


	m_arWords.RemoveAll();
	if (m_nCancel)
		return E_ABORT;
 	return S_OK;
}

int CDict::TryCrypt(int nWordIndex) {
	CMyWord *pMyWord;
	int j;
	char cLast,cLast2;
	int fBad=0;
	pMyWord = &m_arWords.GetAt(nWordIndex) ;
	if (m_pCallback) {
 		if (nWordIndex*2 >= m_nWords || nWordIndex > 2) {	// more than half match
			char szSolution[MAXCIPHER];
			wsprintf(szSolution,"%3d ",nWordIndex);
			ApplyCrypt(szSolution+4);
			CComBSTR bstr(szSolution);	// allocs space, dtor will free

			m_pCallback->PartialResult(bstr,&m_nCancel);
		}
	}
	if (pMyWord->fSkipit) {
		return TryCrypt(nWordIndex+1);
	}
	if (pMyWord->nLen == 1) {
		fBad=0;	//for bpt
	} else {
		for (j = 0 ; j < pMyWord->nLen ; j++) {
			cLast=m_CryptKey[pMyWord->szWord[j]-'a'];
			m_szPattern[j] = cLast == 0 ? '?': cLast;
		}
		m_szPattern[j]=0;
		if (m_szPattern[0] != '?') {
			if (m_szPattern[1]=='?') {
				cLast2='z';
				StartDict(m_szPattern[0]);
			} else {
				cLast2=m_szPattern[1];
				StartDict(m_szPattern[0],cLast2);
			}
			cLast = m_szPattern[0];
		} else {
			StartDict('a');
			cLast=cLast2='z';
		}
		char CryptTryTemp[26],cTemp,*cPtr;
		memcpy(CryptTryTemp,m_CryptKey,26);
		while (1) {
			while (m_szWord[0] != cLast  && memchr(m_CryptKey,m_szWord[0],26)!=0) {	// if the cipher letter is already assigned, then we can skip it
				StartDict(m_szWord[0] +1,0);
			}
			if (!FindMatch2(cLast,cLast2))
				break;
			fBad=0;
			for (j=0 ; !fBad && j < pMyWord->nLen ; j++) {	// for each letter in ciphertext
//				if (j && memchr(pMyWord->szWord, pMyWord->szWord[j],j-1)) {// already checked this one
//					continue;
//				}
				if ((cTemp=m_CryptKey[pMyWord->szWord[j]-'a']) !=0 && cTemp != m_szWord[j] ) {// already used and not cipher value
					fBad=1;
					break;
				} else {
					if ((cPtr=(char *)memchr(m_CryptKey,m_szWord[j],26)) && (cPtr - m_CryptKey+'a' != pMyWord->szWord[j])) {	// if the plaintext is already in the key
						fBad=1;
					}
					if (!fBad) {
						m_CryptKey[pMyWord->szWord[j]-'a'] = m_szWord[j];
					}
				}
			}
			if (fBad && j == 0) { //bad on first letter, let's skip 
				StartDict(m_szWord[0] +1,0);
			}

			if (!fBad) {
				// now loop through all the words, fill them out by the code, see if any of the complete ones are words
				char szTestWord[MAXWORDLEN], *ptrTestword;
				CMyWord *pMyWord2;
				int iWord,iLet,nIncomplete,fComplete;
				CryptDebugShow();
				m_nCryptCount++;
				for (nIncomplete =-1, iWord =nWordIndex+1, pMyWord2 =(iWord < m_nWords ? &m_arWords.GetAt(iWord): 0) ; iWord < m_nWords ; iWord++, pMyWord2++) {
					if (pMyWord2->fSkipit) {
						continue;
					}
					for (fComplete=1,iLet = 0,ptrTestword=szTestWord ; iLet < pMyWord2->nLen ; iLet++) {	// loop through each ltr of this word, apply code
						*ptrTestword = m_CryptKey[pMyWord2->szWord[iLet]-'a'];
						if (!*ptrTestword++) {	// if code isn't complete yet
							fComplete=0;
							if (nIncomplete == -1) {
								nIncomplete = iWord;	// remember which one is first to recur on
							}
							break;
						}
					}
					if (fComplete) {	// got complete word
						*ptrTestword = 0;	//null term
						CSaveState saveit(this);
						if (!isWord(szTestWord)) {
							fBad=1;
							break;
						}
					}
				}
				if (!fBad && nIncomplete != -1) {	// something to recur on
					CSaveState saveit(this);
					if (m_nCancel)
						return 0;
					fBad = !TryCrypt(nIncomplete);
					if (m_nCancel)
						return 0;
				}
				if (!fBad) {	// done testing all words, success
					return 1;
				}
			}
			if (fBad) {
				memcpy(m_CryptKey, CryptTryTemp,26);
			}
		}
	}
	return 0;
}

void CDict::ApplyCrypt(char *szSolution) {
	char  *ptr, *optr=szSolution;
	for (ptr = m_szCrypt ; *ptr ; ptr++,optr++) {
		if (IsCharAlpha(*ptr)) {
			*optr=m_CryptKey[*ptr - 'a'];
			if (!*optr)
				*optr=' ';
		} else {
			if (*ptr) {
				*optr= *ptr;
			} else {
				*optr = ' ';
			}
		}
	}
	*optr++=0;
}


HRESULT CDict::CryptDebugShow() {
	HRESULT hr = S_OK;

#if _DEBUG
	char szSolution[MAXCIPHER];
	ApplyCrypt(szSolution);
	char szBuff[MAXCIPHER];
	wsprintf(szBuff,"%s %6d\n",szSolution,m_nCryptCount);
	OutputDebugString(szBuff);
	OutputDebugString(m_szCrypt);
	OutputDebugString("\n");
#endif _DEBUG
	return hr;
}

HRESULT CDict::get_NumLookups(ULONG *pnLookups) {
	*pnLookups = m_nLookups;
	return S_OK;
}

/*
Each of nDigits can be a digit 0-9 or a letter a-z, which is 36 distinct values.
Separators can be placed in any one of nDigits - 1 places.  
To get all possible combinations for nDigits, we have 
nValues =nDigits^36		// total # of values of length nDigits
NSepPlaces = 2^(nDigits-1)	// for each value, a separator can or can not appear in nDigits-1 places
nTotal possible phrases = nSepPlaces * nValues = nDigits ^ 36 * 2^(nDigits-1).

For a  3 digit phone #, that's about 10^17 possible phrases
For a  7 digit phone #, that's 10^32
For a 10 digit phone #, that's 10^38
n=10
?n^36*(2^(n-1))
?LOG10(n^36*(2^(n-1)))

*/

HRESULT CDict::PhoneNumber(BSTR bstrDigits, BSTR * bstrRetval) {
	*bstrRetval = 0;
	char szNum[20];
	char *ptr;
	char *szPhoneNumber = CW2A(bstrDigits);
	if (SysStringLen(bstrDigits) > sizeof(szNum)) {
		return E_INVALIDARG;
	}
	// now strip off '-'
	for (ptr = szNum ; *szPhoneNumber ; szPhoneNumber++) {
		if (*szPhoneNumber>='0' && *szPhoneNumber <='9') {
			*ptr++ = *szPhoneNumber;
		}
	}
	*ptr=0;
	// now szNum is a phone # like "6423946369". Now enumerate all possible phrases without separators


	if (m_pCallback) {
		CComBSTR bstr("sample");	// allocs space, dtor will free

		m_pCallback->PartialResult(bstr,&m_nCancel);
	}

	return S_OK;
}



void CWords::AddWord(char *szWord) {
	HRESULT hr;
	VARIANT vt;
	VariantInit(&vt);
	vt.vt = VT_BSTR;
	CComBSTR bstr(szWord);	// allocs space, dtor will free
//	bstr.CopyTo(&vt);
	vt.bstrVal = bstr;
	
	m_coll.Add(vt);	// allocs space
	if (m_pDict->m_pCallback) {
		hr = m_pDict->m_pCallback->PartialResult(bstr,&m_pDict->m_nCancel);
	}
}

HRESULT CWords::get__NewEnum(IUnknown **pVal) {
	HRESULT hr;
	typedef CComObject<CComEnum<IEnumVARIANT, &IID_IEnumVARIANT, VARIANT, _Copy<VARIANT> > > enumvar;
	enumvar *p = new enumvar;
	hr = p->Init(m_coll.GetData()  ,m_coll.GetData() + m_coll.GetCount() ,0);
	if (hr == S_OK) {
		hr = p->QueryInterface(IID_IEnumVARIANT, (void **) pVal);
	}
	return S_OK;
}


HRESULT CWords::get_Item(ULONG nIndex, VARIANT *pVal) {
	HRESULT hr = E_FAIL;
	VariantInit(pVal);
	//*pVal = 0;

	if (nIndex >=m_coll.GetCount())
		return E_INVALIDARG;
	hr = VariantCopy(pVal,&m_coll.GetAt(nIndex));
	return hr;
}

/*
PUBLIC ox as dictionary.dict
ocallback=NEWOBJECT('dictcallback')
ox=NEWOBJECT('dictionary.dict')
ox.Callback=ocallback
?ox.isword("testing")
ox.FindAnagram("testing",0)


DEFINE CLASS dictcallback as Custom
	IMPLEMENTS IDictCallBack IN dictionary.dict
	PROCEDURE IDictCallBack_PartialResult(cWord,nCancel)
		?cWord
		nCancel=1
		retu
	
ENDDEFINE


Create the dictionaries:
CLEAR ALL
CLEAR


DIMENSION alet[26,26]
alet=0
#define TAB1	"acdegilmnorstu"	&& 14
#define TAB2	"bfhjkpqvwxzy"		&& 12
*				012345678901234
PUBLIC nibPart	&& 0 means 1st, 1 means 2nd
PUBLIC nibHavePart
PUBLIC cWordBuff
cWordBuff=""
nibHavePart=.f.
nibPart = 0
ctab1=	TAB1+CHR(0)+CHR(1)
ctab2= TAB2

#if .t.	&& test routine

USE words ORDER 1
csearch="ab"
SEEK csearch
BROWSE LAST nowa
MODIFY COMMAND PROGRAM() NOWAIT 
cdict="dictsmall.bin"
cdict="dictbig.bin"
fd=FOPEN(cdict,0)
?fd
FOR i = 1 TO 26
	?i,chr(i+96),' '
	FOR j= 1 TO 26
		cstr=FREAD(fd,4)
		alet[i,j]=bintonum(cstr)
		??PADR(alet[i,j],7)
	ENDFOR
ENDFOR
#define BOFFSET ((26*26*2+1)*4)

nind1=ASC(csearch)-96
IF LEN(csearch)>1
	nind2=ASC(SUBSTR(csearch,2))-96
ELSE
	nind2=1
ENDIF
cWordBuff=LEFT(csearch,2)

noff=INT(alet[nind1,nind2]/2) + BOFFSET
FSEEK(fd,noff,0)
IF MOD(alet[nind1,nind2],2)=1	&& if odd, then read/discard 1 nib
	getnextnib()
ENDIF 
FOR i = 1 TO 770
	cword=getnextword()
	IF MOD(i,1000)=0
		?cword
	ENDIF
	IF LEN(cword)=0
		EXIT
	ENDIF 
	?cword
	IF !SEEK(cword)
		?cword
	endif
	IF FEOF(fd)
*		EXIT
	endif
ENDFOR
FCLOSE(fd)

PROCEDURE GetNextWord()
	LOCAL cstr,nKeep
	cstr=""
	nkeep=0
	DO WHILE .t.
		nnib=getnextnib()
		IF nnib != 15
			nkeep=nkeep+nnib
			EXIT
		ENDIF 
		nkeep=nkeep+nnib
	ENDDO
	DO WHILE .t.
		nNibble = getnextnib()
		IF nNibble<15
			IF nNibble>0
				cstr=cstr+SUBSTR(TAB1,nNibble,1)
			ELSE
				cWordBuff=LEFT(cwordBuff,nKeep)+cstr
				RETURN cWordBuff
			ENDIF 
		ELSE
			nNibble=getnextnib()
			cstr=cstr+SUBSTR(TAB2, nNibble,1)
		ENDIF 
	ENDDO 
RETURN 

#endif

#if .t.	&& create from scratch
CLOSE DATABASES all
CREATE TABLE words free (word c(30),runLen i, nibs c(100))
IF .t.	&& .t.=bigdict or .f.=small dict
	fBigDict=.t.
	CREATE TABLE badwords free (word c(30))
	CREATE TABLE lines FREE (line c(250))
	APPEND FROM d:\calvinh\spelldictionary.htm sdf
	LOCATE
	*BROWSE LAST nowait
	81
	SCAN REST WHILE RECNO() < 22838
		nwords=GETWORDCOUNT(line)
		FOR i = 1 TO nwords
			cword=LOWER(GETWORDNUM(lines.line,i))
			fGood =.t.
			FOR j= 1 TO LEN(cword)
				IF !ISALPHA(SUBSTR(cword,j,1))
	*				?cword
					fGood = .f.
					EXIT 
				ENDIF 
			ENDFOR
			IF fGood
				INSERT INTO words (word) VALUES (cword)
			ELSE
				INSERT INTO badwords VALUES (cword)
				
			ENDIF 
		ENDFOR
		
	ENDSCAN
ELSE
	fBigDict=.f.
	APPEND FROM wordssmall FOR AT("'",word)=0
ENDIF

SELECT words
INDEX on word TAG word
BROWSE LAST NOWAIT 
#endif

USE words EXCLUSIVE
#if 1
REPLACE ALL runlen WITH 0
cOld=""
SCAN
	oldlen = LEN(cOld)
	cWord = TRIM(word)
	nLen = LEN(cWord)
	
	FOR i = 1 to nLen
		IF i > oldlen OR SUBSTR(cWord,i,1) != SUBSTR(cOld,i,1)
			REPLACE runLen WITH i-1
			EXIT 
		ENDIF 
	ENDFOR 
	cOld=cWord
ENDSCAN
#endif

LOCATE 
BROWSE LAST nowai
nNibs=0
nindOld1=0
nindOld2=0
*153	abdominohysterotomy           runlen=18
SET ASSERTS ON
nRounds=0
IF fBigDict
	fd = FCREATE("dictbig.bin")
ELSE
	fd = FCREATE("dictsmall.bin")
ENDIF 
FWRITE(fd,REPLICATE(CHR(0),(26*26*2+1 )*4))
SCAN NEXT 900000
	cWord = TRIM(word)
	nLen = LEN(cWord)

	nind1 = ASC(cWord)-96
	IF nLen > 1
		nind2=ASC(SUBSTR(cWord,2))-96
	ELSE
		nind2=1
	ENDIF 
	IF nindOld1 != nind1 OR nindOld2 != nind2
		aLet[nind1,nind2]=INT(nNibs)
		?nind1,nind2,nNibs,cWord
	ENDIF 
	nindOld1 = nind1
	nindOld2 = nind2
*	?PADL(cWord,30)+" "+TRANSFORM(runlen)," "
	cNib=getlen(runlen)
	FOR i = 1+runlen TO nLen
		cNib=cNib+getnib(SUBSTR(cWord,i,1))
	ENDFOR 
	cNib=cNib+"0"
	FOR i = 1 TO LEN(cNib)
		outnib(SUBSTR(cNib,i,1))
	ENDFOR 
*	??cNib
	nNibs=nNibs+LEN(cNib)
	REPLACE nibs WITH cNib
	cOld = cWord
ENDSCAN
outnib("0")	&& pad in case odd number
outnib("0")	
outnib("0")	
LOCATE
IF !fBigDict
	COPY TO wordsSmall
	SELECT 0
	USE wordssmall
	INDEX ON word TAG word
	USE
	SELECT words
ENDIF 
FSEEK(fd,0)
FOR i = 1 TO 26
	FOR j = 1 TO 26
		FWRITE(fd,numtoword(alet[i,j]))
	ENDFOR
ENDFOR
FWRITE(fd,numtoword(RECCOUNT()))
SELECT DISTINCT LEFT(word,2) as lets ,count(*) from words GROUP BY 1 INTO CURSOR totals
INDEX ON lets TAG lets
FOR i = 1 TO 26
	?CHR(96+i)
	FOR j = 1 TO 26
		clets=CHR(96+i) + CHR(96+j)
		IF SEEK(clets)
			IF j=1
				nLets=cnt+1
			ELSE 
				nLets=cnt
				IF j=1
					SKIP
					nLets=nLets+cnt
				ENDIF 
			ENDIF 
		ELSE 
			nLets=0
		ENDIF 
		??' ',PADR(nLets,6)
		FWRITE(fd,numtoword(nLets))
	ENDFOR
ENDFOR




FCLOSE(fd)
?nNibs
nZeros=0
FOR i = 1 TO 26
	?CHR(96+i)
	FOR j = 1 TO 26
		??" "+padr(alet[i,j],7)
		IF alet[i,j]=0
			nZeros=nZeros+1
		ENDIF 
	ENDFOR
ENDFOR
?"nZeros=",nZeros
?"nRounds=",nRounds

RETURN

PROCEDURE GetNextNib() as Character
	LOCAL cnib
	nibHavePart = !nibHavePart
	IF !nibHavePart
		RETURN nibpart
	ELSE
		cnib=ASC(FREAD(fd,1))
		nibpart=MOD(cnib,16)
		RETURN INT(cnib/16)
	ENDIF
	RETURN 

PROCEDURE outnib(nNib as Character)
*!*		cc=strtobase(nNib)
*!*		FWRITE(fd,CHR(INT(cc)))
*!*		retu
	nibHavePart = !nibHavePart
	IF nibHavePart
		nibPart = strtobase(nNib)
	ELSE
		cByte=strtobase(nNib) + nibPart*16
		FWRITE(fd,CHR(INT(cByte)))
	ENDIF
	
	retu

PROCEDURE getlen(nLen as integer) as String
	LOCAL cret
	cret=""
	DO WHILE nLen >= 15
		cret=cret+"F"
		nLen = nLen - 15
	enddo
	cret=cret+intTOBase(nLen)
	RETURN cret

PROCEDURE getnib(nchr as Character) as String
	LOCAL cres as String
	n=AT(nchr,TAB1)
	IF n=0
		n=AT(nchr,TAB2)
		RETURN "F"+ intToBase(n)
	ENDIF
	RETURN intToBase(n)
RETURN
	PROCEDURE BinToNum(cstr as String) as Integer	&&CHR(1)+CHR(1)+CHR(0)+CHR(0) -> 257
		LOCAL nNum,nLen,i
		nNum=0
		nLen = LEN(cstr)
		FOR i = 1 TO nLen
			nNum=nNum+ 256^(i-1) *ASC(SUBSTR(cstr,i,1))
		ENDFOR
		RETURN INT(nNum)

	PROCEDURE NUMToWord(n)
		LOCAL i,n,cres
		* n is single integer that's put into 4 bytes
		cres=""
		FOR i = 0 TO 3
			cres=cres+CHR(BITAND(n,0xff))
			n = INT(n / 256)
		ENDFOR
		RETURN cres

	PROCEDURE StrToBase(cStr as String) as Integer	&& "01111111" -> 127
		LOCAL nVal,i,nLen,cDig,nDig
		nLen=LEN(cStr)
		nVal = 0
		FOR i = 1 TO nLen
			cDig=SUBSTR(cStr,i,1)
			IF ISDIGIT(cDig)
				nDig = VAL(cDig)
			ELSE
				nDig = ASC(UPPER(cDig))-64+9
			endif
			nVal = nVal + nDig * 16 ^ (nLen-i)
		ENDFOR
		RETURN nVal

	#define CDIGS "0123456789abcdef"
	PROCEDURE intToBase(nNum as Integer) as string	&& intToBase(31,16) = "1F"
		LOCAL cstr,nSize,i,nBit
		cStr=""
		IF nNum=0
			RETURN "0"
		endif
		
		nSize=LOG(nNum)/LOG(16)
		nSize=INT(nSize)
		FOR i = nSize TO  0 STEP -1
			nBit = INT(nNum / 16^i)
			nNum = nNum - nBit * 16^i
			cStr = cStr+SUBSTR(CDIGS,nBit+1,1)
		ENDFOR
		RETURN cstr







*/


