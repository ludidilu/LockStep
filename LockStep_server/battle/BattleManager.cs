using System.Collections.Generic;
using System.IO;
using System;
using LockStep_lib;
using superService;

internal class BattleManager : SuperService
{
    public static BattleManager Instance;

    internal long tick { private set; get; }

    private Dictionary<int, PlayerUnit> dic = new Dictionary<int, PlayerUnit>();

    internal void Login(int _uid, PlayerUnit _unit)
    {
        if (!dic.ContainsKey(_uid))
        {
            dic.Add(_uid, _unit);
        }
        else
        {
            dic[_uid] = _unit;
        }

        byte[] refreshData = Core.ServerLogin(_uid);

        SendData(_uid, false, refreshData);
    }

    internal void Logout(int _uid)
    {

    }

    internal void ReceiveData(int _playerUnit, byte[] _bytes)
    {
        if (dic.ContainsKey(_playerUnit))
        {
            using (MemoryStream ms = new MemoryStream(_bytes))
            {
                using (BinaryReader br = new BinaryReader(ms))
                {
                    byte type = br.ReadByte();

                    if (type == 0)
                    {
                        Core.ServerGetCommand(_playerUnit, br);
                    }
                    else if (type == 1)
                    {
                        long t = br.ReadInt64();

                        using (MemoryStream ms2 = new MemoryStream())
                        {
                            using (BinaryWriter bw = new BinaryWriter(ms2))
                            {
                                bw.Write((byte)1);

                                bw.Write(t);

                                SendData(_playerUnit, true, ms2.ToArray());
                            }
                        }
                    }
                }
            }
        }
    }

    internal void Update()
    {
        using (MemoryStream ms = new MemoryStream())
        {
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write((byte)0);

                Core.ServerRefreshCommand(bw);

                IEnumerator<int> enumerator = dic.Keys.GetEnumerator();

                while (enumerator.MoveNext())
                {
                    SendData(enumerator.Current, true, ms.ToArray());
                }
            }
        }

        Core.Update();
    }

    private void SendData(int _uid, bool _isPush, byte[] _bytes)
    {
        PlayerUnit unit;

        if (dic.TryGetValue(_uid, out unit))
        {
            Action dele = delegate ()
            {
                unit.SendData(_isPush, _bytes);
            };

            unit.Process(dele);
        }
    }
}