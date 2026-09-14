using UnityEngine;

public class BlankSpell : SpellBase
{
    public override void OnCastStart()
    {
        OnSpellCast();
    }

    public override void OnChannelComplete(ChannelingGameScript.ChannelingResult result){}

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
