/// <summary>状態効果の付与・解除・存在確認を受け付ける契約。効果の進行方法は実装側が定義する。</summary>
public interface IStatusEffectReceiver
{
    /// <summary>
    /// 受け取った効果インスタンスをリストに追加、または既存と合成する。
    /// </summary>
    /// <param name="instance">効果定義と付与元を保持した効果インスタンス。</param>
    void ApplyStatusEffect(StatusEffectInstance instance);
    /// <summary>
    /// 指定IDの効果を解除する。重複方針の適用は付与側が担当する。
    /// </summary>
    /// <param name="id">状態効果定義の識別子。</param>
    void RemoveStatusEffect(string id);
    /// <summary>
    /// 指定 ID の効果が存在するかどうかを返す。
    /// </summary>
    /// <param name="id">状態効果定義の識別子。</param>
    /// <returns>指定IDの効果が適用中ならtrue。</returns>
    bool HasStatusEffect(string id);
}
