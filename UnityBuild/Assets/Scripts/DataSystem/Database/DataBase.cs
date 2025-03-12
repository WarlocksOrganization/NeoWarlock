namespace DataSystem.Database
{
    public static partial class Database
    {
        public static void LoadDataBase()
        {
            LoadAttackData();
            LoadMovementSkillData();
            LoadPlayerCardData();
        }
    }
}