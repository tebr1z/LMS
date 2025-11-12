# NuGet Paketləri - LMS Proyekti

Bu sənəd LMS proyekti üçün lazım olan bütün NuGet paketlərini siyahıya alır.

## 📦 Hazırda İstifadə Olunan Paketlər

### LMS.API Proyekti

| Paket Adı | Versiya | Məqsəd |
|-----------|---------|--------|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 8.0.0 | JWT Authentication |
| `Microsoft.AspNetCore.OpenApi` | 8.0.3 | OpenAPI dəstəyi |
| `Swashbuckle.AspNetCore` | 6.4.0 | Swagger UI |
| `System.IdentityModel.Tokens.Jwt` | 8.14.0 | JWT token işləmə |

### LMS.Application Proyekti

| Paket Adı | Versiya | Məqsəd |
|-----------|---------|--------|
| `AutoMapper` | 12.0.1 | Object mapping |
| `AutoMapper.Extensions.Microsoft.DependencyInjection` | 12.0.1 | AutoMapper DI dəstəyi |
| `FluentValidation` | 12.1.0 | Validation |
| `FluentValidation.DependencyInjectionExtensions` | 12.1.0 | FluentValidation DI dəstəyi |
| `MediatR` | 13.1.0 | CQRS pattern |
| `Microsoft.Extensions.Localization` | 10.0.0 | Localization |

### LMS.Infrastructure Proyekti

| Paket Adı | Versiya | Məqsəd |
|-----------|---------|--------|
| `Microsoft.AspNetCore.Identity` | 2.2.0 | ASP.NET Core Identity |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | 8.0.0 | Identity EF Core dəstəyi |
| `Microsoft.AspNetCore.SignalR.Core` | 1.1.0 | SignalR real-time notifications |
| `Microsoft.EntityFrameworkCore` | 8.0.2 | Entity Framework Core |
| `Microsoft.EntityFrameworkCore.Design` | 8.0.2 | EF Core migrations |
| `Microsoft.EntityFrameworkCore.Proxies` | 8.0.2 | Lazy loading proxies |
| `Pomelo.EntityFrameworkCore.MySql` | 8.0.2 | MySQL provider |
| `System.IdentityModel.Tokens.Jwt` | 8.0.0 | JWT token işləmə |

### LMS.Domain Proyekti

- **Paket yoxdur** (təmiz domain layer)

---

## 🔮 Gələcək Modullar Üçün Lazım Olan Paketlər

### File Storage (AWS S3)

**LMS.Infrastructure** proyektinə əlavə edin:

```xml
<PackageReference Include="AWSSDK.S3" Version="3.7.400.50" />
```

**Visual Studio-da:**
1. `LMS.Infrastructure` proyektinə sağ klik
2. **Manage NuGet Packages**
3. **Browse** sekmesində `AWSSDK.S3` axtarın
4. **Install** basın

---

### Email Notifications

**LMS.Infrastructure** proyektinə əlavə edin (birini seçin):

**Seçim 1: SMTP (System.Net.Mail)**
- .NET 8-də daxildir, əlavə paket lazım deyil

**Seçim 2: SendGrid**
```xml
<PackageReference Include="SendGrid" Version="9.29.3" />
```

**Seçim 3: MailKit (SMTP client)**
```xml
<PackageReference Include="MailKit" Version="4.3.0" />
```

---

### Payment Processing

**LMS.Infrastructure** proyektinə əlavə edin (birini seçin):

**Stripe:**
```xml
<PackageReference Include="Stripe.net" Version="45.0.0" />
```

**PayPal:**
```xml
<PackageReference Include="PayPal" Version="1.9.1" />
```

---

### Redis Caching (İstəyə bağlı)

**LMS.Infrastructure** proyektinə əlavə edin:

```xml
<PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="8.0.0" />
```

---

### Serilog (Logging - İstəyə bağlı)

**LMS.API** və **LMS.Infrastructure** proyektlərinə əlavə edin:

```xml
<PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
<PackageReference Include="Serilog.Sinks.Console" Version="5.0.0" />
<PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
```

---

## 🚀 Visual Studio 2022-də Paketləri Yükləmək

### Metod 1: Automatic Restore (Avtomatik)

Visual Studio 2022 avtomatik olaraq paketləri restore edir. Əgər restore olunmayıbsa:

1. **Solution Explorer**-da solution-a sağ klik edin
2. **Restore NuGet Packages** seçin

### Metod 2: Package Manager Console

1. **Tools** → **NuGet Package Manager** → **Package Manager Console**
2. Aşağıdakı komandları çalıştırın:

```powershell
# Solution-dəki bütün paketləri restore et
dotnet restore

# Və ya konkret proyekt üçün
dotnet restore LMS.API/LMS.API.csproj
```

### Metod 3: Manage NuGet Packages UI

1. **Solution Explorer**-da proyektə sağ klik edin
2. **Manage NuGet Packages** seçin
3. **Installed** sekmesində paketləri görə bilərsiniz
4. **Browse** sekmesindən yeni paket əlavə edə bilərsiniz

### Metod 4: Command Line

```bash
# Solution klasöründə
dotnet restore LMS.Backend.sln

# Və ya konkret proyekt üçün
cd LMS.API
dotnet restore
```

---

## ✅ Paketlərin Yoxlanılması

### Visual Studio-da:

1. **Solution Explorer**-da proyektə sağ klik
2. **Edit Project File** (və ya `.csproj` faylını açın)
3. `<PackageReference>` elementlərini yoxlayın

### Command Line-da:

```bash
# Bütün paketləri list et
dotnet list package

# Konkret proyekt üçün
dotnet list LMS.API/LMS.API.csproj package
```

---

## 📋 Paket Versiyalarının Yenilənməsi

### Visual Studio-da:

1. **Tools** → **NuGet Package Manager** → **Manage NuGet Packages for Solution**
2. **Updates** sekmesində yenilənə bilən paketləri görə bilərsiniz
3. Yeniləmək istədiyiniz paketləri seçin və **Update** basın

### Command Line-da:

```bash
# Bütün paketləri yenilə
dotnet list package --outdated

# Konkret paketi yenilə
dotnet add LMS.API/LMS.API.csproj package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.1
```

---

## 🔧 Paket Problemlərinin Həlli

### Problem: Paket restore olunmur

**Həll:**
```bash
# NuGet cache-i təmizlə
dotnet nuget locals all --clear

# Yenidən restore et
dotnet restore
```

### Problem: Paket versiyası uyğun deyil

**Həll:**
1. `.csproj` faylında versiyanı yeniləyin
2. `dotnet restore` çalıştırın

### Problem: Paket tapılmır

**Həll:**
1. NuGet source-ları yoxlayın: **Tools** → **NuGet Package Manager** → **Package Manager Settings** → **Package Sources**
2. `https://api.nuget.org/v3/index.json` aktiv olmalıdır

---

## 📝 Paket Əlavə Etmək (Nümunə)

### Visual Studio-da:

1. **LMS.Infrastructure** proyektinə sağ klik
2. **Manage NuGet Packages**
3. **Browse** sekmesində `AWSSDK.S3` axtarın
4. Versiyanı seçin (3.7.400.50)
5. **Install** basın

### Command Line-da:

```bash
cd LMS.Infrastructure
dotnet add package AWSSDK.S3 --version 3.7.400.50
```

---

## 🎯 Tövsiyə Olunan Paketlər (İstəyə bağlı)

| Paket | Versiya | Məqsəd |
|-------|---------|--------|
| `Serilog.AspNetCore` | 8.0.0 | Strukturlaşdırılmış logging |
| `Microsoft.Extensions.Caching.StackExchangeRedis` | 8.0.0 | Redis caching |
| `FluentAssertions` | 6.12.0 | Test assertions |
| `xunit` | 2.6.0 | Unit testing framework |
| `Moq` | 4.20.0 | Mocking framework |
| `Bogus` | 34.0.2 | Test data generator |

---

## ⚠️ Qeydlər

1. **.NET 8.0** istifadə olunur - paket versiyaları .NET 8 ilə uyğun olmalıdır
2. **Pomelo.EntityFrameworkCore.MySql** MySQL üçün istifadə olunur
3. Bütün paketlər **NuGet.org**-dan gəlir
4. Paket versiyaları müntəzəm yenilənməlidir (təhlükəsizlik yeniləmələri üçün)

---

## 🔍 Paket Versiyalarının Yoxlanılması

Visual Studio-da:
- **Tools** → **NuGet Package Manager** → **Manage NuGet Packages for Solution**
- **Installed** sekmesində bütün paketləri görə bilərsiniz

Command Line-da:
```bash
dotnet list package
```

