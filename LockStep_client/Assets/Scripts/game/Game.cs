﻿using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Connection;
using LockStep_lib;
using publicTools;

public class Game : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer downSr;

    [SerializeField]
    private SpriteRenderer moveSr;

    [SerializeField]
    private Camera mainCamera;

    [SerializeField]
    private GameObject unitSource;

    private Client client;

    private int id;

    private Dictionary<int, GameObject> dic = new Dictionary<int, GameObject>();

    private Unit myUnit;

    void Awake()
    {
        Connection.Log.Init(Debug.Log);

        Core.Init();

        ConfigDictionary.Instance.LoadLocalConfig(Path.Combine(Application.streamingAssetsPath, "local.xml"));

        client = new Client();

        client.Init(ConfigDictionary.Instance.ip, ConfigDictionary.Instance.port, ConfigDictionary.Instance.uid, GetData, Disconnect);

        client.Connect(ConnectSuccess, ConnectFail);
    }

    private void ConnectSuccess(BinaryReader _br)
    {
        id = _br.ReadInt32();

        Core.ClientGetRefreshCommand(_br);

        IEnumerator<Unit> enumerator = Core.unitDic.Values.GetEnumerator();

        while (enumerator.MoveNext())
        {
            Unit unit = enumerator.Current;

            GameObject go = Instantiate(unitSource);

            go.transform.position = new Vector3((float)unit.posX, (float)unit.posY, 0);

            dic.Add(unit.id, go);
        }
    }

    private void ConnectFail()
    {

    }

    private void GetData(BinaryReader _br)
    {
        Core.ClientGetRefreshCommand(_br);

        Core.Update();

        IEnumerator<Unit> enumerator = Core.unitDic.Values.GetEnumerator();

        while (enumerator.MoveNext())
        {
            Unit unit = enumerator.Current;

            GameObject go;

            if (!dic.TryGetValue(unit.id, out go))
            {
                go = Instantiate(unitSource);

                dic.Add(unit.id, go);

                if (unit.id == id)
                {
                    myUnit = unit;
                }
            }

            go.transform.position = new Vector3((float)unit.posX, (float)unit.posY, 0);
        }
    }

    private void Disconnect()
    {

    }

    private Vector2 downPos;

    // Update is called once per frame
    void Update()
    {
        client.Update();

        if (myUnit != null)
        {
            if (Input.GetMouseButtonDown(0))
            {
                downPos = Input.mousePosition;

                Vector2 v = mainCamera.ScreenToWorldPoint(downPos);

                downSr.gameObject.SetActive(true);

                moveSr.gameObject.SetActive(true);

                downSr.transform.position = new Vector3(v.x, v.y, downSr.transform.position.z);

                moveSr.transform.position = new Vector3(v.x, v.y, moveSr.transform.position.z);
            }
            else if (Input.GetMouseButton(0))
            {
                Vector2 pos = Input.mousePosition;

                float dis = Vector2.Distance(downPos, pos);

                if (dis > Constant.MAX_MOUSE_DISTANCE)
                {
                    pos = Vector2.Lerp(downPos, pos, Constant.MAX_MOUSE_DISTANCE / dis);
                }

                Vector2 v = mainCamera.ScreenToWorldPoint(pos);

                moveSr.transform.position = new Vector3(v.x, v.y, moveSr.transform.position.z);

                int mouseX = (int)Mathf.Round(pos.x) - (int)downPos.x;

                int mouseY = (int)Mathf.Round(pos.y) - (int)downPos.y;

                if (mouseX != myUnit.mouseX || mouseY != myUnit.mouseY)
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (BinaryWriter bw = new BinaryWriter(ms))
                        {
                            bw.Write(mouseX);

                            bw.Write(mouseY);

                            Debug.Log("x:" + mouseX + "    y:" + mouseY);

                            client.Send(ms);
                        }
                    }
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                downSr.gameObject.SetActive(false);

                moveSr.gameObject.SetActive(false);

                using (MemoryStream ms = new MemoryStream())
                {
                    using (BinaryWriter bw = new BinaryWriter(ms))
                    {
                        bw.Write(0);

                        bw.Write(0);

                        client.Send(ms);
                    }
                }
            }
        }
    }
}