using CrudUsingAjax.DataBase;
using CrudUsingAjax.Models;
using CrudUsingAjax.Repositories;
using ExcelDataReader;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using System;
using System.Data;
using System.IO;
using System.Linq;

namespace CrudUsingAjax.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly IEmployeeRepository<Employee> _employeeRepository;
        private readonly IEmployeeRepository<Department> _departmentRepository;
        private readonly ILogger<EmployeeController> _logger;

        public EmployeeController(IEmployeeRepository<Employee> employeeRepository, IEmployeeRepository<Department> departmentRepository, ILogger<EmployeeController> logger)
        {
            _employeeRepository = employeeRepository;
            _departmentRepository = departmentRepository;
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
                var start = HttpContext.Request.Query["start"].FirstOrDefault();
                var length = HttpContext.Request.Query["length"].FirstOrDefault();
                var searchValue = HttpContext.Request.Query["search[Value]"].FirstOrDefault();
                var sortColumn = HttpContext.Request.Query["order[0][column]"].FirstOrDefault();
                var sortColumnDirection = HttpContext.Request.Query["order[0][dir]"].FirstOrDefault();

                //_logger.LogInformation($"===>>> Draw: {draw}, Start: {start}, Length: {length}, Search: {searchValue}, SortColumn: {sortColumn}, SortDirection: {sortColumnDirection}");

                int skip = start != null ? int.Parse(start) : 0;
                int pageLength = length != null ? int.Parse(length) : 0;
                int recordsTotal = 0;

                //_logger.LogInformation($"===>>> Page Length : {pageLength}");
                //_logger.LogInformation($"===>>> Skip : {skip}");

                var employeeData = _employeeRepository.GetAll().AsQueryable();

                // Search
                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    employeeData = employeeData.Where(m => m.First_Name.Contains(searchValue) ||
                                                           m.Last_Name.Contains(searchValue) ||
                                                           m.Email.Contains(searchValue) ||
                                                           m.Phone_Number.Contains(searchValue) ||
                                                           m.Gender.Contains(searchValue) ||
                                                           m.Address.Contains(searchValue));
                }

                // Sorting
                switch (sortColumn)
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
                        employeeData = sortColumnDirection == "asc" ? employeeData.OrderBy(e => e.Gender) : employeeData.OrderByDescending(e => e.Gender);
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
                //_logger.LogInformation($"===>>> Filtered Count : {recordsTotal}");

                var data = employeeData.Skip(skip).Take(pageLength).Select(e => new
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
                }).ToList();

                var JsonData = (new { draw = Convert.ToInt32(draw), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

                //_logger.LogInformation($"===>>> JsonData : {JsonData}");

                return Json(JsonData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching employee list");
                return Json(new { success = false });
            }
        }

        // Get employee details
        [HttpGet]
        public JsonResult GetEmpData(int id)
        {
            //_logger.LogInformation($"Fetching data for employee with ID: {id}");
            try
            {
                var employee = _employeeRepository.GetById(id);
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

        // Add employee Data
        [HttpPost]
        public JsonResult AddNewEmpData([FromForm] Employee employeeData, [FromForm] IFormFile image)
        {
            try
            {
                var existingEmployee = _employeeRepository.GetAll().FirstOrDefault(e => e.Email == employeeData.Email);
                if (existingEmployee != null)
                {
                    return Json(new
                    {
                        success = false,
                        error = "Email already exists."
                    });
                }

                // Handle image upload
                if (image != null)
                {
                    var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(image.FileName)}";
                    var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/image", uniqueFileName);
                    using (var stream = new FileStream(imagePath, FileMode.Create))
                    {
                        image.CopyTo(stream);
                    }
                    employeeData.Profile_Image = "/image/" + uniqueFileName;
                }

                // Validate Department_Id
                var department = _departmentRepository.GetById(employeeData.Department_Id);
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
                _employeeRepository.Add(employeeData);
                _employeeRepository.SaveChanges();

                return Json(new { success = true, data = employeeData, message = "Employee successfully added" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return Json(
                    new
                    {
                        success = false,
                    });
            }
        }

        // Update employee Data
        public JsonResult UpdateEmpData([FromForm] Employee employee, [FromForm] IFormFile image)
        {
            try
            {
                var existedEmployeeData = _employeeRepository.GetAll().AsNoTracking().FirstOrDefault(e => e.Employee_Id == employee.Employee_Id);

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
                    var existingEmployeeWithEmail = _employeeRepository.GetAll().FirstOrDefault(e => e.Email == employee.Email);
                    if (existingEmployeeWithEmail != null)
                    {
                        return Json(new
                        {
                            success = false,
                            error = "Email already exists."
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
                _employeeRepository.Update(employee);
                _employeeRepository.SaveChanges();

                return Json(new
                {
                    success = true,
                    data = employee,
                    message = "Employee successfully updated"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return Json(
                    new
                    {
                        success = false,
                    });
            }
        }

        // Delete employee Data
        [HttpPost]
        public JsonResult DeleteEmpData(int id)
        {
            try
            {
                _employeeRepository.Delete(id);
                _employeeRepository.SaveChanges();
                return Json(new
                {
                    success = true,
                    message = "Employee successfully deleted"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return Json(new
                {
                    success = false,
                });
            }
        }

        // Upload employee Data
        [HttpPost]
        public JsonResult UploadEmpData(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return Json(new { success = false, error = "File is empty or not selected" });
                }

                string dataFileName = Path.GetFileName(file.FileName);
                string extension = Path.GetExtension(dataFileName).ToLower();
                var allowedExtensions = new[] { ".xls", ".xlsx", ".csv" };

                if (!allowedExtensions.Contains(extension))
                {
                    return Json(new { success = false, error = "Only Excel files (.xls, .xlsx, .csv) are allowed." });
                }

                var uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Uploaded_Files");

                if (!Directory.Exists(uploadDirectory))
                {
                    Directory.CreateDirectory(uploadDirectory);
                }

                var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                var filePath = Path.Combine(uploadDirectory, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }

                var employeeList = new List<Employee>();

                // _logger.LogInformation($"Reading data from file: {filePath}");

                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    IExcelDataReader reader = null;

                    if (extension == ".xls")
                    {
                        reader = ExcelReaderFactory.CreateBinaryReader(stream);
                    }
                    else if (extension == ".xlsx")
                    {
                        reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                    }
                    else if (extension == ".csv")
                    {
                        reader = ExcelReaderFactory.CreateCsvReader(stream);
                    }

                    if (reader != null)
                    {
                        using (reader)
                        {
                            while (reader.Read())
                            {
                                if (reader.Depth == 0) continue;

                                var departmentStr = reader.GetValue(6)?.ToString();
                                var departmentId = int.TryParse(departmentStr, out var deptId) ? deptId : 0;

                                //_logger.LogInformation($"Read Department Id: {departmentId}");

                                if (_departmentRepository.GetById(departmentId) == null)
                                {
                                    _logger.LogError($"Department ID : {departmentId} not found in database");
                                    continue;
                                }

                                var email = reader.GetValue(3)?.ToString();
                                var existingEmployee = _employeeRepository.GetAll().FirstOrDefault(e => e.Email == email);

                                if (existingEmployee != null)
                                {
                                    _logger.LogError("Some emails already exist");
                                    continue;
                                }

                                var employee = new Employee
                                {
                                    First_Name = reader.GetValue(1)?.ToString(),
                                    Last_Name = reader.GetValue(2)?.ToString(),
                                    Email = reader.GetValue(3)?.ToString(),
                                    Phone_Number = reader.GetValue(4)?.ToString(),
                                    Gender = reader.GetValue(5)?.ToString(),
                                    Department_Id = departmentId,
                                    Joining_Date = DateTime.TryParse(reader.GetValue(7)?.ToString(), out var joiningDate) ? new DateOnly?(DateOnly.FromDateTime(joiningDate)) : null,
                                    Address = reader.GetValue(8)?.ToString(),
                                };

                                // Handle image upload
                                var imageFileName = reader.GetValue(9)?.ToString();
                                if (!string.IsNullOrWhiteSpace(imageFileName))
                                {
                                    var uniqueImageName = $"{Guid.NewGuid()}_{Path.GetFileName(imageFileName)}";
                                    var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/image", uniqueImageName);

                                    using (var imageStream = new FileStream(imagePath, FileMode.Create))
                                    {
                                        file.CopyTo(imageStream);
                                    }
                                    employee.Profile_Image = "/image/" + uniqueImageName;
                                }
                                else
                                {
                                    _logger.LogError($"{imageFileName} does not exist.");
                                }
                                employeeList.Add(employee);
                            }
                        }
                    }
                }
                if (employeeList.Count > 0)
                {
                    _employeeRepository.AddRange(employeeList);
                    _employeeRepository.SaveChanges();
                    return Json(new { success = true, message = "Employee successfully uploaded" });
                }
                else
                {
                    return Json(new { success = false, error = "No valid employee to upload." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return Json(new { success = false });
            }
        }

        public IActionResult DownloadEmpExcel()
        {
            try
            {
                var employees = _employeeRepository.GetAll().Include(e => e.Department).ToList();

                var datatable = new DataTable("Employees");
                datatable.Columns.AddRange(new DataColumn[]
                {
                new DataColumn("First Name"),
                new DataColumn("Last Name"),
                new DataColumn("Email"),
                new DataColumn("Phone Number"),
                new DataColumn("Gender"),
                new DataColumn("Department"),
                new DataColumn("Joining Date"),
                new DataColumn("Address"),
                new DataColumn("Profile Image")
                });

                foreach (var employee in employees)
                {
                    datatable.Rows.Add(
                            employee.First_Name,
                            employee.Last_Name,
                            employee.Email,
                            employee.Phone_Number,
                            employee.Gender,
                            employee.Department.DepartmentName,
                            employee.Joining_Date,
                            employee.Address,
                            employee.Profile_Image
                        );
                }

                var memoryStream = new MemoryStream();
                using (var excelPackege = new ExcelPackage(memoryStream))
                {
                    var worksheet = excelPackege.Workbook.Worksheets.Add("Employees");
                    worksheet.Cells["A1"].LoadFromDataTable(datatable, true);
                    excelPackege.Save();
                }
                memoryStream.Position = 0;
                return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Employees.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return Json(new
                {
                    success = false,
                });
            }
        }
    }
}

