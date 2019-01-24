// Dict.h : Declaration of the CDict

#pragma once
#include "resource.h"       // main symbols
#include "atlcoll.h"

#define MAXWORDLEN 32
#define MAXCIPHER 1024

#define BOFFSET ((26*26*2+1)*4)
// 26^2 table of nibble ptrs
// 1	cnt of total words
// 26^2 table of cnts 

typedef  char NIB;
class CDict;

// IWords
[
	object,
	uuid("42502EC6-ED35-4626-AAA2-151F122F4A5E"),
	dual,	helpstring("IWords Interface"),
	pointer_default(unique)
]
__interface IWords : IDispatch
{
	[propget, id(1), helpstring("Count")] HRESULT Count([out, retval] ULONG* pVal);
	[propget, id(DISPID_VALUE), helpstring("Item")] HRESULT Item([in]ULONG nIndex, [out, retval] VARIANT *pVal);
	[propget, id(DISPID_NEWENUM),restricted, helpstring("property _NewEnum")] HRESULT _NewEnum([out, retval] IUnknown ** pVal);
};

// CWords

[
	coclass,
	threading("apartment"),
	uuid("74E978DE-7A03-456A-A7E2-6BA40E5C6B33"),
	helpstring("Words Class")
]
class ATL_NO_VTABLE CWords : 
	public IWords
{
public:
	CWords()
	{
	}
	HRESULT get_Item(ULONG nIndex, VARIANT *pVal) ;
	HRESULT get_Count(ULONG* pVal) {
		*pVal = m_coll.GetCount();
		return S_OK;
	}
	HRESULT get__NewEnum(IUnknown **pVal);
	HRESULT FinalConstruct()
	{
		return S_OK;
	}
	
	void FinalRelease() 
	{
	}
public:
	void AddWord(char *szWord);
	void Clear() {
		m_coll.RemoveAll();
	}
protected:
private:
	CAtlArray<CComVariant> m_coll;
	CDict *m_pDict;
	friend class CDict;
};


// IDictCallback
[
	uuid("0CED18E5-8870-4F62-B1CB-E50C3BCA8FB3"),
	oleautomation,
	dual,	helpstring("IDict Interface"),
	pointer_default(unique)
]
__interface IDictCallback: IDispatch
{
	[id(1), helpstring("PartialResult")] HRESULT PartialResult(BSTR bstrWord, [in,out] ULONG* nCancel);

};


// IDict
[
	object,
	uuid("0CED18E4-8870-4F62-B1CB-E50C3BCA8FB3"),
	oleautomation,
	dual,	helpstring("IDict Interface"),
	pointer_default(unique)
]
__interface IDict : IDispatch
{
	[id(1), helpstring("method IsWord")] HRESULT IsWord(BSTR bstrWord, [out,retval] VARIANT_BOOL* nRetval);
	[propget,id(2), helpstring("1=BigDict(171201), 2=SmallDict (53869)")] HRESULT DictNum([out,retval] ULONG* pnDict);
	[propput,id(2), helpstring("1=BigDict, 2=SmallDict")] HRESULT DictNum([in] ULONG nDict);
	[id(3), helpstring("0 means continue random sequence, 1=Tickcount, >1 is seed")] HRESULT RandWord([in] LONG nRandomSeed, [out,retval] BSTR* bstrRandWord);
	[propget, id(4), helpstring("Words collection")] HRESULT Words([out, retval] IWords** pVal);
	[id(5), helpstring("FindAnagram (nSubWords = 0 for complete anag, >0 for min length of subwords")] HRESULT FindAnagram([in] BSTR bstrWord, [in] LONG nSubWords, [out, retval] ULONG *pnRetval);
	[id(6), helpstring("Soundex")] HRESULT Soundex([in] BSTR bstrWord, [out,retval] ULONG * pnResults);
	[id(7), helpstring("'*' any series of letters,'?' any single letter,'0' vowels a,e,i,o,u,'1' any nonvowel,'2' the previous letter")] HRESULT FindMatch(BSTR bstrPattern, [out,retval] BSTR * bstrRetval);
	[id(8), helpstring("'*' any series of letters,'?' any single letter,'0' vowels a,e,i,o,u,'1' any nonvowel,'2' the previous letter")] HRESULT FindMatches([in] BSTR bstrPattern , [out,retval] ULONG * pnRetval);
	[id(9), helpstring("Next Match")] HRESULT NextMatch([out,retval] BSTR * bstrRetval);
	[propput,id(10), helpstring("Callback")] HRESULT Callback(IDictCallback *pCallback);
	[propget,id(11), helpstring("# of dictionary lookups")] HRESULT NumLookups([out, retval]	ULONG *pnLookups);
	[id(12), helpstring("Cryptogram")] HRESULT Cryptogram([in] BSTR bstrCryptogram,[in] ULONG nMode, [out,retval] BSTR * bstrRetval);
	[id(13), helpstring("Phrases from phone numbers. Specify phone #")] HRESULT PhoneNumber([in] BSTR bstrDigits, [out,retval] BSTR * bstrRetval);
    

};



// CDict

[
	coclass,
	source(IDictCallback),
	threading("apartment"),
	support_error_info("IDict"),
	vi_progid("Dictionary.Dict"),
	progid("Dictionary.Dict.1"),
	version(1.0),
	uuid("3ED98B67-96FC-42A1-A361-2141CC07D1C4"),
	helpstring("Dict Class")
]
class ATL_NO_VTABLE CDict : 
	public IDict
{
public:
	CDict();
	~CDict();
	HRESULT FinalConstruct()
	{
		return S_OK;
	}
	HRESULT IsWord(BSTR bstrWord, VARIANT_BOOL* nRetval);
	HRESULT get_DictNum(ULONG *pnDict) ;
	HRESULT put_DictNum(ULONG nDict) ;
	HRESULT RandWord(LONG nRandomSeed, BSTR *bstrRandWord);
	HRESULT FindAnagram(BSTR bstrWord, LONG nSubWords, ULONG *pnRetval);
	HRESULT get_Words( IWords** pVal);
	HRESULT Soundex(BSTR bstrWord, ULONG * pnResults);
	HRESULT FindMatch(BSTR bstrPattern, BSTR * bstrRetval);
	HRESULT FindMatches(BSTR bstrPattern , ULONG * pnRetval);
	HRESULT NextMatch(BSTR * bstrRetval);
	HRESULT get_NumLookups(ULONG *pnRetval);
	HRESULT put_Callback(IDictCallback *pCallback) {
		HRESULT hr;
		hr = pCallback->QueryInterface(__uuidof(IDictCallback), (void **) &m_pCallback);
		return hr;
	}
	HRESULT Cryptogram(BSTR bstrCryptogram, ULONG nMode, BSTR * bstrRetval);
	HRESULT PhoneNumber(BSTR bstrDigits, BSTR * bstrRetval);
	void FinalRelease() 
		{
		}

public:
private:
	class CMyWord {
	public:
		CMyWord():fSkipit(0) {
	#if _DEBUG
			ZeroMemory(szWord, sizeof(szWord));
	#endif _DEBUG
		}
		char szWord[MAXWORDLEN];
		int nLen;
		int nPos;	//index into original cryptogram string
		int fSkipit;	// skip: might be not in dictionary (like proper name)
	};
	CAtlArray<CMyWord> m_arWords;
	char m_CryptKey[26];	// 0-25 Cipher index returns plain text
	char m_szCrypt[MAXCIPHER];	// the original ciphertext
	int CDict::TryCrypt(int nWordIndex);
	HRESULT CDict::CryptDebugShow();
	void CDict::ApplyCrypt(char *szSolution);
	int m_nCryptCount;
	int m_nWords;
	BSTR * m_pbstrRetval;


	int m_nLookups;
	CComPtr<IDictCallback> m_pCallback;
	friend class CWords;
	char *m_TAB1;
	char *m_TAB2;
	int m_nLen;
	int m_nSubwords;
	int m_nDictionaryTotalWordCount;
	int	m_nDictLoaded;	//0 means nothing loaded, 1 = Bigdict, 2 = smalldict
	NIB *m_DictBase;
	NIB * m_pNib;	// ptr to the nibble 
	NIB m_PartNib;
	int m_fHavePartNib;
	NIB GetNextNib();
	char *GetNextWord();
	void StartDict(char cLet1,char cLet2='a');
	char m_szWord[MAXWORDLEN];
	char m_szPattern[MAXWORDLEN];
	CComObject<CWords> * m_pWords;	//Words collection
	char *CDict::soundex(char *s, char *ans);
	BOOL CDict::FindMatch2(char cLastChar,char cLastChar2='z', char *szStart=0);
	BOOL CDict::letmatch(int c,char *q);
	BOOL CDict::ismatch(void);
	BOOL isWord(char *szWord);
	HRESULT CDict::DoAnagram(int nlevel);
	ULONG m_nCancel;		// non-zero means abort current long-running calculation
	friend class CSaveState;
};


	class CSaveState {
	public:
		CSaveState(CDict *pDict) {
			m_pDict = pDict;
			strcpy(szWord,m_pDict->m_szWord);
			strcpy(szPattern,m_pDict->m_szPattern);
			pNib = m_pDict->m_pNib;
			PartNib = m_pDict->m_PartNib;
			fHavePartNib = m_pDict->m_fHavePartNib;
		}
		~CSaveState() {
			strcpy(m_pDict->m_szWord,szWord);
			strcpy(m_pDict->m_szPattern,szPattern);
			m_pDict->m_pNib = pNib;
			m_pDict->m_PartNib = PartNib;
			m_pDict->m_fHavePartNib = fHavePartNib;
		}
		char szWord[MAXWORDLEN];
		char szPattern[MAXWORDLEN];
		NIB * pNib;	// ptr to the nibble 
		NIB PartNib;
		int fHavePartNib;
	private:
		CDict *m_pDict;
	};
