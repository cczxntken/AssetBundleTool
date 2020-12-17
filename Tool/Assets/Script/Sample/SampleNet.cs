using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitEngine.Net;
public class SampleNet : MonoBehaviour
{
    public enum TestType
    {
        tcp = 0,
        udp,
        kcp,
    }
    // Start is called before the first frame update
    SendData tetstdata = new SendData(10);

    public TestType state = TestType.tcp;
    public bool isShowDebug = true;
    private void Awake()
    {
        switch (state)
        {
            case TestType.tcp:
                TestTCP();
                break;
            case TestType.udp:
                TestUDP();
                break;
            case TestType.kcp:
                TestKCP();
                break;
        }

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
        UDPNet.ShowMsgLog(isShowDebug);
        //UDPNet.SetOutputDelgate(OutputEvent);
        UDPNet.Connect();
    }
    void TestTCP()
    {
        TCPNet.Init("127.0.0.1", 20240);
        TCPNet.ShowMsgLog(isShowDebug);
        //TCPNet.SetOutputDelgate(OutputEvent);
        TCPNet.Connect();
    }
    void TestKCP()
    {
        KCPNet.Init("127.0.0.1", 20250);
        KCPNet.ShowMsgLog(isShowDebug);
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
        switch (state)
        {
            case TestType.tcp:
                UpdateTCP();
                break;
            case TestType.udp:
                UpdateUDP();
                break;
            case TestType.kcp:
                UpdateKCP();
                break;
        }
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
