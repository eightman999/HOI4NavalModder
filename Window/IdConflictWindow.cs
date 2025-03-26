using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace HOI4NavalModder
{
    /// <summary>
    /// ID衝突時に表示する警告ダイアログ
    /// </summary>
    public partial class IdConflictWindow : Window
    {
        public enum ConflictResolution
        {
            Cancel,
            Overwrite,
            SaveAsNew
        }

        private Button _cancelButton;
        private Button _overwriteButton;
        private Button _saveAsNewButton;
        private TextBlock _conflictMessageText;

        public ConflictResolution Result { get; private set; } = ConflictResolution.Cancel;

        public IdConflictWindow()
        {
            InitializeComponent();
        }

        public IdConflictWindow(string conflictId)
        {
            InitializeComponent();

            // UI要素の取得
            _cancelButton = this.FindControl<Button>("CancelButton");
            _overwriteButton = this.FindControl<Button>("OverwriteButton");
            _saveAsNewButton = this.FindControl<Button>("SaveAsNewButton");
            _conflictMessageText = this.FindControl<TextBlock>("ConflictMessageText");

            // イベントハンドラの設定
            _cancelButton.Click += OnCancelClick;
            _overwriteButton.Click += OnOverwriteClick;
            _saveAsNewButton.Click += OnSaveAsNewClick;

            // メッセージの設定
            _conflictMessageText.Text = $"ID「{conflictId}」は既に存在します。どのように処理しますか？";
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            Result = ConflictResolution.Cancel;
            Close(Result);
        }

        private void OnOverwriteClick(object sender, RoutedEventArgs e)
        {
            Result = ConflictResolution.Overwrite;
            Close(Result);
        }

        private void OnSaveAsNewClick(object sender, RoutedEventArgs e)
        {
            Result = ConflictResolution.SaveAsNew;
            Close(Result);
        }
    }
}