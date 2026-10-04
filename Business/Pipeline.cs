using UniversityMS.Domain;

namespace UniversityMS.Business;

// ===== PIPE-AND-FILTER STYLE =====
// Each filter is an independent component: it receives data, transforms/validates it,
// and passes it through the "pipe" to the next filter. Filters know nothing about each other.
public interface IFilter<T>
{
    T Process(T input);
}

public class Pipeline<T>
{
    private readonly List<IFilter<T>> _filters = new();
    public Pipeline<T> Add(IFilter<T> f) { _filters.Add(f); return this; }
    public T Run(T input)
    {
        foreach (var f in _filters) input = f.Process(input);   // output of one = input of next
        return input;
    }
}

// ---------- Pipeline 1: Enrollment validation ----------
public class EnrollmentContext
{
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public Student? Student { get; set; }
    public Course? Course { get; set; }
    public List<string> Errors { get; } = new();
    public bool IsValid => Errors.Count == 0;
}

public class StudentExistsFilter : IFilter<EnrollmentContext>
{
    private readonly IStudentRepository _students;
    public StudentExistsFilter(IStudentRepository s) => _students = s;
    public EnrollmentContext Process(EnrollmentContext c)
    {
        c.Student = _students.Get(c.StudentId);
        if (c.Student == null) c.Errors.Add("Student not found.");
        return c;
    }
}

public class CourseExistsFilter : IFilter<EnrollmentContext>
{
    private readonly ICourseRepository _courses;
    public CourseExistsFilter(ICourseRepository r) => _courses = r;
    public EnrollmentContext Process(EnrollmentContext c)
    {
        if (!c.IsValid) return c;                       // skip when an earlier filter failed
        c.Course = _courses.Get(c.CourseId);
        if (c.Course == null) c.Errors.Add("Course not found.");
        return c;
    }
}

public class DuplicateFilter : IFilter<EnrollmentContext>
{
    private readonly IEnrollmentRepository _enr;
    public DuplicateFilter(IEnrollmentRepository e) => _enr = e;
    public EnrollmentContext Process(EnrollmentContext c)
    {
        if (c.IsValid && _enr.Exists(c.StudentId, c.CourseId))
            c.Errors.Add("Student is already enrolled in this course.");
        return c;
    }
}

public class CapacityFilter : IFilter<EnrollmentContext>
{
    public EnrollmentContext Process(EnrollmentContext c)
    {
        if (c.IsValid && c.Course!.Enrolled >= c.Course.Capacity)
            c.Errors.Add($"Course {c.Course.Code} is full ({c.Course.Capacity}/{c.Course.Capacity}).");
        return c;
    }
}

public class CreditLimitFilter : IFilter<EnrollmentContext>
{
    public const int MaxCredits = 18;
    private readonly IEnrollmentRepository _enr;
    public CreditLimitFilter(IEnrollmentRepository e) => _enr = e;
    public EnrollmentContext Process(EnrollmentContext c)
    {
        if (!c.IsValid) return c;
        int current = _enr.GetByStudent(c.StudentId).Sum(x => x.Credits);
        if (current + c.Course!.Credits > MaxCredits)
            c.Errors.Add($"Credit limit exceeded ({current}+{c.Course.Credits} > {MaxCredits}).");
        return c;
    }
}

// ---------- Pipeline 2: Transcript / GPA report ----------
public class TranscriptRow
{
    public string CourseCode { get; set; } = "";
    public string CourseTitle { get; set; } = "";
    public int Credits { get; set; }
    public string Grade { get; set; } = "";
    public double Points { get; set; }
}

public class TranscriptContext
{
    public int StudentId { get; set; }
    public List<Enrollment> Enrollments { get; set; } = new();
    public List<TranscriptRow> Rows { get; set; } = new();
    public int TotalCredits { get; set; }
    public double Gpa { get; set; }
}

public class LoadEnrollmentsFilter : IFilter<TranscriptContext>
{
    private readonly IEnrollmentRepository _enr;
    public LoadEnrollmentsFilter(IEnrollmentRepository e) => _enr = e;
    public TranscriptContext Process(TranscriptContext c) { c.Enrollments = _enr.GetByStudent(c.StudentId); return c; }
}

public class GradedOnlyFilter : IFilter<TranscriptContext>
{
    public TranscriptContext Process(TranscriptContext c)
    {
        c.Enrollments = c.Enrollments.Where(e => !string.IsNullOrEmpty(e.Grade)).ToList();
        return c;
    }
}

public class GradePointFilter : IFilter<TranscriptContext>
{
    private static readonly Dictionary<string, double> Scale =
        new() { ["A"] = 4.0, ["B"] = 3.0, ["C"] = 2.0, ["D"] = 1.0, ["F"] = 0.0 };
    public TranscriptContext Process(TranscriptContext c)
    {
        c.Rows = c.Enrollments.Select(e => new TranscriptRow
        {
            CourseCode = e.CourseCode, CourseTitle = e.CourseTitle, Credits = e.Credits,
            Grade = e.Grade!, Points = Scale.GetValueOrDefault(e.Grade!.ToUpper(), 0)
        }).ToList();
        return c;
    }
}

public class GpaFilter : IFilter<TranscriptContext>
{
    public TranscriptContext Process(TranscriptContext c)
    {
        c.TotalCredits = c.Rows.Sum(r => r.Credits);
        c.Gpa = c.TotalCredits == 0 ? 0 : Math.Round(c.Rows.Sum(r => r.Points * r.Credits) / c.TotalCredits, 2);
        return c;
    }
}
