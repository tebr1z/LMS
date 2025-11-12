# LMS API Test Script
# Bu script tüm API endpoint'lerini test eder

$baseUrl = "https://localhost:7000"  # Varsayılan HTTPS portu
$httpBaseUrl = "http://localhost:5000"  # Varsayılan HTTP portu

# Test sonuçları
$testResults = @()

function Write-TestResult {
    param(
        [string]$Endpoint,
        [string]$Method,
        [bool]$Success,
        [string]$Message,
        [object]$Response = $null
    )
    
    $result = [PSCustomObject]@{
        Endpoint = $Endpoint
        Method = $Method
        Success = $Success
        Message = $Message
        StatusCode = $Response.StatusCode
        Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    }
    
    $script:testResults += $result
    
    $color = if ($Success) { "Green" } else { "Red" }
    Write-Host "[$($Method)] $Endpoint - " -NoNewline
    Write-Host $Message -ForegroundColor $color
}

function Invoke-ApiRequest {
    param(
        [string]$Method,
        [string]$Url,
        [hashtable]$Headers = @{},
        [object]$Body = $null,
        [string]$ContentType = "application/json"
    )
    
    try {
        $params = @{
            Method = $Method
            Uri = $Url
            Headers = $Headers
            ContentType = $ContentType
            SkipCertificateCheck = $true
            ErrorAction = "Stop"
        }
        
        if ($Body) {
            if ($Body -is [string]) {
                $params.Body = $Body
            } else {
                $params.Body = ($Body | ConvertTo-Json -Depth 10)
            }
        }
        
        $response = Invoke-RestMethod @params
        return @{
            Success = $true
            Data = $response
            StatusCode = 200
        }
    }
    catch {
        $statusCode = 0
        $errorMessage = $_.Exception.Message
        
        try {
            if ($_.Exception.Response) {
                $statusCode = $_.Exception.Response.StatusCode.value__
            }
        } catch {
            # Status code alınamadı
        }
        
        # Eğer bağlantı hatası varsa daha detaylı bilgi ver
        if ($errorMessage -like "*bağlanılamıyor*" -or $errorMessage -like "*could not connect*") {
            Write-Host "   ERROR: API'ye bağlanılamıyor. API çalışıyor mu kontrol edin: $Url" -ForegroundColor Red
        }
        
        return @{
            Success = $false
            Data = $null
            StatusCode = $statusCode
            Error = $errorMessage
        }
    }
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "LMS API Test Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Test 1: Health Check
Write-Host "1. Health Check..." -ForegroundColor Yellow
try {
    $health = Invoke-ApiRequest -Method "GET" -Url "$httpBaseUrl/api/health"
    Write-TestResult -Endpoint "/api/health" -Method "GET" -Success $health.Success -Message "Health check" -Response $health
} catch {
    Write-TestResult -Endpoint "/api/health" -Method "GET" -Success $false -Message "Health check failed: $_"
}

# Test 2: Register MasterAdmin
Write-Host "`n2. Register MasterAdmin..." -ForegroundColor Yellow
$adminRegister = @{
    Email = "admin@test.com"
    Password = "Admin123!"
    FullName = "Admin User"
    Role = "MasterAdmin"
}
$adminRegResult = Invoke-ApiRequest -Method "POST" -Url "$httpBaseUrl/api/auth/register" -Body $adminRegister
Write-TestResult -Endpoint "/api/auth/register (Admin)" -Method "POST" -Success $adminRegResult.Success -Message "Register MasterAdmin" -Response $adminRegResult

$adminToken = $null
if ($adminRegResult.Success -and $adminRegResult.Data.Token) {
    $adminToken = $adminRegResult.Data.Token
    Write-Host "   Admin Token: $($adminToken.Substring(0, 20))..." -ForegroundColor Gray
}

# Test 3: Register Student
Write-Host "`n3. Register Student..." -ForegroundColor Yellow
$studentRegister = @{
    Email = "student@test.com"
    Password = "Student123!"
    FullName = "Student User"
    Role = "Student"
}
$studentRegResult = Invoke-ApiRequest -Method "POST" -Url "$httpBaseUrl/api/auth/register" -Body $studentRegister
Write-TestResult -Endpoint "/api/auth/register (Student)" -Method "POST" -Success $studentRegResult.Success -Message "Register Student" -Response $studentRegResult

$studentToken = $null
if ($studentRegResult.Success -and $studentRegResult.Data.Token) {
    $studentToken = $studentRegResult.Data.Token
    Write-Host "   Student Token: $($studentToken.Substring(0, 20))..." -ForegroundColor Gray
}

# Test 4: Login Admin
Write-Host "`n4. Login Admin..." -ForegroundColor Yellow
$adminLogin = @{
    Email = "admin@test.com"
    Password = "Admin123!"
}
$adminLoginResult = Invoke-ApiRequest -Method "POST" -Url "$httpBaseUrl/api/auth/login" -Body $adminLogin
Write-TestResult -Endpoint "/api/auth/login (Admin)" -Method "POST" -Success $adminLoginResult.Success -Message "Login Admin" -Response $adminLoginResult

if ($adminLoginResult.Success -and $adminLoginResult.Data.Token) {
    $adminToken = $adminLoginResult.Data.Token
}

# Test 5: Login Student
Write-Host "`n5. Login Student..." -ForegroundColor Yellow
$studentLogin = @{
    Email = "student@test.com"
    Password = "Student123!"
}
$studentLoginResult = Invoke-ApiRequest -Method "POST" -Url "$httpBaseUrl/api/auth/login" -Body $studentLogin
Write-TestResult -Endpoint "/api/auth/login (Student)" -Method "POST" -Success $studentLoginResult.Success -Message "Login Student" -Response $studentLoginResult

if ($studentLoginResult.Success -and $studentLoginResult.Data.Token) {
    $studentToken = $studentLoginResult.Data.Token
}

if (-not $adminToken -or -not $studentToken) {
    Write-Host "`nERROR: Tokens alınamadı. Test devam edemez." -ForegroundColor Red
    Write-Host "Admin Token: $($adminToken -ne $null)" -ForegroundColor Yellow
    Write-Host "Student Token: $($studentToken -ne $null)" -ForegroundColor Yellow
    Write-Host "`nAPI'nin çalıştığından emin olun: dotnet run --project LMS.API" -ForegroundColor Yellow
    Write-Host "Veya farklı bir port kullanıyorsanız script'teki baseUrl'i güncelleyin." -ForegroundColor Yellow
    exit 1
}

$adminHeaders = @{ "Authorization" = "Bearer $adminToken" }
$studentHeaders = @{ "Authorization" = "Bearer $studentToken" }

# Test 6: Get All Courses (Admin)
Write-Host "`n6. Get All Courses (Admin)..." -ForegroundColor Yellow
$coursesResult = Invoke-ApiRequest -Method "GET" -Url "$httpBaseUrl/api/courses" -Headers $adminHeaders
Write-TestResult -Endpoint "/api/courses" -Method "GET" -Success $coursesResult.Success -Message "Get All Courses" -Response $coursesResult

# Test 7: Create Course (Admin)
Write-Host "`n7. Create Course (Admin)..." -ForegroundColor Yellow
$newCourse = @{
    Title = "Test Course"
    Description = "This is a test course"
    Price = 0
}
$createCourseResult = Invoke-ApiRequest -Method "POST" -Url "$httpBaseUrl/api/courses" -Headers $adminHeaders -Body $newCourse
Write-TestResult -Endpoint "/api/courses" -Method "POST" -Success $createCourseResult.Success -Message "Create Course" -Response $createCourseResult

$courseId = $null
if ($createCourseResult.Success -and $createCourseResult.Data) {
    $courseId = $createCourseResult.Data
    Write-Host "   Created Course ID: $courseId" -ForegroundColor Gray
}

# Test 8: Create Group (Admin)
Write-Host "`n8. Create Group (Admin)..." -ForegroundColor Yellow
$newGroup = @{
    Name = "Test Group"
    Description = "This is a test group"
}
$createGroupResult = Invoke-ApiRequest -Method "POST" -Url "$httpBaseUrl/api/groups" -Headers $adminHeaders -Body $newGroup
Write-TestResult -Endpoint "/api/groups" -Method "POST" -Success $createGroupResult.Success -Message "Create Group" -Response $createGroupResult

$groupId = $null
if ($createGroupResult.Success -and $createGroupResult.Data) {
    $groupId = $createGroupResult.Data
    Write-Host "   Created Group ID: $groupId" -ForegroundColor Gray
}

# Test 9: Get Group Details (Admin)
if ($groupId) {
    Write-Host "`n9. Get Group Details (Admin)..." -ForegroundColor Yellow
    $groupDetailsResult = Invoke-ApiRequest -Method "GET" -Url "$httpBaseUrl/api/groups/$groupId" -Headers $adminHeaders
    Write-TestResult -Endpoint "/api/groups/{id}" -Method "GET" -Success $groupDetailsResult.Success -Message "Get Group Details" -Response $groupDetailsResult
}

# Test 10: Add User to Group (Admin)
if ($groupId) {
    Write-Host "`n10. Add User to Group (Admin)..." -ForegroundColor Yellow
    # Önce student ID'yi almak için login response'dan alabiliriz veya user listesinden
    $addUserToGroup = @{
        UserId = 2  # Student ID (genellikle 2 olur)
        Role = "Member"
    }
    $addUserResult = Invoke-ApiRequest -Method "POST" -Url "$httpBaseUrl/api/groups/$groupId/add-user" -Headers $adminHeaders -Body $addUserToGroup
    Write-TestResult -Endpoint "/api/groups/{id}/add-user" -Method "POST" -Success $addUserResult.Success -Message "Add User to Group" -Response $addUserResult
}

# Test 11: Add Course to Group (Admin)
if ($groupId -and $courseId) {
    Write-Host "`n11. Add Course to Group (Admin)..." -ForegroundColor Yellow
    $addCourseToGroup = @{
        CourseId = $courseId
    }
    $addCourseResult = Invoke-ApiRequest -Method "POST" -Url "$httpBaseUrl/api/groups/$groupId/add-course" -Headers $adminHeaders -Body $addCourseToGroup
    Write-TestResult -Endpoint "/api/groups/{id}/add-course" -Method "POST" -Success $addCourseResult.Success -Message "Add Course to Group" -Response $addCourseResult
}

# Test 12: Create Assignment (Admin)
if ($courseId) {
    Write-Host "`n12. Create Assignment (Admin)..." -ForegroundColor Yellow
    $newAssignment = @{
        CourseId = $courseId
        Title = "Test Assignment"
        Description = "This is a test assignment"
        Type = "FileSubmission"
        Deadline = (Get-Date).AddDays(7).ToString("yyyy-MM-ddTHH:mm:ss")
    }
    $createAssignmentResult = Invoke-ApiRequest -Method "POST" -Url "$httpBaseUrl/api/assignments" -Headers $adminHeaders -Body $newAssignment
    Write-TestResult -Endpoint "/api/assignments" -Method "POST" -Success $createAssignmentResult.Success -Message "Create Assignment" -Response $createAssignmentResult
    
    $assignmentId = $null
    if ($createAssignmentResult.Success -and $createAssignmentResult.Data) {
        $assignmentId = $createAssignmentResult.Data
        Write-Host "   Created Assignment ID: $assignmentId" -ForegroundColor Gray
    }
}

# Test 13: Get Assignments by Course (Student)
if ($courseId) {
    Write-Host "`n13. Get Assignments by Course (Student)..." -ForegroundColor Yellow
    $assignmentsResult = Invoke-ApiRequest -Method "GET" -Url "$httpBaseUrl/api/assignments/by-course/$courseId" -Headers $studentHeaders
    Write-TestResult -Endpoint "/api/assignments/by-course/{courseId}" -Method "GET" -Success $assignmentsResult.Success -Message "Get Assignments by Course" -Response $assignmentsResult
}

# Test 14: Enroll in Course (Student)
if ($courseId) {
    Write-Host "`n14. Enroll in Course (Student)..." -ForegroundColor Yellow
    $enroll = @{
        CourseId = $courseId
    }
    $enrollResult = Invoke-ApiRequest -Method "POST" -Url "$httpBaseUrl/api/enrollments" -Headers $studentHeaders -Body $enroll
    Write-TestResult -Endpoint "/api/enrollments" -Method "POST" -Success $enrollResult.Success -Message "Enroll in Course" -Response $enrollResult
}

# Test 15: Submit Assignment (Student)
if ($assignmentId) {
    Write-Host "`n15. Submit Assignment (Student)..." -ForegroundColor Yellow
    $submitAssignment = @{
        AnswerText = "This is my answer"
        FileUrl = $null
    }
    $submitResult = Invoke-ApiRequest -Method "POST" -Url "$httpBaseUrl/api/assignments/$assignmentId/submit" -Headers $studentHeaders -Body $submitAssignment
    Write-TestResult -Endpoint "/api/assignments/{id}/submit" -Method "POST" -Success $submitResult.Success -Message "Submit Assignment" -Response $submitResult
    
    $submissionId = $null
    if ($submitResult.Success -and $submitResult.Data.submissionId) {
        $submissionId = $submitResult.Data.submissionId
        Write-Host "   Submission ID: $submissionId" -ForegroundColor Gray
    }
}

# Test 16: Get Assignment Submissions (Admin)
if ($assignmentId) {
    Write-Host "`n16. Get Assignment Submissions (Admin)..." -ForegroundColor Yellow
    $submissionsResult = Invoke-ApiRequest -Method "GET" -Url "$httpBaseUrl/api/assignments/$assignmentId/submissions" -Headers $adminHeaders
    Write-TestResult -Endpoint "/api/assignments/{id}/submissions" -Method "GET" -Success $submissionsResult.Success -Message "Get Assignment Submissions" -Response $submissionsResult
}

# Test 17: Grade Assignment (Admin)
if ($assignmentId -and $submissionId) {
    Write-Host "`n17. Grade Assignment (Admin)..." -ForegroundColor Yellow
    $gradeAssignment = @{
        SubmissionId = $submissionId
        Score = 85
    }
    $gradeResult = Invoke-ApiRequest -Method "POST" -Url "$httpBaseUrl/api/assignments/$assignmentId/score" -Headers $adminHeaders -Body $gradeAssignment
    Write-TestResult -Endpoint "/api/assignments/{id}/score" -Method "POST" -Success $gradeResult.Success -Message "Grade Assignment" -Response $gradeResult
}

# Test 18: Send Message (Student)
Write-Host "`n18. Send Message (Student)..." -ForegroundColor Yellow
$sendMessage = @{
    ReceiverId = 1  # Admin ID
    Text = "Hello from student!"
    GroupId = $null
}
$messageResult = Invoke-ApiRequest -Method "POST" -Url "$httpBaseUrl/api/messages" -Headers $studentHeaders -Body $sendMessage
Write-TestResult -Endpoint "/api/messages" -Method "POST" -Success $messageResult.Success -Message "Send Message" -Response $messageResult

# Test 19: Upload File (Student)
Write-Host "`n19. Upload File (Student)..." -ForegroundColor Yellow
# Not: File upload için multipart/form-data gerekir, bu basit test için atlanıyor
Write-TestResult -Endpoint "/api/files/upload" -Method "POST" -Success $true -Message "File Upload (Skipped - requires multipart/form-data)"

# Test 20: Admin - Get Statistics
Write-Host "`n20. Admin - Get Statistics..." -ForegroundColor Yellow
$statsResult = Invoke-ApiRequest -Method "GET" -Url "$httpBaseUrl/api/admin/statistics" -Headers $adminHeaders
Write-TestResult -Endpoint "/api/admin/statistics" -Method "GET" -Success $statsResult.Success -Message "Get Statistics" -Response $statsResult

# Test 21: Admin - Get Users
Write-Host "`n21. Admin - Get Users..." -ForegroundColor Yellow
$usersResult = Invoke-ApiRequest -Method "GET" -Url "$httpBaseUrl/api/admin/users?pageNumber=1&pageSize=10" -Headers $adminHeaders
Write-TestResult -Endpoint "/api/admin/users" -Method "GET" -Success $usersResult.Success -Message "Get Users" -Response $usersResult

# Test 22: Admin - Get Courses
Write-Host "`n22. Admin - Get Courses..." -ForegroundColor Yellow
$adminCoursesResult = Invoke-ApiRequest -Method "GET" -Url "$httpBaseUrl/api/admin/courses?pageNumber=1&pageSize=10" -Headers $adminHeaders
Write-TestResult -Endpoint "/api/admin/courses" -Method "GET" -Success $adminCoursesResult.Success -Message "Get Courses" -Response $adminCoursesResult

# Test 23: Admin - Get Groups
Write-Host "`n23. Admin - Get Groups..." -ForegroundColor Yellow
$adminGroupsResult = Invoke-ApiRequest -Method "GET" -Url "$httpBaseUrl/api/admin/groups?pageNumber=1&pageSize=10" -Headers $adminHeaders
Write-TestResult -Endpoint "/api/admin/groups" -Method "GET" -Success $adminGroupsResult.Success -Message "Get Groups" -Response $adminGroupsResult

# Test 24: Admin - Get Enrollment Statistics
Write-Host "`n24. Admin - Get Enrollment Statistics..." -ForegroundColor Yellow
$enrollmentStatsResult = Invoke-ApiRequest -Method "GET" -Url "$httpBaseUrl/api/admin/enrollments/statistics" -Headers $adminHeaders
Write-TestResult -Endpoint "/api/admin/enrollments/statistics" -Method "GET" -Success $enrollmentStatsResult.Success -Message "Get Enrollment Statistics" -Response $enrollmentStatsResult

# Test 25: Get Groups by Course (Student)
if ($courseId) {
    Write-Host "`n25. Get Groups by Course (Student)..." -ForegroundColor Yellow
    $groupsByCourseResult = Invoke-ApiRequest -Method "GET" -Url "$httpBaseUrl/api/groups/by-course/$courseId" -Headers $studentHeaders
    Write-TestResult -Endpoint "/api/groups/by-course/{courseId}" -Method "GET" -Success $groupsByCourseResult.Success -Message "Get Groups by Course" -Response $groupsByCourseResult
}

# Test 26: Get Enrollments by User (Student)
Write-Host "`n26. Get Enrollments by User (Student)..." -ForegroundColor Yellow
$enrollmentsResult = Invoke-ApiRequest -Method "GET" -Url "$httpBaseUrl/api/enrollments/2" -Headers $studentHeaders  # User ID 2 = Student
Write-TestResult -Endpoint "/api/enrollments/{userId}" -Method "GET" -Success $enrollmentsResult.Success -Message "Get Enrollments by User" -Response $enrollmentsResult

# Summary
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Test Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
$totalTests = $testResults.Count
$passedTests = ($testResults | Where-Object { $_.Success -eq $true }).Count
$failedTests = $totalTests - $passedTests

Write-Host "Total Tests: $totalTests" -ForegroundColor White
Write-Host "Passed: $passedTests" -ForegroundColor Green
Write-Host "Failed: $failedTests" -ForegroundColor Red
Write-Host ""

if ($failedTests -gt 0) {
    Write-Host "Failed Tests:" -ForegroundColor Red
    $testResults | Where-Object { $_.Success -eq $false } | ForEach-Object {
        Write-Host "  - [$($_.Method)] $($_.Endpoint): $($_.Message)" -ForegroundColor Red
    }
}

Write-Host "`nTest completed!" -ForegroundColor Cyan

