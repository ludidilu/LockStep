using Connection;
using System.IO;
using System;

internal class PlayerUnit : UnitBase
{
    private int uid = -1;

    public void Logout()
    {
        uid = -1;
    }

    public override void Kick()
    {
        base.Kick();

        if (uid != -1)
        {
            int tmpUid = uid;

            Action dele = delegate ()
            {
                BattleManager.Instance.Logout(tmpUid);
            };

            BattleManager.Instance.Process(dele);

            Logout();
        }
    }

    public override void ReceiveData(byte[] _bytes)
    {
        if (uid != -1)
        {
            int tmpUid = uid;

            Action dele = delegate ()
            {
                BattleManager.Instance.ReceiveData(tmpUid, _bytes);
            };

            BattleManager.Instance.Process(dele);
        }
        else
        {
            using (MemoryStream ms = new MemoryStream(_bytes))
            {
                using (BinaryReader br = new BinaryReader(ms))
                {
                    try
                    {
                        uid = br.ReadInt32();

                        int tmpUid = uid;

                        Action dele = delegate ()
                        {
                            BattleManager.Instance.Login(tmpUid, this);
                        };

                        BattleManager.Instance.Process(dele);
                    }
                    catch (Exception e)
                    {

                    }
                }
            }
        }
    }
}