using CrudUsingAjax.DataBase;
using CrudUsingAjax.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrudUsingAjax.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<EmployeeController> _logger;
        public EmployeeController(AppDbContext context, ILogger<EmployeeController> logger)
        {
            _context = context;
            _logger = logger;
        }
        public IActionResult Index()
        {
            return View();
        }

        // Get employee list
        public JsonResult EmployeeList()
        {
            try
            {
                var draw = HttpContext.Request.Query["draw"].FirstOrDefault();
                var start = Request.Query["start"].FirstOrDefault();
                var length = Request.Query["length"].FirstOrDefault();
                var searchValue = Request.Query["search[Value]"].FirstOrDefault();
                var sortColumn = Request.Query["order[0][column]"].FirstOrDefault();
                var sortColumnDirection = Request.Query["order[0][dir]"].FirstOrDefault();

                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;
                int recordsTotal = 0;

                var employeeData = _context.EmployeesDB.Include(e => e.Department).AsQueryable();

                // Search
                if (!string.IsNullOrWhiteSpace(searchValue)) 
                { 
                    employeeData = employeeData.Where( m => m.First_Name.Contains(searchValue) || 
                                                            m.Last_Name.Contains(searchValue) ||
                                                            m.Email.Contains(searchValue) || 
                                                            m.Phone_Number.Contains(searchValue) ||
                                                            m.Gender.Contains(searchValue) ||
                                                            m.Address.Contains(searchValue));
                }

                // Sorting
                switch(sortColumn)
                {
                    case "0":
                        employeeData = sortColumnDirection == "asc" ? employeeData.OrderBy(e => e.First_Name) : employeeData.OrderByDescending(e => e.First_Name);
                        break;
                    case "1":
                        employeeData = sortColumnDirection == "asc" ? employeeData.OrderBy(e => e.Last_Name) : employeeData.OrderByDescending(e => e.Last_Name);
                        break;
                    case "2":
                        employeeData = sortColumnDirection == "asc" ? employeeData.OrderBy(e => e.Email) : employeeData.OrderByDescending(e => e.Email);
                        break;
                    case "3":
                        employeeData = sortColumnDirection == "asc" ? employeeData.OrderBy(e => e.Phone_Number) : employeeData.OrderByDescending(e => e.Phone_Number);
                        break;
                    case "4":
                        employeeData = sortColumnDirection == "asc" ? employeeData.OrderBy(e => e.Gender): employeeData.OrderByDescending(e => e.Gender);
                        break;
                    case "5":
                        employeeData = sortColumnDirection == "asc" ? employeeData.OrderBy(e => e.Department.DepartmentName) : employeeData.OrderByDescending(e => e.Department.DepartmentName);
                        break;
                    case "6":
                        employeeData = sortColumnDirection == "asc" ? employeeData.OrderBy(e => e.Joining_Date) : employeeData.OrderByDescending(e => e.Joining_Date);
                        break;
                    case "7":
                        employeeData = sortColumnDirection == "asc" ? employeeData.OrderBy(e => e.Address) : employeeData.OrderByDescending(e => e.Address);
                        break;
                    default:
                        employeeData = sortColumnDirection == "asc" ? employeeData.OrderBy(e => e.First_Name) : employeeData.OrderByDescending(e => e.First_Name);
                        break;
                }

                recordsTotal = employeeData.Count();
    
                var data = employeeData.Skip(skip).Take(pageSize).Select(e => new
                {
                    employee_Id = e.Employee_Id,
                    first_Name = e.First_Name,
                    last_Name = e.Last_Name,
                    email = e.Email,
                    phone_Number = e.Phone_Number,
                    gender = e.Gender,
                    departmentName = e.Department.DepartmentName.ToString(), 
                    joining_Date = e.Joining_Date,
                    address = e.Address,
                });

                return Json(new { draw = draw, recordFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching employee list");
                return Json(new { success = false, data = ex.Message });
            }
        }

        // Get employee details
        [HttpGet]
        public JsonResult GetEmpData(int id)
        {
            _logger.LogInformation($"Fetching data for employee with ID: {id}");
            try
            {
                var employee = _context.EmployeesDB.Include(e => e.Department).FirstOrDefault(e => e.Employee_Id == id);
                if (employee != null)
                {
                    var res = new
                    {
                        success = true,
                        data = new
                        {
                            employee_Id = employee.Employee_Id,
                            first_Name = employee.First_Name,
                            last_Name = employee.Last_Name,
                            email = employee.Email,
                            phone_Number = employee.Phone_Number,
                            gender = employee.Gender,
                            department_Id = employee.Department_Id,
                            joining_Date = employee.Joining_Date,   
                            address = employee.Address,
                        }
                    };
                    return Json(res);
                }
                return Json(new { success = false, error = "Employee not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching employee data");
                return Json(new { success = false, data = ex.Message });
            }
        }

        // Add Emp Data
        [HttpPost]
        public JsonResult AddNewEmpData([FromForm] Employee employeeData, [FromForm] IFormFile image)
        {
            try
            {

                var existingEmployee = _context.EmployeesDB.FirstOrDefault(e => e.Email == employeeData.Email);
                if (existingEmployee != null)
                {
                    return Json(new
                    {
                        success = false,
                        error = "Email already exists in the database."
                    });
                }

                // Handle image upload
                if (image != null)
                {
                    var Date = DateTime.Now.ToString("yyyyMMddHHmmss");
                    var uniqueFileName = $"{Date}_{image.FileName}";
                    var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/image", uniqueFileName);
                    using (var stream = new FileStream(imagePath, FileMode.Create))
                    {
                        image.CopyTo(stream);
                    }
                    employeeData.Profile_Image = "/image/" + uniqueFileName;
                }

                // Validate Department_Id
                var department = _context.DepartmentDB.Find(employeeData.Department_Id);
                if (department == null)
                {
                    _logger.LogError("Invalid Department_Id: " + employeeData.Department_Id);
                    return Json(new
                    {
                        success = false,
                        error = "Cancel form submission"
                    });
                }

                // Save employee data
                _context.EmployeesDB.Add(employeeData);
                _context.SaveChanges();

                return Json(new { success = true, data = employeeData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return Json(
                    new
                    {
                        success = false,
                        error = ex.Message
                    });
            }
        }

        // Update Employee Data
        public JsonResult UpdateEmpData([FromForm] Employee employee, [FromForm] IFormFile image)
        {
            try
            {
                var existedEmployeeData = _context.EmployeesDB.AsNoTracking().FirstOrDefault(e => e.Employee_Id == employee.Employee_Id);

                if (existedEmployeeData == null)
                {
                    return Json(new
                    {
                        success = false,
                        error = "Employee not found"
                    });
                }

                if (!string.IsNullOrWhiteSpace(employee.Email) && employee.Email != existedEmployeeData.Email)
                {
                    var existingEmployeeWithEmail = _context.EmployeesDB.FirstOrDefault(e => e.Email == employee.Email);
                    if (existingEmployeeWithEmail != null)
                    {
                        return Json(new
                        {
                            success = false,
                            error = "Email already exists in the database."
                        });
                    }
                }

                // Update only the fields that have new values provided by the user

                if (string.IsNullOrWhiteSpace(employee.First_Name))
                {
                    employee.First_Name = existedEmployeeData.First_Name;
                }
                if (string.IsNullOrWhiteSpace(employee.Last_Name))
                {
                    employee.Last_Name = existedEmployeeData.Last_Name;
                }
                if (string.IsNullOrWhiteSpace(employee.Email))
                {
                    employee.Email = existedEmployeeData.Email;
                }
                if (string.IsNullOrWhiteSpace(employee.Phone_Number))
                {
                    employee.Phone_Number = existedEmployeeData.Phone_Number;
                }
                if (string.IsNullOrWhiteSpace(employee.Gender))
                {
                    employee.Gender = existedEmployeeData.Gender;
                }
                if (employee.Department_Id == 0)
                {
                    employee.Department_Id = existedEmployeeData.Department_Id;
                }
                if (employee.Joining_Date == null)
                {
                    employee.Joining_Date = existedEmployeeData.Joining_Date;
                }
                if (string.IsNullOrWhiteSpace(employee.Address))
                {
                    employee.Address = existedEmployeeData.Address;
                }

                // Update employee data
                _context.EmployeesDB.Update(employee);
                _context.SaveChanges();

                return Json(new
                {
                    success = true,
                    data = employee,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return Json(
                    new
                    {
                        success = false,
                        error = ex.Message
                    });
            }
        }
        
        // Delete Emp Data
        [HttpPost]
        public JsonResult DeleteEmpData(int id)
        {
            var employee = _context.EmployeesDB.Find(id);

            if (employee != null)
            {
                _context.EmployeesDB.Remove(employee);
                _context.SaveChanges();
                return Json(
                    new
                    {
                        success = true
                    });
            }
            return Json(
                new
                {
                    success = false
                });
        }
    }
}
