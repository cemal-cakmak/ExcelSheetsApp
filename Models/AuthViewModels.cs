using System.ComponentModel.DataAnnotations;

namespace ExcelSheetsApp.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Kullanıcı adı gereklidir")]
    [Display(Name = "Kullanıcı Adı")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "Şifre gereklidir")]
    [DataType(DataType.Password)]
    [Display(Name = "Şifre")]
    public string Password { get; set; } = "";

    [Display(Name = "Beni Hatırla")]
    public bool RememberMe { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Kullanıcı adı gereklidir")]
    [Display(Name = "Kullanıcı Adı")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "E-posta gereklidir")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Ad Soyad gereklidir")]
    [Display(Name = "Ad Soyad")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Şifre gereklidir")]
    [StringLength(100, ErrorMessage = "Şifre en az {2} karakter olmalıdır.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Şifre")]
    public string Password { get; set; } = "";

    [DataType(DataType.Password)]
    [Display(Name = "Şifre Tekrar")]
    [Compare("Password", ErrorMessage = "Şifreler eşleşmiyor.")]
    public string ConfirmPassword { get; set; } = "";
}
