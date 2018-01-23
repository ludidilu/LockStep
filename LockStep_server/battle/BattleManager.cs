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

        MemoryStream ms2 = Core.Login(tmpID);

        using (MemoryStream ms = new MemoryStream())
        {
            byte[] bytes = BitConverter.GetBytes(tmpID);

            ms.Write(bytes, 0, bytes.Length);

            ms2.WriteTo(ms);

            return ms.ToArray();
        }
    }

    internal void Logout(PlayerUnit _playerUnit)
    {

    }

    internal void ReceiveData(PlayerUnit _playerUnit, byte[] _bytes)
    {

    }

    internal void Update()
    {
        Core.Update();
    }
}