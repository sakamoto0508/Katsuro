public interface IStatusEffectReceiver
{
    /// <summary>
    /// 受け取った効果インスタンスをリストに追加、または既存と合成する。
    /// </summary>
    /// <param name="instance"></param>
    void ApplyStatusEffect(StatusEffectInstance instance);
    /// <summary>
    /// 既存効果がある場合はスタックポリシーに従って処理。
    /// </summary>
    /// <param name="id"></param>
    void RemoveStatusEffect(string id);
    /// <summary>
    /// 指定 ID の効果が存在するかどうかを返す。
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    bool HasStatusEffect(string id);
}
