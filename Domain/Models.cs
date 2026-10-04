namespace UniversityMS.Domain;

// ===== OBJECT-ORIENTED DESIGN =====
// Abstraction + Inheritance + Polymorphism: Person is abstract, subclasses define Role.
public abstract class Person
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public abstract string Role { get; }          // polymorphic member
    public virtual string Describe() => $"{Role}: {Name} ({Email})";
}

public class Student : Person
{
    public string Program { get; set; } = "";
    public override string Role => "Student";
    public override string Describe() => base.Describe() + $" - {Program}";
}

public class Teacher : Person
{
    public string Department { get; set; } = "";
    public override string Role => "Teacher";
    public override string Describe() => base.Describe() + $" - {Department}";
}

public class Course
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Title { get; set; } = "";
    public int Credits { get; set; }
    public int Capacity { get; set; }
    public int TeacherId { get; set; }
    public string TeacherName { get; set; } = "";
    public int Enrolled { get; set; }
}

public class Enrollment
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public string StudentName { get; set; } = "";
    public string CourseCode { get; set; } = "";
    public string CourseTitle { get; set; } = "";
    public int Credits { get; set; }
    public string? Grade { get; set; }
}

// Interfaces (Dependency Inversion): upper layers depend on abstractions, not implementations.
public interface IStudentRepository
{
    List<Student> GetAll();
    Student? Get(int id);
    int Add(Student s);
    void Delete(int id);
}
public interface ICourseRepository
{
    List<Course> GetAll();
    Course? Get(int id);
    int Add(Course c);
    void Delete(int id);
    List<Teacher> GetTeachers();
}
public interface IEnrollmentRepository
{
    List<Enrollment> GetAll();
    List<Enrollment> GetByStudent(int studentId);
    bool Exists(int studentId, int courseId);
    int Add(int studentId, int courseId);
    void SetGrade(int id, string grade);
    void Delete(int id);
}
