using Microsoft.Data.Sqlite;

namespace UniversityMS.Data;

// ===== SHARED-DATA STYLE =====
// One central database acts as the shared data store. Every component (students, courses,
// enrollment, reporting) communicates ONLY by reading/writing this shared store.
public class Db
{
    private readonly string _cs;
    public Db(string file = "university.db") => _cs = $"Data Source={file}";

    private SqliteConnection Open()
    {
        var c = new SqliteConnection(_cs);
        c.Open();
        using var pragma = c.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON;";
        pragma.ExecuteNonQuery();
        return c;
    }

    private static void Bind(SqliteCommand cmd, (string, object?)[] ps)
    {
        foreach (var (n, v) in ps) cmd.Parameters.AddWithValue(n, v ?? DBNull.Value);
    }

    public void Execute(string sql, params (string, object?)[] ps)
    {
        using var c = Open(); using var cmd = c.CreateCommand();
        cmd.CommandText = sql; Bind(cmd, ps); cmd.ExecuteNonQuery();
    }

    public int Insert(string sql, params (string, object?)[] ps)
    {
        using var c = Open(); using var cmd = c.CreateCommand();
        cmd.CommandText = sql + "; SELECT last_insert_rowid();"; Bind(cmd, ps);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public List<T> Query<T>(string sql, Func<SqliteDataReader, T> map, params (string, object?)[] ps)
    {
        var list = new List<T>();
        using var c = Open(); using var cmd = c.CreateCommand();
        cmd.CommandText = sql; Bind(cmd, ps);
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(map(r));
        return list;
    }

    public void Init()
    {
        Execute(@"
CREATE TABLE IF NOT EXISTS Students(Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, Email TEXT NOT NULL UNIQUE, Program TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS Teachers(Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, Email TEXT NOT NULL UNIQUE, Department TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS Courses(Id INTEGER PRIMARY KEY AUTOINCREMENT, Code TEXT NOT NULL UNIQUE, Title TEXT NOT NULL, Credits INTEGER NOT NULL, Capacity INTEGER NOT NULL,
  TeacherId INTEGER NOT NULL REFERENCES Teachers(Id));
CREATE TABLE IF NOT EXISTS Enrollments(Id INTEGER PRIMARY KEY AUTOINCREMENT,
  StudentId INTEGER NOT NULL REFERENCES Students(Id) ON DELETE CASCADE,
  CourseId INTEGER NOT NULL REFERENCES Courses(Id) ON DELETE CASCADE,
  Grade TEXT NULL, UNIQUE(StudentId, CourseId));");

        if (Query("SELECT COUNT(*) FROM Teachers", r => r.GetInt32(0))[0] == 0)
        {
            Execute(@"
INSERT INTO Teachers(Name,Email,Department) VALUES('Dr. Ayesha Khan','ayesha@uni.edu','Computer Science'),('Dr. Bilal Ahmed','bilal@uni.edu','Mathematics');
INSERT INTO Courses(Code,Title,Credits,Capacity,TeacherId) VALUES('CS101','Programming Fundamentals',3,30,1),('CS201','Software Architecture',3,2,1),('MT101','Calculus',4,40,2);
INSERT INTO Students(Name,Email,Program) VALUES('Ali Raza','ali@uni.edu','BSCS'),('Sara Malik','sara@uni.edu','BSSE');");
        }
    }
}
