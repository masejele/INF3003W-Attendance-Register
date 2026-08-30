Masechaba Jele
JLXMAS002

# UCT Attendance Register

## 1. Project Overview

UCT Attendance Register is a web-based attendance management system developed for INF3003W. 
The system allows students to mark and view their attendance, while lecturers can create,
manage and monitor attendance sessions.

The system uses role-based access so that students and lecturers are provided with
different dashboards and functionality.

## 2. Features

### Student Features
- Student login and authentication
- Test Email: student@inf3003.local
- Password: Student123!
- Student dashboard
- View student profile
- View attendance records
- Mark attendance for an active attendance session
- Confirmation message after successfully marking attendance
- Navigation between student pages

### Lecturer Features
- Lecturer login and authentication
- Test Email: lecturer@inf3003.local
- Password: Lecturer123!
- Lecturer dashboard
- Create attendance sessions
- View attendance sessions
- Edit attendance sessions
- Delete attendance sessions
- View student attendance
- View attendance reports
- Navigation between lecturer pages

## 3. Technologies Used

- C#
- ASP.NET Core
- Razor Pages
- Entity Framework Core
- SQLite
- ASP.NET Core Identity
- HTML/CSS
- Visual Studio Code

## 4. System Structure

The application is organised using Razor Pages.

### Pages
- `Pages/Student` – student-facing functionality
- `Pages/Lecturer` – lecturer-facing functionality
- `Pages/Shared` – shared layouts and components
- `Areas/Identity` – authentication and login functionality

### Models
- `ApplicationUser`
- `AttendanceRecord`
- `AttendanceSession`
- `Course`

### Data
- `ApplicationDbContext`
- `SeedData`

## 5. Authentication and Authorisation

ASP.NET Core Identity is used to manage user authentication.

Users log into the system using their credentials. Based on their role, they are directed
to the appropriate dashboard.

Students are provided with access to student functionality, while lecturers are provided
with access to lecturer functionality.

## 6. Student Workflow

1. Student logs into the system.
2. Student is directed to the Student Dashboard.
3. Student can:
   - Mark attendance
   - View attendance
   - View their profile
4. The student can return to the dashboard from the individual pages.

## 7. Lecturer Workflow

1. Lecturer logs into the system.
2. Lecturer is directed to the Lecturer Dashboard.
3. Lecturer can access attendance management, attendance viewing and reports.
4. The lecturer can create an attendance session.
5. Existing sessions can be edited or deleted.

## 8. Database

The system uses SQLite as its database through Entity Framework Core.

The database stores information relating to:
- Users
- Courses
- Attendance sessions
- Attendance records

Entity Framework Core is used to communicate between the application and database.

## 9. Running the Application

### Requirements

- .NET SDK
- Visual Studio Code or Visual Studio
- SQLite

### Steps

1. Clone the repository.
2. Open the project folder.
3. Open the terminal in the project directory.
4. Restore the required packages:

```bash
dotnet restore
