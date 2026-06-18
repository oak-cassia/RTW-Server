using RTWWebServer.MasterDatas;

namespace RTWTest.WebServer.MasterData;

// JSON을 직접 역직렬화하는 로더의 정상/실패 경로 가드. 기존 ValidateOnStart가 하던
// "잘못된 마스터 데이터면 기동 실패"를 로더 단위로 검증한다.
[TestFixture]
public class MasterDataLoaderTests
{
    private string _directory = null!;

    [SetUp]
    public void SetUp()
    {
        _directory = Path.Combine(Path.GetTempPath(), "masterdata_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Test]
    public void Load_ValidFiles_ReturnsPopulatedSet()
    {
        WriteAllValid();

        var set = new MasterDataLoader(_directory).Load();

        Assert.That(set.Characters.Keys, Is.EquivalentTo(new[] { 1, 2 }));
        Assert.That(set.Furniture.Keys, Is.EquivalentTo(new[] { 2001 }));
        Assert.That(set.RoomGrades.Keys, Is.EquivalentTo(new[] { 1, 2 }));
        Assert.That(set.Characters[1].Name, Is.EqualTo("Character1"));
        Assert.That(set.RoomGrades[2].Width, Is.EqualTo(50));
    }

    [Test]
    public void Load_MissingGradeOne_Throws()
    {
        WriteAllValid();
        Write("RoomGradeMaster.json", """{ "RoomGrades": [ { "Grade": 2, "Width": 50, "Height": 50 } ] }""");

        Assert.Throws<InvalidOperationException>(() => new MasterDataLoader(_directory).Load());
    }

    [Test]
    public void Load_DuplicateFurnitureId_Throws()
    {
        WriteAllValid();
        Write("FurnitureMaster.json", """
            {
              "Furniture": [
                { "Id": 2001, "Name": "A", "Category": 1, "Width": 1, "Height": 1 },
                { "Id": 2001, "Name": "B", "Category": 1, "Width": 1, "Height": 1 }
              ]
            }
            """);

        Assert.Throws<InvalidOperationException>(() => new MasterDataLoader(_directory).Load());
    }

    [Test]
    public void Load_MissingFile_Throws()
    {
        // RoomGradeMaster.json을 쓰지 않는다.
        WriteCharacters();
        WriteFurniture();

        Assert.Throws<FileNotFoundException>(() => new MasterDataLoader(_directory).Load());
    }

    [Test]
    public void Load_WrongRootProperty_Throws()
    {
        WriteAllValid();
        Write("CharacterMaster.json", """{ "Wrong": [] }""");

        Assert.Throws<InvalidOperationException>(() => new MasterDataLoader(_directory).Load());
    }

    private void WriteAllValid()
    {
        WriteCharacters();
        WriteFurniture();
        WriteRoomGrades();
    }

    private void WriteCharacters() => Write("CharacterMaster.json", """
        {
          "Characters": [
            { "Id": 1, "Name": "Character1", "Portfolio": 50, "Development": 60, "JobSearching": 70 },
            { "Id": 2, "Name": "Character2", "Portfolio": 80, "Development": 90, "JobSearching": 75 }
          ]
        }
        """);

    private void WriteFurniture() => Write("FurnitureMaster.json", """
        { "Furniture": [ { "Id": 2001, "Name": "Desk", "Category": 1, "Width": 1, "Height": 1 } ] }
        """);

    private void WriteRoomGrades() => Write("RoomGradeMaster.json", """
        { "RoomGrades": [ { "Grade": 1, "Width": 30, "Height": 30 }, { "Grade": 2, "Width": 50, "Height": 50 } ] }
        """);

    private void Write(string fileName, string content)
    {
        File.WriteAllText(Path.Combine(_directory, fileName), content);
    }
}
