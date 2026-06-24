namespace RTWWebServer.MasterDatas.Models;

// 마스터가 비용을 어떤 재화로 받을지 지정한다. User의 두 재화 필드와 1:1 대응한다.
// (JSON에서는 정수값으로 표기: 0=Free(골드/FreeCurrency), 1=Premium(PremiumCurrency))
public enum CurrencyType
{
    Free = 0,
    Premium = 1,
}
