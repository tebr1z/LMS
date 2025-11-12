# Visual Studio Setup Guide

## Problem: "A project with an Output Type of Class Library cannot be started directly"

Bu xəta, Visual Studio-da startup project olaraq Class Library projesi (LMS.Application, LMS.Domain, LMS.Infrastructure) seçildiyində baş verir.

## Həll:

### Metod 1: Visual Studio-da Startup Project Seçmək

1. **Solution Explorer**-da `LMS.Backend.sln` solution-ı açın
2. **LMS.API** proyektinə sağ klik edin
3. **"Set as Startup Project"** seçin
4. Və ya Solution-a sağ klik edin → **Properties** → **Startup Project** → **Single startup project** → **LMS.API** seçin

### Metod 2: Command Line-dan

```bash
# Solution klasöründə
dotnet sln LMS.Backend.sln set-startup-project LMS.API/LMS.API.csproj
```

### Metod 3: Manual olaraq Run

1. **LMS.API** proyektinə sağ klik edin
2. **"Debug"** → **"Start New Instance"** seçin
3. Və ya **F5** basın (LMS.API seçili olanda)

## Yoxlama:

- Solution Explorer-da **LMS.API** proyekti **qalın** yazı ilə görünməlidir
- Toolbar-da startup project dropdown-da **LMS.API** seçili olmalıdır

## Qeyd:

- **LMS.API** - Web API proyekti (çalışdırıla bilər) ✅
- **LMS.Application** - Class Library (çalışdırıla bilməz) ❌
- **LMS.Domain** - Class Library (çalışdırıla bilməz) ❌
- **LMS.Infrastructure** - Class Library (çalışdırıla bilməz) ❌

