using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace HOI4NavalModder;

// NavalBaseクラス - 港の情報を格納
public class NavalBase
{
    public NavalBase(int provinceId, int level, string ownerTag, string stateName, int stateId)
    {
        ProvinceId = provinceId;
        Level = level;
        OwnerTag = ownerTag;
        StateName = stateName;
        StateId = stateId;
        Position = new Point(0, 0); // デフォルト位置
    }

    public int ProvinceId { get; set; }
    public int Level { get; set; }
    public string OwnerTag { get; set; }
    public string StateName { get; set; }
    public int StateId { get; set; }
    public Point Position { get; set; } // マップ上の位置
    public Border MarkerElement { get; set; } // UI要素
    public bool HasCustomPosition { get; private set; }

    // 位置を設定
    public void SetScreenPosition(Point position)
    {
        Position = position;
        HasCustomPosition = true;
    }

    // レベルに応じたマーカースタイルを取得
    public (SolidColorBrush Background, SolidColorBrush Border) GetMarkerStyle()
    {
        if (Level >= 7 && Level <= 10)
            // 7-10: 金の丸・銀縁取り
            return (new SolidColorBrush(Color.Parse("#FFD700")), new SolidColorBrush(Color.Parse("#C0C0C0")));

        if (Level >= 4 && Level <= 6)
            // 4-6: 青の丸・白縁取り
            return (new SolidColorBrush(Color.Parse("#1E90FF")), new SolidColorBrush(Colors.White));

        // 1-3: 緑の丸・黒縁取り
        return (new SolidColorBrush(Color.Parse("#32CD32")), new SolidColorBrush(Colors.Black));
    }

    // ツールチップ用の情報
    public override string ToString()
    {
        return $"港湾施設 Lv.{Level}\n" +
               $"プロヴィンス: {ProvinceId}\n" +
               $"ステート: {StateName} (ID: {StateId})\n" +
               $"支配国: {OwnerTag}";
    }
}