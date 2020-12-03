using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitEngine.Net;
public class SampleNet : MonoBehaviour
{
    // Start is called before the first frame update
    SendData tetstdata = new SendData(10);
    private void Awake()
    {
        //TestTCP();
        TestUDP();
        //TestKCP();
        tetstdata.AddInt(2);
        Application.runInBackground = true;
    }

    // Update is called once per frame
    void TestUDP()
    {
        UDPNet.Init("127.0.0.1", 20236);
        UDPNet.ShowMsgLog(true);
        UDPNet.Connect();
    }
    void TestTCP()
    {
        TCPNet.Init("127.0.0.1", 20240);
        TCPNet.ShowMsgLog(true);
        TCPNet.Connect();
    }
    void TestKCP()
    {
        KCPNet.Init("127.0.0.1", 20250);
        KCPNet.ShowMsgLog(true);
        KCPNet.Connect();
        KCPNet.Reg(11,TestRec);
    }

    void TestRec(object pData)
    {
        ReceiveData tdata = pData as ReceiveData;
        //DLog.Log("TestRec " + tdata.Cmd);
    }
    void Update()
    {
        //UpdateTCP();
        UpdateUDP();
        //UpdateKCP();
    }

    void UpdateKCP()
    {
        KCPNet.SendObject(tetstdata);
    }
    void UpdateUDP()
    {
        UDPNet.SendObject(tetstdata);
    }
    void UpdateTCP()
    {
        if (TCPNet.IsConnect())
        {
            TCPNet.SendObject(tetstdata);
        }
    }
}
