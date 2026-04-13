namespace Ngaq.Local.Domains.Word;

using Ngaq.Core.Infra;
using Ngaq.Core.Model.Po.Kv;
using Ngaq.Core.Shared.StudyPlan.Models.PreFilter;
using Ngaq.Core.Shared.Word.Models.Po.Kv;
using Tsinswreng.CsTempus;

/// <summary>
/// 預篩選值比較工具。
/// 統一處理單詞屬性值提取、相等比較與數值比較，避免 Dao/Svc 重複實現。
/// </summary>
public static class WordFilterValueUtil{

	/// <summary>
	/// 從 <see cref="PoWordProp"/> 提取可比較值。
	/// </summary>
	/// <param name="Prop">單詞屬性實體。</param>
	/// <returns>用於比較的原始值。</returns>
	public static obj? GetPropValue(PoWordProp Prop){
		return Prop.VType switch{
			EKvType.Str => Prop.VStr,
			EKvType.I64 => Prop.VI64,
			EKvType.F64 => Prop.VF64,
			EKvType.Binary => Prop.VBinary,
			_ => null,
		};
	}

	/// <summary>
	/// 按指定值類型判斷候選值與期望值是否相等。
	/// </summary>
	/// <param name="Candidate">候選值。</param>
	/// <param name="Expected">期望值。</param>
	/// <param name="ValueType">比較類型。</param>
	/// <returns>是否相等。</returns>
	public static bool AreEqual(obj? Candidate, obj? Expected, EValueType ValueType){
		if(ValueType == EValueType.Number){
			if(!TryToF64(Candidate, out var cn) || !TryToF64(Expected, out var en)){
				return false;
			}
			return cn == en;
		}
		if(Candidate is null || Expected is null){
			return Candidate is null && Expected is null;
		}
		return string.Equals(Candidate.ToString(), Expected.ToString(), StringComparison.Ordinal);
	}

	/// <summary>
	/// 比較兩個值的數值大小。任一值不可轉數值時返回 <see cref="int.MinValue"/>。
	/// </summary>
	/// <param name="Left">左值。</param>
	/// <param name="Right">右值。</param>
	/// <returns>比較結果。</returns>
	public static int CompareNumber(obj? Left, obj? Right){
		if(!TryToF64(Left, out var l) || !TryToF64(Right, out var r)){
			return int.MinValue;
		}
		return l.CompareTo(r);
	}

	/// <summary>
	/// 嘗試把多種可接受類型轉為 f64，供數值比較使用。
	/// </summary>
	/// <param name="Value">待轉換值。</param>
	/// <param name="Number">轉換成功後的數值。</param>
	/// <returns>是否轉換成功。</returns>
	public static bool TryToF64(obj? Value, out f64 Number){
		switch(Value){
			case null:
				Number = default;
				return false;
			case byte v:
				Number = v;
				return true;
			case sbyte v:
				Number = v;
				return true;
			case short v:
				Number = v;
				return true;
			case ushort v:
				Number = v;
				return true;
			case int v:
				Number = v;
				return true;
			case uint v:
				Number = v;
				return true;
			case long v:
				Number = v;
				return true;
			case ulong v:
				Number = v;
				return true;
			case float v:
				Number = v;
				return true;
			case double v:
				Number = v;
				return true;
			case decimal v:
				Number = (double)v;
				return true;
			case Tempus v:
				Number = v.Value;
				return true;
			default:
				if(double.TryParse(Value.ToString(), out var parsed)){
					Number = parsed;
					return true;
				}
				Number = default;
				return false;
		}
	}
}
