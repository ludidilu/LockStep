using System.Collections.Generic;
using System.IO;
using System;
using LockStep_lib;

internal class BattleManager
{
    private static BattleManager _Instance;

    internal static BattleManager Instance
    {
        get
        {
            if (_Instance == null)
            {
                _Instance = new BattleManager();
            }

            return _Instance;
        }
    }

    internal long tick { private set; get; }

    private Dictionary<PlayerUnit, int> dic = new Dictionary<PlayerUnit, int>();

    private int id;

    internal byte[] Login(PlayerUnit _playerUnit)
    {
        int tmpID;

        if (!dic.TryGetValue(_playerUnit, out tmpID))
        {
            id++;

            tmpID = id;

            dic.Add(_playerUnit, tmpID);
        }

        byte[] refreshData = Core.ServerLogin(tmpID);

        byte[] idBytes = BitConverter.GetBytes(tmpID);


        byte[] result = new byte[idBytes.Length + refreshData.Length];

        Array.Copy(idBytes, result, idBytes.Length);

        Array.Copy(refreshData, 0, result, idBytes.Length, refreshData.Length);

        return result;
    }

    internal void Logout(PlayerUnit _playerUnit)
    {

    }

    internal void ReceiveData(PlayerUnit _playerUnit, byte[] _bytes)
    {
        int id;

        if (dic.TryGetValue(_playerUnit, out id))
        {
            using (MemoryStream ms = new MemoryStream(_bytes))
            {
                using (BinaryReader br = new BinaryReader(ms))
                {
                    byte type = br.ReadByte();

                    if (type == 0)
                    {
                        Core.ServerGetCommand(id, br);
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

                                _playerUnit.SendData(true, ms2);
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

                IEnumerator<PlayerUnit> enumerator = dic.Keys.GetEnumerator();

                while (enumerator.MoveNext())
                {
                    enumerator.Current.SendData(true, ms);
                }
            }
        }

        Core.Update();
    }
}