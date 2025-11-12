# Migration Guide - İlk Migration Yaratmaq

## Visual Studio 2022-də Migration Yaratmaq

### Metod 1: Package Manager Console (Tövsiyə olunur)

**Əvvəlcə:** `Microsoft.EntityFrameworkCore.Tools` paketi `LMS.API` proyektinə əlavə edilməlidir (artıq əlavə edilib).

1. **Visual Studio 2022**-də solution-ı açın
2. **Tools** → **NuGet Package Manager** → **Package Manager Console**
3. Package Manager Console-da **Default project** olaraq **LMS.API** seçin
4. Aşağıdakı komandı çalıştırın:

```powershell
Add-Migration InitialCreate -Project LMS.Infrastructure -StartupProject LMS.API -Context LmsDbContext
```

**Qeyd:** Əgər `Add-Migration` komutu tanınmırsa:
- Visual Studio-nu yenidən başladın
- Və ya Package Manager Console-da `Install-Package Microsoft.EntityFrameworkCore.Tools -Project LMS.API` çalıştırın

4. Migration yaradıldıqdan sonra database-ə tətbiq etmək üçün:

```powershell
Update-Database -Project LMS.Infrastructure -StartupProject LMS.API -Context LmsDbContext
```

---

### Metod 2: Command Line (dotnet ef tools ilə)

Əgər `dotnet ef` tools yüklüdürsə:

```bash
# Migration yaratmaq
dotnet ef migrations add InitialCreate --project LMS.Infrastructure --startup-project LMS.API --context LmsDbContext

# Database-ə tətbiq etmək
dotnet ef database update --project LMS.Infrastructure --startup-project LMS.API --context LmsDbContext
```

---

### Metod 3: Docker Compose ilə (Avtomatik)

Docker Compose ilə işlədikdə, migration avtomatik olaraq `Program.cs`-də `MigrateDatabaseAsync()` metodu ilə tətbiq olunur.

```bash
docker-compose up -d
```

---

## Migration Strukturu

Migration faylları `LMS.Infrastructure/Data/Migrations/` klasöründə yaradılacaq:

```
LMS.Infrastructure/
  Data/
    Migrations/
      20240101000000_InitialCreate.cs
      20240101000000_InitialCreate.Designer.cs
```

---

## Connection String

Migration yaratmaq üçün `appsettings.Development.json` və ya `appsettings.json`-da connection string olmalıdır:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=LmsDb;User=root;Password=rootpassword;Port=3306;AllowUserVariables=True;Convert Zero Datetime=True"
  }
}
```

---

## Design-Time DbContext Factory

`LMS.Infrastructure/Data/DesignTimeDbContextFactory.cs` faylı migration yaratmaq üçün istifadə olunur. Bu fayl artıq yaradılıb və migration zamanı connection string-i təmin edir.

---

## Əsas Komandalar

### Migration Yaratmaq
```powershell
Add-Migration MigrationAdi -Project LMS.Infrastructure -StartupProject LMS.API -Context LmsDbContext
```

### Migration-ı Geri Almaq (son migration-ı silmək)
```powershell
Remove-Migration -Project LMS.Infrastructure -StartupProject LMS.API -Context LmsDbContext
```

### Database-ə Tətbiq Etmək
```powershell
Update-Database -Project LMS.Infrastructure -StartupProject LMS.API -Context LmsDbContext
```

### Migration Siyahısını Görmək
```powershell
Get-Migrations -Project LMS.Infrastructure -StartupProject LMS.API -Context LmsDbContext
```

---

## Qeydlər

1. **Migration yaratmazdan əvvəl** database-in mövcud olması lazım deyil
2. **Migration tətbiq etməzdən əvvəl** database-in mövcud olması lazımdır
3. **Docker Compose** istifadə edərkən migration avtomatik tətbiq olunur
4. **Connection string** MySQL formatında olmalıdır (Pomelo provider üçün)

---

## Problem Həlləri

### Problem: "dotnet ef" tapılmır

**Həll:**
```bash
dotnet tool install --global dotnet-ef
```

### Problem: Connection string tapılmır

**Həll:** `appsettings.json` və ya `appsettings.Development.json`-da `DefaultConnection` yoxlayın

### Problem: Design-time factory tapılmır

**Həll:** `LMS.Infrastructure/Data/DesignTimeDbContextFactory.cs` faylının mövcud olduğunu yoxlayın

---

## Növbəti Addımlar

1. Migration yaradın (yuxarıdakı metodlardan birini istifadə edin)
2. Database-ə tətbiq edin
3. Docker Compose ilə test edin

