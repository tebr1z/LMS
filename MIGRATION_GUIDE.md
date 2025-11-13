# Visual Studio'da Migration ve Database Update Rehberi

## Yöntem 1: Package Manager Console (Önerilen)

### Adım 1: Package Manager Console'u Açın
1. Visual Studio'da **View** > **Other Windows** > **Package Manager Console** menüsünü seçin
2. Veya kısayol: **Tools** > **NuGet Package Manager** > **Package Manager Console**

### Adım 2: Default Project'i Ayarlayın
Package Manager Console'un üst kısmından **Default project** dropdown'ından **LMS.Infrastructure** projesini seçin.

### Adım 3: Mevcut Migration'ları Kontrol Edin
```powershell
Get-Migration
```
veya
```powershell
dotnet ef migrations list
```

### Adım 4: Yeni Migration Oluşturun
```powershell
Add-Migration AddNotificationsAndStats -Project LMS.Infrastructure -StartupProject LMS.API
```
Add-Migration AddAssigmentFeedBack -Project LMS.Infrastructure -StartupProject LMS.API
**Açıklama:**
- `AddNotificationsAndStats` = Migration adı (istediğiniz ismi verebilirsiniz)
- `-Project LMS.Infrastructure` = Migration dosyalarının oluşturulacağı proje
- `-StartupProject LMS.API` = Connection string'in okunacağı startup projesi

### Adım 5: Migration'ı Veritabanına Uygulayın
```powershell
Update-Database -Project LMS.Infrastructure -StartupProject LMS.API
```

### Adım 6: Son Durumu Kontrol Edin
```powershell
Get-Migration
```

---

## Yöntem 2: .NET CLI (Terminal/PowerShell)

Visual Studio'da **View** > **Terminal** menüsünden terminali açın.

### Adım 1: Proje Klasörüne Geçin
```powershell
cd "C:\Users\Windows 11\Desktop\Github Repo\LMS"
```

### Adım 2: Migration Listesini Görün
```powershell
dotnet ef migrations list --project LMS.Infrastructure --startup-project LMS.API
```

### Adım 3: Yeni Migration Oluşturun
```powershell
dotnet ef migrations add AddNotificationsAndStats --project LMS.Infrastructure --startup-project LMS.API
```

### Adım 4: Database'i Güncelleyin
```powershell
dotnet ef database update --project LMS.Infrastructure --startup-project LMS.API
```

---

## Yöntem 3: Visual Studio SQL Server Object Explorer

### Adım 1: Migration Dosyalarını Oluşturun
Package Manager Console'dan migration oluşturun (Yöntem 1, Adım 4)

### Adım 2: Migration SQL Script'ini Oluşturun
```powershell
Script-Migration -Project LMS.Infrastructure -StartupProject LMS.API
```

### Adım 3: SQL Script'i SQL Server'da Çalıştırın
1. Visual Studio'da **View** > **SQL Server Object Explorer** açın
2. Veritabanınıza bağlanın
3. **New Query** seçin
4. Oluşturulan SQL script'ini yapıştırın ve çalıştırın

---

## Oluşturulacak Yeni Tablolar ve Kolonlar

### Yeni Tablolar:
1. **Notifications** - Bildirimler tablosu
   - Id, UserId, Title, Body, Data, IsRead, CreatedAt, ReadAt, Channel, Type

2. **SystemSettings** - Sistem ayarları tablosu
   - Id, Key, Value, Description, Category, CreatedAt

3. **StudentStats** - Öğrenci istatistikleri tablosu
   - Id, StudentId, CourseInstanceId, TotalPoints, AveragePercent, vb.

### Mevcut Tablolara Eklenen Kolonlar:
1. **AssignmentSubmissions** tablosuna:
   - `Passed` (bool?) - Ödev geçti mi?
   - `IsExcellent` (bool?) - Mükemmel performans mı?
   - `PercentageScore` (decimal) - Yüzdelik skor

---

## Sorun Giderme

### Hata: "dotnet-ef tool not found"
**Çözüm:** Tool'u yükleyin:
```powershell
dotnet tool install --global dotnet-ef --version 8.0.0
```

### Hata: "Unable to create an object of type 'LmsDbContext'"
**Çözüm:** `DesignTimeDbContextFactory` dosyasının doğru konfigüre edildiğinden emin olun.

### Hata: "Connection string not found"
**Çözüm:** `appsettings.json` dosyasında connection string'in doğru olduğundan emin olun:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=TABRIZ\\SQLEXPRESS;Database=LmsDb;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### Migration Dosyası Nerede?
Migration dosyaları şu klasörde oluşturulur:
```
LMS.Infrastructure/Migrations/
```

---

## Migration Rollback (Geri Alma)

Eğer bir migration'ı geri almak isterseniz:

```powershell
Update-Database -Migration [ÖncekiMigrationAdı] -Project LMS.Infrastructure -StartupProject LMS.API
```

Örnek: Bir önceki migration'a dönmek için:
```powershell
Update-Database -Migration InitialCreate -Project LMS.Infrastructure -StartupProject LMS.API
```

---

## Migration'ı Silme

Eğer oluşturduğunuz migration'ı silmek isterseniz:

```powershell
Remove-Migration -Project LMS.Infrastructure -StartupProject LMS.API
```

**Dikkat:** Migration henüz database'e uygulanmadıysa (pending) silinebilir. Eğer zaten uygulanmışsa, önce geri almanız gerekir.

---

## Hızlı Referans

### En Çok Kullanılan Komutlar:

| İşlem | Package Manager Console | .NET CLI |
|-------|------------------------|----------|
| Migration Listesi | `Get-Migration` | `dotnet ef migrations list` |
| Migration Oluştur | `Add-Migration [Ad]` | `dotnet ef migrations add [Ad]` |
| Database Update | `Update-Database` | `dotnet ef database update` |
| Migration Sil | `Remove-Migration` | `dotnet ef migrations remove` |
| Script Oluştur | `Script-Migration` | `dotnet ef migrations script` |

### Proje Ayarları:
- **Project:** `LMS.Infrastructure`
- **Startup Project:** `LMS.API`

