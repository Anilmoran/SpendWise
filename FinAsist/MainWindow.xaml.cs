using System;
using System.IO;
using System.Collections.Generic;
using System.Windows;
using Microsoft.Win32;
using Microsoft.ML;
using Microsoft.ML.Data;
using ExcelDataReader;
using System.Data;
using System.Linq;
using LiveCharts;
using LiveCharts.Wpf;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FinAsist
{
    public class HarcamaVerisi { [LoadColumn(0)] public string Aciklama { get; set; } [LoadColumn(1)] public string Kategori { get; set; } }
    public class KategoriTahmini { [ColumnName("PredictedLabel")] public string TahminEdilenKategori { get; set; } }
    public class Harcama { public string Tarih { get; set; } public string Aciklama { get; set; } public double Tutar { get; set; } public string Kategori { get; set; } }

    public partial class MainWindow : Window
    {
        MLContext mlContext = new MLContext();
        ITransformer egitilmisModel = default!;
        List<Harcama> mevcutHarcamalar = new List<Harcama>();

        public MainWindow()
        {
            InitializeComponent();
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            QuestPDF.Settings.License = LicenseType.Community;

            ModeliEgit();
        }

        private void ModeliEgit()
        {
            var egitimVerileri = new List<HarcamaVerisi>() {
                new HarcamaVerisi { Aciklama = "MIGROS", Kategori = "Market" },
                new HarcamaVerisi { Aciklama = "A101", Kategori = "Market" },
                new HarcamaVerisi { Aciklama = "BIM", Kategori = "Market" },
                new HarcamaVerisi { Aciklama = "SOK", Kategori = "Market" },
                new HarcamaVerisi { Aciklama = "STARBUCKS", Kategori = "Yeme & İçme" },
                new HarcamaVerisi { Aciklama = "YEMEKSEPETI", Kategori = "Yeme & İçme" },
                new HarcamaVerisi { Aciklama = "GETIRYEMEK", Kategori = "Yeme & İçme" },
                new HarcamaVerisi { Aciklama = "SHELL", Kategori = "Akaryakıt" },
                new HarcamaVerisi { Aciklama = "OPET", Kategori = "Akaryakıt" },
                new HarcamaVerisi { Aciklama = "NETFLIX", Kategori = "Abonelik" },
                new HarcamaVerisi { Aciklama = "SPOTIFY", Kategori = "Abonelik" },
                new HarcamaVerisi { Aciklama = "TURKCELL", Kategori = "Abonelik" },
                new HarcamaVerisi { Aciklama = "TRENDYOL", Kategori = "Alışveriş" }
            };

            var data = mlContext.Data.LoadFromEnumerable(egitimVerileri);
            var pipeline = mlContext.Transforms.Conversion.MapValueToKey("Label", "Kategori")
                .Append(mlContext.Transforms.Text.FeaturizeText("Features", "Aciklama"))
                .Append(mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy())
                .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));
            egitilmisModel = pipeline.Fit(data);
        }

        private void BtnEkstreYukle_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog opp = new OpenFileDialog { Filter = "Excel Dosyaları|*.xls;*.xlsx" };
            if (opp.ShowDialog() == true)
            {
                try
                {
                    mevcutHarcamalar.Clear();
                    using (var stream = File.Open(opp.FileName, FileMode.Open, FileAccess.Read))
                    using (var reader = ExcelReaderFactory.CreateReader(stream))
                    {
                        var dt = reader.AsDataSet().Tables[0];
                        double toplamHarcama = 0;
                        var motor = mlContext.Model.CreatePredictionEngine<HarcamaVerisi, KategoriTahmini>(egitilmisModel);

                        for (int i = 12; i < dt.Rows.Count; i++)
                        {
                            if (dt.Rows[i][0] == null || dt.Rows[i][3] == null || dt.Rows[i][3] == DBNull.Value) continue;

                            double tutar;
                            if (!double.TryParse(dt.Rows[i][3].ToString(), out tutar)) continue;
                            if (tutar >= 0) continue;

                            string gercekAciklama = dt.Rows[i][2]?.ToString() ?? "Bilinmiyor";

                            var h = new Harcama
                            {
                                Tarih = dt.Rows[i][0].ToString(),
                                Aciklama = gercekAciklama,
                                Tutar = Math.Abs(tutar)
                            };

                            string aciklamaKucuk = h.Aciklama.ToLower();
                            if (aciklamaKucuk.Contains("virman") || aciklamaKucuk.Contains("eft") || aciklamaKucuk.Contains("havale") || aciklamaKucuk.Contains("fast"))
                            {
                                h.Kategori = "Transfer / Ödeme";
                            }
                            else
                            {
                                h.Kategori = motor.Predict(new HarcamaVerisi { Aciklama = h.Aciklama }).TahminEdilenKategori;
                            }

                            mevcutHarcamalar.Add(h);
                            toplamHarcama += h.Tutar;
                        }

                        GridHarcamalar.ItemsSource = null;
                        GridHarcamalar.ItemsSource = mevcutHarcamalar;
                        TxtToplamHarcama.Text = $"Toplam Harcama: {toplamHarcama:N2} TL";

                        AsistaniVeGrafigiCalistir();
                    }
                }
                catch (Exception ex) { MessageBox.Show("Hata: " + ex.Message); }
            }
        }

        private void BtnAsistan_Click(object sender, RoutedEventArgs e)
        {
            AsistaniVeGrafigiCalistir();
        }

        private void AsistaniVeGrafigiCalistir()
        {
            if (!mevcutHarcamalar.Any()) return;

            var reelHarcamalar = mevcutHarcamalar.Where(x => x.Kategori != "Transfer / Ödeme").ToList();
            double reelHarcamaToplami = reelHarcamalar.Sum(x => x.Tutar);

            double hedef;
            double.TryParse(TxtHedefButce.Text, out hedef);

            string mesaj = "";
            if (reelHarcamaToplami > hedef)
            {
                var enCok = reelHarcamalar.GroupBy(x => x.Kategori).OrderByDescending(g => g.Sum(s => s.Tutar)).FirstOrDefault()?.Key;
                mesaj += $"⚠️ Bütçeni {reelHarcamaToplami - hedef:N2} TL aştın!\nEn büyük deliğin '{enCok}'.\n\n";
            }
            else
            {
                mesaj += $"✅ Harika! Hedefinin {hedef - reelHarcamaToplami:N2} TL altındasın.\n\n";
            }

            var abonelikler = reelHarcamalar.Where(x => x.Kategori == "Abonelik").ToList();
            if (abonelikler.Any())
            {
                mesaj += $"🔍 SABİT GİDER:\nBu ay aboneliklere {abonelikler.Sum(x => x.Tutar):N2} TL gitti.";
            }
            else
            {
                mesaj += $"🔍 SABİT GİDER:\nBu ay tespit edilen abonelik yok.";
            }
            TxtAsistanMesaj.Text = mesaj;

            var grafikSerileri = new SeriesCollection();
            var kategoriGruplari = mevcutHarcamalar.Where(x => x.Kategori != "Transfer / Ödeme").GroupBy(x => x.Kategori);

            foreach (var grup in kategoriGruplari)
            {
                grafikSerileri.Add(new PieSeries
                {
                    Title = grup.Key,
                    Values = new ChartValues<double> { grup.Sum(x => x.Tutar) },
                    DataLabels = true,
                    LabelPoint = chartPoint => string.Format("{0:N0} TL", chartPoint.Y)
                });
            }
            ChartHarcamalar.Series = grafikSerileri;
        }

        private void BtnPdfIndir_Click(object sender, RoutedEventArgs e)
        {
            if (!mevcutHarcamalar.Any())
            {
                MessageBox.Show("Önce bir ekstre yüklemelisiniz!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveFileDialog sfd = new SaveFileDialog { Filter = "PDF Dosyası|*.pdf", FileName = "NexFin_Rapor.pdf" };
            if (sfd.ShowDialog() == true)
            {
                try
                {
                    Document.Create(container =>
                    {
                        container.Page(page =>
                        {
                            page.Size(PageSizes.A4);
                            page.Margin(2, Unit.Centimetre);
                            page.PageColor(Colors.White);
                            page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial")); // Türkçe karakterler için Arial

                            // Başlık
                            page.Header().Text("NexFin AI - Finansal Analiz Raporu")
                                .SemiBold().FontSize(22).FontColor(Colors.Green.Darken2);

                            page.Content().PaddingVertical(1, Unit.Centimetre).Column(x =>
                            {
                                x.Spacing(20);

                                // Asistan Raporu
                                x.Item().Text("🤖 AI Asistan Raporu").Bold().FontSize(15).FontColor(Colors.Grey.Darken3);
                                x.Item().Text(TxtAsistanMesaj.Text).FontSize(12).FontColor(Colors.Black);

                                // Toplam Harcama
                                x.Item().Text(TxtToplamHarcama.Text).Bold().FontSize(16).FontColor(Colors.Red.Medium);

                                // Tablo
                                x.Item().Text("📊 Harcama Dökümü").Bold().FontSize(15).FontColor(Colors.Grey.Darken3);
                                x.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(2); // Tarih
                                        columns.RelativeColumn(5); // Açıklama
                                        columns.RelativeColumn(3); // Kategori
                                        columns.RelativeColumn(2); // Tutar
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).PaddingBottom(5).Text("Tarih").Bold();
                                        header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).PaddingBottom(5).Text("Açıklama").Bold();
                                        header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).PaddingBottom(5).Text("Kategori").Bold();
                                        header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).PaddingBottom(5).Text("Tutar (TL)").Bold();
                                    });

                                    foreach (var h in mevcutHarcamalar)
                                    {
                                        table.Cell().PaddingVertical(2).Text(h.Tarih);
                                        table.Cell().PaddingVertical(2).Text(h.Aciklama);
                                        table.Cell().PaddingVertical(2).Text(h.Kategori);
                                        table.Cell().PaddingVertical(2).Text(h.Tutar.ToString("N2"));
                                    }
                                });
                            });

                            // Sayfa Numaraları
                            page.Footer().AlignCenter().Text(x =>
                            {
                                x.Span("Sayfa ");
                                x.CurrentPageNumber();
                                x.Span(" / ");
                                x.TotalPages();
                            });
                        });
                    })
                    .GeneratePdf(sfd.FileName);

                    MessageBox.Show("PDF Raporu başarıyla oluşturuldu!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);

                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(sfd.FileName) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("PDF oluşturulurken hata: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}