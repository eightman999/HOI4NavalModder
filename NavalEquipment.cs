using System.Collections.Generic;

namespace HOI4NavalModder
{
    /// <summary>
    /// 装備データを表すクラス
    /// </summary>
    public class NavalEquipment
    {
        /// <summary>
        /// 装備ID
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// 装備名
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// カテゴリ（SMLG, SMMG, SMHG, SMSHGなど）
        /// </summary>
        public string Category { get; set; }
        
        /// <summary>
        /// サブカテゴリ（単装砲、連装砲など）
        /// </summary>
        public string SubCategory { get; set; }
        
        /// <summary>
        /// 開発年
        /// </summary>
        public int Year { get; set; }
        
        /// <summary>
        /// ティア（開発世代）
        /// </summary>
        public int Tier { get; set; }
        
        /// <summary>
        /// 開発国
        /// </summary>
        public string Country { get; set; }
        
        /// <summary>
        /// 攻撃力（種類に応じて異なる意味を持つ）
        /// </summary>
        public double Attack { get; set; }
        
        /// <summary>
        /// 防御力（種類に応じて異なる意味を持つ）
        /// </summary>
        public double Defense { get; set; }
        
        /// <summary>
        /// 特殊能力や特性
        /// </summary>
        public string SpecialAbility { get; set; }
        
        /// <summary>
        /// その他のプロパティ（モジュールの種類に応じて異なる）
        /// </summary>
        public Dictionary<string, object> AdditionalProperties { get; set; }
        
        public NavalEquipment()
        {
            AdditionalProperties = new Dictionary<string, object>();
        }
    }
    
    /// <summary>
    /// カテゴリ選択の結果を格納するクラス
    /// </summary>
    public class CategorySelectionResult
    {
        /// <summary>
        /// 選択されたカテゴリID
        /// </summary>
        public string CategoryId { get; set; }
        
        /// <summary>
        /// 選択されたティアID
        /// </summary>
        public int TierId { get; set; }
    }
    
    /// <summary>
    /// 装備カテゴリ情報
    /// </summary>
    public class NavalCategory
    {
        /// <summary>
        /// カテゴリID（SMLG, SMMG, SMHGなど）
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// カテゴリ名（小口径砲、中口径砲など）
        /// </summary>
        public string Name { get; set; }
    }
}