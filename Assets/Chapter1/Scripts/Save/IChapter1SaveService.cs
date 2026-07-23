namespace DormitoryMystery.Chapter1
{
    public interface IChapter1SaveService
    {
        string SavePath { get; }
        void Save(Chapter1SaveData data);
        Chapter1SaveData Load();
        bool HasSave();
        void DeleteSave();
    }
}
