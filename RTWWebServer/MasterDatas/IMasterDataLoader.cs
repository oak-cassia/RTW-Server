namespace RTWWebServer.MasterDatas;

public interface IMasterDataLoader
{
    // 마스터 데이터 파일을 읽어 검증된 불변 스냅샷을 만든다. 잘못된 데이터/누락 파일은 예외(fail-fast).
    MasterDataSet Load();
}
