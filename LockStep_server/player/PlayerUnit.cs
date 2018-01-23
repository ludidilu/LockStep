using Connection;

internal class PlayerUnit : UnitBase
{
    public override byte[] Login()
    {
        return BattleManager.Instance.Login(this);
    }

    public override void Kick(bool _logout)
    {
        base.Kick(_logout);

        if (_logout)
        {
            BattleManager.Instance.Logout(this);
        }
    }

    public override void ReceiveData(byte[] _bytes)
    {
        BattleManager.Instance.ReceiveData(this, _bytes);
    }
}