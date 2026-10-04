using UniversityMS.Business;
using UniversityMS.Data;
using UniversityMS.Domain;

var builder = WebApplication.CreateBuilder(args);

// ===== LAYERED STYLE: composition root wires Presentation -> Business -> Data -> Database =====
var db = new Db("university.db");
db.Init();
builder.Services.AddSingleton(db);
builder.Services.AddSingleton<IStudentRepository, StudentRepository>();
builder.Services.AddSingleton<ICourseRepository, CourseRepository>();
builder.Services.AddSingleton<IEnrollmentRepository, EnrollmentRepository>();
builder.Services.AddSingleton<StudentService>();
builder.Services.AddSingleton<CourseService>();
builder.Services.AddSingleton<EnrollmentService>();
builder.Services.AddSingleton<ReportService>();

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();   // serves the frontend in wwwroot/

// ===== PRESENTATION LAYER (REST API): no SQL or business rules here =====
app.MapGet("/api/students", (StudentService s) => s.GetAll().Select(x => new { x.Id, x.Name, x.Email, x.Program, x.Role }));
app.MapPost("/api/students", (StudentService s, StudentDto d) =>
{
    var r = s.Add(d.Name, d.Email, d.Program);
    return r.ok ? Results.Ok(new { message = r.msg }) : Results.BadRequest(new { message = r.msg });
});
app.MapDelete("/api/students/{id:int}", (StudentService s, int id) => { s.Delete(id); return Results.Ok(); });

app.MapGet("/api/teachers", (CourseService s) => s.GetTeachers().Select(x => new { x.Id, x.Name, x.Department }));
app.MapGet("/api/courses", (CourseService s) => s.GetAll());
app.MapPost("/api/courses", (CourseService s, Course c) =>
{
    var r = s.Add(c);
    return r.ok ? Results.Ok(new { message = r.msg }) : Results.BadRequest(new { message = r.msg });
});
app.MapDelete("/api/courses/{id:int}", (CourseService s, int id) => { s.Delete(id); return Results.Ok(); });

app.MapGet("/api/enrollments", (EnrollmentService s) => s.GetAll());
app.MapPost("/api/enrollments", (EnrollmentService s, EnrollDto d) =>
{
    var r = s.Enroll(d.StudentId, d.CourseId);
    return r.ok ? Results.Ok(new { message = "Enrolled successfully." })
                : Results.BadRequest(new { message = string.Join(" ", r.errors) });
});
app.MapPut("/api/enrollments/{id:int}/grade", (EnrollmentService s, int id, GradeDto d) =>
{
    var r = s.SetGrade(id, d.Grade);
    return r.ok ? Results.Ok(new { message = r.msg }) : Results.BadRequest(new { message = r.msg });
});
app.MapDelete("/api/enrollments/{id:int}", (EnrollmentService s, int id) => { s.Drop(id); return Results.Ok(); });

app.MapGet("/api/students/{id:int}/transcript", (ReportService r, int id) =>
{
    var t = r.Transcript(id);
    return Results.Ok(new { t.Rows, t.TotalCredits, t.Gpa });
});

app.Run();

record StudentDto(string Name, string Email, string Program);
record EnrollDto(int StudentId, int CourseId);
record GradeDto(string Grade);
