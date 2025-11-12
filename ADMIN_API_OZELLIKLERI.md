# AdminController API Özellikleri

## Genel Bilgiler
- **Base Route:** `/api/admin`
- **Yetkilendirme:** Tüm endpoint'ler `MasterAdmin` rolü gerektirir
- **Format:** RESTful API
- **Response Format:** Tüm endpoint'ler `ApiResponse<T>` formatında yanıt döner

---

## 1. Sistem İstatistikleri
**Endpoint:** `GET /api/admin/statistics`

**Açıklama:** Sistem genelinde istatistik bilgilerini getirir.

**Response Model:**
```json
{
  "totalUsers": 0,
  "totalCourses": 0,
  "totalEnrollments": 0,
  "activeUsers": 0,
  "usersByRole": {
    "Student": 0,
    "Instructor": 0,
    "Admin": 0
  },
  "lastUpdated": "2024-01-01T00:00:00Z"
}
```

**Özellikler:**
- Toplam kullanıcı sayısı
- Toplam kurs sayısı
- Toplam kayıt sayısı
- Aktif kullanıcı sayısı
- Rol bazında kullanıcı dağılımı
- Son güncelleme zamanı

---

## 2. Kullanıcı Yönetimi
**Endpoint:** `GET /api/admin/users`

**Açıklama:** Sayfalanmış kullanıcı listesini getirir.

**Query Parameters:**
- `pageNumber` (int, default: 1) - Sayfa numarası
- `pageSize` (int, default: 10) - Sayfa başına kayıt sayısı

**Response Model:**
```json
{
  "users": [
    {
      "id": 1,
      "email": "user@example.com",
      "fullName": "John Doe",
      "role": "Student",
      "createdAt": "2024-01-01T00:00:00Z",
      "isActive": true,
      "enrollmentCount": 5
    }
  ],
  "totalCount": 100,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 10
}
```

**Özellikler:**
- Sayfalama desteği
- Kullanıcı bilgileri (ID, email, ad, rol)
- Oluşturulma tarihi
- Aktiflik durumu
- Her kullanıcının kayıt sayısı
- Toplam sayfa bilgisi

---

## 3. Kullanıcı Rolü Güncelleme
**Endpoint:** `PUT /api/admin/users/{userId}/role`

**Açıklama:** Belirli bir kullanıcının rolünü günceller.

**Path Parameters:**
- `userId` (int) - Güncellenecek kullanıcının ID'si

**Request Body:**
```json
{
  "role": "Instructor"
}
```

**Response:** Başarılı olursa `true` döner.

**Özellikler:**
- Kullanıcı rolü değiştirme
- MasterAdmin yetkisi gerektirir
- Hata durumlarında detaylı hata mesajı

---

## 4. Kurs Yönetimi
**Endpoint:** `GET /api/admin/courses`

**Açıklama:** Sayfalanmış kurs listesini getirir.

**Query Parameters:**
- `pageNumber` (int, default: 1) - Sayfa numarası
- `pageSize` (int, default: 10) - Sayfa başına kayıt sayısı

**Response Model:**
```json
{
  "courses": [
    {
      "id": 1,
      "title": "Introduction to Programming",
      "description": "Learn the basics of programming",
      "creatorName": "Jane Smith",
      "enrollmentCount": 50,
      "createdAt": "2024-01-01T00:00:00Z",
      "isActive": true
    }
  ],
  "totalCount": 25,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 3
}
```

**Özellikler:**
- Sayfalama desteği
- Kurs bilgileri (ID, başlık, açıklama)
- Kurs oluşturucu bilgisi
- Her kursun kayıt sayısı
- Oluşturulma tarihi
- Aktiflik durumu
- Toplam sayfa bilgisi

---

## 5. Kayıt İstatistikleri
**Endpoint:** `GET /api/admin/enrollments/statistics`

**Açıklama:** Kayıt istatistiklerini getirir. Tarih aralığına göre filtreleme yapılabilir.

**Query Parameters:**
- `startDate` (DateTime?, optional) - Başlangıç tarihi filtresi
- `endDate` (DateTime?, optional) - Bitiş tarihi filtresi

**Response Model:**
```json
{
  "totalEnrollments": 500,
  "enrollmentsThisMonth": 50,
  "enrollmentsThisWeek": 10,
  "enrollmentsByCourse": {
    "1": 100,
    "2": 150,
    "3": 250
  },
  "dailyEnrollments": [
    {
      "date": "2024-01-01T00:00:00Z",
      "count": 5
    },
    {
      "date": "2024-01-02T00:00:00Z",
      "count": 8
    }
  ]
}
```

**Özellikler:**
- Toplam kayıt sayısı
- Bu ayki kayıt sayısı
- Bu haftaki kayıt sayısı
- Kurs bazında kayıt dağılımı
- Günlük kayıt istatistikleri (tarih bazında)
- Tarih aralığına göre filtreleme
- Günlük kayıtlar tarih sırasına göre sıralanır

---

## 6. Grup Yönetimi
**Endpoint:** `GET /api/admin/groups`

**Açıklama:** Sayfalanmış grup listesini getirir. (MasterAdmin özel)

**Query Parameters:**
- `pageNumber` (int, default: 1) - Sayfa numarası
- `pageSize` (int, default: 10) - Sayfa başına kayıt sayısı

**Response Model:**
```json
{
  "groups": [
    {
      "id": 1,
      "name": "Web Development Group",
      "description": "Group for web developers",
      "creatorName": "John Doe",
      "memberCount": 25,
      "courseCount": 5,
      "createdAt": "2024-01-01T00:00:00Z"
    }
  ],
  "totalCount": 10,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 1
}
```

**Özellikler:**
- Sayfalama desteği
- Grup bilgileri (ID, isim, açıklama)
- Grup oluşturucu bilgisi
- Üye sayısı
- Kurs sayısı
- Oluşturulma tarihi
- Toplam sayfa bilgisi

---

## Ortak Özellikler

### Hata Yönetimi
- Tüm endpoint'ler try-catch blokları ile korunur
- Hata durumlarında detaylı loglama yapılır
- Kullanıcıya anlamlı hata mesajları döner
- HTTP 500 status code ile hata yanıtı döner

### Response Format
Tüm endpoint'ler aşağıdaki formatı kullanır:
```json
{
  "success": true,
  "data": { /* endpoint'e özel veri */ },
  "message": "İşlem başarılı mesajı",
  "errors": null
}
```

Hata durumunda:
```json
{
  "success": false,
  "data": null,
  "message": "Hata açıklaması",
  "errors": "Detaylı hata mesajı"
}
```

### Güvenlik
- Tüm endpoint'ler `[Authorize(Roles = "MasterAdmin")]` ile korunur
- Sadece MasterAdmin rolüne sahip kullanıcılar erişebilir
- JWT token tabanlı kimlik doğrulama

### Performans
- Sayfalama desteği ile büyük veri setlerinde performans optimizasyonu
- CancellationToken desteği ile asenkron işlemler
- Veritabanı sorguları optimize edilmiş

---

## Kullanım Örnekleri

### Sistem İstatistiklerini Getirme
```http
GET /api/admin/statistics
Authorization: Bearer {token}
```

### Kullanıcıları Listeleme (2. sayfa, 20 kayıt)
```http
GET /api/admin/users?pageNumber=2&pageSize=20
Authorization: Bearer {token}
```

### Kullanıcı Rolü Güncelleme
```http
PUT /api/admin/users/5/role
Authorization: Bearer {token}
Content-Type: application/json

{
  "role": "Instructor"
}
```

### Kayıt İstatistikleri (Tarih Filtreli)
```http
GET /api/admin/enrollments/statistics?startDate=2024-01-01&endDate=2024-01-31
Authorization: Bearer {token}
```

