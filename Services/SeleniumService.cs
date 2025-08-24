using System.Data;
using ClosedXML.Excel;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using ExcelSheetsApp.Models;
using System.Text.RegularExpressions;

namespace ExcelSheetsApp.Services;

public class SeleniumService
{
    private readonly ILogger<SeleniumService> _logger;
    private static ChromeDriver? _driverInstance; // Static instance to keep it alive
    private static readonly object _lock = new object(); // For thread safety

    public SeleniumService(ILogger<SeleniumService> logger)
    {
        _logger = logger;
    }

    public async Task<SeleniumViewModel> FillWebsiteDataAsync(string excelFilePath, string selectedSheet, string websiteUrl)
    {
        var result = new SeleniumViewModel
        {
            WebsiteUrl = websiteUrl,
            IsProcessing = true,
            Status = "İşlem başlatılıyor...",
            Logs = new List<string>()
        };

        try
        {
            result.Logs.Add($"Excel dosyası okunuyor... (Sayfa: {selectedSheet})");
            var excelData = await ReadExcelDataAsync(excelFilePath, selectedSheet);
            result.TotalCount = excelData.Count;
            result.Logs.Add($"{excelData.Count} soru bulundu.");
            
            // Railway'de Chrome test et
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                result.Logs.Add("🐧 Linux ortamı tespit edildi (Railway)");
                result.Logs.Add("🔧 Chrome binary kontrolü yapılıyor...");
                
                if (!System.IO.File.Exists("/usr/bin/chromium-browser"))
                {
                    result.Logs.Add("❌ Chrome binary bulunamadı!");
                    throw new Exception("Chrome binary Railway'de mevcut değil");
                }
                else
                {
                    result.Logs.Add("✅ Chrome binary bulundu: /usr/bin/chromium-browser");
                }
            }

            // Sayfa numarasını belirle (1'den başlayarak)
            var sheetIndex = await GetSheetIndexAsync(excelFilePath, selectedSheet);
            var pageNumber = sheetIndex + 1;
            
            result.Logs.Add($"Sayfa {pageNumber} için ID aralığı belirleniyor...");
            
            // ID aralığını belirle
            var (startId, endId) = GetIdRangeForPage(pageNumber);
            result.Logs.Add($"ID aralığı: {startId} - {endId}");

            // Chrome driver'ı yönet
            ChromeDriver driver;
            bool isNewDriver = false;
            lock (_lock)
            {
                if (_driverInstance == null)
                {
                    result.Logs.Add("Yeni Chrome tarayıcısı başlatılıyor...");
                    
                    try
                    {
                        // Railway için Chrome seçenekleri
                        var options = new ChromeOptions();
                        options.AddArgument("--headless");
                        options.AddArgument("--no-sandbox");
                        options.AddArgument("--disable-dev-shm-usage");
                        options.AddArgument("--disable-gpu");
                        options.AddArgument("--disable-software-rasterizer");
                        options.AddArgument("--disable-background-timer-throttling");
                        options.AddArgument("--disable-backgrounding-occluded-windows");
                        options.AddArgument("--disable-renderer-backgrounding");
                        options.AddArgument("--disable-features=TranslateUI");
                        options.AddArgument("--disable-extensions");
                        options.AddArgument("--disable-default-apps");
                        options.AddArgument("--disable-web-security");
                        options.AddArgument("--allow-running-insecure-content");
                        options.AddArgument("--window-size=1920,1080");
                        options.AddArgument("--remote-debugging-port=9222");
                        
                        // Alpine Linux için binary paths
                        if (Environment.OSVersion.Platform == PlatformID.Unix)
                        {
                            // Railway/Alpine Linux
                            options.BinaryLocation = "/usr/bin/chromium-browser";
                            result.Logs.Add("Alpine Linux Chrome binary kullanılıyor...");
                        }
                        
                        result.Logs.Add("ChromeDriver oluşturuluyor...");
                        _driverInstance = new ChromeDriver(options);
                        
                        _driverInstance.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
                        _driverInstance.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(60);
                        
                        result.Logs.Add("✅ Chrome başarıyla başlatıldı!");
                    }
                    catch (Exception ex)
                    {
                        result.Logs.Add($"❌ Chrome başlatma hatası: {ex.Message}");
                        throw new Exception($"Chrome başlatılamadı: {ex.Message}", ex);
                    }
                    
                    result.Logs.Add("Website'e gidiliyor...");
                    _driverInstance.Navigate().GoToUrl(websiteUrl);
                    
                    // Headless mode için otomatik işlem
                    if (Environment.OSVersion.Platform == PlatformID.Unix)
                    {
                        result.Status = "Headless modda otomatik form doldurma başlatılıyor...";
                        result.Logs.Add("🤖 Railway headless mode - Otomatik işlem başlatılıyor...");
                        isNewDriver = true;
                    }
                    else
                    {
                        result.Status = "Lütfen manuel olarak giriş yapın ve form sayfasına gidin...";
                        result.Logs.Add("Kullanıcı giriş yapması bekleniyor...");
                        result.Logs.Add("⚠️ ÖNEMLİ: Giriş yaptıktan sonra form sayfasına gittiğinizden emin olun!");
                        isNewDriver = true;
                    }
                }
                else
                {
                    result.Logs.Add("Mevcut Chrome penceresi kullanılıyor...");
                    result.Logs.Add($"Şu anda sayfa: {_driverInstance.Url}");
                }
                driver = _driverInstance;
            }

            // Yeni driver ise platformuna göre işlem yap
            if (isNewDriver)
            {
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    // Railway headless - kısa bekleme sonra direkt devam
                    result.Logs.Add("🚀 Headless mode - 5 saniye sayfa yüklenmesi bekleniyor...");
                    await Task.Delay(5000);
                }
                else
                {
                    // Lokal - manuel kullanıcı işlemi bekle
                    await Task.Delay(45000); // 45 saniye bekle
                }
            }

            // Platform'a göre sayfa yönetimi
            var targetPageUrl = GetTargetPageUrl(pageNumber);
            
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                // Railway headless - direkt form sayfasına git
                result.Status = "Headless modda form sayfasına gidiliyor...";
                result.Logs.Add($"🤖 Otomatik olarak form sayfasına gidiliyor: {GetPageName(pageNumber)}");
                result.Logs.Add($"🔗 Hedef URL: {targetPageUrl}");
                
                // Direkt form sayfasına git
                driver.Navigate().GoToUrl(targetPageUrl);
                result.Logs.Add("✅ Form sayfası yüklendi, 3 saniye bekleniyor...");
                await Task.Delay(3000);
            }
            else
            {
                // Lokal - kullanıcı manuel açsın
                result.Status = $"Lütfen web sitesinde '{GetPageName(pageNumber)}' sayfasını açın...";
                result.Logs.Add($"📋 Şimdi web sitesinde '{GetPageName(pageNumber)}' sayfasını açmanız gerekiyor.");
                result.Logs.Add($"🔗 Hedef URL: {targetPageUrl}");
                result.Logs.Add("⏳ Sayfa açıldıktan sonra 10 saniye bekleyeceğim...");
                await Task.Delay(10000); // 10 saniye bekle
            }

            result.Status = "Form dolduruluyor...";
            result.Logs.Add("Form doldurma işlemi başlatılıyor...");
            result.Logs.Add($"Toplam {excelData.Count} soru doldurulacak...");

            // Sayfadaki mevcut textarea ID'lerini topla ve sırala
            var textareaElements = driver.FindElements(By.CssSelector("textarea[id^='ContentPlaceHolder1_txtsoru'][id$='aciklama']"));
            var availableIds = new List<int>();
            foreach (var el in textareaElements)
            {
                var idStr = el.GetAttribute("id");
                var m = Regex.Match(idStr ?? string.Empty, @"txtsoru(\d+)aciklama");
                if (m.Success && int.TryParse(m.Groups[1].Value, out int num))
                {
                    availableIds.Add(num);
                }
            }
            availableIds = availableIds.Distinct().OrderBy(n => n).ToList();
            if (availableIds.Count > 0)
            {
                result.Logs.Add($"Sayfada bulunan ID'ler: {string.Join(", ", availableIds.Take(10))}{(availableIds.Count > 10 ? ", ..." : string.Empty)}");
            }

            var processedCount = 0;
            var failedCount = 0;
            var orderedData = excelData.OrderBy(k => k.Key).ToList();
            for (int i = 0; i < orderedData.Count; i++)
            {
                var kvp = orderedData[i];
                try
                {
                    var questionNumber = kvp.Key;
                    var answer = kvp.Value.answer;
                    var dropdownValue = kvp.Value.dropdownValue;
                    
                    // Beklenen ID (sayfa aralığına göre)
                    var expectedId = startId + questionNumber - 1;
                    // Eğer beklenen ID mevcut değilse, sıralı listeden indeks ile eşleştir (fallback)
                    var actualId = availableIds.Contains(expectedId)
                        ? expectedId
                        : (availableIds.Count > i ? availableIds[i] : expectedId);

                    // 1. TEXTAREA DOLDUR
                    var textareaId = $"ContentPlaceHolder1_txtsoru{actualId}aciklama";
                    result.Logs.Add($"Textarea aranıyor: {textareaId}...");

                    var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                    var textareaElement = wait.Until(d => d.FindElement(By.Id(textareaId)));

                    // Görünür değilse görünür yapmayı dene
                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({behavior: 'smooth', block: 'center'});", textareaElement);

                    textareaElement.Clear();
                    textareaElement.SendKeys(answer);

                    // 2. DROPDOWN SEÇ
                    var dropdownId = $"ContentPlaceHolder1_drpcevap{questionNumber}";
                    result.Logs.Add($"Dropdown aranıyor: {dropdownId}...");

                    try
                    {
                        var dropdownElement = driver.FindElement(By.Id(dropdownId));
                        
                        // Dropdown değerini eşleştir
                        var mappedDropdownValue = MapDropdownValue(dropdownValue);
                        result.Logs.Add($"Excel'den gelen: '{dropdownValue}' -> Eşleştirilen: '{mappedDropdownValue}'");
                        
                        // Dropdown'ın mevcut seçeneklerini kontrol et
                        var options = dropdownElement.FindElements(By.TagName("option"));
                        var optionTexts = options.Select(o => o.Text.Trim()).ToList();
                        result.Logs.Add($"Dropdown seçenekleri: {string.Join(", ", optionTexts)}");
                        
                        // Daha güçlü dropdown seçimi - birden fazla yöntem dene
                        var script = @"
                            var select = arguments[0];
                            var targetText = arguments[1];
                            var optionTexts = arguments[2];
                            
                            console.log('Dropdown seçimi başlatılıyor...');
                            console.log('Hedef değer:', targetText);
                            console.log('Mevcut seçenekler:', optionTexts);
                            
                            // 1. Yöntem: Tam eşleşme
                            for(var i = 0; i < select.options.length; i++) {
                                var optionText = select.options[i].text.trim();
                                if(optionText.toLowerCase() === targetText.toLowerCase()) {
                                    select.selectedIndex = i;
                                    select.value = select.options[i].value;
                                    console.log('Tam eşleşme bulundu:', optionText);
                                    
                                    // Event'leri tetikle
                                    select.dispatchEvent(new Event('change', { bubbles: true }));
                                    select.dispatchEvent(new Event('input', { bubbles: true }));
                                    select.dispatchEvent(new Event('blur', { bubbles: true }));
                                    
                                    return { success: true, method: 'exact', selected: optionText };
                                }
                            }
                            
                            // 2. Yöntem: Kısmi eşleşme
                            for(var i = 0; i < select.options.length; i++) {
                                var optionText = select.options[i].text.trim();
                                if(optionText.toLowerCase().includes(targetText.toLowerCase()) || 
                                   targetText.toLowerCase().includes(optionText.toLowerCase())) {
                                    select.selectedIndex = i;
                                    select.value = select.options[i].value;
                                    console.log('Kısmi eşleşme bulundu:', optionText);
                                    
                                    // Event'leri tetikle
                                    select.dispatchEvent(new Event('change', { bubbles: true }));
                                    select.dispatchEvent(new Event('input', { bubbles: true }));
                                    select.dispatchEvent(new Event('blur', { bubbles: true }));
                                    
                                    return { success: true, method: 'partial', selected: optionText };
                                }
                            }
                            
                            // 3. Yöntem: İlk seçenek (varsayılan)
                            if(select.options.length > 0) {
                                select.selectedIndex = 0;
                                select.value = select.options[0].value;
                                console.log('Varsayılan seçenek seçildi:', select.options[0].text);
                                
                                // Event'leri tetikle
                                select.dispatchEvent(new Event('change', { bubbles: true }));
                                select.dispatchEvent(new Event('input', { bubbles: true }));
                                select.dispatchEvent(new Event('blur', { bubbles: true }));
                                
                                return { success: true, method: 'default', selected: select.options[0].text };
                            }
                            
                            return { success: false, method: 'none', selected: null };
                        ";
                        
                        var jsResult = ((IJavaScriptExecutor)driver).ExecuteScript(script, dropdownElement, mappedDropdownValue, optionTexts);
                        
                        // Sonucu kontrol et
                        if (jsResult != null)
                        {
                            var resultDict = jsResult as Dictionary<string, object>;
                            if (resultDict != null && resultDict.ContainsKey("success") && (bool)resultDict["success"])
                            {
                                var method = resultDict["method"]?.ToString();
                                var selected = resultDict["selected"]?.ToString();
                                result.Logs.Add($"✅ Soru {questionNumber} -> ID {actualId} dolduruldu. Dropdown: {mappedDropdownValue} (Yöntem: {method}, Seçilen: {selected})");
                            }
                            else
                            {
                                result.Logs.Add($"⚠️ Dropdown seçilemedi: '{mappedDropdownValue}' bulunamadı");
                                result.Logs.Add($"✅ Soru {questionNumber} -> ID {actualId} sadece textarea dolduruldu.");
                            }
                        }
                        else
                        {
                            result.Logs.Add($"⚠️ Dropdown seçilemedi: JavaScript sonucu null");
                            result.Logs.Add($"✅ Soru {questionNumber} -> ID {actualId} sadece textarea dolduruldu.");
                        }
                    }
                    catch (Exception dropdownEx)
                    {
                        result.Logs.Add($"⚠️ Dropdown seçilemedi: {dropdownEx.Message}");
                        result.Logs.Add($"✅ Soru {questionNumber} -> ID {actualId} sadece textarea dolduruldu.");
                    }

                    processedCount++;
                    result.ProcessedCount = processedCount;

                    await Task.Delay(300); // Biraz daha uzun bekle
                }
                catch (Exception ex)
                {
                    failedCount++;
                    result.Logs.Add($"❌ Soru {kvp.Key} doldurulamadı: {ex.Message}");
                    result.Logs.Add($"   Beklenen/Den. ID: {startId + kvp.Key - 1} / {(availableIds.Count > i ? availableIds[i] : -1)}");
                }
            }

            result.Status = "İşlem tamamlandı! Tarayıcı açık kalacak.";
            result.Logs.Add($"Tüm sorular dolduruldu. Başarılı: {processedCount}, Başarısız: {failedCount}");
            result.Logs.Add("Tarayıcı açık kalacak. Manuel olarak kapatabilirsiniz.");
            result.Logs.Add($"✅ Sayfa '{selectedSheet}' (Sayfa {pageNumber}) başarıyla dolduruldu!");
            result.Logs.Add("📝 Tarayıcı açık kalacak, verileri kontrol edebilirsiniz.");
            result.Logs.Add("🔒 Tarayıcıyı manuel olarak kapatabilirsiniz.");
            result.Logs.Add("💡 Sonraki sayfa için dropdown'dan yeni sayfa seçin ve tekrar doldurun.");
            
            if (failedCount > 0)
            {
                result.Logs.Add($"⚠️ {failedCount} soru doldurulamadı. Manuel kontrol gerekli!");
            }
            
            result.IsSuccess = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Selenium işlemi sırasında hata oluştu");
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
            result.Status = "Hata oluştu!";
            result.Logs.Add($"Hata: {ex.Message}");
        }
        finally
        {
            result.IsProcessing = false;
        }

        return result;
    }

    private async Task<Dictionary<int, (string answer, string dropdownValue)>> ReadExcelDataAsync(string excelFilePath, string selectedSheet)
    {
        var excelData = new Dictionary<int, (string answer, string dropdownValue)>();

        await Task.Run(() =>
        {
            using var workbook = new XLWorkbook(excelFilePath);
            var ws = workbook.Worksheet(selectedSheet);

            if (ws == null) throw new Exception($"Excel sayfası '{selectedSheet}' bulunamadı.");

            var range = ws.RangeUsed();
            if (range == null) throw new Exception($"Excel sayfası '{selectedSheet}' boş.");

            var firstRow = range.FirstRowUsed();
            var firstColumn = range.FirstColumnUsed().ColumnNumber();
            var lastColumn = range.LastColumnUsed().ColumnNumber();
            var headerRow = firstRow.RowNumber();
            var dataStartRow = headerRow + 1;
            var lastRow = range.LastRowUsed().RowNumber();

            int snColumnIndex = -1;
            int answerColumnIndex = -1;
            int dropdownColumnIndex = -1;

            // Sütun başlıklarını logla
            var headers = new List<string>();
            for (int col = firstColumn; col <= lastColumn; col++)
            {
                var headerCell = ws.Cell(headerRow, col);
                var headerText = headerCell.GetString().Trim();
                headers.Add(headerText);
            }

            // SN sütununu bul - daha esnek arama
            for (int col = firstColumn; col <= lastColumn; col++)
            {
                var headerCell = ws.Cell(headerRow, col);
                var headerText = headerCell.GetString().Trim();

                // SN için farklı varyasyonları kontrol et
                if (headerText.Equals("SN", StringComparison.OrdinalIgnoreCase) ||
                    headerText.Equals("S.N", StringComparison.OrdinalIgnoreCase) ||
                    headerText.Equals("S.NO", StringComparison.OrdinalIgnoreCase) ||
                    headerText.Equals("SIRA", StringComparison.OrdinalIgnoreCase) ||
                    headerText.Equals("SIRA NO", StringComparison.OrdinalIgnoreCase) ||
                    headerText.Equals("NO", StringComparison.OrdinalIgnoreCase))
                {
                    snColumnIndex = col;
                }
                // Cevap sütunu için daha esnek arama
                else if (headerText.Contains("DOKÜMANTASYON", StringComparison.OrdinalIgnoreCase) || 
                         headerText.Contains("UYGULAMALAR", StringComparison.OrdinalIgnoreCase) ||
                         headerText.Contains("CEVAP", StringComparison.OrdinalIgnoreCase) ||
                         headerText.Contains("AÇIKLAMA", StringComparison.OrdinalIgnoreCase) ||
                         headerText.Contains("NOT", StringComparison.OrdinalIgnoreCase))
                {
                    answerColumnIndex = col;
                }
                // Dropdown sütunu için arama (4. sütun - Evet/Hayır)
                else if (headerText.Equals("EVET", StringComparison.OrdinalIgnoreCase) ||
                         headerText.Equals("HAYIR", StringComparison.OrdinalIgnoreCase) ||
                         headerText.Equals("VAR", StringComparison.OrdinalIgnoreCase) ||
                         headerText.Equals("YOK", StringComparison.OrdinalIgnoreCase) ||
                         headerText.Equals("UYGUN", StringComparison.OrdinalIgnoreCase) ||
                         headerText.Equals("UYGUN DEĞİL", StringComparison.OrdinalIgnoreCase))
                {
                    dropdownColumnIndex = col;
                }
            }

            // Eğer SN sütunu bulunamadıysa, ilk sütunu kullan (genelde SN olur)
            if (snColumnIndex == -1)
            {
                snColumnIndex = firstColumn;
            }

            // Eğer cevap sütunu bulunamadıysa, 3. sütunu kullan
            if (answerColumnIndex == -1)
            {
                answerColumnIndex = firstColumn + 2; // 3. sütun (0-based index)
            }

            // Eğer dropdown sütunu bulunamadıysa, 4. sütunu kullan
            if (dropdownColumnIndex == -1)
            {
                dropdownColumnIndex = firstColumn + 3; // 4. sütun (0-based index)
            }

            if (snColumnIndex == -1 || answerColumnIndex == -1 || dropdownColumnIndex == -1)
            {
                var headerList = string.Join(", ", headers);
                throw new Exception($"Sayfa '{selectedSheet}' içinde gerekli sütunlar bulunamadı. Mevcut sütunlar: {headerList}");
            }

            for (int row = dataStartRow; row <= lastRow; row++)
            {
                var snCell = ws.Cell(row, snColumnIndex);
                var answerCell = ws.Cell(row, answerColumnIndex);
                var dropdownCell = ws.Cell(row, dropdownColumnIndex);

                // SN'den soru numarasını çıkar (1(BU) -> 1)
                var snText = snCell.GetString().Trim();
                var questionNumber = ExtractQuestionNumber(snText);

                if (questionNumber > 0)
                {
                    var answer = answerCell.GetString().Trim();
                    var dropdownValue = dropdownCell.GetString().Trim();
                    
                    if (!string.IsNullOrWhiteSpace(answer))
                    {
                        excelData[questionNumber] = (answer, dropdownValue);
                    }
                }
            }
        });

        return excelData;
    }

    private int ExtractQuestionNumber(string snText)
    {
        // "1(BU)" -> 1, "2(BU)" -> 2, "(BU)" -> 0 (alt soru)
        var match = System.Text.RegularExpressions.Regex.Match(snText, @"^(\d+)");
        if (match.Success && int.TryParse(match.Groups[1].Value, out int number))
        {
            return number;
        }
        return 0; // Alt sorular için 0 döndür
    }

    private string MapDropdownValue(string excelValue)
    {
        // Excel'deki değeri web sitesindeki dropdown değerine eşleştir
        var cleanValue = excelValue?.Trim().ToUpper();
        
        // Debug için log ekle
        _logger.LogInformation($"Excel dropdown değeri: '{excelValue}' -> Temizlenmiş: '{cleanValue}'");
        
        return cleanValue switch
        {
            "EVET" => "Evet",
            "HAYIR" => "Hayır",
            "VAR" => "Var",
            "YOK" => "Yok",
            "UYGUN" => "Uygun",
            "UYGUN DEĞİL" => "Uygun Değil",
            "BULUNUYOR" => "Bulunuyor",
            "BULUNMUYOR" => "Bulunmuyor",
            "1" => "Evet",
            "0" => "Hayır",
            "TRUE" => "Evet",
            "FALSE" => "Hayır",
            "YES" => "Evet",
            "NO" => "Hayır",
            _ => "Evet" // Varsayılan değer
        };
    }

    private async Task<int> GetSheetIndexAsync(string excelFilePath, string selectedSheet)
    {
        return await Task.Run(() =>
        {
            using var workbook = new XLWorkbook(excelFilePath);
            var worksheets = workbook.Worksheets.ToList();
            return worksheets.FindIndex(ws => ws.Name == selectedSheet);
        });
    }

    private (int startId, int endId) GetIdRangeForPage(int pageNumber)
    {
        return pageNumber switch
        {
            1 => (1, 50),      // Sayfa 1: ID 1-50
            2 => (51, 101),    // Sayfa 2: ID 51-101
            3 => (102, 152),   // Sayfa 3: ID 102-152
            4 => (153, 203),   // Sayfa 4: ID 153-203
            5 => (204, 254),   // Sayfa 5: ID 204-254
            _ => (1, 50)       // Varsayılan
        };
    }

    // Yeni metodlar ekleyelim
    private string GetTargetPageUrl(int pageNumber)
    {
        return pageNumber switch
        {
            1 => "https://merkezisgb.meb.gov.tr/belgelendirme/OtbPortal/tetkikgorevlisi/raporguncellebolum1.aspx",
            2 => "https://merkezisgb.meb.gov.tr/belgelendirme/OtbPortal/tetkikgorevlisi/raporguncellebolum2.aspx",
            3 => "https://merkezisgb.meb.gov.tr/belgelendirme/OtbPortal/tetkikgorevlisi/raporguncellebolum3.aspx",
            4 => "https://merkezisgb.meb.gov.tr/belgelendirme/OtbPortal/tetkikgorevlisi/raporguncellebolum4.aspx",
            5 => "https://merkezisgb.meb.gov.tr/belgelendirme/OtbPortal/tetkikgorevlisi/raporguncellebolum5.aspx",
            _ => "https://merkezisgb.meb.gov.tr/belgelendirme/OtbPortal/tetkikgorevlisi/raporguncellebolum1.aspx"
        };
    }

    private string GetPageName(int pageNumber)
    {
        return pageNumber switch
        {
            1 => "Bölüm 1",
            2 => "Bölüm 2-8",
            3 => "Bölüm 3",
            4 => "Bölüm 4",
            5 => "Bölüm 5",
            _ => $"Bölüm {pageNumber}"
        };
    }

    public static void QuitDriver()
    {
        lock (_lock)
        {
            if (_driverInstance != null)
            {
                _driverInstance.Quit();
                _driverInstance.Dispose();
                _driverInstance = null;
            }
        }
    }
}

