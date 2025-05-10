using System;
using System.IO;
using System.Reflection;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using HOI4NavalModder.Window;

namespace HOI4NavalModder;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        RegisterFonts();
        // foreach (var fontFamily in FontManager.Current.SystemFonts) Console.WriteLine($"利用可能なフォント: {fontFamily.Name}");
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow();

        base.OnFrameworkInitializationCompleted();
        // アプリケーション全体でダークテーマを強制
        RequestedThemeVariant = ThemeVariant.Dark;
    }

    private void RegisterFonts()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();

            // 埋め込みリソースから NotoSansJP-Regular.otf を取得
            var fontStream = assembly.GetManifestResourceStream("HOI4NavalModder.Assets.Fonts.JF-Dot-jiskan24.ttf");

            if (fontStream != null)
            {
                // 一時ファイルに保存
                var tempFile = Path.GetTempFileName() + ".otf";
                using (var fileStream = File.Create(tempFile))
                {
                    fontStream.CopyTo(fileStream);
                }

                // フォントを登録
                var fontUri = new Uri(tempFile);
                var fontFamily = new FontFamily(fontUri.AbsolutePath);

                Console.WriteLine("フォントを正常に読み込みました");
                Console.WriteLine($"フォントファミリー: {fontFamily.Name}");
            }
            else
            {
                Console.WriteLine("埋め込みフォントリソースが見つかりません");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"フォント登録エラー: {ex.Message}");
        }
    }
}