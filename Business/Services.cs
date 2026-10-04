using UniversityMS.Domain;

namespace UniversityMS.Business;

// ===== LAYERED STYLE: BUSINESS LOGIC LAYER =====
// Depends only on repository interfaces (Domain), never on SQL or HTTP.
public class StudentService
{
    private readonly IStudentRepository _repo;
    public StudentService(IStudentRepository repo) => _repo = repo;
    public List<Student> GetAll() => _repo.GetAll();
    public (bool ok, string msg, int id) Add(string name, string email, string program)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(program))
            return (false, "Name and program are required.", 0);
        if (!email.Contains('@')) return (false, "Invalid email.", 0);
        try { return (true, "Student added.", _repo.Add(new Student { Name = name.Trim(), Email = email.Trim(), Program = program.Trim() })); }
        catch (Exception) { return (false, "Email already exists.", 0); }
    }
    public void Delete(int id) => _repo.Delete(id);
}

public class CourseService
{
    private readonly ICourseRepository _repo;
    public CourseService(ICourseRepository repo) => _repo = repo;
    public List<Course> GetAll() => _repo.GetAll();
    public List<Teacher> GetTeachers() => _repo.GetTeachers();
    public (bool ok, string msg, int id) Add(Course c)
    {
        if (string.IsNullOrWhiteSpace(c.Code) || string.IsNullOrWhiteSpace(c.Title))
            return (false, "Code and title are required.", 0);
        if (c.Credits is < 1 or > 6) return (false, "Credits must be between 1 and 6.", 0);
        if (c.Capacity < 1) return (false, "Capacity must be at least 1.", 0);
        try { return (true, "Course added.", _repo.Add(c)); }
        catch (Exception) { return (false, "Course code exists or teacher invalid.", 0); }
    }
    public void Delete(int id) => _repo.Delete(id);
}

public class EnrollmentService
{
    private readonly IEnrollmentRepository _enr;
    private readonly Pipeline<EnrollmentContext> _validation;
    private static readonly string[] Grades = { "A", "B", "C", "D", "F" };

    public EnrollmentService(IEnrollmentRepository enr, IStudentRepository students, ICourseRepository courses)
    {
        _enr = enr;
        // Compose the pipe-and-filter pipeline: order matters.
        _validation = new Pipeline<EnrollmentContext>()
            .Add(new StudentExistsFilter(students))
            .Add(new CourseExistsFilter(courses))
            .Add(new DuplicateFilter(enr))
            .Add(new CapacityFilter())
            .Add(new CreditLimitFilter(enr));
    }

    public List<Enrollment> GetAll() => _enr.GetAll();

    public (bool ok, List<string> errors) Enroll(int studentId, int courseId)
    {
        var result = _validation.Run(new EnrollmentContext { StudentId = studentId, CourseId = courseId });
        if (!result.IsValid) return (false, result.Errors);
        _enr.Add(studentId, courseId);
        return (true, new List<string>());
    }

    public (bool ok, string msg) SetGrade(int id, string grade)
    {
        grade = (grade ?? "").ToUpper();
        if (!Grades.Contains(grade)) return (false, "Grade must be A, B, C, D or F.");
        _enr.SetGrade(id, grade);
        return (true, "Grade saved.");
    }

    public void Drop(int id) => _enr.Delete(id);
}

public class ReportService
{
    private readonly Pipeline<TranscriptContext> _pipeline;
    public ReportService(IEnrollmentRepository enr)
    {
        _pipeline = new Pipeline<TranscriptContext>()
            .Add(new LoadEnrollmentsFilter(enr))
            .Add(new GradedOnlyFilter())
            .Add(new GradePointFilter())
            .Add(new GpaFilter());
    }
    public TranscriptContext Transcript(int studentId) => _pipeline.Run(new TranscriptContext { StudentId = studentId });
}
