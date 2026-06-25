# Legal Office App — Issue Fixes

## Summary of Changes

### Issue 1 — Data not syncing across devices / MEGA connection
**Root cause:** The app stores all submission *metadata* in a local SQLite file which does not travel between devices.

**Solution (two-part):**

#### Part A — Firestore for metadata sync (cross-device)
The `Submission` model now has a `FirestoreId` column.  When a submission is saved, also write it to Firebase Firestore using your existing `firebase` npm dependency on the web companion, *or* use the Firebase REST API directly from C#:

```csharp
// Example: write to Firestore via REST after InsertSubmission()
// Add this in SubmissionControl.Submit_Click after the local DB insert
async Task SyncToFirestore(Submission s, int localId)
{
    var payload = new
    {
        fields = new
        {
            bookNumber   = new { stringValue = s.BookNumber },
            notaryName   = new { stringValue = s.NotaryName },
            ptrNumber    = new { stringValue = s.PtrNumber },
            ibpNumber    = new { stringValue = s.IbpNumber },
            yearCovered  = new { stringValue = s.YearCovered },
            megaLink     = new { stringValue = s.MegaLink },
            fileName     = new { stringValue = s.FileName },
            status       = new { stringValue = s.Status },
            submittedBy  = new { stringValue = s.SubmittedBy },
            dateSubmitted= new { stringValue = s.DateSubmitted.ToString("o") }
        }
    };
    string json = System.Text.Json.JsonSerializer.Serialize(payload);
    using var http = new System.Net.Http.HttpClient();
    // Replace PROJECT_ID and COLLECTION with your values
    string url = "https://firestore.googleapis.com/v1/projects/PROJECT_ID/databases/(default)/documents/submissions";
    var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
    var resp = await http.PostAsync(url, content);
    string body = await resp.Content.ReadAsStringAsync();
    // Extract the document name (Firestore ID) from the response and save it
    // DatabaseService.Instance.UpdateSubmissionFirestoreId(localId, firestoreDocId);
}
```

For a simpler approach, install the **Google.Cloud.Firestore** NuGet package and use the SDK directly.

#### Part B — MEGA connection failures
- Ensure `appsettings.json` exists in the output directory with valid MEGA credentials.
- The app already gracefully handles MEGA failures (saves record locally, shows warning).
- If MEGA is consistently unreachable, check firewall/proxy settings or test credentials at mega.nz.

---

### Issue 2 — Update and Delete for submissions and accounts

**Files changed:** `DatabaseService.cs`, `TrackingControl.cs`, `UserManagementControl.cs` (new)

#### Submissions (TrackingControl)
- Added **✎ Edit** button column — opens `EditSubmissionDialog`, allows changing Book Number, Notary Name, PTR, IBP, Date of Commission, Year Covered. Only visible to Admins.
- Added **🗑 Del** button column — after confirmation, deletes the local record (admin only). A note reminds admin to delete the MEGA file separately.
- Both buttons are hidden from Staff users.

#### User Accounts (UserManagementControl — new file)
- New **User Management** page in the sidebar (Admin only).
- Lists all users with **✎ Edit** (change name, role, active status, password) and **Deactivate / Reactivate** buttons.
- **+ New User** button at the top to create accounts.
- Deactivating your own account is blocked to prevent lockout.
- All account events are logged with `category = "account"` — hidden from staff.

---

### Issue 3 — Logo icon not used

**File changed:** `MainForm.cs`

The form now loads `logo.ico` from the executable directory:
```csharp
string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.ico");
if (File.Exists(iconPath))
    this.Icon = new Icon(iconPath);
```

**Action required:** Copy your logo icon file (ICO format) to the project root and add it to the `.csproj`:
```xml
<ItemGroup>
  <None Update="logo.ico">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

Then set the application icon in `.csproj`:
```xml
<PropertyGroup>
  <ApplicationIcon>logo.ico</ApplicationIcon>
</PropertyGroup>
```

---

### Issue 4 — Staff activity log shows account creation events

**Files changed:** `DatabaseService.cs`, `LogsControl.cs`, `SessionManager.cs`

**How it works:**
- Every log entry now has a `category` field: `"file"` (submission/approval/login events) or `"account"` (user create/edit/deactivate events).
- `LogsControl.ApplyFilter()` passes `categoryFilter = "file"` for Staff users, so `AccountCreate`, `AccountEdit`, `AccountDeactivate`, `AccountReactivate` actions are never returned.
- Admins see all categories.
- The `Action` dropdown in LogsControl also omits account action types for Staff users.
- **Existing records** in the database default to `category = 'file'` (safe — they were all file events before this change).

---

## Files to Replace

| File | Action |
|------|--------|
| `DatabaseService.cs` | Replace |
| `TrackingControl.cs` | Replace |
| `LogsControl.cs` | Replace |
| `MainForm.cs` | Replace |
| `SessionManager.cs` | Replace |
| `UserManagementControl.cs` | **New file — add to project** |

No changes needed to: `SubmissionControl.cs`, `DashboardControl.cs`, `LoginForm.cs`, `MegaService.cs`, `Program.cs`, `.csproj` (except optional icon entry).
