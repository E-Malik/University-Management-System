using Microsoft.Data.Sqlite;
using UniversityMS.Domain;

namespace UniversityMS.Data;

// ===== LAYERED STYLE: DATA ACCESS LAYER =====
public class StudentRepository : IStudentRepository
{
    private readonly Db _db;
    public StudentRepository(Db db) => _db = db;
    private static Student Map(SqliteDataReader r) =>
        new() { Id = r.GetInt32(0), Name = r.GetString(1), Email = r.GetString(2), Program = r.GetString(3) };
    public List<Student> GetAll() => _db.Query("SELECT Id,Name,Email,Program FROM Students ORDER BY Name", Map);
    public Student? Get(int id) => _db.Query("SELECT Id,Name,Email,Program FROM Students WHERE Id=@i", Map, ("@i", id)).FirstOrDefault();
    public int Add(Student s) => _db.Insert("INSERT INTO Students(Name,Email,Program) VALUES(@n,@e,@p)",
        ("@n", s.Name), ("@e", s.Email), ("@p", s.Program));
    public void Delete(int id) => _db.Execute("DELETE FROM Students WHERE Id=@i", ("@i", id));
}

public class CourseRepository : ICourseRepository
{
    private readonly Db _db;
    public CourseRepository(Db db) => _db = db;
    private const string Sql = @"SELECT c.Id,c.Code,c.Title,c.Credits,c.Capacity,c.TeacherId,t.Name,
        (SELECT COUNT(*) FROM Enrollments e WHERE e.CourseId=c.Id) FROM Courses c JOIN Teachers t ON t.Id=c.TeacherId";
    private static Course Map(SqliteDataReader r) => new()
    { Id = r.GetInt32(0), Code = r.GetString(1), Title = r.GetString(2), Credits = r.GetInt32(3),
      Capacity = r.GetInt32(4), TeacherId = r.GetInt32(5), TeacherName = r.GetString(6), Enrolled = r.GetInt32(7) };
    public List<Course> GetAll() => _db.Query(Sql + " ORDER BY c.Code", Map);
    public Course? Get(int id) => _db.Query(Sql + " WHERE c.Id=@i", Map, ("@i", id)).FirstOrDefault();
    public int Add(Course c) => _db.Insert("INSERT INTO Courses(Code,Title,Credits,Capacity,TeacherId) VALUES(@c,@t,@cr,@cap,@tid)",
        ("@c", c.Code), ("@t", c.Title), ("@cr", c.Credits), ("@cap", c.Capacity), ("@tid", c.TeacherId));
    public void Delete(int id) => _db.Execute("DELETE FROM Courses WHERE Id=@i", ("@i", id));
    public List<Teacher> GetTeachers() => _db.Query("SELECT Id,Name,Email,Department FROM Teachers",
        r => new Teacher { Id = r.GetInt32(0), Name = r.GetString(1), Email = r.GetString(2), Department = r.GetString(3) });
}

public class EnrollmentRepository : IEnrollmentRepository
{
    private readonly Db _db;
    public EnrollmentRepository(Db db) => _db = db;
    private const string Sql = @"SELECT e.Id,e.StudentId,e.CourseId,s.Name,c.Code,c.Title,c.Credits,e.Grade
        FROM Enrollments e JOIN Students s ON s.Id=e.StudentId JOIN Courses c ON c.Id=e.CourseId";
    private static Enrollment Map(SqliteDataReader r) => new()
    { Id = r.GetInt32(0), StudentId = r.GetInt32(1), CourseId = r.GetInt32(2), StudentName = r.GetString(3),
      CourseCode = r.GetString(4), CourseTitle = r.GetString(5), Credits = r.GetInt32(6),
      Grade = r.IsDBNull(7) ? null : r.GetString(7) };
    public List<Enrollment> GetAll() => _db.Query(Sql + " ORDER BY e.Id DESC", Map);
    public List<Enrollment> GetByStudent(int sid) => _db.Query(Sql + " WHERE e.StudentId=@s", Map, ("@s", sid));
    public bool Exists(int sid, int cid) =>
        _db.Query("SELECT COUNT(*) FROM Enrollments WHERE StudentId=@s AND CourseId=@c", r => r.GetInt32(0), ("@s", sid), ("@c", cid))[0] > 0;
    public int Add(int sid, int cid) => _db.Insert("INSERT INTO Enrollments(StudentId,CourseId) VALUES(@s,@c)", ("@s", sid), ("@c", cid));
    public void SetGrade(int id, string grade) => _db.Execute("UPDATE Enrollments SET Grade=@g WHERE Id=@i", ("@g", grade), ("@i", id));
    public void Delete(int id) => _db.Execute("DELETE FROM Enrollments WHERE Id=@i", ("@i", id));
}
