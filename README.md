# 🚀 SpendWise AI - Akıllı Finans Asistanı

SpendWise AI, banka ekstrelerini (.xls, .xlsx) analiz eden, harcamaları yapay zeka ile otomatik olarak kategorize eden ve kullanıcıya finansal içgörüler sunan bir masaüstü uygulamasıdır. 

Sıkıcı banka verilerini saniyeler içinde anlamlı grafiklere, bütçe uyarılarına PDF raporlarına dönüştürür.

## ✨ Öne Çıkan Özellikler
- **🧠 AI Destekli Kategori Tahmini:** ML.NET altyapısı sayesinde banka açıklamalarındaki (MIGROS, STARBUCKS vb.) anlamsız kodları ve kelimeleri analiz ederek harcamayı doğru kategoriye atar.
- **🕵️‍♂️ Sabit Gider & Abonelik Avcısı:** Ekstrenizdeki Netflix, Spotify, Turkcell gibi düzenli ödemeleri tespit edip size özel "Sabit Gider Raporu" sunar.
- **🧹 Akıllı Veri Temizleme:** Gelen paraları (maaş, iade) ve hesaplar arası para transferlerini (EFT/Virman/FAST) tespit edip harcama bütçesinden hariç tutar.
- **📊 İnteraktif Görselleştirme:** LiveCharts altyapısı ile harcama dağılımınızı şık bir neon pasta grafiği üzerinden sunar.
- **📄 Tek Tıkla PDF Raporu:** Tüm analiz sonuçlarınızı, yapay zeka tavsiyelerini ve harcama dökümünüzü QuestPDF altyapısıyla profesyonel bir PDF dosyasına çevirir.

## 🛠 Kullanılan Teknolojiler & Kütüphaneler
- **Geliştirme Ortamı:** .NET / C# / WPF (Windows Presentation Foundation)
- **Makine Öğrenmesi:** `Microsoft.ML` (Multiclass Classification - SdcaMaximumEntropy)
- **Veri Okuma:** `ExcelDataReader` & `ExcelDataReader.DataSet`
- **Grafik Motoru:** `LiveCharts.Wpf`
- **PDF Motoru:** `QuestPDF`
