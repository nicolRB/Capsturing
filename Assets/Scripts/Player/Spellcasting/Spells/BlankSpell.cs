using UnityEngine;

public class BlankSpell : SpellBase
{
    public override void OnCastStart()
    {
        Debug.Log("Blank spell: channeling started.");
    }

    public override void OnChannelComplete(ChannelingGameScript.ChannelingResult result)
    {
        Debug.Log($"Blank spell: channel complete. Perfects={result.perfects} Goods={result.goods} Misses={result.misses}");
    }

    public override void OnSpellCast()
    {
        Debug.Log("Blank spell: spell cast.");
        RaiseSpellResolved();
    }

    public override void Cancel()
    {
        Debug.Log("Blank spell: spell canceled.");
        RaiseSpellResolved();
    }
}
