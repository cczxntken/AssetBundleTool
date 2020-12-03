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
        //TestUDP();
        TestKCP();
        tetstdata.AddInt(2);
        Application.runInBackground = true;
    }

    void OutputEvent(byte[] pBuffer,int pSize)
    {
        ShowBuffer(pBuffer,pSize);
    }

    void ShowBuffer(byte[] pBuffer ,int pSize)
    {
        System.Text.StringBuilder bufferstr = new System.Text.StringBuilder();
        bufferstr.Append("{");
        for (int i = 0; i < pSize; i++)
        {
            if (i != 0)
                bufferstr.Append(",");
            bufferstr.Append(pBuffer[i]);
        }
        bufferstr.Append("}");
        string tmsg = string.Format("{0}", bufferstr);
        Debug.Log(tmsg);
    }

    // Update is called once per frame
    void TestUDP()
    {
        UDPNet.Init("127.0.0.1", 20236);
        UDPNet.ShowMsgLog(true);
        UDPNet.SetOutputDelgate(OutputEvent);
        UDPNet.Connect();
    }
    void TestTCP()
    {
        TCPNet.Init("127.0.0.1", 20240);
        TCPNet.ShowMsgLog(true);
        TCPNet.SetOutputDelgate(OutputEvent);
        TCPNet.Connect();
    }
    void TestKCP()
    {
        KCPNet.Init("127.0.0.1", 20250);
        KCPNet.ShowMsgLog(true);
        KCPNet.Connect();
        //KCPNet.SetOutputDelgate(OutputEvent);
    }

    void TestRec(object pData)
    {
        ReceiveData tdata = pData as ReceiveData;
        //DLog.Log("TestRec " + tdata.Cmd);
    }
    void Update()
    {
        //UpdateTCP();
        //UpdateUDP();
        UpdateKCP();
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
