# 🚂 Railway.app Deployment Rehberi

## Adım 1: GitHub Repository Hazırlama

1. GitHub'da yeni bir repository oluşturun
2. Projenizi GitHub'a yükleyin:

```bash
git init
git add .
git commit -m "Initial commit - ExcelSheetsApp"
git branch -M main
git remote add origin https://github.com/KULLANICI_ADINIZ/ExcelSheetsApp.git
git push -u origin main
```

## Adım 2: Railway.app Hesabı

1. https://railway.app adresine gidin
2. "Login with GitHub" ile giriş yapın
3. GitHub repository'nizle bağlantı kurun

## Adım 3: Proje Oluşturma

1. Railway dashboard'da "New Project" tıklayın
2. "Deploy from GitHub repo" seçin
3. ExcelSheetsApp repository'nizi seçin
4. Otomatik deployment başlayacak

## Adım 4: Environment Variables (Opsiyonel)

Railway dashboard'da:
- Settings > Environment sekmesinde
- Gerekirse custom değişkenler ekleyin

## Adım 5: Custom Domain (Opsiyonel - Ücretsiz)

1. Settings > Networking
2. "Generate Domain" tıklayın
3. Özel domain istiyorsanız "Custom Domain" ekleyin

## ✅ Deployment Süreci

- İlk deployment: ~3-5 dakika
- Sonraki deploymentler: ~1-2 dakika
- Her GitHub push'da otomatik deployment

## 🔧 Sorun Giderme

### Database Hatası
Railway container'da database dosyası `/app/data/` klasöründe saklanır ve kalıcıdır.

### Memory Limit
Ücretsiz tier 512MB RAM limit. Çok büyük Excel dosyaları sorun yaratabilir.

### Build Hatası
Railway logs'u kontrol edin: Settings > Logs

## 📊 Ücretsiz Limitler

- 500 saat/ay çalışma süresi
- 1GB RAM
- 1GB Disk
- Unlimited bandwidth
- Custom domain desteği

## 🌐 Live URL

Deployment sonrası Railway size `https://excelsheets-production.up.railway.app` benzeri bir URL verecek.

Admin Giriş:
- Kullanıcı: admin
- Şifre: admin123
