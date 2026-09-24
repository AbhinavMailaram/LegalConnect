# LegalConnect — Online Lawyer–Client Connection and Appointment Management System

**LegalConnect** is a full-stack, academically-scoped web application built with **ASP.NET Core MVC (.NET 8 LTS)**, **C#**, **Entity Framework Core (Code-First)**, **SQL Server (LocalDB)**, and **ASP.NET Core Identity** with role-based authorization.

The system connects individuals seeking legal assistance (**Clients**) with verified legal professionals (**Lawyers**), while an **Administrator** oversees lawyer verification, user management, and legal specializations.

---

## 🚀 Key Highlights & Architecture

- **Clean Layered MVC Architecture:** Separation of Concerns via Controllers, ViewModels, Domain Entities, DbContext, and Service Layer with Dependency Injection (`ILawyerService`, `IAppointmentService`, `INotificationService`, `IFileService`).
- **Role-Based Authorization:** Strict `[Authorize(Roles = "...")]` enforcement for `Admin`, `Lawyer`, and `Client`. Unauthorized URL cross-role access is blocked and redirected to Access Denied.
- **SQL Server Relational Integrity:** Configured with Entity Framework Core Fluent API using `Restrict` delete behaviors to prevent SQL Server multiple cascade path conflicts on circular and multi-parent foreign keys (`Appointments`, `Messages`, `Documents`).
- **Lawyer Verification Lifecycle:** Lawyers register in `Pending` status and cannot appear in search results until manually reviewed and approved by an Admin.
- **Conflict-Free Appointment Booking:** Validates client double-booking and checks date/time requests strictly against the lawyer's declared weekly availability schedule.
- **Case Discussions & Document Management:** Scoped direct messaging between client and lawyer, with secure file uploads (PDF, JPG, PNG, DOCX up to 5MB).
- **Interactive Swagger Documentation:** Web API controllers exposed under `/swagger` for automated API testing.

---

## 🛠 Tech Stack

| Layer | Technology |
|---|---|
| **Frontend** | Razor Views (ASP.NET Core MVC), Bootstrap 5, Bootstrap Icons, Minimal Vanilla JS |
| **Backend** | ASP.NET Core MVC (.NET 8 LTS) |
| **Language** | C# 12 |
| **Database** | SQL Server / LocalDB (`(localdb)\mssqllocaldb`) |
| **ORM** | Entity Framework Core 8 (Code-First with Migrations) |
| **Authentication** | ASP.NET Core Identity (Secure password hashing via PBKDF2) |
| **Authorization** | Role-Based Access Control (`Admin`, `Lawyer`, `Client`) |
| **API Testing** | Swagger / Swashbuckle OpenAPI UI |

---

## 🔑 Demo Seed Accounts (Ready for Evaluation)

The database automatically seeds pre-configured test accounts and sample appointments on startup:

| Role | Email | Password | Details |
|---|---|---|---|
| **Admin** | `admin@legalconnect.com` | `Admin@123` | Full administrative control, lawyer verification, user administration, specializations CRUD |
| **Lawyer (Approved)** | `lawyer@legalconnect.com` | `Lawyer@123` | **Adv. Rahul Sharma** (Criminal Law, 8 yrs experience, Mumbai, active weekly availability schedule) |
| **Client** | `client@legalconnect.com` | `Client@123` | **Priya Patel** (Client profile, sample completed & upcoming appointments) |

*Note: On the login page (`/Account/Login`), one-click credential buttons are also provided to fill in credentials immediately.*

---

## 📋 Pre-Seeded Legal Practice Categories (10 Specializations)

1. **Criminal Law**
2. **Civil Law**
3. **Family Law**
4. **Property Law**
5. **Corporate Law**
6. **Cyber Law**
7. **Consumer Law**
8. **Employment Law**
9. **Tax Law**
10. **Intellectual Property Law**

---

## 💻 How to Run the Application

### 1. Prerequisites
- **.NET 8.0 SDK** or later
- **SQL Server LocalDB** (included with Visual Studio or installed standalone)

### 2. Database Update & Startup

From the project root (`LegalConnect/LegalConnect.Web`):

```powershell
# Restore dependencies
dotnet restore

# Apply Entity Framework Core database migrations (LocalDB)
dotnet ef database update

# Run the web application
dotnet run
```

### 3. Accessing the Platform

Open your web browser and navigate to:
- **Web App Home:** `http://localhost:5248` (or the port displayed in your terminal, e.g., `https://localhost:7001`)
- **Swagger API Docs:** `http://localhost:5248/swagger`

---

## 🧪 Testing the Complete Workflow

### Step 1: Client Search & Appointment Booking
1. Log in as **Client** (`client@legalconnect.com` / `Client@123`).
2. Go to **Find Lawyers**, filter by "Criminal Law" or "Mumbai".
3. Click **Profile** on Adv. Rahul Sharma to inspect the public profile, qualifications, and weekly availability slots.
4. Click **Request Appointment**, select a valid available date (e.g., upcoming Monday at `10:00`), enter a case problem description, and submit.
5. Notice that attempting to request the same slot again will be cleanly rejected with a collision error.

### Step 2: Lawyer Acceptance & Discussion
1. Log in as **Lawyer** (`lawyer@legalconnect.com` / `Lawyer@123`).
2. The **Lawyer Dashboard** will show the incoming consultation request under **Pending Consultation Requests**.
3. Click **Accept**. The appointment moves to **Upcoming / Accepted**.
4. Click **View Case** or **Messages** to exchange inquiries and view uploaded documents.
5. After the consultation date, click **Mark Completed** to transition the appointment to Completed.

### Step 3: Lawyer Registration & Admin Verification
1. Register a new lawyer from `/Account/Register?role=Lawyer`.
2. Notice the account shows a **Verification Pending** status banner.
3. Open a separate browser or incognito window, navigate to **Find Lawyers**: the pending lawyer does **not** appear in search results.
4. Log in as **Admin** (`admin@legalconnect.com` / `Admin@123`).
5. Open **Verify Lawyers**, review the lawyer's credentials, and click **Approve**.
6. The lawyer immediately appears in public search directories and can receive bookings.

### Step 4: Admin Controls & Specializations CRUD
1. As **Admin**, visit **Specializations** to create, edit, or delete practice categories.
2. Visit **User Management** to activate/deactivate accounts or delete records.
3. Visit **Reports** to inspect appointment status distribution, lawyer directory shares, and system metrics.

---

## 📁 Solution Structure

```
LegalConnect/
├── LegalConnect.sln
├── README.md
├── test_flows.ps1                   # Automated 3x3 authorization & login test
├── test_appointments.ps1            # Automated appointment conflict & accept test
├── test_lawyer_verification.ps1     # Automated lawyer verification test
└── LegalConnect.Web/
    ├── Controllers/
    │   ├── AccountController.cs
    │   ├── HomeController.cs
    │   ├── LawyerController.cs
    │   ├── ClientController.cs
    │   ├── AppointmentController.cs
    │   ├── AdminController.cs
    │   ├── MessageController.cs
    │   ├── DocumentController.cs
    │   ├── SpecializationController.cs
    │   └── Api/
    │       └── LawyersApiController.cs
    ├── Data/
    │   ├── LegalConnectDbContext.cs
    │   └── SeedData.cs
    ├── Models/
    │   ├── Entities/
    │   │   ├── ApplicationUser.cs
    │   │   ├── ClientProfile.cs
    │   │   ├── LawyerProfile.cs
    │   │   ├── Specialization.cs
    │   │   ├── Availability.cs
    │   │   ├── Appointment.cs
    │   │   ├── Message.cs
    │   │   ├── Document.cs
    │   │   ├── Notification.cs
    │   │   └── Enums.cs
    │   └── ViewModels/
    │       ├── AccountViewModels.cs
    │       ├── LawyerViewModels.cs
    │       ├── ClientViewModels.cs
    │       ├── AppointmentViewModels.cs
    │       ├── AdminViewModels.cs
    │       └── CommunicationViewModels.cs
    ├── Services/
    │   ├── ILawyerService.cs / LawyerService.cs
    │   ├── IAppointmentService.cs / AppointmentService.cs
    │   ├── INotificationService.cs / NotificationService.cs
    │   └── IFileService.cs / FileService.cs
    ├── Views/
    │   ├── Shared/ (_Layout.cshtml with role-adaptive navigation)
    │   ├── Home/ (Index, About, Contact, Notifications)
    │   ├── Account/ (Login, Register, ForgotPassword, ResetPassword, AccessDenied)
    │   ├── Lawyer/ (Dashboard, Details, EditProfile, Availability, Appointments)
    │   ├── Client/ (Dashboard, FindLawyers, EditProfile, MyAppointments)
    │   ├── Appointment/ (Book, Details)
    │   ├── Admin/ (Dashboard, Users, Lawyers, VerifyLawyers, Appointments, Reports)
    │   ├── Specialization/ (Index, Create, Edit)
    │   ├── Message/ (Index, Thread)
    │   └── Document/ (Index)
    ├── Migrations/
    ├── wwwroot/
    │   ├── css/
    │   ├── images/
    │   └── uploads/
    ├── appsettings.json
    └── Program.cs
```

---

## 🛡 Academic Compliance & Scope Boundary
This project strictly follows the requested academic criteria:
- **No external third-party dependencies** beyond standard ASP.NET Core libraries and Bootstrap.
- **Passwords securely hashed** with PBKDF2 via ASP.NET Core Identity (no plain text).
- **No out-of-scope bloat** (payment gateways, video conferencing APIs, or AI legal advice bots avoided per project scope guidelines).
