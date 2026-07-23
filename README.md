# SE AI Audit Project Template

## 1. Project Information

| Item | Description |
|---|---|
| Course | PRN232 |
| Class | SE18D06 |
| Semester | SU26 |
| Group | Group 5 |
| Topic | TripMate - Travel Booking Platform |
| Repository |  |

---

## 2. Team Members

| No | Student ID | Full Name | GitHub Username | Role | Main Responsibility |
|---:|---|---|---|---|---|
| 1 | DE190150 | Lương Minh Phú | LuongFu | Leader | Backend project setup, architecture, testing and documentation |
| 2 | DE180845 | Nguyễn Hữu Sơn | NguyenHuuSon151204  | Member | Fullstack feature development |
| 3 | DE180869 | Dương Khánh Hoà | KhanhHoa212  | Member | Fullstack feature development |
| 4 | DE190458 | Nguyễn Văn Nhật Nam | seap1geon | Member | Mobile development and web testing feature |
| 5 | DE190646 | Lê Xuân Sơn | songitgud | Member | Fullstack feature development |

---

## 3. Project Structure

```text
src/
docs/
.github/
README.md
```

---

## 4. Required AI Audit Documents

Each group must maintain the following documents:

```text
docs/AI_AUDIT_LOG.md
docs/PROMPTS.md
docs/REFLECTION.md
docs/CHANGELOG.md
```

---

## 5. Workflow

Students must follow this workflow:

```text
Issue → Branch → Commit → Pull Request → Review → Merge
```

Direct push to the `main` branch should be avoided.

---

## 6. Branch Naming Convention

```text
feature/studentid-task-name
bugfix/studentid-error-name
docs/studentid-update-audit-log
test/studentid-test-case-name
```

Example:

```text
feature/se123456-login-page
bugfix/se123456-login-validation
docs/se123456-update-ai-audit-log
```

---

## 7. Commit Message Convention

```text
[StudentID] type: short description
```

Examples:

```text
[SE123456] feat: add login page
[SE123456] fix: fix login validation
[SE123456] docs: update AI audit log
[SE123456] test: add login test cases
```

Common types:

```text
feat, fix, docs, test, refactor, style, chore
```

---

## 8. How to Run

### Prerequisites

Ensure you have the following installed before running the project:

- **.NET SDK**: `>= 9.0`
  ```bash
  dotnet --version
  ```

- **Node.js & npm**: *(For frontend toolings/Tailwind CSS)*
  ```bash
  npm --version
  ```

### 1. Clone & Setup

Clone the repository to your local machine:

```bash
git clone <repository-url>
cd tripmate_flutter
```

### 2. Environment Variables (.env)

The project relies on environment variables for API keys and database configuration (Supabase, PayOS, Cloudinary, etc.).
Copy the example environment file or create a new `.env` file at the root of the `.NET` project and update it with your actual credentials:

```bash
cp .env.example .env
```
*(Make sure to fill in all the required keys before running the server).*

### 3. Running the Web Application (Backend & Web UI)

Navigate to the Web API project directory:

```bash
cd source/web/TripMate_Webapi/TripMate_Webapi
```

Restore dependencies and run the .NET application:

```bash
dotnet restore
dotnet run
```
The web application will start. You can visit the provided local URL (e.g. `http://localhost:5000` or `https://localhost:7000`) in your browser.


---

## 9. AI Usage Rule

Students are allowed to use AI tools such as ChatGPT, Gemini, Claude, GitHub Copilot, Cursor, Antigravity, or similar tools.

However, all important AI usage must be recorded in:

```text
docs/AI_AUDIT_LOG.md
docs/PROMPTS.md
docs/CHANGELOG.md
docs/REFLECTION.md
```

Students must be able to explain, verify, and defend all submitted work.
