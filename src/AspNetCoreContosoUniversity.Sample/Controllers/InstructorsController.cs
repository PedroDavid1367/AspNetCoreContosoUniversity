using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AspNetCoreContosoUniversity.Sample.Data;
using AspNetCoreContosoUniversity.Sample.Models;
using AspNetCoreContosoUniversity.Sample.Models.SchoolViewModels;

namespace AspNetCoreContosoUniversity.Sample.Controllers
{
  public class InstructorsController : Controller
  {
    public InstructorsController(SchoolContext context)
    {
      _context = context;
    }

    // GET: Instructors
    public async Task<IActionResult> Index(int? id, int? courseID)
    {
      var viewModel = new InstructorIndexData();
      viewModel.Instructors = await _context.Instructors
        .Include(i => i.OfficeAssignment)
        .Include(i => i.Courses)
          .ThenInclude(ca => ca.Course)
        //.ThenInclude(c => c.Enrollments)
        //  .ThenInclude(e => e.Student)
        .Include(i => i.Courses)
          .ThenInclude(ca => ca.Course)
            .ThenInclude(c => c.Department)
        .AsNoTracking()
        .OrderBy(i => i.LastName)
        .ToListAsync();

      if (id != null)
      {
        ViewData["InstructorID"] = id.Value;
        Instructor instructor = viewModel.Instructors
          .Where(i => i.ID == id.Value).Single();
        viewModel.Courses = instructor.Courses.Select(ca => ca.Course);
      }

      //if (courseID != null)
      //{
      //  ViewData["CourseID"] = courseID.Value;
      //  viewModel.Enrollments = viewModel.Courses
      //    .Where(c => c.CourseID == courseID)
      //    .Single().Enrollments;
      //}

      if (courseID != null)
      {
        ViewData["CourseID"] = courseID.Value;
        _context.Enrollments
          .Include(e => e.Student)
          .Where(e => e.CourseID == courseID.Value).Load();
        viewModel.Enrollments = viewModel.Courses
          .Where(c => c.CourseID == courseID).Single().Enrollments;
      }

      return View(viewModel);
    }

    // GET: Instructors/Details/5
    public async Task<IActionResult> Details(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var instructor = await _context.Instructors.SingleOrDefaultAsync(m => m.ID == id);
      if (instructor == null)
      {
        return NotFound();
      }

      return View(instructor);
    }

    // GET: Instructors/Create
    public IActionResult Create()
    {
      return View();
    }

    // POST: Instructors/Create
    // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
    // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ID,FirstMidName,HireDate,LastName")] Instructor instructor)
    {
      if (ModelState.IsValid)
      {
        _context.Add(instructor);
        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
      }
      return View(instructor);
    }

    // GET: Instructors/Edit/5
    //public async Task<IActionResult> Edit(int? id)
    //{
    //  if (id == null)
    //  {
    //    return NotFound();
    //  }

    //  var instructor = await _context.Instructors
    //    .Include(i => i.OfficeAssignment)
    //    .AsNoTracking()
    //    .SingleOrDefaultAsync(m => m.ID == id);
    //  if (instructor == null)
    //  {
    //    return NotFound();
    //  }
    //  return View(instructor);
    //}

    public async Task<IActionResult> Edit(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var instructor = await _context.Instructors
        .Include(i => i.OfficeAssignment)
        .Include(i => i.Courses)
          .ThenInclude(ca => ca.Course)
        .AsNoTracking()
        .SingleOrDefaultAsync(m => m.ID == id);
      if (instructor == null)
      {
        return NotFound();
      }
      PopulateAssignedCourseData(instructor);
      return View(instructor);
    }

    private void PopulateAssignedCourseData(Instructor instructor)
    {
      var allCourses = _context.Courses;
      var instructorCourses = new HashSet<int>
        (instructor.Courses.Select(c => c.Course.CourseID));
      var viewModel = new List<AssignedCourseData>();
      foreach (var course in allCourses)
      {
        viewModel.Add(new AssignedCourseData
        {
          CourseID = course.CourseID,
          Title = course.Title,
          Assigned = instructorCourses.Contains(course.CourseID)
        });
      }
      ViewData["Courses"] = viewModel;
    }

    // POST: Instructors/Edit/5
    //[HttpPost, ActionName("Edit")]
    //[ValidateAntiForgeryToken]
    //public async Task<IActionResult> EditPost(int? id)
    //{
    //  if (id == null)
    //  {
    //    return NotFound();
    //  }

    //  var instructorToUpdate = await _context.Instructors
    //    .Include(i => i.OfficeAssignment)
    //    .SingleOrDefaultAsync(s => s.ID == id);

    //  if (await TryUpdateModelAsync<Instructor>(
    //    instructorToUpdate,
    //    "",
    //    i => i.FirstMidName,
    //    i => i.LastName,
    //    i => i.HireDate,
    //    i => i.OfficeAssignment))
    //  {
    //    if (string.IsNullOrWhiteSpace(instructorToUpdate.OfficeAssignment?.Location))
    //    {
    //      instructorToUpdate.OfficeAssignment = null;
    //    }
    //    try
    //    {
    //      await _context.SaveChangesAsync();
    //    }
    //    catch (DbUpdateException /* ex */)
    //    {
    //      //Log the error (uncomment ex variable name and write a log.)
    //      ModelState.AddModelError("", "Unable to save changes. " +
    //        "Try again, and if the problem persists, " +
    //        "see your system administrator.");
    //    }
    //    return RedirectToAction("Index");
    //  }
    //  return View(instructorToUpdate);
    //}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, string[] selectedCourses)
    {
      if (id == null)
      {
        return NotFound();
      }

      var instructorToUpdate = await _context.Instructors
        .Include(i => i.OfficeAssignment)
        .Include(i => i.Courses)
          .ThenInclude(i => i.Course)
        .SingleOrDefaultAsync(m => m.ID == id);

      if (await TryUpdateModelAsync<Instructor>(
          instructorToUpdate,
          "",
          i => i.FirstMidName, 
          i => i.LastName, 
          i => i.HireDate, 
          i => i.OfficeAssignment))
      {
        if (string.IsNullOrWhiteSpace(instructorToUpdate.OfficeAssignment?.Location))
        {
          instructorToUpdate.OfficeAssignment = null;
        }
        UpdateInstructorCourses(selectedCourses, instructorToUpdate);
        try
        {
          await _context.SaveChangesAsync();
        }
        catch (DbUpdateException /* ex */)
        {
          //Log the error (uncomment ex variable name and write a log.)
          ModelState.AddModelError("", "Unable to save changes. " +
            "Try again, and if the problem persists, " +
            "see your system administrator.");
        }
        return RedirectToAction("Index");
      }
      return View(instructorToUpdate);
    }

    private void UpdateInstructorCourses(string[] selectedCourses, Instructor instructorToUpdate)
    {
      if (selectedCourses == null)
      {
        instructorToUpdate.Courses = new List<CourseAssignment>();
        return;
      }

      var selectedCoursesHS = new HashSet<string>(selectedCourses);
      var instructorCourses = new HashSet<int>
          (instructorToUpdate.Courses.Select(c => c.Course.CourseID));
      foreach (var course in _context.Courses)
      {
        if (selectedCoursesHS.Contains(course.CourseID.ToString()))
        {
          if (!instructorCourses.Contains(course.CourseID))
          {
            instructorToUpdate.Courses.Add(new CourseAssignment
            {
              InstructorID = instructorToUpdate.ID,
              CourseID = course.CourseID
            });
          }
        }
        else
        {

          if (instructorCourses.Contains(course.CourseID))
          {
            CourseAssignment courseToRemove = instructorToUpdate.Courses
              .SingleOrDefault(i => i.CourseID == course.CourseID);
            _context.Remove(courseToRemove);
          }
        }
      }
    }

    // GET: Instructors/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var instructor = await _context.Instructors.SingleOrDefaultAsync(m => m.ID == id);
      if (instructor == null)
      {
        return NotFound();
      }

      return View(instructor);
    }

    // POST: Instructors/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
      var instructor = await _context.Instructors.SingleOrDefaultAsync(m => m.ID == id);
      _context.Instructors.Remove(instructor);
      await _context.SaveChangesAsync();
      return RedirectToAction("Index");
    }

    private bool InstructorExists(int id)
    {
      return _context.Instructors.Any(e => e.ID == id);
    }

    private readonly SchoolContext _context;
  }
}
