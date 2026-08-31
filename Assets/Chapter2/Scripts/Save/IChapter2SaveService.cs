namespace DormitoryMystery.Chapter2
{
    public interface IChapter2SaveService
    {
        string SavePath { get; }
        void Save(Chapter2SaveData data);
        Chapter2SaveData Load();
        bool HasSave();
        void DeleteSave();
    }
}
